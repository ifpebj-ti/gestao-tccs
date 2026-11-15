const assert = require('node:assert/strict');
const fs = require('node:fs');
const { badgePayload } = require('./controlled-publication.cjs');

async function updateBadge(env = process.env, request = fetch) {
  assert.match(env.GITHUB_REPOSITORY || '', /^[\w.-]+\/[\w.-]+$/);
  assert.match(env.GITHUB_SHA || '', /^[a-f0-9]{40}$/);
  assert.match(env.GITHUB_RUN_ID || '', /^\d+$/);
  assert.match(env.CREATED_TAG || '', /^\d+\.\d+\.\d+$/);
  assert.ok(env.GITHUB_TOKEN, 'GitHub token missing');
  const root = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}`;
  async function api(path, method = 'GET', body, allowMissing = false) {
    const response = await request(`${root}/${path}`, {
      method, redirect: 'error',
      headers: { Authorization: `Bearer ${env.GITHUB_TOKEN}`, Accept: 'application/vnd.github+json',
        'Content-Type': 'application/json', 'X-GitHub-Api-Version': '2022-11-28' },
      ...(body ? { body: JSON.stringify(body) } : {})
    });
    if (allowMissing && response.status === 404) return null;
    if (!response.ok) throw new Error(`Badge API request failed (${method}, HTTP ${response.status})`);
    return response.json();
  }
  const branch = `ci/coverage-${env.GITHUB_SHA}-${env.GITHUB_RUN_ID}`;
  const existing = await api(`git/ref/heads/${branch}`, 'GET', undefined, true);
  const main = await api('git/ref/heads/main');
  const readme = await api(`contents/README.md?ref=${env.GITHUB_SHA}`);
  const payload = badgePayload(readme, env.COVERAGE_PERCENTAGE);
  if (!existing) {
    // Do not propose an older release measurement over changes already merged to main.
    assert.equal(main.object.sha, env.GITHUB_SHA, 'Main advanced; prepare a reviewed badge update manually');
    if (payload.content === readme.content.replace(/\s/g, '')) return { unchanged: true };
    const source = await api(`git/commits/${env.GITHUB_SHA}`);
    const blob = await api('git/blobs', 'POST', { content: payload.content, encoding: 'base64' });
    const tree = await api('git/trees', 'POST', { base_tree: source.tree.sha,
      tree: [{ path: 'README.md', mode: '100644', type: 'blob', sha: blob.sha }] });
    const commit = await api('git/commits', 'POST', {
      message: payload.message, tree: tree.sha, parents: [env.GITHUB_SHA] });
    // Only a new branch is created. No protected branch or existing ref is updated.
    await api('git/refs', 'POST', { ref: `refs/heads/${branch}`, sha: commit.sha });
  } else {
    // A retry must not trust a modified or unrelated branch with the expected name.
    const comparison = await api(`compare/${env.GITHUB_SHA}...${existing.object.sha}`);
    assert.equal(comparison.status, 'ahead');
    assert.equal(comparison.ahead_by, 1);
    assert.equal(comparison.files.length, 1);
    assert.equal(comparison.files[0].filename, 'README.md');
    const current = await api(`contents/README.md?ref=${existing.object.sha}`);
    assert.equal(current.content.replace(/\s/g, ''), payload.content, 'Badge branch was modified');
  }
  const prs = await api(`pulls?state=all&base=main&head=${encodeURIComponent(env.GITHUB_REPOSITORY.split('/')[0] + ':' + branch)}`);
  if (prs.length) {
    assert.equal(prs.length, 1);
    assert.equal(prs[0].state, 'open', 'Badge PR already closed; review before retrying');
    return { url: prs[0].html_url };
  }
  const run = `https://github.com/${env.GITHUB_REPOSITORY}/actions/runs/${env.GITHUB_RUN_ID}`;
  const body = `## Tipo de mudança\n\n- [ ] novo-marco\n- [ ] nova-feature-refactor\n- [ ] bug-fix\n- [x] outros\n\n---\n\n## Descrição\n\nRelacionada à #748. Atualiza somente o badge de cobertura do backend para ${env.COVERAGE_PERCENTAGE}%, arredondado para baixo.\n\nRelease: ${env.CREATED_TAG}\nCommit avaliado: ${env.GITHUB_SHA}\nExecução: ${run}\n\n## Critérios de conclusão e evidências\n\n- [ ] Conferir a medição no CI da release.\n- [ ] Confirmar que o diff altera somente o badge do README.\n- [ ] Aprovar a execução dos workflows, se solicitada pelo GitHub, e aguardar o Quality gate.\n- [ ] Obter a aprovação de outro integrante.\n\nComo validar: comparar o badge com a cobertura aprovada na execução indicada. A cobertura se refere ao commit da release, não a alterações posteriores.\n\nImpacto e rollback: altera apenas documentação; reverter por PR se necessário. Não gera release nem deploy.\n\n## Revisão por outra pessoa\n\nRespeitar as proteções da main. Esta automação não aprova nem integra a PR.\n`;
  const pr = await api('pulls', 'POST', { title: `docs: atualiza cobertura da release ${env.CREATED_TAG}`,
    head: branch, base: 'main', body });
  return { url: pr.html_url };
}

if (require.main === module) {
  updateBadge().then(result => {
    const message = result.unchanged ? 'Badge already matches the approved coverage.' : `Badge PR: ${result.url}`;
    console.log(message);
    if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, `${message}\n`);
  }).catch(error => {
    console.error(error.message);
    if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY,
      'Badge PR failed. Review the badge step; the README was not updated automatically.\n');
    process.exitCode = 1;
  });
}
module.exports = { updateBadge };
