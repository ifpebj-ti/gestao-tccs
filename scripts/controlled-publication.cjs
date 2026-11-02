// HTTP requests are read-only. Registry writes use guarded imagetools create calls.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const { execFileSync } = require('node:child_process');

const accept = [
  'application/vnd.oci.image.index.v1+json', 'application/vnd.oci.image.manifest.v1+json',
  'application/vnd.docker.distribution.manifest.list.v2+json',
  'application/vnd.docker.distribution.manifest.v2+json'
].join(', ');

function candidateTag(env) {
  assert.match(env.GITHUB_SHA || '', /^[a-f0-9]{40}$/, 'A full lowercase commit SHA is required');
  for (const key of ['GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT']) {
    assert.match(env[key] || '', /^[1-9][0-9]*$/, `${key} must be a positive integer`);
  }
  return `candidate-sha-${env.GITHUB_SHA}-run-${env.GITHUB_RUN_ID}-attempt-${env.GITHUB_RUN_ATTEMPT}`;
}

function validateCandidate(tag, env) {
  const base = candidateTag(env);
  assert.ok([base, `${base}-amd64`, `${base}-arm64`].includes(tag), 'Only this run\'s new candidate tags are allowed');
  return tag;
}

function publicationPlan(env) {
  assert.equal(env.REPOSITORY_VISIBILITY, 'public', 'Stop: review billing before running on a non-public repository');
  assert.ok(['true', 'false'].includes(env.PUBLISH_CANDIDATES), 'Explicit publication boolean required');
  if (env.PUBLISH_CANDIDATES === 'true') {
    assert.equal(env.CONFIRM_SHA, env.GITHUB_SHA, 'Publishing requires explicit confirmation of the exact full SHA');
  }
  const url = new URL(env.CANDIDATE_API_URL);
  assert.ok(['http:', 'https:'].includes(url.protocol) && !url.username && !url.password, 'API URL must be HTTP(S) without credentials');
  assert.ok(!/[\r\n]/.test(env.CANDIDATE_API_URL), 'API URL must be a single line');
  const owner = (env.GITHUB_REPOSITORY_OWNER || '').toLowerCase();
  assert.match(owner, /^[a-z0-9][a-z0-9-]*$/, 'Invalid image owner');
  return { owner, tag: candidateTag(env), publish: env.PUBLISH_CANDIDATES === 'true' };
}

