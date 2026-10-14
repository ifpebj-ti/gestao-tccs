const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');
const { decideReceivedProposal, loadReceivedProposals } = loadSource(
  'src/lib/receivedProposals.ts'
);
const { getSidebarItems } = loadSource('src/components/Sidebar/menuConfig.tsx');
test('professor recebe propostas sem acesso de cadastro; gestão preserva cadastro', () => {
  const advisor = getSidebarItems(['ADVISOR'], { status: 'none' });
  assert.ok(advisor.some((x) => x.href === '/receivedProposals'));
  assert.ok(!advisor.some((x) => x.href === '/newTCC'));
  assert.ok(
    getSidebarItems(['ADVISOR', 'COORDINATOR'], { status: 'none' }).some(
      (x) => x.href === '/newTCC'
    )
  );
});
test('aprovação e recusa usam PATCH e identificador na rota real', async () => {
  const original = global.fetch;
  const calls = [];
  global.fetch = async (url, init) => {
    calls.push({ url, init });
    return new Response('{}');
  };
  try {
    await decideReceivedProposal('http://api', 'test-token', 42);
    await decideReceivedProposal(
      'http://api',
      'test-token',
      43,
      '  Ajustar tema  '
    );
    assert.equal(calls[0].url, 'http://api/Tcc/42/approve-proposal');
    assert.equal(calls[1].url, 'http://api/Tcc/43/reject-proposal');
    for (const call of calls) {
      assert.equal(call.init.method, 'PATCH');
      assert.equal(call.init.headers.Authorization, 'Bearer test-token');
    }
    assert.deepEqual(JSON.parse(calls[1].init.body), {
      reason: 'Ajustar tema'
    });
    await assert.rejects(
      decideReceivedProposal('http://api', 'token', 43, '   ')
    );
    assert.equal(calls.length, 2);
  } finally {
    global.fetch = original;
  }
});
test('falha da API preserva justificativa retornada em ProblemDetails', async () => {
  const original = global.fetch;
  global.fetch = async () =>
    new Response(
      JSON.stringify({
        detail: 'A proposta aguarda o aceite de todos os participantes.'
      }),
      { status: 409 }
    );
  try {
    await assert.rejects(
      decideReceivedProposal('http://api', 'token', 42),
      /aguarda o aceite/
    );
  } finally {
    global.fetch = original;
  }
});
test('lista filtra pelo docente e carrega resumo completo', async () => {
  const original = global.fetch;
  const urls = [];
  global.fetch = async (url) => {
    urls.push(url);
    return new Response(
      JSON.stringify(
        url.includes('/filter')
          ? [{ tccId: 42, studanteNames: ['Estudante'] }]
          : {
              infoTcc: {
                title: 'Projeto',
                summary: 'Resumo completo',
                pendingParticipantCount: 0
              }
            }
      )
    );
  };
  try {
    const items = await loadReceivedProposals('http://api', 'token', '7');
    assert.equal(items[0].summary, 'Resumo completo');
    assert.equal(
      urls[0],
      'http://api/Tcc/filter?userId=7&StatusTcc=PENDING_APPROVAL'
    );
  } finally {
    global.fetch = original;
  }
});
