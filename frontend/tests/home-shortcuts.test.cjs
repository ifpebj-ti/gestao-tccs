const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource, root } = require('./load-source.cjs');
const { HomeShortcuts } = loadSource('src/components/home/HomeShortcuts.tsx');
const { CollapseCard } = loadSource('src/components/CollapseCard/index.tsx');

// Avalia as próprias condições da página, sem cookies, rede ou tokens reais.
const source = fs.readFileSync(
  path.join(root, 'src/app/(auth)/homePage/page.tsx'),
  'utf8'
);
const ast = ts.createSourceFile(
  'home.tsx',
  source,
  ts.ScriptTarget.Latest,
  true,
  ts.ScriptKind.TSX
);
const names = new Set([
  'canView',
  'isStudent',
  'canShowPendingSignatures',
  'supportText',
  'shortcuts'
]);
const declarations = [];
function visit(node) {
  if (
    ts.isVariableStatement(node) &&
    node.declarationList.declarations.some((declaration) =>
      names.has(declaration.name.getText(ast))
    )
  )
    declarations.push(node.getText(ast));
  ts.forEachChild(node, visit);
}
visit(ast);
assert.equal(declarations.length, names.size);
const modelCode = ts.transpileModule(
  declarations.join('\n') + '\nresult = { shortcuts, supportText };',
  {
    compilerOptions: { target: ts.ScriptTarget.ES2020 }
  }
).outputText;

function model(state = {}) {
  const context = {
    profile: null,
    userTCCs: [],
    completedUserTCC: [],
    tccs: null,
    advisorPendingProposals: [],
    push: () => {},
    setIsAdvisorModalOpen: () => {},
    handleMyTccClick: () => {},
    ...state
  };
  for (const icon of [
    'FileSignature',
    'Inbox',
    'GraduationCap',
    'CheckCircle2',
    'FilePlus2',
    'UserPlus',
    'Users',
    'Building2',
    'Calendar'
  ])
    context[icon] = () => null;
  vm.runInNewContext(modelCode, context);
  return context.result;
}

test('os atalhos mantêm as permissões de cada perfil', () => {
  const expected = {
    ADMIN: [
      'Cadastrar novo usuário',
      'Usuários',
      'Instituições e Cursos',
      'Semestres Letivos'
    ],
    ADVISOR: [
      'Assinaturas pendentes',
      'Propostas de TCC pendentes',
      'TCCs em andamento',
      'TCCs concluídos'
    ],
    COORDINATOR: [
      'Assinaturas pendentes',
      'Propostas de TCC pendentes',
      'TCCs em andamento',
      'TCCs concluídos',
      'Cadastrar nova proposta',
      'Cadastrar novo usuário',
      'Usuários'
    ],
    SUPERVISOR: [
      'Assinaturas pendentes',
      'Propostas de TCC pendentes',
      'TCCs em andamento',
      'TCCs concluídos',
      'Cadastrar nova proposta',
      'Cadastrar novo usuário',
      'Usuários'
    ],
    LIBRARY: ['Assinaturas pendentes', 'TCCs em andamento', 'TCCs concluídos'],
    BANKING: ['Assinaturas pendentes', 'TCCs em andamento'],
    STUDENT: []
  };
  for (const [profile, titles] of Object.entries(expected)) {
    assert.deepEqual(
      Array.from(
        model({ profile }).shortcuts.filter((card) => card.visible),
        (card) => card.title
      ),
      titles,
      profile
    );
  }
  assert.equal(
    model().shortcuts.some((card) => card.visible),
    false
  );
});

test('discente só acessa Meu TCC e assinaturas quando há trabalho em andamento ou concluído', () => {
  for (const state of [
    { userTCCs: [{ tccId: 1 }] },
    { completedUserTCC: [{ tccId: 2 }] }
  ]) {
    const cards = model({ profile: 'STUDENT', ...state }).shortcuts.filter(
      (card) => card.visible
    );
    assert.deepEqual(
      Array.from(cards, (card) => card.title),
      ['Assinaturas pendentes', 'Meu TCC']
    );
  }
});

test('múltiplos perfis preservam a união das permissões e a regra de assinaturas do discente', () => {
  const cards = model({ profile: ['STUDENT', 'ADVISOR'] }).shortcuts;
  assert.equal(
    cards.find((card) => card.title === 'Assinaturas pendentes').visible,
    false
  );
  assert.equal(
    cards.find((card) => card.title === 'Propostas de TCC pendentes').visible,
    true
  );
  const combined = model({ profile: ['ADMIN', 'LIBRARY'] }).shortcuts.filter(
    (card) => card.visible
  );
  assert.equal(combined.length, 7);
});

