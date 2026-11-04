const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');
const { getSidebarItems } = loadSource('src/components/Sidebar/menuConfig.tsx');
const { filterTccs } = loadSource('src/lib/tccSearch.ts');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { TccSearch } = loadSource('src/components/TccSearch.tsx');

test('coordenação mantém somente os sete destinos solicitados, inclusive com perfil de orientação', () => {
  for (const roles of [['COORDINATOR'], ['COORDINATOR', 'ADVISOR']]) {
    const items = getSidebarItems(roles, { status: 'none' }, 2);
    assert.deepEqual(items.map(({ label }) => label), [
      'Início', 'Banca', 'Pendências', 'Em andamento', 'Concluídos', 'Usuários', 'Minha conta'
    ]);
    assert.equal(items[1].href, '/banking');
    assert.equal(items[1].badge, 2);
  }
});

test('orientação e supervisão preservam seus atalhos de propostas e assinaturas', () => {
  for (const role of ['ADVISOR', 'SUPERVISOR']) {
    const items = getSidebarItems([role], { status: 'none' });
    assert.ok(items.some(({ href }) => href === '/receivedProposals'));
    assert.ok(items.some(({ href }) => href === '/pendingSignatures'));
  }
});

test('administração com perfil herdado de coordenação preserva seu menu de gestão', () => {
  const items = getSidebarItems(['ADMIN', 'COORDINATOR'], { status: 'none' });
  assert.ok(items.some(({ href }) => href === '/instituicoes'));
  assert.ok(items.some(({ href }) => href === '/semestres'));
  assert.ok(items.some(({ href }) => href === '/newTCC'));
});

const records = [
  { tccId: 1, studanteNames: ['João Silva', 'Maria Santos'], title: 'Educação inclusiva', advisorUserId: 12 },
  { tccId: 2, studanteNames: ['Ana Lima'], title: 'Sistemas distribuídos', advisorUserId: 13 },
  { tccId: 3, studanteNames: [] }
];
const advisors = { 12: 'Lúcia Almeida', 13: 'Pedro Costa' };

test('busca encontra estudante, título e professor ignorando acentos, caixa e espaços extras', () => {
  for (const query of ['  JOAO  ', 'maria santos', 'EDUCACAO', 'lucia almeida', 'joao inclusiva']) {
    assert.deepEqual(filterTccs(records, query, advisors).map(({ tccId }) => tccId), [1]);
  }
  assert.deepEqual(filterTccs(records, 'pedro', advisors).map(({ tccId }) => tccId), [2]);
});

test('busca vazia preserva os registros e uma busca sem correspondência retorna lista vazia', () => {
  assert.equal(filterTccs(records, '   ', advisors), records);
  assert.deepEqual(filterTccs(records, 'inexistente', advisors), []);
  assert.deepEqual(filterTccs(records, 'ana', {}).map(({ tccId }) => tccId), [2]);
  assert.equal(records.length, 3);
});

test('busca do professor considera estudante e título sem consultar nomes de orientadores', () => {
  assert.deepEqual(filterTccs(records, 'pedro', {}), []);
  assert.deepEqual(filterTccs(records, 'sistemas', {}).map(({ tccId }) => tccId), [2]);
  const html = renderToStaticMarkup(React.createElement(TccSearch, {
    value: '', onChange() {}, includeAdvisor: false, advisorSearchUnavailable: true
  }));
  assert.match(html, /Nome do estudante ou título do TCC/);
  assert.doesNotMatch(html, /ou professor|carregar os professores/);
});
