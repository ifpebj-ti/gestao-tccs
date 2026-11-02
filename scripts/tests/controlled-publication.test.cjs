const { test } = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const { spawnSync } = require('node:child_process');
const { candidateTag, validateCandidate, publicationPlan, Registry, releasePreflight, verifyReleaseSource,
  assembleRelease, promoteRelease, badgePayload, main } = require('../controlled-publication.cjs');

const sha = 'a'.repeat(40);
const env = { GITHUB_SHA: sha, GITHUB_RUN_ID: '12345', GITHUB_RUN_ATTEMPT: '1',
  GITHUB_REPOSITORY_OWNER: 'IFPEBJ-TI', REPOSITORY_VISIBILITY: 'public',
  PUBLISH_CANDIDATES: 'false', CANDIDATE_API_URL: 'http://127.0.0.1:8080/api' };
const image = 'ghcr.io/ifpebj-ti/gestaotccs-frontend';
const digest = bytes => `sha256:${crypto.createHash('sha256').update(bytes).digest('hex')}`;
const json = data => JSON.stringify(data);

test('publication is off by default and requires the exact selected SHA when enabled', () => {
  assert.equal(publicationPlan(env).publish, false);
  for (const confirmation of ['', 'main', 'a'.repeat(7), 'b'.repeat(40)]) {
    assert.throws(() => publicationPlan({ ...env, PUBLISH_CANDIDATES: 'true', CONFIRM_SHA: confirmation }));
  }
  assert.equal(publicationPlan({ ...env, PUBLISH_CANDIDATES: 'true', CONFIRM_SHA: sha }).publish, true);
});

test('budget guard rejects non-public or missing visibility', () => {
  for (const visibility of ['private', 'internal', '', undefined]) {
    assert.throws(() => publicationPlan({ ...env, REPOSITORY_VISIBILITY: visibility }));
  }
});

test('only tags tied to this full SHA, run and attempt can reach publication commands', () => {
  const tag = candidateTag(env);
  for (const value of [tag, `${tag}-amd64`, `${tag}-arm64`]) assert.equal(validateCandidate(value, env), value);
  for (const value of ['latest', '2.12.0', '2.13.0', 'validation', `${tag}-ppc64le`,
    candidateTag({ ...env, GITHUB_RUN_ID: '12346' }), candidateTag({ ...env, GITHUB_RUN_ATTEMPT: '2' })]) {
    assert.throws(() => validateCandidate(value, env));
  }
  assert.throws(() => candidateTag({ ...env, GITHUB_SHA: 'a'.repeat(7) }));
  assert.throws(() => candidateTag({ ...env, GITHUB_RUN_ID: '1; curl example.com' }));
});

test('API build argument rejects credentials, non-HTTP URLs and multiline values', () => {
  for (const url of ['file:///etc/passwd', 'https://user:password@example.com/api', 'https://example.com/api\nINJECTED=1']) {
    assert.throws(() => publicationPlan({ ...env, CANDIDATE_API_URL: url }));
  }
});

for (const status of [200, 401, 403, 429, 500, 503]) {
  test(`existence check fails closed for HTTP ${status}`, async () => {
    const registry = new Registry(image, { env: {}, fetcher: async url => url.includes('/token?')
      ? new Response(json({ token: 'test-only' })) : new Response(null, { status }) });
    await assert.rejects(registry.assertAbsent(candidateTag(env)));
  });
}

test('only an authenticated manifest HEAD with HTTP 404 establishes absence', async () => {
  const requests = [];
  const registry = new Registry(image, { env: {}, fetcher: async (url, options) => {
    requests.push({ url, options });
    return url.includes('/token?') ? new Response(json({ token: 'test-only' })) : new Response(null, { status: 404 });
  } });
  await registry.assertAbsent(candidateTag(env));
  assert.equal(requests[1].options.method, 'HEAD');
  assert.equal(requests[1].options.headers.Authorization, 'Bearer test-only');
});

