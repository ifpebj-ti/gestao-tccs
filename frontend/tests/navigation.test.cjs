const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');
const {
  getSidebarItems,
  isSidebarItemActive,
  studentTccHref,
  activeSidebarHref
} = loadSource('src/components/Sidebar/menuConfig.tsx');
const { resolveStudentNavigation } = loadSource('src/lib/studentNavigation.ts');
const { isValidProfilePhone, formatPhone, phoneDigits } =
  loadSource('src/lib/phone.ts');

test('discente sem trabalho aprovado vê proposta, mas não Meu TCC', () => {
  const items = getSidebarItems(['STUDENT'], { status: 'none' });
  assert.ok(items.some((item) => item.href === '/newTCC'));
  assert.equal(
    items.some((item) => item.label === 'Meu TCC'),
    false
  );
});

test('aprovação substitui proposta por Meu TCC com o ID correto', () => {
  const items = getSidebarItems(['STUDENT'], { status: 'active', tccId: 42 });
  assert.equal(
    items.some((item) => item.href === '/newTCC'),
    false
  );
  assert.equal(
    items.find((item) => item.label === 'Meu TCC').href,
    '/myTCC/signatures?id=42'
  );
  assert.ok(items.some((item) => item.href === '/pendingSignatures'));
});

test('trabalho concluído mantém Meu TCC e abre a rota já existente', () => {
  const items = getSidebarItems(['STUDENT'], {
    status: 'completed',
    tccId: 42
  });
  assert.equal(
    items.find((item) => item.label === 'Meu TCC').href,
    '/completedTCCs/42'
  );
  assert.equal(
    items.some((item) => item.href === '/newTCC'),
    false
  );
  assert.equal(
    items.filter((item) => isSidebarItemActive(item, '/completedTCCs/42'))
      .length,
    1
  );
});

test('carregamento e erro não são confundidos com ausência de trabalho', () => {
  for (const status of ['loading', 'error']) {
    const items = getSidebarItems(['STUDENT'], { status });
    assert.equal(
      items.some((item) =>
        ['/newTCC', '/pendingSignatures'].includes(item.href)
      ),
      false
    );
  }
});

test('Início não fica ativo em proposta e apenas um item representa a rota', () => {
  const items = getSidebarItems(['STUDENT'], { status: 'none' });
  const active = items.filter((item) => isSidebarItemActive(item, '/newTCC'));
  assert.equal(active.length, 1);
  assert.equal(active[0].href, '/newTCC');
});

test('navegação respeita múltiplos perfis e não reabre proposta de discente aprovado', () => {
  const items = getSidebarItems(['STUDENT', 'ADVISOR'], {
    status: 'active',
    tccId: 42
  });
  assert.ok(items.some((item) => item.label === 'Meu TCC'));
  assert.ok(items.some((item) => item.href === '/ongoingTCCs'));
  assert.equal(
    items.some((item) => item.href === '/newTCC'),
    false
  );
});

test('todos os perfis têm acesso à própria conta e gestores têm links existentes', () => {
  for (const role of [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'STUDENT',
    'BANKING',
    'LIBRARY'
  ]) {
    assert.ok(
      getSidebarItems([role], { status: 'none' }).some(
        (item) => item.href === '/profile'
      )
    );
  }
  assert.ok(
    getSidebarItems(['ADMIN'], { status: 'none' }).some(
      (item) => item.href === '/users'
    )
  );
  assert.equal(
    getSidebarItems(['LIBRARY'], { status: 'none' }).some(
      (item) => item.href === '/newTCC'
    ),
    false
  );
});

test('Meu TCC tem prioridade sobre a lista geral quando ambos os perfis têm acesso', () => {
  const items = getSidebarItems(['STUDENT', 'ADVISOR'], {
    status: 'completed',
    tccId: 42
  });
  assert.equal(
    activeSidebarHref(items, '/completedTCCs/42'),
    '/completedTCCs/42'
  );
  assert.equal(activeSidebarHref(items, '/completedTCCs/99'), '/completedTCCs');
});

test('resposta vazia, aprovação, conclusão e resposta inválida são diferenciadas', () => {
  assert.equal(resolveStudentNavigation([], []).status, 'none');
  assert.equal(resolveStudentNavigation([], [{ tccId: 42 }]).status, 'active');
  assert.equal(
    resolveStudentNavigation([{ tccId: 21 }], [{ tccId: 42 }]).tccId,
    21
  );
  assert.equal(resolveStudentNavigation({}, []).status, 'error');
  assert.equal(resolveStudentNavigation([], [{ tccId: -1 }]).status, 'error');
  assert.equal(studentTccHref({ status: 'loading' }), null);
});

test('telefone aceita DDD com 10/11 dígitos e mantém a máscara equivalente', () => {
  for (const value of ['81999991234', '(81) 99999-1234', '8133331234']) {
    assert.equal(isValidProfilePhone(value), true);
    assert.equal(phoneDigits(formatPhone(value)), phoneDigits(value));
  }
  for (const value of ['', '123', '819999912345678', 'texto81999991234'])
    assert.equal(isValidProfilePhone(value), false);
});

test('máscara não transforma letras nem número longo em telefone válido', () => {
  for (const value of ['texto81999991234', '+5581999991234', '819999912345']) {
    assert.equal(formatPhone(value), value);
    assert.equal(isValidProfilePhone(formatPhone(value)), false);
  }
});
