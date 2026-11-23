const test = require('node:test');
const assert = require('node:assert/strict');
const { verifyReleaseChanges } = require('../release-pr.cjs');

function modification(path, mode = '100644') {
  return `:100644 ${mode} a1b2 c3d4 M\0${path}\0`;
}

const manifest = modification('.release-please-manifest.json');
const changelog = modification('CHANGELOG.md');
const releaseDiff = manifest + changelog;
const previousManifest = '{".": "2.16.0"}';
const nextManifest = '{".": "2.17.0"}';
const nextChangelog = '# Changelog\n\n## [2.17.0](https://example.test/compare)\n\nRelease notes\n';

test('accepts a release that updates only the root version and changelog', () => {
  assert.equal(verifyReleaseChanges(releaseDiff, previousManifest, nextManifest, nextChangelog), '2.17.0');
});

test('rejects extra code changes', () => {
  assert.throws(() => verifyReleaseChanges(releaseDiff + modification('scripts/build.sh'), previousManifest, nextManifest, nextChangelog), /Only the release manifest and changelog/);
});

test('requires both files to change', () => {
  assert.throws(() => verifyReleaseChanges(manifest, previousManifest, nextManifest, nextChangelog), /Only the release manifest and changelog/);
});

test('rejects replacing the changelog with a symlink', () => {
  const diff = manifest + modification('CHANGELOG.md', '120000');
  assert.throws(() => verifyReleaseChanges(diff, previousManifest, nextManifest, nextChangelog), /regular files/);
});

test('rejects versions that do not increase', () => {
  for (const version of ['2.16.0', '2.15.99', '1.100.0']) {
    const manifestWithVersion = JSON.stringify({ '.': version });
    assert.throws(() => verifyReleaseChanges(releaseDiff, previousManifest, manifestWithVersion, nextChangelog), /must increase/);
  }
});

test('rejects manifests with additional components', () => {
  const manifestWithExtra = '{".": "2.17.0", "frontend": "9.9.9"}';
  assert.throws(() => verifyReleaseChanges(releaseDiff, previousManifest, manifestWithExtra, nextChangelog), /only the root/);
});

test('requires the first release heading to match the new version', () => {
  const changelogWithPriorReleaseFirst = nextChangelog.replace('## [2.17.0]', '## [2.16.0](https://example.test/previous)\n\n## [2.17.0]');
  assert.throws(() => verifyReleaseChanges(releaseDiff, previousManifest, nextManifest, changelogWithPriorReleaseFirst), /new release entry/);
});