test('token failure is not mistaken for an absent image', async () => {
  const registry = new Registry(image, { env: {}, fetcher: async () => new Response(null, { status: 403 }) });
  await assert.rejects(registry.assertAbsent(candidateTag(env)), /authentication failed/);
});

function fixture(arches, revisions = arches.map(() => sha), badDigest = false, declaredArches = arches) {
  const files = new Map();
  const manifests = arches.map((arch, index) => {
    const config = json({ os: 'linux', architecture: arch, config: { Labels: { 'org.opencontainers.image.revision': revisions[index] } } });
    const configDigest = digest(config);
    files.set(`/blobs/${configDigest}`, config);
    const manifest = json({ schemaVersion: 2, config: { digest: configDigest }, layers: [] });
    const manifestDigest = digest(manifest);
    files.set(`/manifests/${manifestDigest}`, manifest);
    return { digest: manifestDigest, platform: { os: 'linux', architecture: declaredArches[index] } };
  });
  const root = json({ schemaVersion: 2, manifests });
  files.set('/manifests/candidate', root);
  return new Registry(image, { env: {}, fetcher: async url => {
    if (url.includes('/token?')) return new Response(json({ token: 'test-only' }));
    const key = new URL(url).pathname.replace(`/v2/ifpebj-ti/gestaotccs-frontend`, '');
    const body = files.get(key);
    assert.ok(body, `Unexpected registry request: ${key}`);
    return new Response(body, { headers: key.includes('/manifests/')
      ? { 'docker-content-digest': badDigest ? `sha256:${'0'.repeat(64)}` : digest(body) } : {} });
  } });
}

test('verifies actual image configurations for both platforms and the approved revision', async () => {
  const result = await fixture(['amd64', 'arm64']).verify('candidate', ['amd64', 'arm64'], sha);
  assert.deepEqual(result.images.map(item => item.platform), ['linux/amd64', 'linux/arm64']);
});

test('rejects missing, unexpected and duplicate platforms', async () => {
  for (const arches of [['amd64'], ['amd64', 'ppc64le'], ['amd64', 'amd64']]) {
    await assert.rejects(fixture(arches).verify('candidate', ['amd64', 'arm64'], sha));
  }
});

test('rejects a different commit and corrupted registry content', async () => {
  await assert.rejects(fixture(['amd64', 'arm64'], [sha, 'b'.repeat(40)]).verify('candidate', ['amd64', 'arm64'], sha));
  await assert.rejects(fixture(['amd64'], [sha], true).inspect('candidate'), /digest mismatch/);
});

test('rejects an index that swaps platform descriptors even when both image configs exist', async () => {
  await assert.rejects(fixture(['amd64', 'arm64'], undefined, false, ['arm64', 'amd64'])
    .verify('candidate', ['amd64', 'arm64'], sha), /Index architecture differs/);
});

test('index descriptors cannot redirect verification to a mutable tag', async () => {
  const registry = new Registry(image, { env: {}, fetcher: async url => url.includes('/token?')
    ? new Response(json({ token: 'test-only' }))
    : new Response(json({ manifests: [{ digest: 'latest', platform: { os: 'linux', architecture: 'amd64' } }] })) });
  await assert.rejects(registry.inspect('candidate'), /immutable digest/);
});

test('assembly cannot invoke Docker when publication is disabled or confirmation is missing', async () => {
  await assert.rejects(main(['assemble'], env), /disabled/);
  await assert.rejects(main(['assemble'], { ...env, PUBLISH_CANDIDATES: 'true' }), /confirmation/);
});

test('registry host and image names are restricted to the two intended packages', () => {
  for (const name of ['docker.io/example/app', 'ghcr.io/ifpebj-ti/other-image', 'ghcr.io/ifpebj-ti/gestaotccs-frontend;echo']) {
    assert.throws(() => new Registry(name));
  }
});

