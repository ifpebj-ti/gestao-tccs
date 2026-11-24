const test = require('node:test');
const assert = require('node:assert/strict');
const { validatePullRequest } = require('../check-pull-request.cjs');

function pullRequest(head, base, body = '', sourceId = 1) {
  return { head: { ref: head, repo: { id: sourceId } }, base: { ref: base, repo: { id: 1 } }, body };
}

test('a PR de trabalho referencia a issue no campo do template', () => {
  assert.equal(validatePullRequest(pullRequest('fix/arquivo', 'develop', 'Issue vinculada: Relates to #17')), undefined);
  assert.match(validatePullRequest(pullRequest('fix/arquivo', 'develop', 'Issue vinculada: <!-- Closes #123 -->')), /Issue vinculada/);
  assert.match(validatePullRequest(pullRequest('fix/arquivo', 'develop', '<!--\nIssue vinculada: #17\n-->')), /Issue vinculada/);
  assert.match(validatePullRequest(pullRequest('fix/arquivo', 'develop', '<!--\nIssue vinculada: #17')), /Issue vinculada/);
  assert.equal(validatePullRequest(pullRequest('fix/arquivo', 'develop', '<!-- comentário -->\nIssue vinculada: #17')), undefined);
});

test('a main recebe a develop do próprio repositório e as branches de publicação previstas', () => {
  assert.equal(validatePullRequest(pullRequest('develop', 'main')), undefined);
  assert.equal(validatePullRequest(pullRequest('release-please--branches--main', 'main')), undefined);
  assert.match(validatePullRequest(pullRequest('feature/teste', 'main')), /devem vir de develop/);
  assert.match(validatePullRequest(pullRequest('develop', 'main', '', 2)), /devem vir de develop/);
});

test('o Dependabot não precisa preencher o template na develop', () => {
  const pr = pullRequest('dependabot/npm', 'develop');
  pr.user = { login: 'dependabot[bot]', type: 'Bot' };
  assert.equal(validatePullRequest(pr), undefined);
});
