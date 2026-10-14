const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const origin = 'http://localhost:3000';
const output = path.resolve(
  __dirname,
  '../docs/refatoracao/evidencias/filtro-semestre-posicao'
);
async function run() {
  fs.mkdirSync(output, { recursive: true });
  const target = await (
    await fetch('http://127.0.0.1:9230/json/new?about:blank', { method: 'PUT' })
  ).json();
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve) =>
    socket.addEventListener('open', resolve, { once: true })
  );
  let sequence = 0;
  const pending = new Map();
  const calls = [];
  const checks = [];
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = ++sequence;
      pending.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });
  socket.addEventListener('message', async ({ data }) => {
    const message = JSON.parse(data);
    if (message.id) {
      const callback = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) callback?.reject(new Error(message.error.message));
      else callback?.resolve(message.result);
    } else if (message.method === 'Fetch.requestPaused') {
      const { requestId, request } = message.params;
      const url = new URL(request.url);
      if (url.pathname.startsWith('/api/')) {
        let data = [];
        if (url.pathname === '/api/Semester/all')
          data = [
            { id: 1, name: '2026.2', isActive: true },
            { id: 2, name: '2026.1', isActive: false }
          ];
        else if (url.pathname === '/api/Tcc/filter') {
          calls.push({
            path: url.pathname,
            query: Object.fromEntries(url.searchParams),
            method: request.method
          });
          const rows = [
            { tccId: 41, studanteNames: ['Trabalho atual'], semesterId: 1 },
            { tccId: 42, studanteNames: ['Trabalho anterior'], semesterId: 2 }
          ];
          data = rows.filter(
            (row) =>
              !url.searchParams.has('SemesterId') ||
              row.semesterId === Number(url.searchParams.get('SemesterId'))
          );
        }
        await send('Fetch.fulfillRequest', {
          requestId,
          responseCode: 200,
          responseHeaders: [
            { name: 'Content-Type', value: 'application/json' },
            { name: 'Access-Control-Allow-Origin', value: origin },
            {
              name: 'Access-Control-Allow-Headers',
              value: 'Authorization, Content-Type'
            },
            { name: 'Access-Control-Allow-Methods', value: 'GET,OPTIONS' }
          ],
          body: Buffer.from(JSON.stringify(data)).toString('base64')
        });
      } else if (
        ['localhost', '127.0.0.1'].includes(url.hostname) ||
        url.protocol === 'data:'
      )
        await send('Fetch.continueRequest', { requestId });
      else
        await send('Fetch.failRequest', {
          requestId,
          errorReason: 'BlockedByClient'
        });
    }
  });
  const evaluate = async (expression) => {
    const value = await send('Runtime.evaluate', {
      expression,
      returnByValue: true,
      awaitPromise: true
    });
    if (value.exceptionDetails) throw new Error(value.exceptionDetails.text);
    return value.result.value;
  };
  const wait = async (expression) => {
    for (let i = 0; i < 250; i++) {
      if (await evaluate(`Boolean(${expression})`)) return;
      await new Promise((resolve) => setTimeout(resolve, 100));
    }
    throw new Error(`Timeout: ${expression}`);
  };
  const screenshot = async (name) => {
    const image = await send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(
      path.join(output, name),
      Buffer.from(image.data, 'base64')
    );
  };
  const select = async (label) => {
    await evaluate(`document.getElementById('semester-filter').focus()`);
    await send('Input.dispatchKeyEvent', {
      type: 'keyDown',
      key: 'Enter',
      code: 'Enter'
    });
    await send('Input.dispatchKeyEvent', {
      type: 'keyUp',
      key: 'Enter',
      code: 'Enter'
    });
    await wait(`document.querySelector('[role=menuitemradio]')`);
    await evaluate(
      `Array.from(document.querySelectorAll('[role=menuitemradio]')).find(x=>x.textContent.includes(${JSON.stringify(label)})).click()`
    );
    await wait(`!document.querySelector('[role=menuitemradio]')`);
    await wait(`document.activeElement.id === 'semester-filter'`);
  };
  try {
    await send('Page.enable');
    await send('Network.enable');
    await send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    const encode = (value) =>
      Buffer.from(JSON.stringify(value)).toString('base64url');
    await send('Network.setCookie', {
      name: 'token',
      value: `${encode({ alg: 'none' })}.${encode({ userId: '3', unique_name: 'Professor exemplo', role: 'ADVISOR', exp: Math.floor(Date.now() / 1000) + 3600 })}.preview`,
      url: origin,
      path: '/'
    });
    for (const route of ['ongoingTCCs', 'completedTCCs']) {
      await send('Emulation.setDeviceMetricsOverride', {
        width: 1440,
        height: 1000,
        deviceScaleFactor: 1,
        mobile: false
      });
      await send('Page.navigate', { url: origin + '/' + route });
      await wait(
        `document.getElementById('semester-filter') && document.body.textContent.includes('Trabalho anterior')`
      );
      const position = await evaluate(`(()=>{const title=document.querySelector('h1').getBoundingClientRect();const filter=document.getElementById('semester-filter').parentElement.getBoundingClientRect();return {left:Math.abs(title.left-filter.left),gap:filter.top-title.bottom};})()`);
      assert.ok(position.left < 2, 'Filtro deve alinhar à esquerda com o título');
      assert.ok(position.gap >= 12 && position.gap <= 28, 'Filtro deve ficar próximo e abaixo do título');
      checks.push(route + ': filtro alinhado abaixo do título');
      assert.equal(
        await evaluate(`document.querySelectorAll('select').length`),
        0
      );
      await select('2026.2');
      await wait(
        `document.body.textContent.includes('Trabalho atual') && !document.body.textContent.includes('Trabalho anterior')`
      );
      assert.equal(calls.at(-1).query.SemesterId, '1');
      if (route === 'ongoingTCCs') assert.equal(calls.at(-1).query.UserId, '3');
      assert.equal(
        calls.at(-1).query.StatusTcc,
        route === 'ongoingTCCs' ? 'IN_PROGRESS' : 'COMPLETED'
      );
      await screenshot(route + '-filtrado.png');
      checks.push(
        route +
          ': seleção visual mantém SemesterId, status e restrição de usuário aplicável'
      );
      await select('Todos os semestres');
      await wait(`document.body.textContent.includes('Trabalho anterior')`);
      assert.equal(calls.at(-1).query.SemesterId, undefined);
      checks.push(
        route + ': Todos os semestres remove filtro e restaura listagem'
      );
      await send('Emulation.setDeviceMetricsOverride', {
        width: 390,
        height: 844,
        deviceScaleFactor: 1,
        mobile: true
      });
      assert.equal(
        await evaluate('document.documentElement.scrollWidth <= innerWidth'),
        true
      );
      await select('2026.1');
      await wait(
        `!document.body.textContent.includes('Trabalho atual') && document.body.textContent.includes('Trabalho anterior')`
      );
      assert.equal(calls.at(-1).query.SemesterId, '2');
      await screenshot(route + '-mobile.png');
      checks.push(route + ': filtro também funciona no celular sem overflow');
    }
    fs.writeFileSync(
      path.join(output, 'resultado.json'),
      JSON.stringify(
        {
          fixture:
            'API interceptada e dados fictícios; sem alterações no banco real',
          checks,
          calls
        },
        null,
        2
      )
    );
    console.log(JSON.stringify({ checks }, null, 2));
  } catch (error) {
    await screenshot('falha-preview.png').catch(() => {});
    throw error;
  } finally {
    await send('Browser.close').catch(() => {});
    socket.close();
  }
}
run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