test('a descrição distingue carregamento de ausência real de pendências', () => {
  const signature = (state) =>
    model({ profile: 'ADVISOR', ...state }).shortcuts[0];
  assert.equal(
    signature({}).description,
    'Documentos que aguardam sua validação digital'
  );
  assert.equal(
    signature({ tccs: { pendingSignature: 0, tccInprogress: 0 } }).description,
    'Nenhuma pendência'
  );
  assert.equal(
    signature({ tccs: { pendingSignature: 3, tccInprogress: 0 } }).description,
    '3 aguardando sua assinatura'
  );
  assert.equal(
    model({ profile: 'ADVISOR' }).shortcuts[1].description,
    'Propostas submetidas por estudantes aguardando aceite'
  );
  assert.equal(
    model({ profile: 'ADVISOR', advisorPendingProposals: [{}, {}] })
      .shortcuts[1].description,
    '2 aguardando seu aceite'
  );
});

test('o apoio apresenta atividades coerentes com o perfil', () => {
  assert.match(model({ profile: 'STUDENT' }).supportText, /sua atenção/);
  assert.match(model({ profile: 'ADMIN' }).supportText, /Gerencie usuários/);
  assert.match(model({ profile: 'ADVISOR' }).supportText, /Avalie propostas/);
  assert.match(
    model({ profile: 'BANKING' }).supportText,
    /aguardam sua assinatura/
  );
});

test('os atalhos mantêm os destinos para assinaturas e propostas recebidas', () => {
  let route;

  const cards = model({
    profile: 'ADVISOR',
    push: (value) => {
      route = value;
    }
  }).shortcuts;
  cards[0].onClick();
  assert.equal(route, '/pendingSignatures');
  cards[1].onClick();
  assert.equal(route, '/receivedProposals');
});

test('grupos sem atalhos visíveis não são renderizados e não há duplicação por viewport', () => {
  assert.equal(
    renderToStaticMarkup(
      React.createElement(HomeShortcuts, { cards: model().shortcuts })
    ).includes('<section'),
    false
  );
  const html = renderToStaticMarkup(
    React.createElement(HomeShortcuts, {
      cards: model({ profile: 'ADMIN' }).shortcuts
    })
  );
  assert.match(html, /Gestão/);
  assert.equal(html.includes('Precisa da sua atenção'), false);
  assert.equal((html.match(/<button/g) || []).length, 4);
  assert.equal((html.match(/Cadastrar novo usuário/g) || []).length, 1);
});

test('a variante plana é opcional e a expansão continua acessível', () => {
  const props = {
    title: 'Documento',
    icon: React.createElement('span'),
    children: 'Conteúdo'
  };
  const original = renderToStaticMarkup(
    React.createElement(CollapseCard, props)
  );
  const flat = renderToStaticMarkup(
    React.createElement(CollapseCard, { ...props, variant: 'flat' })
  );
  assert.match(original, /rounded-lg bg-white/);
  assert.match(flat, /border-b border-\[#ECEAE2\]/);
  assert.match(flat, /aria-expanded="false"/);
  const open = renderToStaticMarkup(
    React.createElement(CollapseCard, {
      ...props,
      variant: 'flat',
      isOpen: true,
      onToggle: () => {}
    })
  );
  assert.match(open, /aria-expanded="true"/);
  assert.match(open, /Conteúdo/);
});

test('home discente renderiza somente pendências positivas, sem duplicar Meu TCC', () => {
  const html = renderToStaticMarkup(
    React.createElement(HomeShortcuts, {
      cards: model({
        profile: 'STUDENT',
        userTCCs: [{ tccId: 42 }],
        tccs: { pendingSignature: 1, tccInprogress: 1 }
      }).shortcuts,
      attentionOnly: true
    })
  );
  assert.match(html, /Assinaturas pendentes/);
  assert.equal(html.includes('Meu TCC'), false);
  assert.equal(html.includes('Acompanhamento'), false);
  assert.equal((html.match(/<button/g) || []).length, 1);
});

test('home discente diferencia carregamento de ausência de pendências', () => {
  const cards = model({
    profile: 'STUDENT',
    userTCCs: [{ tccId: 42 }],
    tccs: { pendingSignature: 0, tccInprogress: 1 }
  }).shortcuts;
  const empty = renderToStaticMarkup(
    React.createElement(HomeShortcuts, { cards, attentionOnly: true })
  );
  const loading = renderToStaticMarkup(
    React.createElement(HomeShortcuts, {
      cards,
      attentionOnly: true,
      loading: true
    })
  );
  assert.match(empty, /Nenhuma pendência para exibir/);
  assert.equal(empty.includes('<button'), false);
  assert.match(loading, /Consultando suas pendências/);
  assert.equal(loading.includes('Nenhuma pendência'), false);
});
