// Disposable integration check. Never reads .env or touches application containers.
const { execFileSync, spawnSync } = require('node:child_process');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');

async function probe() {
  const request = async (url, options) => {
    const response = await fetch(url, { ...options, signal: AbortSignal.timeout(30000) });
    assert.equal(response.status, 200, `${url}: HTTP ${response.status}: ${await response.clone().text()}`);
    return response;
  };
  const html = await (await request('http://frontend:3000')).text();
  assert.ok(html.includes('http://backend:8080/api'), 'Frontend runtime API URL missing');
  await request('http://backend:8080/metrics');
  const login = await request('http://backend:8080/api/Auth/login', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'aluna@discente.ifpe.edu.br', password: 'Senha@123' })
  });
  const { accessToken } = await login.json();
  assert.ok(accessToken, 'Login did not return JWT');
  const profile = await (await request('http://backend:8080/api/User/me', {
    headers: { Authorization: `Bearer ${accessToken}` }
  })).json();
  assert.equal(profile.email, 'aluna@discente.ifpe.edu.br');
  const anonymous = await fetch('http://backend:8080/api/User/me');
  assert.equal(anonymous.status, 401);
  const cors = await fetch('http://backend:8080/api/User/me', {
    method: 'OPTIONS', headers: { Origin: 'http://frontend:3000',
      'Access-Control-Request-Method': 'GET', 'Access-Control-Request-Headers': 'authorization' }
  });
  assert.equal(cors.status, 204);
  assert.equal(cors.headers.get('access-control-allow-origin'), 'http://frontend:3000');
  console.log('PASS: frontend runtime config, API, login, authenticated profile, anonymous 401 and CORS');

  // Exercise Garage's S3 interface with SigV4, using only disposable credentials.
  const sha = value => crypto.createHash('sha256').update(value).digest('hex');
  const hmac = (key, value) => crypto.createHmac('sha256', key).update(value).digest();
  async function s3(method, body = '') {
    const date = new Date().toISOString().replace(/[:-]|\.\d{3}/g, '');
    const day = date.slice(0, 8);
    const scope = `${day}/garage/s3/aws4_request`;
    const payload = sha(body);
    const signedHeaders = 'host;x-amz-content-sha256;x-amz-date';
    const canonical = `${method}\n/validation/smoke.txt\n\nhost:garage:3900\nx-amz-content-sha256:${payload}\nx-amz-date:${date}\n\n${signedHeaders}\n${payload}`;
    const key = hmac(hmac(hmac(hmac(`AWS4${process.env.S3_SECRET}`, day), 'garage'), 's3'), 'aws4_request');
    const signature = hmac(key, `AWS4-HMAC-SHA256\n${date}\n${scope}\n${sha(canonical)}`).toString('hex');
    return request('http://garage:3900/validation/smoke.txt', {
      method, ...(method === 'PUT' ? { body } : {}), headers: {
        'x-amz-date': date, 'x-amz-content-sha256': payload,
        Authorization: `AWS4-HMAC-SHA256 Credential=${process.env.S3_KEY}/${scope}, SignedHeaders=${signedHeaders}, Signature=${signature}`
      }
    });
  }
  await s3('PUT', 'Gradus ARM64 integration');
  assert.equal(await (await s3('GET')).text(), 'Gradus ARM64 integration');
  console.log('PASS: Garage S3 signed upload and download');

  // Send a message over SMTP and verify capture through Mailpit's HTTP API.
  const net = require('node:net');
  await new Promise((resolve, reject) => {
    const socket = net.createConnection({ host: 'mailpit', port: 1025 });
    socket.setTimeout(15000, () => socket.destroy(new Error('SMTP timeout')));
    const commands = [
      ['220', 'EHLO validation\r\n'], ['250', 'MAIL FROM:<validation@example.com>\r\n'],
      ['250', 'RCPT TO:<student@example.com>\r\n'], ['250', 'DATA\r\n'],
      ['354', 'Subject: ARM64 validation\r\nFrom: validation@example.com\r\nTo: student@example.com\r\n\r\nDisposable smoke message\r\n.\r\n'],
      ['250', 'QUIT\r\n'], ['221', null]
    ];
    let buffer = '';
    socket.on('error', reject);
    socket.on('data', data => {
      buffer += data;
      let end;
      while ((end = buffer.indexOf('\r\n')) >= 0) {
        const line = buffer.slice(0, end); buffer = buffer.slice(end + 2);
        if (line[3] === '-') continue;
        const [expected, command] = commands.shift() || [];
        if (!line.startsWith(expected)) { socket.destroy(); reject(new Error(`SMTP unexpected response: ${line}`)); return; }
        if (command) socket.write(command); else { socket.end(); resolve(); }
      }
    });
  });
  const messages = await (await request('http://mailpit:8025/api/v1/messages')).json();
  assert.equal(messages.total, 1);
  console.log('PASS: Mailpit SMTP delivery and HTTP capture');
}

