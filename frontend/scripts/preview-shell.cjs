const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

// Preview com identidade e respostas fictícias. Não lê contas, tokens ou dados reais.
// Requer Chrome isolado em modo headless, com depuração somente em loopback.
const debugPort = Number(process.argv[2] || 9227);
const origin = 'http://localhost:3000';
const output = path.resolve(
  __dirname,
  '../docs/refatoracao/evidencias/tcc-e-arquivos'
);
let scenario = 'none';
let pendingSignature = 1;
let currentPhone = '81988888888';
let currentName = 'Estudante de demonstração';
let passwordStatus = 400;
const passwordRequests = [];
const mutations = [];
const checks = [];
let profileFailure = false;
let studentFailure = false;
const proposalRequests = [];
let fileFailure = false;
let downloadFailure = false;
let emptyFiles = false;
const fileRequests = [];

function fixture(url, method, body) {
  const request = new URL(url);
  if (/\/Tcc\/\d+\/files$/.test(request.pathname))
    return emptyFiles
      ? []
      : [
          {
            id: 10,
            name: 'Anexo assinado de demonstração',
            student: currentName,
            signatureCount: 2,
            lastSignature: '2026-10-13T12:00:00'
          }
        ];
  if (request.pathname.endsWith('/User/me')) {
    if (method === 'PUT' || method === 'PATCH') {
      mutations.push(JSON.parse(body));
      currentPhone = JSON.parse(body).phone;
      currentName = JSON.parse(body).name ?? currentName;
    }
    return {
      id: 101,
      name: currentName,
      email: 'demonstracao@discente.ifpe.edu.br',
      phone: currentPhone,
      registration: 'DEMO-2026',
      siape: null,
      campus: 'Campus de demonstração',
      course: 'Curso de demonstração',
      roles: ['STUDENT'],
      cpf: '123.456.789-09',
      userClass: 'Turma de demonstração',
      shift: 'Noturno'
    };
  }
  if (request.pathname.endsWith('/Home'))
    return {
      pendingSignature: scenario === 'active' ? pendingSignature : 0,
      tccInprogress: scenario === 'active' ? 1 : 0
    };
  if (request.pathname.endsWith('/Tcc/filter')) {
    const status = request.searchParams.get('StatusTcc');
    return (scenario === 'active' && status === 'IN_PROGRESS') ||
      (scenario === 'completed' && status === 'COMPLETED')
      ? [{ tccId: 42, studanteNames: ['Estudante de demonstração'] }]
      : [];
  }
  if (request.pathname.endsWith('/User/filter'))
    return [{ id: 11, name: 'Docente de demonstração' }];
  if (request.pathname.endsWith('/Tcc/workflow'))
    return {
      tccId: 42,
      step: 2,
      signatures: [
        {
          documentId: 1,
          attachmentName: 'Documento de demonstração',
          detailsOnlyDocs: [
            {
              userId: 101,
              userProfile: 'STUDENT',
              userName: currentName,
              isSigned: false
            }
          ],
          detailsNotOnlyDocs: []
        }
      ]
    };
  if (request.pathname.endsWith('/Tcc'))
    return {
      infoStudent: [
        {
          name: currentName,
          email: 'demo@discente.ifpe.edu.br',
          registration: 'DEMO',
          cpf: '123.456.789-09',
          course: 'Curso de demonstração'
        }
      ],
      infoTcc: {
        title: 'Projeto de demonstração',
        summary: 'Resumo fictício para conferir a organização da tela.',
        status: scenario === 'completed' ? 'COMPLETED' : 'IN_PROGRESS',
        presentationDate: null,
        presentationTime: null,
        presentationLocation: '',
        semesterName: '2026.2'
      },
      infoAdvisor: {
        name: 'Docente de demonstração',
        email: 'demo@ifpe.edu.br'
      },
      infoBanking: { members: [] },
      cancellationRequest: false
    };
  if (request.pathname.endsWith('/Campi/all/courses'))
    return [{ id: 1, name: 'Curso de demonstração' }];
  return [];
}