class Registry {
  constructor(image, { fetcher = fetch, env = process.env } = {}) {
    assert.match(image, /^ghcr\.io\/[a-z0-9][a-z0-9-]*\/gestaotccs-(frontend|backend)$/);
    this.image = image;
    this.repository = image.slice('ghcr.io/'.length);
    this.fetcher = fetcher;
    this.env = env;
  }
  async request(url, options = {}) {
    const response = await this.fetcher(url, { ...options, signal: AbortSignal.timeout(30000) });
    return response;
  }
  async token() {
    if (this.bearer) return this.bearer;
    const headers = {};
    if (this.env.GHCR_TOKEN) {
      assert.ok(this.env.GHCR_USERNAME, 'Registry username missing');
      headers.Authorization = `Basic ${Buffer.from(`${this.env.GHCR_USERNAME}:${this.env.GHCR_TOKEN}`).toString('base64')}`;
    }
    const response = await this.request(`https://ghcr.io/token?service=ghcr.io&scope=${encodeURIComponent(`repository:${this.repository}:pull`)}`, { headers });
    assert.equal(response.status, 200, `Registry authentication failed: HTTP ${response.status}`);
    const body = await response.json();
    assert.ok(body.token, 'Registry did not return a token');
    this.bearer = body.token;
    return this.bearer;
  }
  async get(kind, reference, method = 'GET') {
    assert.ok(['manifests', 'blobs'].includes(kind));
    assert.match(reference, /^(?:[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}|sha256:[a-f0-9]{64})$/);
    return this.request(`https://ghcr.io/v2/${this.repository}/${kind}/${reference}`, {
      method, headers: { Authorization: `Bearer ${await this.token()}`, Accept: accept }
    });
  }
  async assertAbsent(tag) {
    const response = await this.get('manifests', tag, 'HEAD');
    assert.equal(response.status, 404,
      response.status === 200 ? `Refusing to overwrite existing tag ${this.image}:${tag}` : `Cannot establish tag absence: HTTP ${response.status}`);
  }
  async manifest(reference) {
    const response = await this.get('manifests', reference);
    assert.equal(response.status, 200, `Cannot read ${this.image}:${reference}: HTTP ${response.status}`);
    const bytes = Buffer.from(await response.arrayBuffer());
    const digest = `sha256:${crypto.createHash('sha256').update(bytes).digest('hex')}`;
    if (reference.startsWith('sha256:')) assert.equal(digest, reference, 'Manifest digest mismatch');
    const advertised = response.headers.get('docker-content-digest');
    if (advertised) assert.equal(digest, advertised, 'Registry content digest mismatch');
    return { digest, body: JSON.parse(bytes.toString('utf8')) };
  }
  async inspect(reference) {
    const root = await this.manifest(reference);
    const images = [];
    const walk = async (entry, depth = 0, declaredPlatform) => {
      assert.ok(depth < 8, 'Manifest nesting too deep');
      if (entry.body.manifests) {
        for (const child of entry.body.manifests) {
          assert.match(child.digest || '', /^sha256:[a-f0-9]{64}$/, 'Index child must reference an immutable digest');
          if (child.platform?.os === 'unknown') continue; // Optional attestations are not runnable images.
          await walk(await this.manifest(child.digest), depth + 1, child.platform || declaredPlatform);
        }
      } else {
        assert.match(entry.body.config?.digest || '', /^sha256:[a-f0-9]{64}$/);
        const response = await this.get('blobs', entry.body.config.digest);
        assert.equal(response.status, 200, 'Cannot read image configuration');
        const bytes = Buffer.from(await response.arrayBuffer());
        assert.equal(`sha256:${crypto.createHash('sha256').update(bytes).digest('hex')}`, entry.body.config.digest, 'Config digest mismatch');
        const config = JSON.parse(bytes.toString('utf8'));
        if (declaredPlatform) {
          assert.equal(config.os, declaredPlatform.os, 'Index OS differs from image configuration');
          assert.equal(config.architecture, declaredPlatform.architecture, 'Index architecture differs from image configuration');
        }
        images.push({ platform: `${config.os}/${config.architecture}`, digest: entry.digest,
          revision: config.config?.Labels?.['org.opencontainers.image.revision'] });
      }
    };
    await walk(root);
    return { image: this.image, reference, digest: root.digest, images };
  }
  async verify(reference, arches, sha) {
    const result = await this.inspect(reference);
    assert.deepEqual(result.images.map(item => item.platform).sort(), arches.map(arch => `linux/${arch}`).sort(), 'Image platforms do not match');
    for (const item of result.images) assert.equal(item.revision, sha, 'Image revision does not match the approved commit');
    return result;
  }
}

function releaseTag(tag) {
  assert.match(tag || '', /^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/, 'Expected a release version, never latest');
  return tag;
}

function releaseRegistries(env, factory = image => new Registry(image, { env })) {
  const owner = (env.GITHUB_REPOSITORY_OWNER || '').toLowerCase();
  assert.match(owner, /^[a-z0-9][a-z0-9-]*$/);
  assert.match(env.GITHUB_SHA || '', /^[a-f0-9]{40}$/);
  return ['backend', 'frontend'].map(service => {
    const image = `ghcr.io/${owner}/gestaotccs-${service}`;
    return { image, registry: factory(image) };
  });
}

async function releasePreflight(tag, env, { fetcher = fetch, registryFactory } = {}) {
  releaseTag(tag);
  assert.match(env.GITHUB_REPOSITORY || '', /^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/);
  const headers = { Accept: 'application/vnd.github+json' };
  if (env.GITHUB_TOKEN) headers.Authorization = `Bearer ${env.GITHUB_TOKEN}`;
  const response = await fetcher(`https://api.github.com/repos/${env.GITHUB_REPOSITORY}/git/ref/tags/${tag}`,
    { headers, signal: AbortSignal.timeout(30000) });
  assert.equal(response.status, 404, response.status === 200
    ? 'Refusing to reuse an existing Git release tag' : `Cannot establish Git tag absence: HTTP ${response.status}`);
  for (const { registry } of releaseRegistries(env, registryFactory)) await registry.assertAbsent(tag);
}

