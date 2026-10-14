const test = require('node:test');
const assert = require('node:assert/strict');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource } = require('./load-source.cjs');
const { SemesterFilter } = loadSource('src/components/SemesterFilter.tsx');
const { PatternSelect } = loadSource('src/components/ui/form-select.tsx');
const semesters = [
  { id: 1, name: '2026.2', isActive: true },
  { id: 2, name: '2026.1', isActive: false }
];
test('filtro de semestre mantém seleção da develop no componente visual compartilhado', () => {
  const html = renderToStaticMarkup(
    React.createElement(SemesterFilter, { semesters, value: 1, onChange() {} })
  );
  assert.match(html, /2026.2.*Ativo/);
  assert.match(html, /for="semester-filter"/);
  assert.match(html, /id="semester-filter"/);
  assert.doesNotMatch(html, /<select/);
});
test('selecionar semestre preserva ID numérico e Todos limpa o filtro', () => {
  let selected = 1;
  const element = SemesterFilter({
    semesters,
    value: null,
    onChange: (value) => {
      selected = value;
    }
  });
  const control = React.Children.toArray(element.props.children).find(
    (child) => child.type === PatternSelect
  );
  control.props.onValueChange('2');
  assert.equal(selected, 2);
  control.props.onValueChange('');
  assert.equal(selected, null);
});
