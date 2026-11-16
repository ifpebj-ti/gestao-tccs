const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');

const {
  isGoogleLoginTokenUsable,
  persistGoogleLoginSession
} = loadSource('src/lib/googleLoginSession.ts');

function createToken(claims) {
  const encode = (value) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none' })}.${encode(claims)}.test-signature`;
}

function createCookieStore() {
  let token;
  const writes = [];
  return {
    writes,
    get: (name) => (name === 'token' ? token : undefined),
    set: (name, value, attributes) => {
      writes.push({ name, value, attributes });
      if (name === 'token') token = value;
    }
  };
}

test('Google callback rejects missing, malformed, expired, or incomplete JWTs', () => {
  assert.equal(persistGoogleLoginSession(), false);

  const now = Math.floor(Date.now() / 1000);
  const cookieStore = createCookieStore();
  for (const token of [
    'not-a-jwt',
    createToken({ userId: '10', role: 'STUDENT', exp: now - 1 }),
    createToken({ userId: '10', role: 'STUDENT' }),
    createToken({ userId: '10', role: 'UNKNOWN', exp: now + 3600 }),
    createToken({ role: 'STUDENT', exp: now + 3600 })
  ]) {
    assert.equal(isGoogleLoginTokenUsable(token), false);
    assert.equal(persistGoogleLoginSession(token, cookieStore, false), false);
  }
  assert.equal(cookieStore.writes.length, 0);
});

test('Google callback persists a valid, unexpired local session cookie', () => {
  const token = createToken({
    userId: '10',
    unique_name: 'Aluno',
    role: 'STUDENT',
    exp: Math.floor(Date.now() / 1000) + 3600
  });
  const cookieStore = createCookieStore();

  assert.equal(isGoogleLoginTokenUsable(token), true);
  assert.equal(persistGoogleLoginSession(token, cookieStore, true), true);
  assert.equal(cookieStore.get('token'), token);
  assert.deepEqual(cookieStore.writes, [{
    name: 'token',
    value: token,
    attributes: {
      expires: 1,
      path: '/',
      sameSite: 'lax',
      secure: true
    }
  }]);
});
