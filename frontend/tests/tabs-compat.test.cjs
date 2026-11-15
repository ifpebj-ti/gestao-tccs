const { test } = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const Tabs = require('@radix-ui/react-tabs');

function render(root = {}, disabled = false) {
  const h = React.createElement;
  return renderToStaticMarkup(h(Tabs.Root, { defaultValue: 'documents', ...root },
    h(Tabs.List, { 'aria-label': 'Trabalho' },
      h(Tabs.Trigger, { value: 'documents' }, 'Documentos'),
      h(Tabs.Trigger, { value: 'signatures', disabled }, 'Assinaturas')),
    h(Tabs.Content, { value: 'documents' }, 'Lista de documentos'),
    h(Tabs.Content, { value: 'signatures' }, 'Lista de assinaturas')));
}

test('Tabs preserves the accessible list name and tab roles', () => {
  const html = render();
  assert.match(html, /role="tablist"/);
  assert.match(html, /aria-label="Trabalho"/);
  assert.equal((html.match(/role="tab"/g) || []).length, 2);
});
test('Tabs keeps the default panel visible and the other panel hidden', () => {
  const html = render();
  assert.match(html, /Lista de documentos/);
  assert.doesNotMatch(html, /Lista de assinaturas/);
  assert.equal((html.match(/aria-selected="true"/g) || []).length, 1);
  assert.match(html, /hidden=""[^>]*role="tabpanel"|role="tabpanel"[^>]*hidden=""/);
});
test('Tabs respects the controlled value used by the parent component', () => {
  const html = render({ value: 'signatures' });
  assert.match(html, /Lista de assinaturas/);
  assert.doesNotMatch(html, /Lista de documentos/);
  assert.equal((html.match(/aria-selected="true"/g) || []).length, 1);
});
test('Tabs preserves the native disabled attribute on unavailable tabs', () => {
  assert.match(render({}, true), /<button[^>]*disabled=""[^>]*>Assinaturas<\/button>/);
});
test('Tabs keeps vertical orientation exposed to assistive technology', () => {
  assert.match(render({ orientation: 'vertical' }), /role="tablist"[^>]*aria-orientation="vertical"/);
});
test('Tabs connects each tab to its corresponding panel with matching IDs', () => {
  const html = render();
  const controls = [...html.matchAll(/aria-controls="([^"]+)"/g)].map(m => m[1]);
  const labelledBy = [...html.matchAll(/aria-labelledby="([^"]+)"/g)].map(m => m[1]);
  assert.equal(controls.length, 2);
  assert.equal(new Set(controls).size, 2);
  assert.equal(labelledBy.length, 2);
  for (const id of [...controls, ...labelledBy]) assert.ok(html.includes(`id="${id}"`));
});
