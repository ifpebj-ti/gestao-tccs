const test = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource } = require('./load-source.cjs');
const { Input } = loadSource('src/components/ui/input.tsx');

test('erro do campo é associado ao input e anunciado como alerta', () => {
  const html = renderToStaticMarkup(
    React.createElement(Input, {
      id: 'teste',
      errorText: 'Valor inválido',
      helperText: 'Informe o valor',
      'aria-describedby': 'descricao-existente'
    })
  );
  assert.match(html, /aria-invalid="true"/);
  const descriptions = /aria-describedby="([^"]+)"/.exec(html)[1].split(' ');
  assert.ok(descriptions.includes('descricao-existente'));
  for (const id of descriptions.filter((id) => id !== 'descricao-existente'))
    assert.ok(html.includes(`id="${id}"`));
  assert.match(html, /role="alert"/);
});
test('campo sem erro preserva propriedades nativas de validação', () => {
  const html = renderToStaticMarkup(
    React.createElement(Input, {
      type: 'number',
      min: 0,
      max: 10,
      required: true,
      disabled: true
    })
  );
  for (const attribute of ['min="0"', 'max="10"', 'required=""', 'disabled=""'])
    assert.ok(html.includes(attribute));
  assert.equal(html.includes('role="alert"'), false);
});
