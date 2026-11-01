const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { createRequire } = require('node:module');
const ts = require('typescript');

const root = path.resolve(__dirname, '..');
const cache = new Map();

// Compila somente os componentes locais para testar com as dependências já instaladas.
function loadSource(relativePath) {
  const filename = path.join(root, relativePath);
  if (cache.has(filename)) return cache.get(filename).exports;
  const source = fs.readFileSync(filename, 'utf8');
  const code = ts.transpileModule(source, {
    compilerOptions: {
      target: ts.ScriptTarget.ES2020,
      module: ts.ModuleKind.CommonJS,
      jsx: ts.JsxEmit.ReactJSX
    }
  }).outputText;
  const module = { exports: {} };
  cache.set(filename, module);
  const nativeRequire = createRequire(filename);
  function requireLocal(request) {
    if (!request.startsWith('@/')) return nativeRequire(request);
    const base = path.join('src', request.slice(2));
    const target = [
      base + '.tsx',
      base + '.ts',
      path.join(base, 'index.tsx')
    ].find((candidate) => fs.existsSync(path.join(root, candidate)));
    if (!target) throw new Error(`Módulo local não encontrado: ${request}`);
    return loadSource(target);
  }
  vm.runInThisContext(`(function(require, module, exports) { ${code}\n})`, {
    filename
  })(requireLocal, module, module.exports);
  return module.exports;
}

module.exports = { loadSource, root };