test('candidate workflow cannot be started by a push, merge, release or schedule', () => {
  const workflow = fs.readFileSync(path.join(__dirname, '../../.github/workflows/multiarch-publication.yml'), 'utf8');
  const events = workflow.match(/^on:\r?\n([\s\S]*?)(?=^\S)/m)?.[1];
  assert.ok(events);
  assert.match(events, /^  workflow_dispatch:/m);
  assert.doesNotMatch(events, /^  (?:push|pull_request|pull_request_target|release|schedule|workflow_run|workflow_call):/m);
  assert.doesNotMatch(workflow, /PORTAINER_WEBHOOK|softprops\/action-gh-release|gh api|upload-artifact|cache-to:/);
  assert.match(workflow, /DOCKER_BUILD_RECORD_UPLOAD: 'false'/);
});

test('production keeps automatic release and deployment; candidate validation is isolated', () => {
  const workflow = fs.readFileSync(path.join(__dirname, '../../.github/workflows/production.yml'), 'utf8');
  const events = workflow.match(/^on:\r?\n([\s\S]*?)(?=^\S)/m)?.[1];
  assert.match(events, /pull_request:/);
  assert.match(events, /- main/);
  assert.match(events, /- closed/);
  assert.match(workflow, /softprops\/action-gh-release/);
  assert.match(workflow, /PORTAINER_WEBHOOK_BACKEND/);
  assert.match(workflow, /PORTAINER_WEBHOOK_FRONTEND/);
  assert.match(workflow, /assemble-release/);
  assert.match(workflow, /promote-release/);
  assert.doesNotMatch(workflow, /vars\.NEXT_PUBLIC_API_URL|PUBLIC_API_URL/);
});

const releaseEnv = { ...env, GITHUB_REPOSITORY: 'ifpebj-ti/gestao-tccs' };

function releaseMocks(failVerification = () => false) {
  const writes = [];
  const reads = [];
  return { writes, reads, options: {
    fetcher: async () => new Response(null, { status: 404 }),
    createManifest: args => writes.push(args),
    registryFactory: name => ({
      assertAbsent: async tag => { reads.push(`absent:${name}:${tag}`); },
      verify: async (tag, arches, revision) => {
        reads.push(`verify:${name}:${tag}`);
        assert.equal(revision, sha);
        if (failVerification(name, tag)) throw new Error('Verification failed');
        return { digest: `sha256:${name.endsWith('backend') ? 'b'.repeat(64) : 'f'.repeat(64)}` };
      }
    })
  } };
}

test('release preflight refuses existing Git tags and authentication/service failures', async () => {
  for (const status of [200, 401, 403, 429, 500]) {
    const mocks = releaseMocks();
    mocks.options.fetcher = async () => new Response(null, { status });
    await assert.rejects(releasePreflight('2.13.0', releaseEnv, mocks.options));
    assert.equal(mocks.reads.length, 0);
    assert.equal(mocks.writes.length, 0);
  }
});

test('release preflight checks both package version tags and rejects existing images', async () => {
  const mocks = releaseMocks();
  await releasePreflight('2.13.0', releaseEnv, mocks.options);
  assert.equal(mocks.reads.filter(item => item.startsWith('absent:')).length, 2);
  mocks.options.registryFactory = () => ({ assertAbsent: async () => { throw new Error('Tag exists'); } });
  await assert.rejects(releasePreflight('2.13.0', releaseEnv, mocks.options));
  await assert.rejects(releasePreflight('latest', releaseEnv, mocks.options));
});

test('checks the real commit behind lightweight and annotated Git release tags', async () => {
  await verifyReleaseSource('2.13.0', releaseEnv,
    async () => new Response(json({ object: { type: 'commit', sha } })));
  let calls = 0;
  await verifyReleaseSource('2.13.0', releaseEnv, async url => {
    calls++;
    return new Response(json({ object: url.includes('/git/ref/')
      ? { type: 'tag', sha: 'c'.repeat(40) } : { type: 'commit', sha } }));
  });
  assert.equal(calls, 2);
});

