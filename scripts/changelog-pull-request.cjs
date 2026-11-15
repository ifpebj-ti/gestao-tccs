const assert = require('node:assert/strict');
const fs = require('node:fs');

function renderChangelog(releases) {
  assert.ok(Array.isArray(releases) && releases.length, 'No releases returned');
  const published = releases.filter(r => !r.draft && !r.prerelease).sort((a, b) =>
    b.published_at.localeCompare(a.published_at));
  const entries = published.map(r => {
    assert.match(r.tag_name, /^v?\d+\.\d+\.\d+$/);
    assert.match(r.published_at, /^\d{4}-\d{2}-\d{2}T/);
    assert.match(r.html_url, /^https:\/\/github\.com\/[\w.-]+\/[\w.-]+\/releases\/tag\//);
    const notes = (r.body || '').replace(/\r\n/g, '\n').trim();
    // Keep release headings below the version heading in this document.
    const body = notes.replace(/^#{1,2} /gm, '### ');
    return `## [${r.tag_name}](${r.html_url}) - ${r.published_at.slice(0, 10)}\n\n${body || 'Notas não fornecidas na release. Consulte o link da versão.'}`;
  });
  return '# Changelog\n\nHistórico das releases publicadas no GitHub, em ordem de publicação. As datas abaixo são as datas de publicação das releases.\n\nEste arquivo é sincronizado por pull request. A automação não aprova nem integra alterações. As notas originais podem mencionar tecnologias e workflows usados naquela versão. O [registro manual anterior](docs/releases/changelog-legado.md) foi preservado como histórico e não descreve necessariamente a configuração atual.\n\n' + entries.join('\n\n---\n\n') + '\n';
}

async function updateChangelog(env = process.env, request = fetch) {
  assert.match(env.GITHUB_REPOSITORY || '', /^[\w.-]+\/[\w.-]+$/);
  assert.match(env.GITHUB_SHA || '', /^[a-f0-9]{40}$/);
  assert.match(env.GITHUB_RUN_ID || '', /^\d+$/);
  assert.ok(env.GITHUB_TOKEN, 'GitHub token missing');
  const root = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}`;
  async function api(path, method = 'GET', body) {
    const response = await request(`${root}/${path}`, { method, redirect: 'error',
      headers: { Authorization: `Bearer ${env.GITHUB_TOKEN}`, Accept: 'application/vnd.github+json',
        'Content-Type': 'application/json', 'X-GitHub-Api-Version': '2022-11-28' },
      ...(body ? { body: JSON.stringify(body) } : {}) });
    if (response.status === 404 && method === 'GET' && path.startsWith('git/ref/heads/ci/')) return null;
    if (!response.ok) throw new Error(`Changelog API failed (${method}, HTTP ${response.status})`);
    return response.json();
  }
  const releases = [];
  for (let page = 1; ; page++) {
    const batch = await api(`releases?per_page=100&page=${page}`);
    assert.ok(Array.isArray(batch));
    releases.push(...batch);
    if (batch.length < 100) break;
  }
  const content = renderChangelog(releases);
  const main = await api('git/ref/heads/main');
  const sourceSha = main.object.sha;
  const current = await api(`contents/CHANGELOG.md?ref=${sourceSha}`);
  if (Buffer.from(current.content, 'base64').toString('utf8') === content) return { unchanged: true };
  const branch = `ci/changelog-${env.GITHUB_RUN_ID}`;
  const prs = await api(`pulls?state=all&base=main&head=${encodeURIComponent(env.GITHUB_REPOSITORY.split('/')[0] + ':' + branch)}`);
  if (prs.length) {
    assert.equal(prs.length, 1);
    assert.equal(prs[0].state, 'open', 'Changelog PR already closed; review before retrying');
    return { url: prs[0].html_url };
  }
  const existing = await api(`git/ref/heads/${branch}`);
  if (existing) {
    const comparison = await api(`compare/${sourceSha}...${existing.object.sha}`);
    assert.equal(comparison.status, 'ahead');
    assert.equal(comparison.ahead_by, 1);
    assert.equal(comparison.files.length, 1);
    assert.equal(comparison.files[0].filename, 'CHANGELOG.md');
    const proposed = await api(`contents/CHANGELOG.md?ref=${existing.object.sha}`);
    assert.equal(Buffer.from(proposed.content, 'base64').toString('utf8'), content);
  } else {
  const source = await api(`git/commits/${sourceSha}`);
  const blob = await api('git/blobs', 'POST', { content, encoding: 'utf-8' });
  const tree = await api('git/trees', 'POST', { base_tree: source.tree.sha,
    tree: [{ path: 'CHANGELOG.md', mode: '100644', type: 'blob', sha: blob.sha }] });
  const commit = await api('git/commits', 'POST', { message: 'docs: sincroniza changelog com as releases publicadas', tree: tree.sha, parents: [sourceSha] });
  // Create a new ref only: never update main, develop, or an existing branch.
  await api('git/refs', 'POST', { ref: `refs/heads/${branch}`, sha: commit.sha });
  }
  const run = `https://github.com/${env.GITHUB_REPOSITORY}/actions/runs/${env.GITHUB_RUN_ID}`;
  const body = `## Tipo de mudança\n\n- [ ] novo-marco\n- [ ] nova-feature-refactor\n- [ ] bug-fix\n- [x] outros\n\n## Descrição\n\nSincroniza o CHANGELOG com as releases públicas, preservando suas notas e datas de publicação. Issue vinculada: não informada.\n\n## Critérios de conclusão e evidências\n\n- [ ] Comparar versões, datas e notas com as releases.\n- [ ] Confirmar que somente CHANGELOG.md foi alterado.\n- [ ] Aprovar a execução dos workflows, se solicitada, e aguardar o Quality gate.\n- [ ] Obter revisão de outro integrante.\n\nExecução do CI: ${run} (geração da PR; conferir também os checks desta PR).\n\nComo validar: comparar o diff com a página de releases do repositório.\n\nImpacto e rollback: documentação apenas; reverter por PR. A opção outros não gera release nem deploy.\n\n## Revisão por outra pessoa\n\nA automação não aprova nem integra esta PR.\n`;
  const pr = await api('pulls', 'POST', { title: 'docs: sincroniza changelog das releases', head: branch, base: 'main', body });
  return { url: pr.html_url };
}

if (require.main === module) updateChangelog().then(result => {
  const message = result.unchanged ? 'Changelog already synchronized.' : `Changelog PR: ${result.url}`;
  console.log(message);
  if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, message + '\n');
}).catch(error => { console.error(error.message); process.exitCode = 1; });

module.exports = { renderChangelog, updateChangelog };
