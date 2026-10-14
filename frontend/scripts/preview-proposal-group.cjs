const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

// Uses an isolated browser and intercepts every API request with fictional data.
const origin = process.argv[3] || 'http://127.0.0.1:3001';
const debugPort = Number(process.argv[2] || 9230);
const output = path.resolve(
  __dirname,
  '../docs/refatoracao/evidencias/proposta-em-grupo/email-e-seletores'
);
const checks = [];
const submissions = [];
const responses = [];
let invitationAvailable = true;
let listFailure = false;
let responseFailure = true;
let pauseResponse = false;
let releaseResponse;
let notifications = [];
const profileUpdates = [];

async function run() {
  const target = await (
    await fetch(`http://127.0.0.1:${debugPort}/json/new?about:blank`, {
      method: 'PUT'
    })
  ).json();
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener('open', resolve, { once: true });
    socket.addEventListener('error', reject, { once: true });
  });
  let sequence = 0;
  const pending = new Map();
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = ++sequence;
      pending.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });
  socket.addEventListener('message', async (event) => {
    const message = JSON.parse(event.data);
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
        let status = 200;
        if (url.pathname.endsWith('/participant-invitations/notifications'))
          data = notifications;
        else if (
          request.method === 'PATCH' &&
          url.pathname.endsWith('/notification')
        ) {
          notifications = [];
          data = 'Notificação lida.';
        } else if (url.pathname.endsWith('/User/me')) {
          if (request.method === 'PATCH')
            profileUpdates.push(JSON.parse(request.postData));
          data = {
            id: 101,
            name: 'Estudante fictício',
            email: 'demo@example.com',
            phone: '81999999999',
            registration: 'TEST-2026',
            cpf: '123.456.789-09',
            userClass: 'Teste',
            shift: 'Noturno',
            campus: 'Campus fictício',
            course: 'Curso fictício',
            roles: ['STUDENT']
          };
        } else if (/\/User\/101$/.test(url.pathname))
          data = {
            id: 101,
            name: 'Estudante fictício',
            email: 'demo@example.com',
            phone: '81999999999',
            registration: 'TEST-2026',
            cpf: '123.456.789-09',
            userClass: 'Teste',
            shift: 'NOTURNO',
            status: 'ACTIVE',
            siape: '1234567',
            titration: 'Especialista',
            profile: [{ id: 1, role: 'STUDENT' }],
            campiCourse: {
              campiId: 7,
              campiName: 'Campus fictício',
              courseId: 9,
              courseName: 'Curso fictício'
            }
          };
        else if (
          url.pathname.endsWith('/Campi/all') ||
          url.pathname.endsWith('/Campi/public/all')
        )
          data = [
            {
              id: 7,
              name: 'Campus fictício',
              courses: [{ id: 9, name: 'Curso fictício' }]
            }
          ];
        else if (url.pathname.endsWith('/Tcc/participant-invitations')) {
          if (listFailure) {
            status = 500;
            data = { detail: 'Falha fictícia' };
          } else
            data = invitationAvailable
              ? [
                  {
                    id: 20,
                    tccId: 10,
                    title: 'Pesquisa em grupo',
                    summary: 'Resumo da proposta fictícia.',
                    requestedBy: 'Colega de demonstração'
                  }
                ]
              : [];
        } else if (
          request.method === 'PATCH' &&
          url.pathname.endsWith('/participant-invitations/20')
        ) {
          responses.push(JSON.parse(request.postData));
          if (pauseResponse)
            await new Promise((resolve) => {
              releaseResponse = resolve;
            });
          if (responseFailure) {
            status = 409;
            data = {
              detail: 'Você já possui um TCC em andamento ou proposta pendente.'
            };
          } else {
            invitationAvailable = false;
            if (JSON.parse(request.postData).accept === false)
              notifications = [
                {
                  id: 20,
                  tccId: 10,
                  title: 'Pesquisa em grupo',
                  declinedBy: 'colega@example.com'
                }
              ];
            data = 'Resposta registrada.';
          }
        } else if (
          request.method === 'POST' &&
          url.pathname.endsWith('/Tcc/proposal')
        ) {
          submissions.push(JSON.parse(request.postData));
          data = { message: 'Proposta enviada' };
        } else if (url.pathname.endsWith('/User/filter'))
          data = [{ id: 3, name: 'Docente de demonstração' }];
        else if (url.pathname.endsWith('/Campi/all/courses'))
          data = [{ id: 7, name: 'Curso fictício' }];
        else if (url.pathname.includes('/Home'))
          data = { pendingSignature: 0, tccInprogress: 0 };
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
        ['127.0.0.1', 'localhost'].includes(url.hostname) ||
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
    const result = await send('Runtime.evaluate', {
      expression,
      returnByValue: true,
      awaitPromise: true
    });
    if (result.exceptionDetails)
      throw new Error(
        result.exceptionDetails.exception?.description ||
          result.exceptionDetails.text
      );
    return result.result.value;
  };
  const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  const waitFor = async (expression) => {
    for (let attempt = 0; attempt < 300; attempt++) {
      if (await evaluate(`Boolean(${expression})`)) return;
      await delay(100);
    }
    throw new Error(`Página não ficou pronta: ${expression}`);
  };
  const navigate = async (pathname) => {
    await send('Page.navigate', { url: origin + pathname });
    await waitFor('document.readyState === "complete"');
  };
  const screenshot = async (name) => {
    const result = await send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(
      path.join(output, name),
      Buffer.from(result.data, 'base64')
    );
  };
  const clickText = async (text) =>
    evaluate(
      `Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes(${JSON.stringify(text)})).click()`
    );
  const openSelect = async (id) => {
    await evaluate(`document.getElementById(${JSON.stringify(id)}).focus()`);
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
    await waitFor('document.querySelector("[role=menuitemradio]")');
  };
  const fill = async (id, value) =>
    evaluate(
      `(() => { const input = document.getElementById(${JSON.stringify(id)}); Object.getOwnPropertyDescriptor(input instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype, 'value').set.call(input, ${JSON.stringify(value)}); input.dispatchEvent(new Event('input', { bubbles: true })); input.dispatchEvent(new Event('change', { bubbles: true })); })()`
    );
  try {
    fs.mkdirSync(output, { recursive: true });
    await send('Page.enable');
    await send('Network.enable');
    await send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    const encode = (value) =>
      Buffer.from(JSON.stringify(value)).toString('base64url');
    const token = `${encode({ alg: 'none' })}.${encode({ userId: '101', unique_name: 'Estudante fictício', role: 'STUDENT', exp: Math.floor(Date.now() / 1000) + 3600 })}.preview`;
    await send('Network.setCookie', {
      name: 'token',
      value: token,
      url: origin,
      path: '/'
    });
    await send('Emulation.setDeviceMetricsOverride', {
      width: 1440,
      height: 1000,
      deviceScaleFactor: 1,
      mobile: false
    });
    await navigate('/newTCC');
    await waitFor(
      'document.getElementById("orientador-trigger") && document.body.textContent.includes("Cadastrar Nova Proposta de TCC")'
    );
    assert.equal(
      await evaluate(
        'getComputedStyle(document.querySelector("form").firstElementChild).marginBottom'
      ),
      '0px'
    );
    assert.equal(
      await evaluate(
        'document.querySelectorAll("form input[type=email]").length'
      ),
      0
    );
    checks.push(
      'Cabeçalho sem margem extra e integrantes opcionais recolhidos inicialmente'
    );
    await clickText('Adicionar outro estudante');
    await fill('participant-0', 'invalid');
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor('document.getElementById("participant-error-0")');
    assert.equal(submissions.length, 0);
    checks.push(
      'E-mail inválido apresenta erro associado ao campo e não envia proposta'
    );
    await fill('participant-0', ' colega@example.com ');
    await clickText('Adicionar outro estudante');
    await fill('participant-1', 'segundo@example.com');
    await clickText('Adicionar outro estudante');
    await fill('participant-2', 'COLEGA@example.com');
    assert.equal(
      await evaluate(
        'Array.from(document.querySelectorAll("button")).some(button => button.textContent.includes("Adicionar outro estudante"))'
      ),
      false
    );
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor(
      'document.body.textContent.includes("Este e-mail já foi informado")'
    );
    assert.equal(submissions.length, 0);
    await fill('participant-2', 'terceiro@example.com');
    await evaluate(
      `document.querySelector('button[aria-label="Remover integrante 3"]').click()`
    );
    assert.equal(
      await evaluate('document.getElementById("participant-1").value'),
      'terceiro@example.com'
    );
    checks.push(
      'Três convidados no máximo, duplicação bloqueada e remoção preserva os outros e-mails'
    );
    const fields = await evaluate(
      'Array.from(document.querySelectorAll("input,textarea")).map(x => ({id:x.id,name:x.name}))'
    );
    for (const field of fields)
      if (field.name === 'title' || field.name === 'summary')
        await fill(
          field.id,
          field.name === 'title' ? 'Projeto fictício' : 'Resumo fictício'
        );
    await openSelect('orientador-trigger');
    await waitFor('document.querySelector("[role=menuitemradio]")');
    assert.equal(
      await evaluate(
        'getComputedStyle(document.querySelector("[role=menuitemradio][data-state=checked]")).backgroundColor'
      ),
      await evaluate(
        `getComputedStyle(document.querySelector('aside[aria-label="Dicas para a proposta"]')).backgroundColor`
      )
    );
    await screenshot('seletor-padrao-verde.png');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Docente de demonstração")).click()'
    );
    await waitFor(
      '!document.querySelector("[role=menuitemradio]") && document.activeElement.id === "orientador-trigger"'
    );
    await openSelect('orientador-trigger');
    await waitFor(
      'document.querySelector("[role=menuitemradio][data-state=checked]")'
    );
    assert.equal(
      await evaluate(
        'document.querySelector("[role=menuitemradio][data-state=checked]").textContent.includes("Docente de demonstração")'
      ),
      true
    );
    await send('Input.dispatchKeyEvent', {
      type: 'keyDown',
      key: 'Escape',
      code: 'Escape'
    });
    await send('Input.dispatchKeyEvent', {
      type: 'keyUp',
      key: 'Escape',
      code: 'Escape'
    });
    await waitFor('!document.querySelector("[role=menuitemradio]")');
    await waitFor('document.activeElement.id === "orientador-trigger"');
    checks.push(
      'Seletor usa a cor exata do card na opção marcada e Escape devolve o foco'
    );
    await screenshot('proposta-grupo-desktop.png');
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor('location.pathname === "/homePage"');
    assert.deepEqual(submissions[0], {
      title: 'Projeto fictício',
      summary: 'Resumo fictício',
      advisorId: 3,
      participantEmails: ['colega@example.com', 'terceiro@example.com']
    });
    checks.push(
      'Proposta válida envia título, resumo, orientador e convite normalizado'
    );
    await waitFor('document.body.textContent.includes("Aceitar participação")');
    assert.equal(
      await evaluate(
        'document.body.textContent.includes("Colega de demonstração") && document.body.textContent.includes("Resumo da proposta fictícia.")'
      ),
      true
    );
    await screenshot('convite-desktop.png');
    checks.push('Home apresenta projeto, resumo, solicitante e consentimento');
    pauseResponse = true;
    await clickText('Aceitar participação');
    await waitFor(
      'Array.from(document.querySelectorAll("article button")).filter(x=>x.disabled).length === 2'
    );
    assert.equal(responses.length, 1);
    releaseResponse();
    pauseResponse = false;
    await waitFor(
      'document.body.textContent.includes("Você já possui um TCC")'
    );
    await waitFor(
      'Array.from(document.querySelectorAll("article button")).filter(x=>x.disabled).length === 0'
    );
    checks.push(
      'Resposta bloqueia ambos os botões; conflito mantém convite e permite nova tentativa'
    );
    listFailure = true;
    await navigate('/homePage');
    await waitFor(
      'document.body.textContent.includes("Não foi possível carregar os convites")'
    );
    listFailure = false;
    await clickText('Tentar novamente');
    await waitFor('document.body.textContent.includes("Aceitar participação")');
    checks.push(
      'Falha na listagem mostra erro e nova tentativa recupera os convites'
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
    await screenshot('convite-mobile.png');
    checks.push('Convite mobile sem rolagem horizontal');
    responseFailure = false;
    await clickText('Aceitar participação');
    await waitFor(
      '!document.body.textContent.includes("Aceitar participação") && location.pathname === "/homePage"'
    );
    assert.deepEqual(responses.at(-1), { accept: true });
    checks.push('Aceite envia consentimento explícito e atualiza a navegação');
    invitationAvailable = true;
    await navigate('/homePage');
    await waitFor('document.body.textContent.includes("Recusar convite")');
    await clickText('Recusar convite');
    await waitFor(
      '!document.body.textContent.includes("Recusar convite") && location.pathname === "/homePage"'
    );
    assert.deepEqual(responses.at(-1), { accept: false });
    checks.push('Recusa envia false e remove convite após atualização');
    await waitFor(
      'document.body.textContent.includes("Participação recusada")'
    );
    assert.equal(
      await evaluate(
        'document.body.textContent.includes("submeta novamente") && !!Array.from(document.querySelectorAll("a")).find(link => link.textContent.includes("Submeter novamente") && link.getAttribute("href") === "/newTCC")'
      ),
      true
    );
    await screenshot('notificacao-recusa-mobile.png');
    await clickText('Marcar como lida');
    await waitFor(
      '!document.body.textContent.includes("Participação recusada")'
    );
    checks.push(
      'Solicitante vê a recusa, pode submeter novamente e marcar a notificação como lida'
    );
    await navigate('/profile');
    await waitFor('document.getElementById("profile-shift")');
    assert.equal(
      await evaluate(
        'document.getElementById("profile-shift").textContent.includes("Noturno")'
      ),
      true
    );
    await openSelect('profile-shift');
    await waitFor('document.querySelector("[role=menuitemradio]")');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Matutino")).click()'
    );
    await waitFor(
      'document.getElementById("profile-shift").textContent.includes("Matutino")'
    );
    await evaluate(
      'document.getElementById("profile-shift").closest("form").requestSubmit()'
    );
    await waitFor(
      'document.body.textContent.includes("Informações atualizadas com sucesso")'
    );
    assert.equal(profileUpdates.at(-1).shift, 1);
    checks.push(
      'Seletor da conta carrega o valor atual e envia turno numérico após edição'
    );
    await navigate('/autoRegister');
    await waitFor('document.getElementById("auto-campus")');
    assert.equal(
      await evaluate('document.getElementById("auto-course").disabled'),
      true
    );
    await openSelect('auto-campus');
    await waitFor('document.querySelector("[role=menuitemradio]")');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Campus fictício")).click()'
    );
    await waitFor('!document.getElementById("auto-course").disabled');
    await openSelect('auto-course');
    await waitFor('document.querySelector("[role=menuitemradio]")');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Curso fictício")).click()'
    );
    assert.equal(
      await evaluate(
        'document.getElementById("auto-course").textContent.includes("Curso fictício")'
      ),
      true
    );
    await screenshot('seletores-autocadastro-mobile.png');
    checks.push(
      'Seletores do autocadastro preservam dependência entre campus e curso'
    );
    await send('Network.setCookie', {
      name: 'token',
      value: `${encode({ alg: 'none' })}.${encode({ userId: '101', unique_name: 'Gestão fictícia', role: 'ADMIN', exp: Math.floor(Date.now() / 1000) + 3600 })}.preview`,
      url: origin,
      path: '/'
    });
    await send('Emulation.setDeviceMetricsOverride', {
      width: 1440,
      height: 1000,
      deviceScaleFactor: 1,
      mobile: false
    });
    await navigate('/newUser');
    await waitFor('document.getElementById("profile")');
    await openSelect('profile');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Orientador")).click()'
    );
    await waitFor(
      'document.getElementById("campusId") && document.activeElement.id === "profile"'
    );
    assert.equal(
      await evaluate('document.getElementById("courseId").disabled'),
      true
    );
    await openSelect('campusId');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Campus fictício")).click()'
    );
    await waitFor('!document.getElementById("courseId").disabled');
    await openSelect('courseId');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Curso fictício")).click()'
    );
    await waitFor(
      'document.getElementById("courseId").textContent.includes("Curso fictício")'
    );
    await screenshot('seletores-gestao-desktop.png');
    checks.push(
      'Cadastro pela gestão mantém perfil, campus, curso e seleção dependente'
    );
    await navigate('/users/details?id=101');
    await waitFor(
      'document.getElementById("profile")?.textContent.includes("Estudante") && document.getElementById("shift")?.textContent.includes("Noturno")'
    );
    assert.equal(
      await evaluate(
        'document.getElementById("status").textContent.includes("Ativo")'
      ),
      true
    );
    await openSelect('status');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Inativo")).click()'
    );
    await waitFor(
      'document.getElementById("status").textContent.includes("Inativo")'
    );
    assert.equal(
      await evaluate('document.querySelectorAll("select").length'),
      0
    );
    checks.push(
      'Edição da gestão exibe dados carregados por reset e atualiza o estado selecionado'
    );
    await navigate('/newTCC');
    await waitFor(
      'document.body.textContent.includes("Informações do(s)") || document.body.textContent.includes("INFORMAÇÕES DO(S)")'
    );
    await openSelect('students.0.courseId');
    await evaluate(
      'Array.from(document.querySelectorAll("[role=menuitemradio]")).find(item => item.textContent.includes("Curso fictício") || item.textContent.includes("Curso de demonstração")).click()'
    );
    await waitFor(
      'document.getElementById("students.0.courseId").textContent.includes("Curso")'
    );
    checks.push(
      'Seletor de curso da proposta da gestão também usa o componente compartilhado'
    );
    fs.writeFileSync(
      path.join(output, 'resultado.json'),
      JSON.stringify(
        {
          fixture: 'Dados fictícios; todas as chamadas de API interceptadas',
          checks
        },
        null,
        2
      )
    );
    console.log(JSON.stringify({ checks, output }, null, 2));
  } catch (error) {
    console.error(
      await evaluate('document.body?.innerText.slice(0,1800)').catch(
        () => 'Página indisponível'
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
