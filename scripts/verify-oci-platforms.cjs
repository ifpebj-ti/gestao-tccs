const { execFileSync } = require('node:child_process');
const assert = require('node:assert/strict');
const archive = process.argv[2];
assert.ok(archive, 'Usage: node scripts/verify-oci-platforms.cjs image.oci.tar');
const read = file => JSON.parse(execFileSync('tar', ['-xOf', archive, file], { encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 }));
const platforms = new Set();
function visit(index, depth = 0) {
  assert.ok(depth < 8, 'OCI index nesting too deep');
  for (const descriptor of index.manifests || []) {
    if (descriptor.platform?.os === 'linux') platforms.add(`linux/${descriptor.platform.architecture}`);
    if (descriptor.mediaType?.includes('image.index') || descriptor.mediaType?.includes('manifest.list')) {
      assert.match(descriptor.digest, /^sha256:[a-f0-9]{64}$/);
      visit(read(`blobs/sha256/${descriptor.digest.slice(7)}`), depth + 1);
    }
  }
}
visit(read('index.json'));
assert.ok(platforms.has('linux/amd64'), 'OCI archive missing AMD64');
assert.ok(platforms.has('linux/arm64'), 'OCI archive missing ARM64');
console.log(JSON.stringify({ archive, platforms: [...platforms], valid: true }));
