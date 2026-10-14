const test = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource } = require('./load-source.cjs');
const { newTccSchema } = loadSource('src/app/schemas/newTccSchema.ts');
const { ProposalInvitationCard, ProposalDeclinedNotification } = loadSource(
  'src/components/home/ProposalInvitations.tsx'
);
const valid = {
  students: [],
  advisorId: 3,
  title: 'Projeto',
  summary: 'Resumo'
};

test('notificação de recusa informa novo envio e permite confirmar a leitura', () => {
  const html = renderToStaticMarkup(
    React.createElement(ProposalDeclinedNotification, {
      notification: { title: '<Projeto>', declinedBy: 'colega@example.com' },
      busy: true,
      onDismiss() {}
    })
  );
  assert.match(html, /&lt;Projeto&gt;/);
  assert.match(html, /não foi enviada ao professor/);
  assert.match(html, /href="\/newTCC"/);
  assert.match(html, /Submeter novamente/);
  assert.match(html, /disabled=""/);
});

test('proposta individual continua válida sem convidado', () => {
  assert.equal(newTccSchema.safeParse(valid).success, true);
  assert.equal(
    newTccSchema.safeParse({ ...valid, participants: [] }).success,
    true
  );
});
test('convite exige e-mail válido e normaliza espaços', () => {
  assert.equal(
    newTccSchema.safeParse({ ...valid, participants: [{ email: 'invalid' }] })
      .success,
    false
  );
  assert.equal(
    newTccSchema.parse({
      ...valid,
      participants: [{ email: ' aluno@example.com ' }]
    }).participants[0].email,
    'aluno@example.com'
  );
});
test('proposta permite três convidados e rejeita excesso e duplicação de e-mails', () => {
  assert.equal(
    newTccSchema.safeParse({
      ...valid,
      participants: ['a', 'b', 'c'].map((name) => ({
        email: `${name}@example.com`
      }))
    }).success,
    true
  );
  assert.equal(
    newTccSchema.safeParse({
      ...valid,
      participants: ['a', 'b', 'c', 'd'].map((name) => ({
        email: `${name}@example.com`
      }))
    }).success,
    false
  );
  assert.equal(
    newTccSchema.safeParse({
      ...valid,
      participants: [{ email: 'a@example.com' }, { email: ' A@example.com ' }]
    }).success,
    false
  );
});
test('convite apresenta projeto, solicitante, consentimento e dois botões acessíveis', () => {
  const html = renderToStaticMarkup(
    React.createElement(ProposalInvitationCard, {
      invitation: {
        id: 1,
        tccId: 2,
        title: '<Pesquisa>',
        summary: 'Resumo',
        requestedBy: 'Ana'
      },
      busy: false,
      onRespond() {}
    })
  );
  assert.match(html, /&lt;Pesquisa&gt;/);
  assert.match(html, /Ana/);
  assert.match(html, /Resumo/);
  assert.match(html, /Aceitar participação/);
  assert.match(html, /Recusar convite/);
  assert.match(html, /Recusar cancela a proposta/);
});
test('ambas as respostas ficam desabilitadas enquanto uma requisição está em andamento', () => {
  const html = renderToStaticMarkup(
    React.createElement(ProposalInvitationCard, {
      invitation: {
        id: 1,
        tccId: 2,
        title: 'Pesquisa',
        summary: 'Resumo',
        requestedBy: 'Ana'
      },
      busy: true,
      onRespond() {}
    })
  );
  assert.equal((html.match(/disabled=""/g) || []).length, 2);
});