test('wrong Git release commit or unavailable API blocks promotion', async () => {
  await assert.rejects(verifyReleaseSource('2.13.0', releaseEnv,
    async () => new Response(json({ object: { type: 'commit', sha: 'b'.repeat(40) } }))));
  await assert.rejects(verifyReleaseSource('2.13.0', releaseEnv, async () => new Response(null, { status: 403 })));
});

test('no SemVer manifest is written if either staged service fails verification', async () => {
  const mocks = releaseMocks(name => name.endsWith('frontend'));
  await assert.rejects(assembleRelease('2.13.0', releaseEnv, mocks.options));
  assert.equal(mocks.writes.length, 0);
});

test('release assembly uses immutable staged digests and never writes latest', async () => {
  const mocks = releaseMocks();
  await assembleRelease('2.13.0', releaseEnv, mocks.options);
  assert.equal(mocks.writes.length, 2);
  for (const args of mocks.writes) {
    assert.match(args[1], /:2\.13\.0$/);
    assert.match(args[2], /@sha256:[a-f0-9]{64}$/);
    assert.match(args[3], /@sha256:[a-f0-9]{64}$/);
  }
});

test('neither latest moves if verification of one published version fails', async () => {
  const mocks = releaseMocks(name => name.endsWith('frontend'));
  await assert.rejects(promoteRelease('2.13.0', releaseEnv, mocks.options));
  assert.equal(mocks.writes.length, 0);
});

test('promotion verifies both latest tags after writing by immutable digest', async () => {
  const mocks = releaseMocks();
  await promoteRelease('2.13.0', releaseEnv, mocks.options);
  assert.equal(mocks.writes.length, 2);
  assert.equal(mocks.reads.filter(item => item.endsWith(':latest')).length, 2);
  for (const args of mocks.writes) assert.match(args[2], /@sha256:[a-f0-9]{64}$/);
});

test('failed registry write stops release assembly and promotion', async () => {
  for (const operation of [assembleRelease, promoteRelease]) {
    const mocks = releaseMocks();
    let calls = 0;
    mocks.options.createManifest = () => { calls++; throw new Error('Registry unavailable'); };
    await assert.rejects(operation('2.13.0', releaseEnv, mocks.options));
    assert.equal(calls, 1);
  }
});

test('failure of the second registry write is propagated instead of claiming atomic success', async () => {
  for (const operation of [assembleRelease, promoteRelease]) {
    const mocks = releaseMocks();
    let calls = 0;
    mocks.options.createManifest = () => { if (++calls === 2) throw new Error('Second write failed'); };
    await assert.rejects(operation('2.13.0', releaseEnv, mocks.options), /Second write failed/);
    assert.equal(calls, 2);
  }
});

test('badge changes only its marker line and preserves the current README blob SHA', () => {
  const content = '# Conteúdo atualizado\nOutra alteração já mergeada.\nold <!-- COVERAGE_BADGE -->\nFim.\n';
  const original = { sha: 'c'.repeat(40), encoding: 'base64', content: Buffer.from(content).toString('base64') };
  const payload = badgePayload(original, '14');
  assert.equal(payload.sha, original.sha);
  assert.equal(Buffer.from(payload.content, 'base64').toString('utf8'), content.replace('old <!-- COVERAGE_BADGE -->',
    '![Coverage](https://img.shields.io/badge/Coverage-14%25-brightgreen) <!-- COVERAGE_BADGE -->'));
  assert.throws(() => badgePayload(original, '101'));
  assert.throws(() => badgePayload({ ...original, content: Buffer.from('No marker').toString('base64') }, '14'));
});