async function main() {
  if (process.argv[2] === '--probe') return probe();
  const platform = process.argv[2];
  assert.ok(['linux/amd64', 'linux/arm64'].includes(platform),
    'Usage: node scripts/multiarch-stack-smoke.cjs linux/arm64 [image-tag-suffix]');
  const arch = platform.split('/')[1];
  const tag = process.argv[3] || `162cf2e-working-${arch}`;
  const dbEngine = process.env.STACK_GARAGE_DB_ENGINE || 'lmdb';
  assert.ok(['lmdb', 'sqlite'].includes(dbEngine), 'Unsupported Garage metadata engine');
  console.log(`Testing ${platform}; Garage metadata engine: ${dbEngine}`);
  const name = `gradus-stack-${arch}-${crypto.randomBytes(5).toString('hex')}`;
  const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'gradus-stack-'));
  const config = path.join(temp, 'garage.toml');
  const containers = [];
  const docker = (...args) => execFileSync('docker', args, { encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'], timeout: 180000, maxBuffer: 8 * 1024 * 1024 });
  const pause = () => new Promise(resolve => setTimeout(resolve, 2000));
  async function wait(label, action) {
    let error;
    for (let i = 0; i < 90; i++) {
      try { return action(); } catch (caught) {
        if (caught.message === 'Garage exited') throw caught;
        error = caught; await pause();
      }
    }
    throw new Error(`${label} not ready: ${error.message}`);
  }
  function run(service, image, args = []) {
    const container = `${name}-${service}`; containers.push(container);
    docker('run', '-d', '--platform', platform, '--network', name, '--network-alias', service,
      '--name', container, ...args, image);
    return container;
  }
  let networkCreated = false;
  try {
    for (const service of ['frontend', 'backend']) {
      assert.equal(docker('image', 'inspect', '--format', '{{.Architecture}}', `gradus-validation/${service}:${tag}`).trim(), arch);
    }
    docker('network', 'create', name); networkCreated = true;
    // Exercise the repository config with fresh credentials and container-local data.
    const garageConfig = fs.readFileSync(path.join(__dirname, '..', 'garage.toml'), 'utf8')
      .replace(/^rpc_secret\s*=.*$/m, `rpc_secret = "${crypto.randomBytes(32).toString('hex')}"`)
      .replace(/^db_engine\s*=.*$/m, `db_engine = "${dbEngine}"`);
    fs.writeFileSync(config, garageConfig);
    const db = run('postgres', 'postgres:15', ['-e', 'POSTGRES_PASSWORD=validation-only', '-e', 'POSTGRES_DB=gradus_stack']);
    const garage = run('garage', 'dxflrs/garage:v1.0.1', ['--mount', `type=bind,source=${config},target=/etc/garage.toml,readonly`]);
    run('mailpit', 'axllent/mailpit:latest');
    await wait('PostgreSQL', () => docker('exec', db, 'pg_isready', '-h', '127.0.0.1', '-U', 'postgres', '-d', 'gradus_stack'));
    const status = await wait('Garage', () => {
      if (docker('inspect', '--format', '{{.State.Running}}', garage).trim() !== 'true') throw new Error('Garage exited');
      return docker('exec', garage, '/garage', 'status');
    });
    const nodeId = status.match(/\b[a-f0-9]{16,64}\b/)?.[0]; assert.ok(nodeId, 'Garage node ID missing');
    docker('exec', garage, '/garage', 'layout', 'assign', '-z', 'validation', '-c', '1G', nodeId);
    docker('exec', garage, '/garage', 'layout', 'apply', '--version', '1');
    docker('exec', garage, '/garage', 'bucket', 'create', 'validation');
    const keyInfo = docker('exec', garage, '/garage', 'key', 'create', 'validation');
    const key = keyInfo.match(/Key ID:\s*(\S+)/)?.[1];
    const secret = keyInfo.match(/Secret key:\s*(\S+)/i)?.[1]; assert.ok(key && secret, 'Garage credentials missing');
    docker('exec', garage, '/garage', 'bucket', 'allow', '--read', '--write', '--owner', 'validation', '--key', key);
    const backend = run('backend', `gradus-validation/backend:${tag}`, [
      '-e', 'ASPNETCORE_ENVIRONMENT=Production',
      '-e', 'ConnectionStrings__DefaultConnection=Host=postgres;Database=gradus_stack;Username=postgres;Password=validation-only',
      '-e', 'JWT_SETTINGS__TOKEN_KEY=validation-only-key-at-least-thirty-two-characters',
      '-e', 'JWT_SETTINGS__ACCESSTOKEN_EXPIRATION_TIME=30',
      '-e', 'STORAGE_SETTINGS__ENDPOINT=http://garage:3900', '-e', `STORAGE_SETTINGS__ACCESS_KEY=${key}`,
      '-e', `STORAGE_SETTINGS__SECRET_KEY=${secret}`, '-e', 'STORAGE_SETTINGS__BUCKET_NAME=validation',
      '-e', 'MailSettings__Host=mailpit', '-e', 'MailSettings__Port=1025',
      '-e', 'MailSettings__Name=Validation', '-e', 'MailSettings__EmailId=validation@example.com',
      '-e', 'CORS_SETTINGS__URL_FRONT=http://frontend:3000',
      '-e', 'URL_LOKI=http://127.0.0.1:3100', '-e', 'URL_TEMPO=http://127.0.0.1:4318'
    ]);
    const frontend = run('frontend', `gradus-validation/frontend:${tag}`, [
      '-e', 'NEXT_PUBLIC_API_URL=http://backend:8080/api', '-e', `S3_KEY=${key}`, '-e', `S3_SECRET=${secret}`,
      '--mount', `type=bind,source=${__filename},target=/smoke.cjs,readonly`
    ]);
    console.log('Five disposable containers started; waiting for API migrations and seed');
    await wait('API', () => docker('exec', backend, 'wget', '-q', '-O', '/dev/null', 'http://127.0.0.1:8080/metrics'));
    process.stdout.write(docker('exec', frontend, 'node', '/smoke.cjs', '--probe'));
    console.log(`PASS: integrated five-service stack ${platform}`);
  } catch (error) {
    for (const container of containers) {
      const logs = spawnSync('docker', ['logs', '--tail', '30', container], { encoding: 'utf8', timeout: 10000 });
      console.error(`${container}\n${logs.stdout || ''}${logs.stderr || ''}`);
    }
    throw error;
  } finally {
    for (const container of containers.reverse()) {
      try { docker('rm', '-fv', container); } catch (error) { console.error(`Cleanup failed: ${container}: ${error.message}`); process.exitCode = 1; }
    }
    if (networkCreated) { try { docker('network', 'rm', name); } catch (error) { console.error(error.message); process.exitCode = 1; } }
    fs.rmSync(config, { force: true }); fs.rmdirSync(temp);
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
