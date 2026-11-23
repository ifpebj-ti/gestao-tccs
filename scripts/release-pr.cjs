const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');

const manifest = '.release-please-manifest.json';
const changelog = 'CHANGELOG.md';
const versionPattern = /^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/;

function readVersion(text) {
  const data = JSON.parse(text);
  assert.ok(data && Object.keys(data).length === 1 && typeof data['.'] === 'string', 'Expected only the root release version');
  assert.match(data['.'], versionPattern, 'Expected a stable SemVer version');
  return data['.'];
}

function verifyReleaseChanges(rawDiff, previousManifest, nextManifest, nextChangelog) {
  const entries = rawDiff.split('\0');
  assert.equal(entries.pop(), '', 'Expected a NUL-terminated Git diff');
  const files = entries.filter((_, index) => index % 2 === 1);
  assert.deepEqual(files.sort(), [manifest, changelog], 'Only the release manifest and changelog may change');
  for (let i = 0; i < entries.length; i += 2) {
    assert.match(entries[i], /^:100644 100644 [0-9a-f]+ [0-9a-f]+ M$/, 'Expected modifications to regular files');
  }

  const previous = readVersion(previousManifest).split('.').map(BigInt);
  const version = readVersion(nextManifest);
  const next = version.split('.').map(BigInt);
  const firstDifference = next.findIndex((part, index) => part !== previous[index]);
  assert.ok(firstDifference >= 0 && next[firstDifference] > previous[firstDifference], 'The release version must increase');
  const firstReleaseHeading = nextChangelog.split(/\r?\n/).find(line => line.startsWith('## '));
  assert.ok(firstReleaseHeading?.startsWith(`## [${version}](`), 'The changelog must start with the new release entry');
  return version;
}

if (require.main === module) {
  const [base, head] = process.argv.slice(2);
  for (const sha of [base, head]) {
    assert.match(sha || '', /^[0-9a-f]{40}$/, 'Expected a full Git commit SHA');
  }
  const git = (...args) => execFileSync('git', args, { encoding: 'utf8' });
  const version = verifyReleaseChanges(
    git('diff', '--raw', '--no-renames', '-z', base, head, '--'),
    git('show', `${base}:${manifest}`),
    git('show', `${head}:${manifest}`),
    git('show', `${head}:${changelog}`)
  );
  console.log(`Verified the release metadata for ${version}.`);
}

module.exports = { verifyReleaseChanges };