async function verifyReleaseSource(tag, env, fetcher = fetch) {
  releaseTag(tag);
  assert.match(env.GITHUB_SHA || '', /^[a-f0-9]{40}$/);
  assert.match(env.GITHUB_REPOSITORY || '', /^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/);
  const headers = { Accept: 'application/vnd.github+json' };
  if (env.GITHUB_TOKEN) headers.Authorization = `Bearer ${env.GITHUB_TOKEN}`;
  let endpoint = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}/git/ref/tags/${tag}`;
  for (let depth = 0; depth < 8; depth++) {
    const response = await fetcher(endpoint, { headers, signal: AbortSignal.timeout(30000) });
    assert.equal(response.status, 200, `Cannot verify Git release source: HTTP ${response.status}`);
    const { object } = await response.json();
    assert.match(object?.sha || '', /^[a-f0-9]{40}$/);
    if (object.type === 'commit') {
      assert.equal(object.sha, env.GITHUB_SHA, 'Git release tag does not match the compiled commit');
      return;
    }
    assert.equal(object.type, 'tag', 'Unsupported Git release object');
    endpoint = `https://api.github.com/repos/${env.GITHUB_REPOSITORY}/git/tags/${object.sha}`;
  }
  throw new Error('Git tag nesting too deep');
}

const createManifest = args => execFileSync('docker', ['buildx', 'imagetools', 'create', ...args],
  { stdio: 'inherit', timeout: 120000 });

async function assembleRelease(tag, env, options = {}) {
  await releasePreflight(tag, env, options);
  const staging = candidateTag(env);
  const prepared = [];
  for (const entry of releaseRegistries(env, options.registryFactory)) {
    const sources = [];
    for (const arch of ['amd64', 'arm64']) {
      const result = await entry.registry.verify(`${staging}-${arch}`, [arch], env.GITHUB_SHA);
      sources.push(`${entry.image}@${result.digest}`);
    }
    prepared.push({ ...entry, sources });
  }
  // Both services and all four staged images must pass before the first SemVer write.
  for (const { image, registry, sources } of prepared) {
    await registry.assertAbsent(tag);
    (options.createManifest || createManifest)(['--tag', `${image}:${tag}`, ...sources]);
    await registry.verify(tag, ['amd64', 'arm64'], env.GITHUB_SHA);
  }
}

async function promoteRelease(tag, env, options = {}) {
  releaseTag(tag);
  const prepared = [];
  for (const entry of releaseRegistries(env, options.registryFactory)) {
    const result = await entry.registry.verify(tag, ['amd64', 'arm64'], env.GITHUB_SHA);
    prepared.push({ ...entry, digest: result.digest });
  }
  // Never move latest for one service if verification of the other service fails.
  for (const { image, digest } of prepared) {
    (options.createManifest || createManifest)(['--tag', `${image}:latest`, `${image}@${digest}`]);
  }
  for (const { registry } of prepared) await registry.verify('latest', ['amd64', 'arm64'], env.GITHUB_SHA);
}

function badgePayload(readme, percentage) {
  assert.match(percentage || '', /^(?:100|[1-9]?[0-9])$/, 'Invalid badge percentage');
  assert.match(readme.sha || '', /^[a-f0-9]{40}$/, 'README blob SHA missing');
  assert.equal(readme.encoding, 'base64');
  const content = Buffer.from(readme.content, 'base64').toString('utf8');
  assert.match(content, /^.*<!-- COVERAGE_BADGE -->.*$/m, 'README badge marker missing');
  const updated = content.replace(/^.*<!-- COVERAGE_BADGE -->.*$/m,
    `![Coverage](https://img.shields.io/badge/Coverage-${percentage}%25-brightgreen) <!-- COVERAGE_BADGE -->`);
  return { message: `ci: atualizar badge de cobertura para ${percentage}%`,
    content: Buffer.from(updated).toString('base64'), sha: readme.sha };
}

