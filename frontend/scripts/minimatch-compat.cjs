// minimatch 11 fixes GHSA-g3hv-75wg-m249. Older ESLint plugins still expect
// its v3 callable/default export and makeRe(...).test(path). Adapt those APIs,
// retaining all v11 matching logic rather than recreating its old regex engine.
const fs = require('node:fs');
const path = require('node:path');

const project = path.resolve(__dirname, '..');
const lock = JSON.parse(fs.readFileSync(path.join(project, 'package-lock.json'), 'utf8'));
for (const location of Object.keys(lock.packages)) {
  if (!/(^|\/)node_modules\/minimatch$/.test(location)) continue;
  const root = path.join(project, location);
  const manifest = path.join(root, 'package.json');
  if (!fs.existsSync(manifest)) continue; // Production-only installations omit lint.
  const metadata = JSON.parse(fs.readFileSync(manifest, 'utf8'));
  if (metadata.version !== '11.0.1') {
    throw new Error(`Review minimatch compatibility before upgrading ${metadata.version}`);
  }
  const additions = [
    [path.join(root, 'dist/commonjs/index.js'), '\nexports.minimatch.makeRe = (pattern, options) => { const matcher = new exports.Minimatch(pattern, options); return { test: value => matcher.match(value) }; };\nmodule.exports = Object.assign(exports.minimatch, exports);\n'],
    [path.join(root, 'dist/esm/index.js'), '\nminimatch.makeRe = (pattern, options) => { const matcher = new Minimatch(pattern, options); return { test: value => matcher.match(value) }; };\nexport default minimatch;\n']
  ];
  for (const [file, addition] of additions) {
    const source = fs.readFileSync(file, 'utf8');
    // Replace only our previous export-only adaptation, if present.
    const previous = file.includes(`${path.sep}commonjs${path.sep}`)
      ? '\nmodule.exports = Object.assign(exports.minimatch, exports);\n'
      : '\nexport default minimatch;\n';
    if (!source.endsWith(addition)) {
      fs.writeFileSync(file, (source.endsWith(previous) ? source.slice(0, -previous.length) : source) + addition);
    }
  }
}
