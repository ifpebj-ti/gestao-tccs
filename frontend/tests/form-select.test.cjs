const test = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource } = require('./load-source.cjs');
const { PatternSelect, selectOptions } = loadSource(
  'src/components/ui/form-select.tsx'
);
const option = (value, text, disabled) =>
  React.createElement('option', { value, disabled }, text);
test('seletor preserva valores numéricos, labels e opções desabilitadas em listas e fragments', () => {
  const options = selectOptions([
    option(0, 'Selecione'),
    React.createElement(
      React.Fragment,
      null,
      option(1, 'Matutino'),
      option(2, 'Vespertino', true)
    )
  ]);
  assert.deepEqual(
    options.map((item) => [item.value, item.label, item.disabled]),
    [
      ['0', 'Selecione', undefined],
      ['1', 'Matutino', undefined],
      ['2', 'Vespertino', true]
    ]
  );
});
test('seletor apresenta o valor atual sem submeter o formulário ao abrir e mantém o estado desabilitado', () => {
  const html = renderToStaticMarkup(
    React.createElement(
      PatternSelect,
      { value: 2, disabled: true, id: 'shift', onValueChange() {} },
      option(1, 'Matutino'),
      option(2, 'Vespertino')
    )
  );
  assert.match(html, /Vespertino/);
  assert.match(html, /type="button"/);
  assert.match(html, /disabled=""/);
  assert.match(html, /aria-haspopup="menu"/);
});
test('PageIntro permite retirar a margem padrão para um cabeçalho dentro de formulário', () => {
  const { PageIntro } = loadSource('src/components/layout/PageIntro.tsx');
  const html = renderToStaticMarkup(
    React.createElement(PageIntro, {
      title: 'Proposta',
      description: 'Descrição',
      className: 'mb-0'
    })
  );
  assert.match(html, /mb-0/);
  assert.doesNotMatch(html, /mb-10/);
});
