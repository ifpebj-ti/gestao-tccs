const test = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource } = require('./load-source.cjs');
const Step = loadSource('src/components/Steps/index.tsx').default;
const { FeatureActionCard } = loadSource(
  'src/components/layout/FeatureActionCard.tsx'
);
const { TccHeader } = loadSource('src/components/tcc-details/TccHeader.tsx');

test('cabeçalho identifica o projeto pelo título sem repetir o proprietário', () => {
  const markup = renderToStaticMarkup(
    React.createElement(TccHeader, {
      title: 'Projeto em grupo',
      cancellationRequested: false
    })
  );
  assert.ok(markup.includes('Projeto em grupo'));
  assert.equal((markup.match(/<h1/g) || []).length, 1);
  assert.ok(!markup.includes('<p'));
});

test('etapas anunciam exatamente uma atual, concluídas e próximas', () => {
  const markup = renderToStaticMarkup(
    React.createElement(Step, { currentStep: 3 })
  );
  assert.equal((markup.match(/aria-current="step"/g) || []).length, 1);
  assert.equal((markup.match(/: concluída/g) || []).length, 2);
  assert.equal((markup.match(/: próxima/g) || []).length, 3);
  assert.ok(markup.includes('Etapa 3 de 6'));
  assert.ok(markup.includes('Desenvolvimento e acompanhamento'));
});

test('card de destaque mantém botão nativo, nome acessível, contagem e estado desabilitado', () => {
  const markup = renderToStaticMarkup(
    React.createElement(FeatureActionCard, {
      heading: 'Assinaturas pendentes',
      description: '2 aguardando sua assinatura',
      actionLabel: 'Ver pendências',
      icon: () => React.createElement('svg'),
      count: 2,
      disabled: true,
      'aria-label': 'Consultar assinaturas'
    })
  );
  assert.ok(markup.startsWith('<button'));
  assert.ok(markup.includes('type="button"'));
  assert.ok(markup.includes('aria-label="Consultar assinaturas"'));
  assert.ok(markup.includes('aria-label="2 pendências"'));
  assert.ok(markup.includes('disabled=""'));
});
