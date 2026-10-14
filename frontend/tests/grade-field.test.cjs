const test = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource } = require('./load-source.cjs');
const { GradeField } = loadSource('src/components/evaluation/GradeField.tsx');

test('cada nota tem um rótulo associado ao campo e mantém a entrada decimal obrigatória', () => {
  const html = renderToStaticMarkup(
    React.createElement(GradeField, {
      label: 'Articulação',
      max: 1.5,
      value: '1.2',
      onChange: () => {}
    })
  );
  const id = /<input[^>]+id="([^"]+)"/.exec(html)?.[1];
  assert.ok(id);
  assert.ok(html.includes(`for="${id}"`));
  assert.match(html, /type="number"/);
  assert.match(html, /step="0.1"/);
  assert.match(html, /min="0"/);
  assert.match(html, /max="1.5"/);
  assert.match(html, /required=""/);
  assert.match(html, /value="1.2"/);
  assert.match(html, /0 a 1,5/);
});
