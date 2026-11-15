// Verify the built application over HTTP without publishing a port outside loopback.
const assert = require('node:assert/strict');
const path = require('node:path');
const http = require('node:http');

async function main() {
  process.env.NODE_ENV = 'production';
  process.env.NEXT_TELEMETRY_DISABLED = '1';
  const directory = path.resolve(process.argv[2] || 'frontend');
  const next = require(path.join(directory, 'node_modules/next'));
  const app = next({ dev: false, dir: directory, hostname: '127.0.0.1', port: 0 });
  let server;
  try {
    await app.prepare();
    server = http.createServer(app.getRequestHandler());
    await new Promise((resolve, reject) => {
      server.once('error', reject);
      server.listen(0, '127.0.0.1', resolve);
    });
    const response = await fetch(`http://127.0.0.1:${server.address().port}/`, {
      signal: AbortSignal.timeout(15000)
    });
    assert.equal(response.status, 200);
    assert.equal(response.headers.get('x-frame-options'), 'DENY');
    assert.equal(response.headers.get('x-content-type-options'), 'nosniff');
    assert.equal(response.headers.get('referrer-policy'), 'no-referrer');
    assert.equal(response.headers.get('permissions-policy'), 'camera=(), microphone=(), geolocation=()');
    assert.equal(response.headers.get('x-powered-by'), null);
    const csp = response.headers.get('content-security-policy');
    assert.match(csp || '', /frame-ancestors 'none'/);
    assert.match(csp || '', /object-src 'none'/);
    assert.match(csp || '', /form-action 'self'/);
    await response.arrayBuffer();
    console.log('PASS: HTTP 200, defensive headers and initial CSP; X-Powered-By absent.');
  } finally {
    if (server) {
      server.closeAllConnections();
      await new Promise(resolve => server.close(resolve));
    }
    await app.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
