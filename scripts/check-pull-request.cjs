const { readFileSync } = require('node:fs');

function validatePullRequest(pr) {
  const sameRepository = pr.head.repo?.id === pr.base.repo?.id && Boolean(pr.base.repo?.id);
  const head = pr.head.ref;
  const base = pr.base.ref;
  const automation = sameRepository && base === 'main' &&
    /^(?:release-please--branches--main|ci\/changelog-\d+|ci\/coverage-[a-f0-9]{40}-\d+)$/.test(head);

  if (base === 'main' && !automation && !(sameRepository && head === 'develop')) {
    return 'PRs para main devem vir de develop ou de uma branch de publicação prevista.';
  }

  const dependabot = pr.user?.type === 'Bot' && pr.user?.login === 'dependabot[bot]';
  const synchronization = sameRepository && (head === 'main' && base === 'develop' || head === 'develop' && base === 'main');
  if (automation || dependabot || synchronization) return;

  const body = (pr.body ?? '').replace(/<!--[\s\S]*?-->/g, '');
  if (!/^\s*Issue vinculada:[ \t]*(?:(?:Closes|Fixes|Resolves|Relates to|Refs)\s+)?#[1-9]\d*\b/im.test(body)) {
    return 'Preencha o campo Issue vinculada com a referência da tarefa.';
  }
}

if (require.main === module) {
  const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
  const failure = validatePullRequest(event.pull_request);
  if (failure) {
    console.error(failure);
    process.exitCode = 1;
  }
}

module.exports = { validatePullRequest };
