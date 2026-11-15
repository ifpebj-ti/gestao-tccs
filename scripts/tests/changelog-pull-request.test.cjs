const { test } = require('node:test');
const assert = require('node:assert/strict');
const { renderChangelog, updateChangelog } = require('../changelog-pull-request.cjs');
const release = { tag_name: '2.13.3', published_at: '2026-11-14T12:00:00Z',
  html_url: 'https://github.com/team/project/releases/tag/2.13.3', body: '## Correções\n\nNotas publicadas.' };
const env = { GITHUB_REPOSITORY: 'team/project', GITHUB_SHA: 'a'.repeat(40),
  GITHUB_RUN_ID: '123', GITHUB_TOKEN: 'test-only' };
function fixture(options = {}) {
  const calls = [];
  async function request(url, init) {
    const path = url.replace('https://api.github.com/repos/team/project/', '');
    const body = init.body ? JSON.parse(init.body) : undefined;
    calls.push({ path, method: init.method, body });
    let data, status = 200;
    if (options.fail === path) status = 403;
    else if (path.startsWith('releases?')) data = [release];
    else if (path === 'git/ref/heads/main') data = { object: { sha: env.GITHUB_SHA } };
    else if (path.startsWith('contents/CHANGELOG.md?')) data = { content: Buffer.from(options.unchanged || (options.existing && !options.modified && path.includes('commit')) ? renderChangelog([release]) : '# Old').toString('base64') };
    else if (path.startsWith('git/ref/heads/ci/')) {
      if (options.existing) data = { object: { sha: 'commit' } };
      else status = 404;
    }
    else if (path.startsWith('compare/')) data = { status: 'ahead', ahead_by: 1, files: [{ filename: 'CHANGELOG.md' }] };
    else if (path.startsWith('git/commits/') && init.method === 'GET') data = { tree: { sha: 'old-tree' } };
    else if (path === 'git/blobs') data = { sha: 'blob' };
    else if (path === 'git/trees') data = { sha: 'tree' };
    else if (path === 'git/commits') data = { sha: 'commit' };
    else if (path === 'git/refs') data = {};
    else throw new Error(`Unexpected route: ${path}`);
    return { ok: status === 200, status, json: async () => data };
  }
  return { calls, request };
}
test('render preserves published notes, sorts dates and omits drafts and prereleases', () => {
  const text = renderChangelog([{ ...release, tag_name: '2.12.0', published_at: '2026-11-02T12:00:00Z' },
    { ...release, tag_name: '9.0.0', draft: true }, { ...release, prerelease: true }, release]);
  assert.ok(text.indexOf('## [2.13.3]') < text.indexOf('## [2.12.0]'));
  assert.ok(text.includes('### Correções\n\nNotas publicadas.'));
  assert.ok(!text.includes('9.0.0'));
  assert.equal((text.match(/## \[2.13.3\]/g) || []).length, 1);
});
test('missing notes are explicit and invalid metadata is rejected', () => {
  assert.match(renderChangelog([{ ...release, body: '' }]), /Notas não fornecidas/);
  assert.throws(() => renderChangelog([{ ...release, tag_name: '../bad' }]));
  assert.throws(() => renderChangelog([]));
});
test('unchanged content performs no writes', async () => {
  const f = fixture({ unchanged: true });
  assert.deepEqual(await updateChangelog(env, f.request), { unchanged: true });
  assert.ok(f.calls.every(c => c.method === 'GET'));
});
test('writes only changelog to a new branch and returns a manual PR link without PR API access', async () => {
  const f = fixture();
  const result = await updateChangelog(env, f.request);
  const writes = f.calls.filter(c => c.method !== 'GET');
  assert.ok(writes.every(c => c.method === 'POST'));
  assert.deepEqual(writes.find(c => c.path === 'git/trees').body.tree.map(t => t.path), ['CHANGELOG.md']);
  assert.equal(writes.find(c => c.path === 'git/refs').body.ref, 'refs/heads/ci/changelog-123');
  assert.equal(result.url, 'https://github.com/team/project/compare/main...ci/changelog-123?expand=1');
  assert.equal(result.manual, true);
  assert.ok(!f.calls.some(c => /pulls|merge|reviews/.test(c.path)));
});
test('permission failure stops without opening a PR', async () => {
  const f = fixture({ fail: 'git/blobs' });
  await assert.rejects(updateChangelog(env, f.request), /HTTP 403/);
  assert.ok(!f.calls.some(c => c.path === 'pulls'));
});
test('retry verifies the prepared branch without rewriting it or using PR permissions', async () => {
  const f = fixture({ existing: true });
  assert.equal((await updateChangelog(env, f.request)).manual, true);
  assert.ok(f.calls.every(c => c.method === 'GET'));
  const modified = fixture({ existing: true, modified: true });
  await assert.rejects(updateChangelog(env, modified.request));
});
