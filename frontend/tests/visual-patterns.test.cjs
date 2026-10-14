const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');
const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { loadSource, root } = require('./load-source.cjs');
const { Button, buttonVariants } = loadSource('src/components/ui/button.tsx');
const { HomeShortcuts } = loadSource('src/components/home/HomeShortcuts.tsx');
const { Inbox } = require('lucide-react');

test('cancelamento e recusa têm fundo vermelho, texto branco e estados mais escuros', () => {
  for (const variant of ['destructive', 'cancel']) {
    const html = renderToStaticMarkup(
      React.createElement(Button, { variant, disabled: true }, 'Cancelar')
    );
    assert.match(html, /bg-red-600/);
    assert.match(html, /text-white/);
    assert.match(html, /hover:bg-red-700/);
    assert.match(html, /active:bg-red-800/);
    assert.match(html, /disabled=""/);
    assert.doesNotMatch(buttonVariants({ variant }), /bg-transparent/);
  }
});

test('home do professor exibe somente notificações com contagem positiva', () => {
  const card = (title, group, count) => ({
    title,
    group,
    indicatorNumber: count,
    visible: true,
    description: 'Descrição',
    icon: Inbox,
    onClick() {}
  });
  const cards = [
    card('Assinaturas pendentes', 'attention', 0),
    card('Propostas recebidas', 'attention', 2),
    card('TCCs em andamento', 'tracking', 3)
  ];
  const html = renderToStaticMarkup(
    React.createElement(HomeShortcuts, { cards, attentionOnly: true })
  );
  assert.match(html, /Propostas recebidas/);
  assert.match(html, /bg-\[#183E2D\]/);
  assert.doesNotMatch(
    html,
    /Assinaturas pendentes|Acompanhamento|TCCs em andamento/
  );
  const empty = renderToStaticMarkup(
    React.createElement(HomeShortcuts, {
      cards: [cards[0]],
      attentionOnly: true
    })
  );
  assert.match(empty, /Nenhuma pendência/);
  assert.doesNotMatch(empty, /<button/);
});

function signatureMarkup({ schedule = false, role = 'ADVISOR' } = {}) {
  const file =
    'src/app/(auth)/pendingSignatures/signature/[documentId]/SignatureClient.tsx';
  const code = ts.transpileModule(
    fs.readFileSync(path.join(root, file), 'utf8'),
    {
      compilerOptions: {
        module: ts.ModuleKind.CommonJS,
        jsx: ts.JsxEmit.ReactJSX,
        esModuleInterop: true
      }
    }
  ).outputText;
  const module = { exports: {} };
  const requireFixture = (name) => {
    if (name === 'next/navigation')
      return {
        useRouter: () => ({ push() {} }),
        useSearchParams: () =>
          new URLSearchParams({
            docName: schedule
              ? 'ANEXO VIII'
              : 'ANEXO I - TERMO DE COMPROMISSO DE ORIENTAÇÃO'
          })
      };
    if (name === '@/app/hooks/useSignaturePage')
      return {
        useSignaturePage: () => ({
          documentHtml: '<p>Documento</p>',
          documentName: 'Documento',
          isLoading: false,
          isSubmitting: false,
          selectedFile: null,
          tccFile: null,
          isAnexoVIII: schedule,
          isAdvisor: role === 'ADVISOR',
          iframeRef: { current: null },
          setSelectedFile() {},
          setTccFile() {},
          handleDownloadDocument: async () => true,
          handleSignDocument() {}
        })
      };
    if (name.startsWith('@/')) {
      const base = 'src/' + name.slice(2);
      return loadSource(
        [base + '.tsx', base + '.ts', base + '/index.tsx'].find((p) =>
          fs.existsSync(path.join(root, p))
        )
      );
    }
    return require(name);
  };
  vm.runInThisContext(`(function(require,module,exports){${code}\n})`)(
    requireFixture,
    module,
    module.exports
  );
  return renderToStaticMarkup(React.createElement(module.exports.default));
}

test('assinatura compartilha título compacto, aviso verde e um único visualizador entre perfis', () => {
  for (const role of [
    'ADVISOR',
    'STUDENT',
    'COORDINATOR',
    'SUPERVISOR',
    'LIBRARY',
    'BANKING',
    'ADMIN'
  ]) {
    const html = signatureMarkup({ role });
    assert.match(html, /Assinar documento/);
    assert.match(html, /text-2xl md:text-3xl/);
    assert.match(html, /Confira antes de confirmar/);
    assert.match(html, /bg-\[#183E2D\]/);
    assert.match(
      html,
      /<section aria-label="Etapas de assinatura"[^>]*bg-\[#183E2D\][^>]*text-\[#FAF9F5\]/
    );
    assert.ok(html.indexOf('<h1') < html.indexOf('Voltar às assinaturas'));
    assert.doesNotMatch(html, /bg-amber|text-amber|truncate/);
    assert.equal((html.match(/<iframe/g) || []).length, 1);
    assert.match(html, /href="https:\/\/www.gov.br/);
    assert.doesNotMatch(html, /<a[^>]*>\s*<button/);
    assert.match(html, /<input[^>]*type="file"[^>]*disabled/);
  }
});

test('Anexo VIII mantém campos de agendamento e arquivo de TCC para orientador', () => {
  const html = signatureMarkup({ schedule: true });
  for (const id of ['signature-date', 'signature-time', 'signature-location']) {
    assert.match(html, new RegExp(`for="${id}"`));
    assert.match(html, new RegExp(`id="${id}"`));
  }
  assert.match(html, /Enviar Arquivo do TCC/);
  assert.match(html, /placeholder:text-\[#FAF9F5\]\/50/);
  assert.equal((html.match(/type="file"/g) || []).length, 2);
});
