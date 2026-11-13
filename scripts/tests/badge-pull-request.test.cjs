const { test } = require('node:test');
const assert = require('node:assert/strict');
const { updateBadge } = require('../badge-pull-request.cjs');
const { badgePayload } = require('../controlled-publication.cjs');
const env = { GITHUB_REPOSITORY: 'team/project', GITHUB_SHA: 'a'.repeat(40),
  GITHUB_RUN_ID: '123', CREATED_TAG: '2.13.2', COVERAGE_PERCENTAGE: '95', GITHUB_TOKEN: 'test-only' };
const original = { sha: 'b'.repeat(40), encoding: 'base64',
  content: Buffer.from('# Project\nold <!-- COVERAGE_BADGE -->\nOther content\n').toString('base64') };

function fixture(options = {}) {
  const calls = [];
  const request = async (url, init) => {
    const route = url.replace('https://api.github.com/repos/team/project/', '');
    const body = init.body ? JSON.parse(init.body) : undefined;
    calls.push({ route, method: init.method, body });
    let data;
    let status = 200;
    if (options.fail === route) status = 403;
    else if (route.startsWith('git/ref/heads/ci/')) {
      if (options.existing) data = { object: { sha: 'c'.repeat(40) } };
      else status = 404;
    } else if (route === 'git/ref/heads/main') data = { object: { sha: options.advanced ? 'd'.repeat(40) : env.GITHUB_SHA } };
    else if (route === `contents/README.md?ref=${env.GITHUB_SHA}`) data = original;
    else if (route.startsWith('contents/README.md?ref=')) data = {
      content: options.modified ? original.content : badgePayload(original, '95').content };
    else if (route.startsWith('compare/')) data = { status: 'ahead', ahead_by: 1,
      files: [{ filename: options.unrelated ? 'other.txt' : 'README.md' }] };
    else if (route.startsWith('git/commits/') && init.method === 'GET') data = { tree: { sha: 'tree-base' } };
    else if (route === 'git/blobs') data = { sha: 'new-blob' };
    else if (route === 'git/trees') data = { sha: 'new-tree' };
    else if (route === 'git/commits') data = { sha: 'c'.repeat(40) };
    else if (route === 'git/refs') data = {};
    else if (route.startsWith('pulls?')) data = options.pr ? [{ state: options.closed ? 'closed' : 'open', html_url: 'https://github.com/team/project/pull/1' }] : [];
    else if (route === 'pulls') data = { html_url: 'https://github.com/team/project/pull/1' };
    else throw new Error(`Unexpected API call: ${route}`);
    return { status, ok: status >= 200 && status < 300, json: async () => data };
  };
  return { calls, request };
}

test('creates only a README commit on a new branch and an outros PR without merging', async () => {
  const mock = fixture();
  await updateBadge(env, mock.request);
  const writes = mock.calls.filter(call => call.method !== 'GET');
  assert.ok(writes.every(call => call.method === 'POST'));
  const tree = writes.find(call => call.route === 'git/trees').body;
  assert.equal(tree.base_tree, 'tree-base');
  assert.deepEqual(tree.tree.map(entry => entry.path), ['README.md']);
  assert.deepEqual(writes.find(call => call.route === 'git/commits').body.parents, [env.GITHUB_SHA]);
  assert.equal(writes.find(call => call.route === 'git/refs').body.ref,
    `refs/heads/ci/coverage-${env.GITHUB_SHA}-123`);
  const pr = writes.find(call => call.route === 'pulls').body;
  assert.equal(pr.base, 'main');
  assert.match(pr.body, /- \[x\] outros/);
  assert.equal((pr.body.match(/- \[x\]/g) || []).length, 1);
  assert.ok(pr.body.includes(env.GITHUB_SHA));
  assert.ok(pr.body.includes('/actions/runs/123'));
  assert.doesNotMatch(JSON.stringify(writes), /\/merge|refs\/heads\/main/);
});

test('retry reuses a verified branch and open PR without writing', async () => {
  const mock = fixture({ existing: true, pr: true });
  const result = await updateBadge(env, mock.request);
  assert.match(result.url, /pull\/1$/);
  assert.ok(mock.calls.every(call => call.method === 'GET'));
});

test('rejects changed retry branches, unrelated files and closed PRs', async () => {
  for (const options of [{ modified: true }, { unrelated: true }, { pr: true, closed: true }]) {
    const mock = fixture({ existing: true, ...options });
    await assert.rejects(updateBadge(env, mock.request));
    assert.ok(mock.calls.every(call => call.method === 'GET'));
  }
});

test('does not publish an older measurement when main advanced', async () => {
  const mock = fixture({ advanced: true });
  await assert.rejects(updateBadge(env, mock.request), /Main advanced/);
  assert.ok(mock.calls.every(call => call.method === 'GET'));
});

test('invalid coverage and API permission failures stop before creating refs', async () => {
  for (const settings of [{ percentage: '101' }, { fail: 'git/ref/heads/main' }, { fail: 'git/blobs' }]) {
    const mock = fixture(settings);
    await assert.rejects(updateBadge({ ...env, COVERAGE_PERCENTAGE: settings.percentage || '95' }, mock.request));
    assert.ok(!mock.calls.some(call => call.route === 'git/refs'));
  }
});

test('unchanged badge does not open a PR', async () => {
  const mock = fixture();
  const request = async (url, init) => {
    if (url.endsWith(`contents/README.md?ref=${env.GITHUB_SHA}`))
      return { ok: true, status: 200, json: async () => ({ ...original, content: badgePayload(original, '95').content }) };
    return mock.request(url, init);
  };
  assert.deepEqual(await updateBadge(env, request), { unchanged: true });
  assert.ok(mock.calls.every(call => call.method === 'GET'));
});