async function run() {
  const response = await fetch(
    `http://127.0.0.1:${debugPort}/json/new?about:blank`,
    { method: 'PUT' }
  );
  const target = await response.json();
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener('open', resolve, { once: true });
    socket.addEventListener('error', reject, { once: true });
  });
  let sequence = 0;
  const pending = new Map();
  const networkRequests = new Map();
  const networkFailures = [];
  function send(method, params = {}) {
    return new Promise((resolve, reject) => {
      const id = ++sequence;
      pending.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });
  }
  socket.addEventListener('message', async (event) => {
    const message = JSON.parse(event.data);
    if (message.id) {
      const request = pending.get(message.id);
      if (!request) return;
      pending.delete(message.id);
      if (message.error) request.reject(new Error(message.error.message));
      else request.resolve(message.result);
    } else if (message.method === 'Network.requestWillBeSent') {
      networkRequests.set(
        message.params.requestId,
        new URL(message.params.request.url).pathname
      );
    } else if (message.method === 'Network.loadingFailed') {
      networkFailures.push({
        path: networkRequests.get(message.params.requestId),
        error: message.params.errorText
      });
    } else if (message.method === 'Fetch.requestPaused') {
      const { requestId, request } = message.params;
      const url = new URL(request.url);
      if (url.pathname.startsWith('/api/')) {
        const passwordRequest =
          url.pathname.endsWith('/User/me/password') &&
          request.method === 'POST';
        if (passwordRequest)
          passwordRequests.push(JSON.parse(request.postData));
        if (request.method === 'POST' && url.pathname.includes('/Tcc')) {
          proposalRequests.push(JSON.parse(request.postData));
        }
        const failed =
          (profileFailure && url.pathname.endsWith('/User/me')) ||
          (studentFailure && url.pathname.endsWith('/Tcc/filter'));
        const fileRoute = /\/Tcc\/\d+\/files/.test(url.pathname);
        if (fileRoute) fileRequests.push(url.pathname);
        const deniedFile = fileRoute && fileFailure;
        const failedDownload =
          fileRoute && url.pathname.endsWith('/download') && downloadFailure;
        const body = Buffer.from(
          JSON.stringify(
            passwordRequest
              ? {
                  detail:
                    passwordStatus === 429
                      ? 'Aguarde um minuto.'
                      : 'A senha atual não confere.'
                }
              : failed
                ? { message: 'Falha fictícia' }
                : fixture(request.url, request.method, request.postData)
          )
        ).toString('base64');
        await send('Fetch.fulfillRequest', {
          requestId,
          responseCode: passwordRequest
            ? passwordStatus
            : deniedFile
              ? 404
              : failedDownload
                ? 502
                : (profileFailure && url.pathname.endsWith('/User/me')) ||
                    (studentFailure && url.pathname.endsWith('/Tcc/filter'))
                  ? 500
                  : 200,
          responseHeaders: [
            { name: 'Content-Type', value: 'application/json' },
            { name: 'Access-Control-Allow-Origin', value: origin },
            {
              name: 'Access-Control-Allow-Headers',
              value: 'Authorization, Content-Type'
            },
            {
              name: 'Access-Control-Allow-Methods',
              value: 'GET,POST,PUT,PATCH,OPTIONS'
            }
          ],
          body
        });
      } else if (
        ['localhost', '127.0.0.1'].includes(url.hostname) ||
        url.protocol === 'data:'
      ) {
        await send('Fetch.continueRequest', { requestId });
      } else
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
        `${result.exceptionDetails.exception?.description ?? result.exceptionDetails.text}; expressão: ${expression.slice(0, 140)}`
      );
    return result.result.value;
  };
  const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  const waitFor = async (expression) => {
    for (let attempt = 0; attempt < 150; attempt++) {
      if (await evaluate(`Boolean(${expression})`)) return;
      await sleep(100);
    }
    throw new Error(`Preview não ficou pronto: ${expression}`);
  };
  const navigate = async (pathname) => {
    await send('Page.navigate', { url: origin + pathname });
    await waitFor('document.readyState === "complete"');
    await sleep(300);
  };
  const screenshot = async (name) => {
    await evaluate('document.fonts.ready.then(() => true)');
    const image = await send('Page.captureScreenshot', {
      format: 'png',
      captureBeyondViewport: false
    });
    fs.writeFileSync(
      path.join(output, name),
      Buffer.from(image.data, 'base64')
    );
  };
  const layoutCheck = async (name) => {
    const size = await evaluate(
      '({ content: document.documentElement.scrollWidth, viewport: innerWidth })'
    );
    assert.ok(
      size.content <= size.viewport,
      `${name}: rolagem horizontal indevida`
    );
    checks.push(name);
  };

  try {
    fs.mkdirSync(output, { recursive: true });
    await send('Page.enable');
    await send('Network.enable');
    await send('Fetch.enable', { patterns: [{ urlPattern: '*' }] });
    const encode = (value) =>
      Buffer.from(JSON.stringify(value)).toString('base64url');
    const token = `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ userId: '101', unique_name: 'Estudante de demonstração', role: 'STUDENT', exp: Math.floor(Date.now() / 1000) + 3600 })}.preview`;
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
      `document.querySelector('nav a[href="/newTCC"]')?.getAttribute('aria-current') === 'page'`
    );
    assert.equal(
      await evaluate(
        'document.querySelectorAll("nav [aria-current=page]").length'
      ),
      1
    );
    await layoutCheck('Proposta desktop sem overflow');
    await screenshot('proposta-desktop-demo.png');
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor(
      'document.body.textContent.includes("O título é obrigatório") && document.body.textContent.includes("O resumo é obrigatório")'
    );
    assert.equal(proposalRequests.length, 0);
    checks.push('Proposta vazia mostra erros e não envia à API');

    await evaluate(`document.querySelector('nav a[href="/homePage"]').click()`);
    await waitFor('location.pathname === "/homePage"');
    await evaluate('history.back()');
    await waitFor(
      `location.pathname === '/newTCC' && document.querySelector('nav a[href="/newTCC"]')?.getAttribute('aria-current') === 'page'`
    );
    await sleep(200);
    assert.equal(
      await evaluate(
        `getComputedStyle(document.querySelector('nav a[href="/newTCC"]')).backgroundColor`
      ),
      'rgb(0, 43, 24)'
    );
    checks.push('Navegação e voltar mantêm um único item ativo');

    await navigate('/profile');
    await waitFor('document.querySelector("#profile-phone")');
    await waitFor('document.querySelector("#change-password-trigger")');
    assert.equal(
      await evaluate('!!document.querySelector("#profile-password")'),
      false
    );
    assert.equal(
      await evaluate(
        `document.querySelectorAll('aside a[href="/profile"]').length`
      ),
      1
    );
    assert.equal(
      await evaluate(
        'document.querySelector("aside").textContent.includes("Sair da conta")'
      ),
      false
    );
    assert.equal(
      await evaluate(
        'document.querySelector("header").textContent.includes("MINHA CONTA")'
      ),
      false
    );
    assert.equal(
      await evaluate(
        'getComputedStyle(document.querySelector("aside")).backgroundColor'
      ),
      'rgb(250, 249, 245)'
    );
    checks.push(
      'Lateral clara, conta única e senha oculta sem informações repetidas'
    );
    assert.equal(
      await evaluate(
        'document.querySelector("#profile-personal").closest("section").querySelectorAll("input:not([readonly]):not([disabled])").length'
      ),
      5
    );
    await layoutCheck('Conta desktop sem overflow');
    await screenshot('minha-conta-desktop-demo.png');
    assert.equal(
      await evaluate('document.querySelector("button[type=submit]").disabled'),
      true
    );
    await evaluate(
      `(() => { const input = document.querySelector('#profile-phone'); Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, 'abc81999991234'); input.dispatchEvent(new Event('input', { bubbles: true })); })()`
    );
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor(
      'document.querySelector("#profile-phone").getAttribute("aria-invalid") === "true"'
    );
    await evaluate('document.querySelector("form").requestSubmit()');
    await sleep(300);
    assert.equal(mutations.length, 0);
    checks.push(
      'Telefone inválido mostra erro e não envia mesmo via requestSubmit'
    );
    await evaluate(
      `(() => { const input = document.querySelector('#profile-phone'); Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, '(81) 99999-1234'); input.dispatchEvent(new Event('input', { bubbles: true })); })()`
    );
    await waitFor('!document.querySelector("button[type=submit]").disabled');
    await evaluate(
      `(() => { const input = document.querySelector('#profile-name'); Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, 'Nome corrigido de demonstração'); input.dispatchEvent(new Event('input', { bubbles: true })); })()`
    );
    profileFailure = true;
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor(
      'document.body.textContent.includes("Não foi possível salvar suas informações")'
    );
    await waitFor('!document.querySelector("button[type=submit]").disabled');
    assert.equal(
      await evaluate('document.querySelector("#profile-phone").value'),
      '(81) 99999-1234'
    );
    checks.push('Falha ao salvar preserva edição e libera nova tentativa');
    mutations.length = 0;
    profileFailure = false;
    await evaluate('document.querySelector("form").requestSubmit()');
    await waitFor(
      'document.body.textContent.includes("Informações atualizadas com sucesso")'
    );
    assert.deepEqual(mutations, [
      {
        name: currentName,
        phone: '81999991234',
        cpf: '123.456.789-09',
        registration: 'DEMO-2026',
        userClass: 'Turma de demonstração',
        shift: 3
      }
    ]);
    checks.push('Formulário envia somente os seis campos autorizados');
    await waitFor(
      'document.querySelector("header").textContent.includes("Nome")'
    );
    checks.push(
      'Nome corrigido aparece no menu do usuário sem alterar permissões'
    );
    await evaluate(
      'document.querySelector("#change-password-trigger").focus()'
    );
    await send('Input.dispatchKeyEvent', {
      type: 'keyDown',
      key: 'Enter',
      code: 'Enter',
      text: '\r',
      unmodifiedText: '\r',
      windowsVirtualKeyCode: 13
    });
    await send('Input.dispatchKeyEvent', {
      type: 'keyUp',
      key: 'Enter',
      code: 'Enter',
      windowsVirtualKeyCode: 13
    });
    await waitFor('document.querySelector("#profile-password form")');
    await waitFor('document.activeElement.id === "password-currentPassword"');
    checks.push(
      'Card abre a senha por teclado e posiciona o foco no formulário'
    );
    await evaluate(
      'document.querySelector("#profile-password form").requestSubmit()'
    );
    await waitFor(
      'document.querySelector("#password-currentPassword").getAttribute("aria-invalid") === "true"'
    );
    assert.equal(passwordRequests.length, 0);
    await evaluate(
      `(() => { for (const [id, value] of [['currentPassword', 'errada-de-teste'], ['newPassword', 'nova-de-teste'], ['confirmPassword', 'nova-de-teste']]) { const input = document.querySelector('#password-' + id); Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, value); input.dispatchEvent(new Event('input', { bubbles: true })); } })()`
    );
    await evaluate(
      'document.querySelector("#profile-password form").requestSubmit()'
    );
    await waitFor(
      'document.body.textContent.includes("A senha atual não confere")'
    );
    assert.equal(await evaluate('location.pathname'), '/profile');
    assert.deepEqual(passwordRequests[0], {
      currentPassword: 'errada-de-teste',
      newPassword: 'nova-de-teste'
    });
    passwordStatus = 429;
    await evaluate(
      'document.querySelector("#profile-password form").requestSubmit()'
    );
    await waitFor(
      'document.body.textContent.includes("Aguarde um minuto antes de tentar novamente")'
    );
    checks.push(
      'Troca interna valida campos, envia só duas senhas e trata senha incorreta e limite de tentativas'
    );
    await send('Input.dispatchKeyEvent', {
      type: 'keyDown',
      key: 'Escape',
      code: 'Escape',
      windowsVirtualKeyCode: 27
    });
    await send('Input.dispatchKeyEvent', {
      type: 'keyUp',
      key: 'Escape',
      code: 'Escape',
      windowsVirtualKeyCode: 27
    });
    await waitFor('!document.querySelector("#profile-password")');
    await waitFor('document.activeElement.id === "change-password-trigger"');
    await evaluate(
      `document.querySelector('[aria-label="Abrir menu do usuário"]').focus()`
    );
    await send('Input.dispatchKeyEvent', {
      type: 'keyDown',
      key: 'Enter',
      code: 'Enter',
      text: '\r',
      windowsVirtualKeyCode: 13
    });
    await send('Input.dispatchKeyEvent', {
      type: 'keyUp',
      key: 'Enter',
      code: 'Enter',
      windowsVirtualKeyCode: 13
    });
    await waitFor(
      `document.querySelector('a[href="/profile?section=password"]')`
    );
    await evaluate(
      `document.querySelector('a[href="/profile?section=password"]').click()`
    );
    await waitFor('document.querySelector("#profile-password form")');
    assert.equal(
      await evaluate(
        'document.querySelector("#password-currentPassword").value'
      ),
      ''
    );
    await screenshot('alterar-senha-desktop-demo.png');
    await evaluate(
      'document.querySelector("[data-slot=dialog-close]").click()'
    );
    await waitFor(
      '!document.querySelector("#profile-password") && location.search === ""'
    );
    checks.push(
      'Senha fecha com Escape, restaura foco e também abre pelo menu do usuário'
    );
    profileFailure = true;
    await navigate('/profile');
    await waitFor(
      'document.body.textContent.includes("Não foi possível carregar sua conta")'
    );
    profileFailure = false;
    await evaluate(
      'Array.from(document.querySelectorAll("button")).find(button => button.textContent.includes("Tentar novamente")).click()'
    );
    await waitFor('document.querySelector("#profile-phone")');
    checks.push('Falha de consulta oferece recuperação sem editar identidade');
    studentFailure = true;
    await navigate('/newTCC');
    await waitFor(
      'document.body.textContent.includes("Não foi possível consultar seu TCC")'
    );
    assert.equal(await evaluate('!!document.querySelector("#titulo")'), false);
    checks.push('Erro na consulta de TCC não libera formulário de proposta');
    studentFailure = false;

    scenario = 'active';
    await navigate('/homePage');
    await waitFor(
      `document.querySelector('nav a[href="/myTCC/signatures?id=42"]')`
    );
    assert.equal(
      await evaluate(`!!document.querySelector('nav a[href="/newTCC"]')`),
      false
    );
    await screenshot('inicio-tcc-aprovado-demo.png');
    checks.push('Aprovação troca proposta por Meu TCC');
    await waitFor('document.querySelector("#home-attention")');
    assert.equal(
      await evaluate('!!document.querySelector("#home-tracking")'),
      false
    );
    assert.equal(
      await evaluate('document.querySelectorAll("#home-attention").length'),
      1
    );
    checks.push('Home da aluna mostra somente pendências');
    assert.equal(
      await evaluate(
        `getComputedStyle(document.querySelector('section[aria-labelledby="home-attention"] button')).cursor`
      ),
      'pointer'
    );
    assert.equal(
      await evaluate(
        `getComputedStyle(document.querySelector('header')).borderBottomWidth`
      ),
      '0px'
    );
    await evaluate(
      `document.querySelector('button[aria-label="Recolher menu"]').click()`
    );
    await waitFor(
      `document.querySelector('button[aria-label="Expandir menu"]')`
    );
    assert.equal(
      await evaluate(
        `Math.round(document.querySelector('aside').getBoundingClientRect().width)`
      ),
      80
    );
    assert.equal(
      await evaluate(
        `document.querySelector('aside a[href="/profile"]').getAttribute('aria-label')`
      ),
      'Minha conta'
    );
    await screenshot('menu-recolhido-demo.png');
    await evaluate(
      `document.querySelector('button[aria-label="Expandir menu"]').click()`
    );
    checks.push(
      'Menu recolhível mantém nomes acessíveis e pendências têm cursor de clique'
    );
    const pendingBounds = await evaluate(
      `(() => { const r = document.querySelector('section[aria-labelledby="home-attention"] button').getBoundingClientRect(); return { x: r.x + 20, y: r.y + 20 }; })()`
    );
    await send('Input.dispatchMouseEvent', {
      type: 'mouseMoved',
      ...pendingBounds
    });
    await sleep(200);
    assert.equal(
      await evaluate(
        `getComputedStyle(document.querySelector('section[aria-labelledby="home-attention"] button')).backgroundColor`
      ),
      'rgb(0, 43, 24)'
    );
    await evaluate(
      `document.querySelector('section[aria-labelledby="home-attention"] button').focus()`
    );
    assert.equal(
      await evaluate(
        `document.activeElement === document.querySelector('section[aria-labelledby="home-attention"] button')`
      ),
      true
    );
    checks.push('Pendência tem hover visível e aceita foco por teclado');
    await send('Input.dispatchKeyEvent', {
      type: 'keyDown',
      key: 'Enter',
      code: 'Enter',
      text: '\r'
    });
    await send('Input.dispatchKeyEvent', {
      type: 'keyUp',
      key: 'Enter',
      code: 'Enter'
    });
    await waitFor(`location.pathname === '/pendingSignatures'`);
    checks.push('Card de pendências abre as assinaturas pelo teclado');
    await navigate('/myTCC/signatures?id=42');
    await waitFor(
      'document.body.textContent.includes("Documento de demonstração")'
    );
    assert.equal(
      await evaluate('!!document.querySelector("nav[aria-label=breadcrumb]")'),
      false
    );
    await layoutCheck('Meu TCC desktop sem overflow');
    await screenshot('meu-tcc-desktop-demo.png');
    assert.equal(
      await evaluate(`document.querySelector('h1').textContent`),
      'Projeto de demonstração'
    );
    assert.equal(
      await evaluate(
        `document.querySelector('[aria-label="Etapas do TCC"] [aria-current="step"]').textContent.includes('2')`
      ),
      true
    );
    assert.equal(
      await evaluate(
        `document.querySelector('h1').getBoundingClientRect().y < document.querySelector('[aria-label="Etapas do TCC"]').getBoundingClientRect().y && document.querySelector('[aria-label="Etapas do TCC"]').getBoundingClientRect().y < document.querySelector('a[href="/myTCC/files?id=42"]').getBoundingClientRect().y`
      ),
      true
    );
    checks.push('TCC apresenta título, etapa atual e abas nessa ordem');
    await navigate('/myTCC/details?id=42');
    await waitFor(`document.querySelector('#edit-tcc-form')`);
    assert.equal(
      await evaluate(
        `document.body.textContent.includes('Baixar documentos assinados')`
      ),
      false
    );
    assert.equal(await evaluate(`!!document.querySelector('h1 + p')`), false);
    checks.push('Informações não repetem proprietário nem ação de download');
    await screenshot('informacoes-tcc-desktop-demo.png');
    await navigate('/myTCC/files?id=42');
    await waitFor(
      `document.querySelector('section[aria-label="Documentos assinados"]')`
    );
    assert.equal(
      await evaluate(
        `document.querySelectorAll('section[aria-label="Documentos assinados"] li').length`
      ),
      1
    );
    await layoutCheck('Pasta de documentos assinados desktop sem overflow');
    await screenshot('arquivos-desktop-demo.png');
    assert.equal(
      await evaluate(
        `document.querySelector('button[aria-label="Baixar Anexo assinado de demonstração"]').textContent.trim()`
      ),
      ''
    );
    checks.push('Cada arquivo tem download por ícone com nome acessível');
    downloadFailure = true;
    await evaluate(
      `document.querySelector('button[aria-label="Baixar Anexo assinado de demonstração"]').click()`
    );
    await waitFor(`document.querySelector('[role="alert"]')`);
    assert.equal(fileRequests.includes('/api/Tcc/42/files/10/download'), true);
    downloadFailure = false;
    emptyFiles = true;
    await navigate('/myTCC/files?id=42');
    await waitFor(
      `document.body.textContent.includes('Nenhum documento assinado disponível')`
    );
    fileFailure = true;
    await navigate('/myTCC/files?id=42');
    await waitFor(
      `document.body.textContent.includes('TCC não encontrado ou acesso indisponível')`
    );
    assert.equal(
      await evaluate(
        `!!document.querySelector('button[aria-label="Baixar Anexo assinado de demonstração"]')`
      ),
      false
    );
    fileFailure = false;
    emptyFiles = false;
    await evaluate(
      `Array.from(document.querySelectorAll('button')).find(button => button.textContent === 'Tentar novamente').click()`
    );
    await waitFor(
      `document.querySelector('section[aria-label="Documentos assinados"]')`
    );
    checks.push(
      'Arquivos tratam download indisponível, pasta vazia, acesso negado e recuperação'
    );
    scenario = 'completed';
    await navigate('/completedTCCs/42');
    await waitFor(
      `document.querySelector('a[href="/completedTCCs/files?id=42"]')`
    );
    await navigate('/completedTCCs/files?id=42');
    await waitFor(
      `document.querySelector('section[aria-label="Documentos assinados"]')`
    );
    assert.equal(
      await evaluate(`!!document.querySelector('a[href="/completedTCCs/42"]')`),
      true
    );
    assert.equal(
      await evaluate(
        `document.querySelector('[aria-label="Etapas do TCC"] [aria-current="step"]').textContent.includes('6')`
      ),
      true
    );
    checks.push(
      'TCC concluído mantém informações, etapa final e pasta com rotas válidas'
    );
    scenario = 'active';
    checks.push('Meu TCC mantém etapas e documentos sem breadcrumb repetido');
    pendingSignature = 0;
    await navigate('/homePage');
    await waitFor(
      'document.body.textContent.includes("Nenhuma pendência para exibir")'
    );
    assert.equal(
      await evaluate('!!document.querySelector("#home-attention")'),
      false
    );
    checks.push('Home sem pendências não repete atalhos do menu');

    await send('Emulation.setDeviceMetricsOverride', {
      width: 390,
      height: 844,
      deviceScaleFactor: 1,
      mobile: true
    });
    await navigate('/myTCC/signatures?id=42');
    await waitFor(
      'document.body.textContent.includes("Documento de demonstração")'
    );
    await layoutCheck('Meu TCC mobile sem overflow');
    await screenshot('meu-tcc-mobile-demo.png');
    await navigate('/myTCC/files?id=42');
    await waitFor(
      `document.querySelector('section[aria-label="Documentos assinados"]')`
    );
    await layoutCheck('Arquivos mobile sem overflow');
    await screenshot('arquivos-mobile-demo.png');
    scenario = 'none';
    await navigate('/newTCC');
    await evaluate(
      `document.querySelector('button[aria-label="Abrir navegação"]').click()`
    );
    await waitFor(`document.querySelector('nav a[href="/newTCC"]')`);
    await evaluate(
      `document.querySelector('[role="dialog"] nav a[href="/newTCC"]').click()`
    );
    await waitFor(`!document.querySelector('[role="dialog"]')`);
    checks.push('Menu mobile fecha ao selecionar uma página');
    await layoutCheck('Proposta mobile sem overflow');
    await screenshot('proposta-mobile-demo.png');
    await navigate('/profile');
    await waitFor('document.querySelector("#change-password-trigger")');
    assert.equal(
      await evaluate('!!document.querySelector("#profile-password")'),
      false
    );
    await layoutCheck('Conta mobile com senha oculta sem overflow');
    await screenshot('minha-conta-mobile-demo.png');
    await evaluate(
      'document.querySelector("#change-password-trigger").click()'
    );
    await waitFor('document.querySelector("#profile-password form")');
    await layoutCheck('Conta mobile sem overflow');
    passwordStatus = 200;
    await evaluate(
      `(() => { for (const [id, value] of [['currentPassword', 'atual-de-teste'], ['newPassword', 'nova-de-teste'], ['confirmPassword', 'nova-de-teste']]) { const input = document.querySelector('#password-' + id); Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(input, value); input.dispatchEvent(new Event('input', { bubbles: true })); } })()`
    );
    await evaluate(
      'document.querySelector("#profile-password form").requestSubmit()'
    );
    await waitFor('location.pathname === "/"');
    assert.equal(await evaluate('document.cookie.includes("token=")'), false);
    checks.push(
      'Sucesso na alteração de senha encerra a sessão local e volta ao login'
    );
    fs.writeFileSync(
      path.join(output, 'preview-checks.json'),
      JSON.stringify(
        {
          fixture: 'Dados fictícios; API interceptada; sem autenticação real',
          checks
        },
        null,
        2
      )
    );
    console.log(JSON.stringify({ checks, screenshots: output }, null, 2));
  } catch (error) {
    console.error(
      JSON.stringify({
        page: await evaluate(
          '({ path: location.pathname, ready: document.readyState, text: document.body?.innerText?.slice(0, 700) })'
        ).catch(() => null),
        networkFailures,
        checks
      })
    );
    await screenshot('falha-preview-demo.png').catch(() => {});
    throw error;
  } finally {
    await send('Browser.close').catch(() => {});
    socket.close();
  }
}

run().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