test('actual versioning script skips outros and preserves the three normal version increments', () => {
  const workflow = fs.readFileSync(path.join(__dirname, '../../.github/workflows/production.yml'), 'utf8');
  const block = workflow.split('      - name: Calculate new tag\n')[1].split('\n      - name:')[0];
  const source = block.split('        run: |\n')[1].split('\n').map(line => line.replace(/^          /, '')).join('\n');
  const bash = process.platform === 'win32' ? 'C:/Program Files/Git/bin/bash.exe' : 'bash';
  for (const [kind, expected] of [['outros', null], ['bug-fix', '2.12.1'], ['nova-feature-refactor', '2.13.0'], ['novo-marco', '3.0.0']]) {
    const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'gradus-version-test-'));
    const output = path.join(folder, 'output');
    try {
      const script = source.replaceAll('${{ steps.last_tag.outputs.last_tag }}', '2.12.0')
        .replaceAll('${{ steps.change_type.outputs.change_type }}', kind);
      const result = spawnSync(bash, ['-e', '-c', script], { env: { ...process.env, GITHUB_OUTPUT: output }, encoding: 'utf8' });
      assert.equal(result.status, 0, result.stderr);
      const actual = fs.readFileSync(output, 'utf8');
      assert.match(actual, new RegExp(`should_release=${expected ? 'true' : 'false'}`));
      if (expected) assert.ok(actual.includes(`created_tag=${expected}`));
      else assert.doesNotMatch(actual, /created_tag=/);
    } finally { fs.rmSync(output, { force: true }); fs.rmdirSync(folder); }
  }
});

test('only the joint successful release job can promote or call either webhook', () => {
  const workflow = fs.readFileSync(path.join(__dirname, '../../.github/workflows/production.yml'), 'utf8');
  const joint = workflow.split('  release-creation:\n')[1];
  assert.match(joint, /needs: \[tag-creation, backend-docker, frontend-docker\]/);
  const badge = joint.split('      - name: Upload Code Coverage Badge from the current README snapshot\n')[1]
    .split('\n      - name:')[0];
  assert.doesNotMatch(joint.replace(badge, ''), /always\(\)|continue-on-error/);
  const builders = workflow.split('  backend-docker:\n')[1].split('  release-creation:\n')[0];
  assert.doesNotMatch(builders, /PORTAINER_WEBHOOK|imagetools create|:latest/);
  assert.equal((workflow.match(/ref: \$\{\{ github.sha \}\}/g) || []).length, 4);
  assert.match(joint, /target_commitish: \$\{\{ github.sha \}\}/);
  assert.ok(joint.indexOf('assemble-release') < joint.indexOf('promote-release'));
  assert.ok(joint.indexOf('verify-release-source') < joint.indexOf('promote-release'));
  assert.ok(joint.indexOf('promote-release') < joint.indexOf('PORTAINER_WEBHOOK_BACKEND'));
  assert.match(joint, /curl --fail-with-body -k -X POST "\$PORTAINER_WEBHOOK_BACKEND"/);
  assert.match(joint, /curl --fail-with-body -k -X POST "\$PORTAINER_WEBHOOK_FRONTEND"/);
});

test('only the administrative badge failure is non-blocking; release and webhook failures remain blocking', () => {
  const workflow = fs.readFileSync(path.join(__dirname, '../../.github/workflows/production.yml'), 'utf8');
  const joint = workflow.split('  release-creation:\n')[1];
  const badge = joint.split('      - name: Upload Code Coverage Badge from the current README snapshot\n')[1]
    .split('\n      - name:')[0];
  assert.match(badge, /^        continue-on-error: true$/m);
  assert.equal((workflow.match(/continue-on-error:/g) || []).length, 1);
  assert.doesNotMatch(joint.replace(badge, ''), /always\(\)|continue-on-error/);
  assert.match(badge, /gh api --method PUT/);
  assert.ok(joint.indexOf('promote-release') < joint.indexOf('Upload Code Coverage Badge'));
  assert.ok(joint.indexOf('Upload Code Coverage Badge') < joint.indexOf('PORTAINER_WEBHOOK_BACKEND'));
});
