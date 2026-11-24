const test = require('node:test');
const assert = require('node:assert/strict');
const { NextRequest } = require('next/server');
const { unstable_doesMiddlewareMatch } = require('next/experimental/testing/server');
const { loadSource } = require('./load-source.cjs');
const { middleware, config } = loadSource('src/middleware.ts');
const roles = [
  'ADMIN',
  'COORDINATOR',
  'SUPERVISOR',
  'ADVISOR',
  'STUDENT',
  'BANKING',
  'LIBRARY'
];
function token(claims) {
  const encode = (value) =>
    Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none' })}.${encode({ userId: '101', exp: Math.floor(Date.now() / 1000) + 3600, ...claims })}.teste`;
}
function access(path, value) {
  return middleware(
    new NextRequest(`http://localhost:3000${path}`, {
      headers: value ? { cookie: `token=${value}` } : {}
    })
  );
}
function allowed(path, claims) {
  assert.equal(access(path, token(claims)).headers.get('location'), null);
}
function redirected(path, value, target) {
  assert.equal(
    new URL(access(path, value).headers.get('location')).pathname,
    target
  );
}
test('todas as rotas protegidas rejeitam visitante sem cookie', () => {
  for (const path of [
    '/homePage',
    '/profile',
    '/newTCC',
    '/newUser',
    '/ongoingTCCs',
    '/myTCC/signatures',
    '/completedTCCs',
    '/pendingSignatures',
    '/libraryPending',
    '/users',
    '/semestres',
    '/instituicoes'
  ])
    redirected(path, null, '/');
  assert.ok(unstable_doesMiddlewareMatch({ config, url: '/instituicoes/new' }));
  assert.ok(unstable_doesMiddlewareMatch({ config, url: '/libraryPending' }));
});

test('as páginas de login e do TCC usam nonces independentes para os scripts', () => {
  const signIn = access('/', null).headers.get('content-security-policy');
  const tcc = access('/myTCC', token({ role: 'STUDENT' })).headers.get('content-security-policy');
  assert.match(signIn, /default-src 'self'/);
  const nonce = /script-src 'self' 'nonce-([a-f0-9]+)'/;
  assert.match(signIn, nonce);
  assert.match(tcc, nonce);
  assert.notEqual(signIn.match(nonce)[1], tcc.match(nonce)[1]);
  assert.equal(unstable_doesMiddlewareMatch({ config, url: '/_next/static/chunk.js' }), false);
});
test('cookie inválido, expirado ou com identidade inválida retorna ao login', () => {
  redirected('/profile', 'invalido', '/');
  for (const claims of [
    { exp: 0, role: 'STUDENT' },
    { exp: '9999999999', role: 'STUDENT' },
    { userId: '-1', role: 'ADMIN' },
    { userId: 'abc', role: 'ADMIN' },
    { role: 'DESCONHECIDO' },
    { role: [] }
  ])
    redirected('/profile', token(claims), '/');
});
test('todos os perfis autenticados acessam início e própria conta', () => {
  for (const role of roles)
    for (const path of ['/profile', '/homePage']) allowed(path, { role });
});
const matrix = {
  '/newTCC': ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'STUDENT'],
  '/receivedProposals': ['ADVISOR', 'COORDINATOR', 'SUPERVISOR'],
  '/users': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/newUser': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/ongoingTCCs': roles.filter(
    (role) => role !== 'STUDENT' && role !== 'LIBRARY'
  ),
  '/completedTCCs': [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR'
  ],
  '/libraryPending': ['LIBRARY', 'ADMIN'],
  '/pendingSignatures': roles.filter((role) => role !== 'LIBRARY'),
  '/myTCC/signatures': ['STUDENT'],
  '/myTCC/files': ['STUDENT'],
  '/ongoingTCCs/files': roles.filter(
    (role) => role !== 'STUDENT' && role !== 'LIBRARY'
  ),
  '/completedTCCs/files': [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'STUDENT'
  ],
  '/semestres': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/instituicoes': ['ADMIN']
};
for (const [path, permitted] of Object.entries(matrix))
  test(`matriz de acesso ${path}`, () => {
    for (const role of roles) {
      if (permitted.includes(role)) allowed(path, { role });
      else redirected(path, token({ role }), '/unauthorized');
    }
  });
test('múltiplos perfis mantêm união de acesso e detalhe concluído discente', () => {
  allowed('/users', { role: ['STUDENT', 'COORDINATOR'] });
  allowed('/myTCC/signatures', { role: ['STUDENT', 'COORDINATOR'] });
  allowed('/completedTCCs/42', { role: 'STUDENT' });
  redirected('/completedTCCs', token({ role: 'STUDENT' }), '/unauthorized');
});
