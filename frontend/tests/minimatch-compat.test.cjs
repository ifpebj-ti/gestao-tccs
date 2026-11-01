const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');

const project = path.resolve(__dirname, '..');
const lock = require('../package-lock.json');
const locations = Object.keys(lock.packages).filter(location =>
  /(^|\/)node_modules\/minimatch$/.test(location));

test('legacy import and React lint rules still detect violations', async () => {
  const { ESLint } = require('eslint');
  const lint = new ESLint({
    overrideConfigFile: true,
    overrideConfig: [{
      languageOptions: { parserOptions: { ecmaFeatures: { jsx: true } } },
      plugins: { import: require('eslint-plugin-import'), react: require('eslint-plugin-react') },
      rules: {
        'import/no-internal-modules': ['error', { forbid: ['react/**'] }],
        'react/jsx-pascal-case': ['error', { ignore: ['Allowed*'] }]
      }
    }]
  });
  const [result] = await lint.lintText("import X from 'react/internal'; export const view = <Bad_name />;", { filePath: 'compat.js' });
  assert.ok(result.messages.some(message => message.ruleId === 'import/no-internal-modules'));
  assert.ok(result.messages.some(message => message.ruleId === 'react/jsx-pascal-case'));
});

test('compatibility installation is idempotent', () => {
  const files = locations.flatMap(location => ['commonjs', 'esm'].map(format =>
    path.join(project, location, 'dist', format, 'index.js')));
  const before = files.map(file => fs.readFileSync(file, 'utf8'));
  execFileSync(process.execPath, [path.join(project, 'scripts/minimatch-compat.cjs')]);
  assert.deepEqual(files.map(file => fs.readFileSync(file, 'utf8')), before);
});

for (const location of locations) {
  test(`patched matching supports legacy and modern imports: ${location}`, async () => {
    const root = path.join(project, location);
    assert.equal(require(path.join(root, 'package.json')).version, '11.0.1');
    const match = require(path.join(root, 'dist/commonjs/index.js'));
    assert.equal(typeof match, 'function');
    assert.equal(match.minimatch, match);
    assert.equal(match('src/page.tsx', '**/*.tsx'), true);
    assert.equal(match('src/page.ts', '**/*.tsx'), false);
    assert.equal(match('src/.hidden.ts', '**/*.ts', { dot: true }), true);
    assert.equal(new match.Minimatch('**/*.{ts,tsx}').match('src/page.ts'), true);
    assert.equal(match.makeRe('*.tsx').test('page.tsx'), true);
    assert.deepEqual(match.match(['a.ts', 'b.js'], '*.ts'), ['a.ts']);
    assert.equal(match.defaults({ matchBase: true })('src/a.ts', '*.ts'), true);
    const esm = await import(pathToFileURL(path.join(root, 'dist/esm/index.js')).href);
    assert.equal(esm.default('src/page.tsx', '**/*.tsx'), true);
  });
}
