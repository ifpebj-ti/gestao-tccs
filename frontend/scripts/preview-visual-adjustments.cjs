const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const origin = 'http://localhost:3000';
const output = path.resolve(
  __dirname,
  '../docs/refatoracao/evidencias/assinatura-painel-verde'
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
  let role = 'ADVISOR';
  let signatureCount = 1;
  let downloadFailure = true;
  let submitFailure = true;
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
        else if (url.pathname === '/api/Home')
          data = { pendingSignature: signatureCount, tccInprogress: 4 };
        else if (url.pathname.endsWith('/filter'))
          data =
            url.searchParams.get('StatusTcc') === 'PENDING_APPROVAL' &&
            role === 'ADVISOR'
              ? items.map((tccId) => ({
                  tccId,
                  studanteNames: ['Estudante exemplo']
                }))
              : [];
        else if (url.pathname === '/api/Tcc')
          data = {
            infoTcc: {
              title: 'Acessibilidade no campus',
              summary:
                'Uma proposta para melhorar a acessibilidade e a autonomia dos estudantes no campus.',
              pendingParticipantCount: 0
            }
          };
        else if (url.pathname.includes('/participant-invitations')) data = [];
        else if (url.pathname === '/api/Signature/document')
          data = {
            isHtml: true,
            url: '<html><body style="padding:30px;font-family:serif"><h2>Documento de teste</h2><p>Conteudo ficticio para conferir o visualizador e as etapas de assinatura.</p></body></html>'
          };
        else if (url.pathname.includes('/Signature/document/download')) {
          if (downloadFailure) status = 500;
          else data = { pdf: 'Documento ficticio' };
          calls.push({
            path: url.pathname,
            method: request.method,
            kind: 'download',
            status
          });
        } else if (
          url.pathname === '/api/Signature' &&
          request.method === 'POST'
        ) {
          status = submitFailure ? 500 : 200;
          calls.push({
            path: url.pathname,
            method: request.method,
            kind: 'signature',
            status
          });
        } else if (url.pathname === '/api/Signature/pending')
          data = [
            {
              tccId: 41,
              studentNames: ['Estudante exemplo'],
              pendingDetails: [
                {
                  documentId: 9,
                  documentName: 'ANEXO I - TERMO DE COMPROMISSO DE ORIENTACAO',
                  idDocumentOwner: 101,
                  userDetails: [
                    {
                      userId: 3,
                      userName: 'Professor exemplo',
                      userProfile: 'ADVISOR',
                      nameDocumentOwner: 'Estudante exemplo',
                      idDocumentOwner: 101
                    }
                  ]
                }
              ]
            }
          ];
        else if (request.method === 'PATCH') {
          calls.push({ path: url.pathname, method: request.method });
          data = { message: 'Sucesso' };
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
            {
              name: 'Access-Control-Allow-Methods',
              value: 'GET,POST,PATCH,OPTIONS'
            }
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
  const navigate = async (route) => {
    await send('Page.navigate', { url: origin + route });
  };
  const viewport = (width, height) =>
    send('Emulation.setDeviceMetricsOverride', {
      width,
      height,
      deviceScaleFactor: 1,
      mobile: width < 600
    });
  const setRole = async (value) => {
    role = value;
    const encode = (value) =>
      Buffer.from(JSON.stringify(value)).toString('base64url');
    await send('Network.setCookie', {
      name: 'token',
      value: `${encode({ alg: 'none' })}.${encode({ userId: '3', unique_name: 'Pessoa de exemplo', role, exp: Math.floor(Date.now() / 1000) + 3600 })}.preview`,
      url: origin,
      path: '/'
    });
  };
  const fill = (id, value) =>
    evaluate(
      `(()=>{const input=document.getElementById(${JSON.stringify(id)}); Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(input,${JSON.stringify(value)});input.dispatchEvent(new Event('input',{bubbles:true}));input.dispatchEvent(new Event('change',{bubbles:true}));})()`
    );
  const upload = (index) =>
    evaluate(
      `(()=>{const input=document.querySelectorAll('input[type=file]')[${index}];const files=new DataTransfer();files.items.add(new File(['%PDF-ficticio'],'documento-assinado.pdf',{type:'application/pdf'}));input.files=files.files;input.dispatchEvent(new Event('change',{bubbles:true}));})()`
    );
  const confirmDisabled = `Array.from(document.querySelectorAll('button')).find(x=>x.textContent.includes('Confirmar Assinatura'))?.disabled`;
  try {
    await send('Page.enable');
    await send('Network.enable');
    await send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    await viewport(1440, 1000);
    await setRole('ADVISOR');
    await navigate('/homePage');
    await wait(
      `Array.from(document.querySelectorAll('button')).filter(x=>x.textContent.includes('Ver pendências')).length === 2`
    );
    assert.equal(
      await evaluate(`document.body.textContent.includes('Acompanhamento')`),
      false
    );
    await screenshot('home-professor.png');
    checks.push(
      'Home do professor tem duas notificações positivas e não repete acompanhamento'
    );
    signatureCount = 0;
    items = [];
    await send('Page.reload');
    await wait(
      `document.body.textContent.includes('Nenhuma pendência para exibir')`
    );
    assert.equal(
      await evaluate(
        `Array.from(document.querySelectorAll('button')).some(x=>x.textContent.includes('Ver pendências'))`
      ),
      false
    );
    checks.push('Professor sem pendências não recebe cards verdes vazios');
    await setRole('STUDENT');
    await navigate('/homePage');
    await wait(
      `document.body.textContent.includes('Nenhuma pendência para exibir')`
    );
    assert.equal(
      await evaluate(
        `Array.from(document.querySelectorAll('button')).some(x=>x.textContent.includes('Ver pendências'))`
      ),
      false
    );
    checks.push(
      'Estudante sem pendências também não recebe notificações vazias'
    );
    await setRole('ADVISOR');
    items = [41, 42];
    await navigate('/receivedProposals');
    await wait(`document.querySelectorAll('article').length === 2`);
    assert.ok(
      await evaluate(
        `document.querySelector('h1').getBoundingClientRect().top < 130`
      )
    );
    assert.equal(
      await evaluate(
        `document.body.textContent.includes('Aguardando avaliação')`
      ),
      false
    );
    const redButton = `Array.from(document.querySelectorAll('button')).find(x=>x.textContent.includes('Recusar'))`;
    const themeColorMatches = (tone) =>
      `(()=>{const sample=document.createElement('div');sample.style.backgroundColor='var(--color-red-${tone})';document.body.appendChild(sample);const expected=getComputedStyle(sample).backgroundColor;const actual=getComputedStyle(${redButton}).backgroundColor;sample.remove();return expected===actual;})()`;
    await wait(themeColorMatches(600));
    assert.equal(
      await evaluate(`getComputedStyle(${redButton}).color`),
      'rgb(255, 255, 255)'
    );
    const rect = await evaluate(
      `(()=>{const r=${redButton}.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2};})()`
    );
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', ...rect });
    await wait(themeColorMatches(700));
    checks.push(
      'Botão de recusa vermelho com texto branco e hover mais escuro'
    );
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: 0, y: 0 });
    await screenshot('propostas-desktop.png');
    await viewport(390, 844);
    await screenshot('propostas-mobile.png');
    assert.equal(
      await evaluate('document.documentElement.scrollWidth <= innerWidth'),
      true
    );
    checks.push(
      'Propostas sem espaço extra no topo e cards compactos responsivos'
    );
    await viewport(1440, 1000);
    await navigate(
      '/pendingSignatures/signature/9?tccId=41&studentId=101&docName=ANEXO%20I%20-%20TERMO%20DE%20COMPROMISSO%20DE%20ORIENTACAO'
    );
    await wait(
      `document.querySelector('iframe') && !Array.from(document.querySelectorAll('button')).find(x=>x.textContent.includes('Baixar documento')).disabled`
    );
    assert.equal(
      await evaluate(`document.querySelectorAll('iframe').length`),
      1
    );
    assert.equal(
      await evaluate(`getComputedStyle(document.querySelector('h1')).fontSize`),
      '30px'
    );
    assert.ok(
      await evaluate(
        `document.querySelector('h1').getBoundingClientRect().top < 105`
      )
    );
    assert.ok(
      await evaluate(
        `document.querySelector('iframe').getBoundingClientRect().top < 190`
      )
    );
    assert.equal(
      await evaluate(
        `getComputedStyle(document.querySelector('section[aria-label="Etapas de assinatura"]')).backgroundColor`
      ),
      'rgb(24, 62, 45)'
    );
    assert.equal(
      await evaluate(
        `getComputedStyle(document.querySelector('section[aria-label="Etapas de assinatura"] h3')).color`
      ),
      'rgb(250, 249, 245)'
    );
    assert.equal(
      await evaluate(
        `getComputedStyle(Array.from(document.querySelectorAll('aside')).find(x=>x.textContent.includes('Confira antes de confirmar'))).backgroundColor`
      ),
      'rgb(0, 43, 24)'
    );
    assert.equal(
      await evaluate(`document.querySelector('input[type=file]').disabled`),
      true
    );
    await screenshot('assinatura-desktop.png');
    checks.push(
      'Assinatura com título menor, aviso verde e visualizador único'
    );
    await click('Baixar documento');
    await wait(
      `document.body.textContent.includes('Não foi possível baixar o documento')`
    );
    assert.equal(
      await evaluate(`document.querySelector('input[type=file]').disabled`),
      true
    );
    checks.push('Download que falha mantém envio bloqueado');
    downloadFailure = false;
    await click('Baixar documento');
    await wait(`!document.querySelector('input[type=file]').disabled`);
    await upload(0);
    await wait(`!(${confirmDisabled})`);
    await click('Confirmar Assinatura');
    await wait(
      `document.body.textContent.includes('Ocorreu um erro ao submeter')`
    );
    await wait(`!(${confirmDisabled})`);
    assert.equal(
      await evaluate(`document.querySelector('input[type=file]').files.length`),
      1
    );
    checks.push('Falha de envio preserva arquivo e permite nova tentativa');
    submitFailure = false;
    await click('Confirmar Assinatura');
    await wait(`location.pathname === '/pendingSignatures'`);
    await wait(`document.body.textContent.includes('Sobre as pendências')`);
    await screenshot('assinaturas-lista.png');
    checks.push('Envio bem-sucedido retorna à lista de assinaturas');
    await navigate(
      '/pendingSignatures/signature/9?tccId=41&studentId=101&docName=ANEXO%20VIII'
    );
    await wait(`document.getElementById('signature-location')`);
    await fill('signature-date', '2026-11-10');
    await fill('signature-time', '14:30');
    await evaluate(`document.getElementById('signature-location').focus()`);
    await send('Input.insertText', { text: 'Sala 20' });
    assert.equal(
      await evaluate(`document.activeElement.id`),
      'signature-location'
    );
    assert.equal(
      await evaluate(`document.getElementById('signature-location').value`),
      'Sala 20'
    );
    await click('Baixar documento');
    await wait(`!document.querySelector('input[type=file]').disabled`);
    await upload(0);
    assert.equal(await evaluate(confirmDisabled), true);
    await upload(1);
    await wait(`!(${confirmDisabled})`);
    checks.push(
      'Anexo VIII preserva foco do agendamento e exige documento assinado e TCC'
    );
    await viewport(390, 844);
    assert.equal(
      await evaluate('document.documentElement.scrollWidth <= innerWidth'),
      true
    );
    await screenshot('assinatura-mobile.png');
    await evaluate(
      `Array.from(document.querySelectorAll('aside')).find(x=>x.textContent.includes('Confira antes de confirmar')).scrollIntoView({block:'center'})`
    );
    await screenshot('assinatura-mobile-acoes.png');
    checks.push('Assinatura se adapta ao celular sem rolagem horizontal');
    fs.writeFileSync(
      path.join(output, 'resultado.json'),
      JSON.stringify(
        {
          fixture:
            'APIs interceptadas com dados e arquivos fictícios; nenhum dado real alterado',
          checks,
          calls
        },
        null,
        2
      )
    );
    console.log(JSON.stringify({ checks, output }, null, 2));
  } catch (error) {
    console.error(
      await evaluate(
        `(()=>{const button=Array.from(document.querySelectorAll('button')).find(x=>x.textContent.includes('Recusar'));return {text:document.body.innerText.slice(0,1200),color:button&&getComputedStyle(button).backgroundColor,theme:getComputedStyle(document.documentElement).getPropertyValue('--color-red-600')};})()`
      )
    );
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
