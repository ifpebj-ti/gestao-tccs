const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const origin = 'http://localhost:3000';
const output = path.resolve(
  __dirname,
  '../docs/refatoracao/evidencias/recusa-campo'
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
  let items = [41, 42];
  let fail = true;
  let listFailure = false;
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
        let data = {};
        let status = 200;
        if (request.method === 'OPTIONS') data = {};
        else if (url.pathname.endsWith('/filter')) {
          data = items.map((tccId) => ({
            tccId,
            studanteNames: ['Estudante de demonstração']
          }));
          if (listFailure) {
            status = 503;
            data = { detail: 'Falha temporária de demonstração' };
          }
        } else if (url.pathname === '/api/Tcc')
          data = {
            infoTcc: {
              title: `Projeto de demonstração ${url.searchParams.get('tccId')}`,
              summary: 'Uma proposta para melhorar a acessibilidade no campus.',
              pendingParticipantCount: 0
            }
          };
        else if (request.method === 'PATCH') {
          calls.push({
            method: request.method,
            path: url.pathname,
            body: request.postData
          });
          if (fail) {
            status = 409;
            data = { detail: 'Conflito de demonstração. Atualize a proposta.' };
          } else {
            items = items.filter((id) => !url.pathname.includes(`/${id}/`));
            data = { message: 'Sucesso' };
          }
        }
        await send('Fetch.fulfillRequest', {
          requestId,
          responseCode: status,
          responseHeaders: [
            { name: 'Content-Type', value: 'application/json' },
            { name: 'Access-Control-Allow-Origin', value: origin },
            {
              name: 'Access-Control-Allow-Headers',
              value: 'Authorization, Content-Type'
            },
            { name: 'Access-Control-Allow-Methods', value: 'GET,PATCH,OPTIONS' }
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
    for (let i = 0; i < 200; i++) {
      if (await evaluate(`Boolean(${expression})`)) return;
      await new Promise((resolve) => setTimeout(resolve, 100));
    }
    throw new Error(`Timeout: ${expression}`);
  };
  const click = (text) =>
    evaluate(
      `Array.from(document.querySelectorAll('button')).find(x=>x.textContent.includes(${JSON.stringify(text)})).click()`
    );
  const screenshot = async (name) => {
    const image = await send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(
      path.join(output, name),
      Buffer.from(image.data, 'base64')
    );
  };
  try {
    await send('Page.enable');
    await send('Network.enable');
    await send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    const encode = (value) =>
      Buffer.from(JSON.stringify(value)).toString('base64url');
    await send('Network.setCookie', {
      name: 'token',
      value: `${encode({ alg: 'none' })}.${encode({ userId: '3', unique_name: 'Professor de demonstração', role: 'ADVISOR', exp: Math.floor(Date.now() / 1000) + 3600 })}.preview`,
      url: origin,
      path: '/'
    });
    await send('Emulation.setDeviceMetricsOverride', {
      width: 1440,
      height: 1000,
      deviceScaleFactor: 1,
      mobile: false
    });
    await send('Page.navigate', { url: origin + '/receivedProposals' });
    await wait('document.querySelectorAll("article").length === 2');
    assert.equal(
      await evaluate('!!document.querySelector("a[href=\'/newTCC\']")'),
      false
    );
    assert.equal(
      await evaluate('!!document.querySelector("[role=dialog]")'),
      false
    );
    checks.push('Página dedicada e menu do professor sem cadastro');
    await screenshot('desktop.png');
    await click('Aceitar proposta');
    await wait(
      'document.querySelector("[role=alert]")?.textContent.includes("Conflito")'
    );
    assert.equal(
      await evaluate('document.querySelectorAll("article").length'),
      2
    );
    checks.push('Erro da API exibido sem remover proposta');
    fail = false;
    await click('Aceitar proposta');
    await wait('document.querySelectorAll("article").length === 1');
    assert.equal(calls[1].path, '/api/Tcc/41/approve-proposal');
    checks.push('Aceite usa PATCH na rota correta e remove item');
    await click('Recusar');
    await wait('document.querySelector("textarea")');
    const initialHeight = await evaluate('document.querySelector("textarea").getBoundingClientRect().height');
    assert.ok(initialHeight < 60, `Campo vazio alto demais: ${initialHeight}px`);
    await screenshot('recusa-vazia-desktop.png');
    const fillReason = value => evaluate(`(()=>{const input=document.querySelector('textarea');Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype,'value').set.call(input,${JSON.stringify(value)});input.dispatchEvent(new Event('input',{bubbles:true}));})()`);
    await fillReason('Rever a metodologia e os objetivos da proposta. '.repeat(18));
    await wait(`document.querySelector('textarea').getBoundingClientRect().height > ${initialHeight + 20}`);
    assert.ok(await evaluate('document.querySelector("textarea").scrollHeight <= document.querySelector("textarea").clientHeight + 2'));
    await screenshot('recusa-multilinha-desktop.png');
    await fillReason('');
    await wait(`document.querySelector('textarea').getBoundingClientRect().height <= ${initialHeight + 2}`);
    checks.push('Justificativa começa compacta, cresce com o texto e encolhe ao apagar');
    assert.equal(
      await evaluate(
        'Array.from(document.querySelectorAll("button")).find(x=>x.textContent.includes("Confirmar recusa")).disabled'
      ),
      true
    );
    await evaluate(
      `(()=>{const input=document.querySelector('textarea'); Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype,'value').set.call(input,'Rever a metodologia proposta.');input.dispatchEvent(new Event('input',{bubbles:true}));})()`
    );
    await wait(
      '!Array.from(document.querySelectorAll("button")).find(x=>x.textContent.includes("Confirmar recusa")).disabled'
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
    await screenshot('recusa-mobile.png');
    await click('Confirmar recusa');
    await wait('document.querySelectorAll("article").length === 0');
    assert.equal(calls[2].path, '/api/Tcc/42/reject-proposal');
    assert.equal(
      JSON.parse(calls[2].body).reason,
      'Rever a metodologia proposta.'
    );
    checks.push(
      'Recusa exige justificativa, envia PATCH e funciona no celular'
    );
    await wait(
      'document.body.textContent.includes("Nenhuma proposta aguardando")'
    );
    checks.push('Estado vazio após avaliar todas as propostas');
    listFailure = true;
    await send('Page.reload');
    await wait(
      'document.querySelector("[role=alert]")?.textContent.includes("Falha temporária")'
    );
    listFailure = false;
    await click('Atualizar propostas');
    await wait(
      '!document.querySelector("[role=alert]") && document.body.textContent.includes("Nenhuma proposta aguardando")'
    );
    checks.push('Falha de carregamento permite tentar novamente');
    fs.writeFileSync(
      path.join(output, 'resultado.json'),
      JSON.stringify(
        {
          fixture:
            'API interceptada com dados fictícios; nenhuma proposta real alterada',
          checks,
          calls
        },
        null,
        2
      )
    );
    console.log(JSON.stringify({ checks }, null, 2));
  } finally {
    await send('Browser.close').catch(() => {});
    socket.close();
  }
}
run().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