async function main(args, env = process.env) {
  const [command, image, reference, arch] = args;
  if (command === 'candidate-tag') { console.log(candidateTag(env)); return; }
  if (command === 'release-preflight') { await releasePreflight(image, env); return; }
  if (command === 'verify-release-source') { await verifyReleaseSource(image, env); return; }
  if (command === 'assemble-release') { await assembleRelease(image, env); return; }
  if (command === 'promote-release') { await promoteRelease(image, env); return; }
  if (command === 'badge-content') {
    console.log(JSON.stringify(badgePayload(JSON.parse(fs.readFileSync(image, 'utf8')), reference)));
    return;
  }
  if (command === 'plan') {
    const plan = publicationPlan(env);
    if (env.GITHUB_OUTPUT) fs.appendFileSync(env.GITHUB_OUTPUT, `owner=${plan.owner}\ntag=${plan.tag}\n`);
    console.log(JSON.stringify(plan));
    return;
  }
  if (command === 'inspect') { // Read-only; permits inspecting 2.12.0 and other existing references.
    console.log(JSON.stringify(await new Registry(image, { env }).inspect(reference), null, 2));
    return;
  }
  if (command === 'verify-published') { // Read-only release verification; never creates or updates tags.
    assert.match(env.GITHUB_SHA || '', /^[a-f0-9]{40}$/);
    console.log(JSON.stringify(await new Registry(image, { env }).verify(reference, ['amd64', 'arm64'], env.GITHUB_SHA), null, 2));
    return;
  }
  if (command === 'absent' || command === 'verify') {
    validateCandidate(reference, env);
    const registry = new Registry(image, { env });
    if (command === 'absent') await registry.assertAbsent(reference);
    else {
      assert.ok(['amd64', 'arm64', 'both'].includes(arch), 'Expected amd64, arm64 or both');
      console.log(JSON.stringify(await registry.verify(reference, arch === 'both' ? ['amd64', 'arm64'] : [arch], env.GITHUB_SHA), null, 2));
    }
    return;
  }
  assert.equal(command, 'assemble', 'Usage: plan | inspect IMAGE REF | absent IMAGE TAG | verify IMAGE TAG ARCH | assemble');
  const plan = publicationPlan(env);
  assert.equal(plan.publish, true, 'Manifest publication is disabled');
  assert.equal(env.CANDIDATE_TAG, plan.tag);
  assert.equal(env.IMAGE_OWNER, plan.owner);
  // Preflight BOTH destination tags and all sources before the first write.
  const prepared = [];
  for (const service of ['frontend', 'backend']) {
    const name = `ghcr.io/${plan.owner}/gestaotccs-${service}`;
    const registry = new Registry(name, { env });
    await registry.assertAbsent(plan.tag);
    const sources = [];
    for (const architecture of ['amd64', 'arm64']) {
      const checked = await registry.verify(`${plan.tag}-${architecture}`, [architecture], env.GITHUB_SHA);
      sources.push(`${name}@${checked.digest}`);
    }
    prepared.push({ name, registry, sources });
  }
  for (const { name, registry, sources } of prepared) {
    await registry.assertAbsent(plan.tag); // Recheck immediately before the mutation.
    execFileSync('docker', ['buildx', 'imagetools', 'create', '--tag', `${name}:${plan.tag}`, ...sources], { stdio: 'inherit', timeout: 120000 });
    const result = await registry.verify(plan.tag, ['amd64', 'arm64'], env.GITHUB_SHA);
    console.log(JSON.stringify(result, null, 2));
    if (env.GITHUB_STEP_SUMMARY) fs.appendFileSync(env.GITHUB_STEP_SUMMARY,
      `\nVerified candidate: \`${name}:${plan.tag}\`\n\nDigest: \`${result.digest}\`\n\nPlatforms: linux/amd64, linux/arm64. No deployment.\n`);
  }
}

module.exports = { candidateTag, validateCandidate, publicationPlan, Registry, releaseTag,
  releasePreflight, verifyReleaseSource, assembleRelease, promoteRelease, badgePayload, main };
if (require.main === module) main(process.argv.slice(2)).catch(error => { console.error(error.message); process.exitCode = 1; });
