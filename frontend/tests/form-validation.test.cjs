const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');

const schemas = Object.assign(
  {},
  ...[
    'login',
    'autoRegister',
    'newUser',
    'editUser',
    'newTcc',
    'editTcc',
    'newPassword',
    'updatePassword',
    'resendAccessCode',
    'verifyAccessCode',
    'verifyTccCode',
    'schedule',
    'registerBanking',
    'tccCancellation'
  ].map((name) => loadSource(`src/app/schemas/${name}Schema.ts`))
);

// Dados exclusivos de teste. Não há envio à API nem leitura de contas reais.
const student = {
  name: 'Estudante fictício',
  email: 'teste@discente.ifpe.edu.br',
  phone: '81999991234',
  cpf: '000.000.000-00',
  registration: 'TESTE',
  userClass: 'Teste',
  shift: 1,
  campusId: 1,
  courseId: 1,
  profile: 'STUDENT',
  password: 'teste1234',
  confirmPassword: 'teste1234'
};
const proposal = {
  students: [],
  advisorId: 1,
  title: 'Título de teste',
  summary: 'Resumo de teste'
};
function accepts(schema, value) {
  assert.equal(schema.safeParse(value).success, true);
}
function rejects(schema, value, field) {
  const result = schema.safeParse(value);
  assert.equal(
    result.success,
    false,
    `Deveria rejeitar ${JSON.stringify(value)}`
  );
  if (field)
    assert.ok(
      result.error.issues.some((issue) => issue.path.join('.') === field)
    );
}

test('login rejeita vazio/e-mail inválido e aceita formato válido', () => {
  accepts(schemas.loginSchema, { email: student.email, password: 'teste' });
  rejects(schemas.loginSchema, { email: '', password: '' });
  rejects(
    schemas.loginSchema,
    { email: 'sem-email', password: 'teste' },
    'email'
  );
});
test('autocadastro valida identidade, contato, seleções e confirmação de senha', () => {
  accepts(schemas.autoRegisterSchema, student);
  for (const [field, value] of Object.entries({
    name: '',
    email: 'teste@gmail.com',
    cpf: '123',
    phone: 'abc',
    registration: '',
    userClass: '',
    shift: 0,
    campusId: 0,
    courseId: 0,
    password: 'curta',
    confirmPassword: 'diferente'
  })) {
    rejects(schemas.autoRegisterSchema, { ...student, [field]: value }, field);
  }
});
test('cadastro pela gestão valida campos obrigatórios e regras de estudante', () => {
  accepts(schemas.newUserSchema, student);
  for (const [field, value] of Object.entries({
    email: 'teste@gmail.com',
    phone: 'abc',
    cpf: '123',
    profile: 'DESCONHECIDO',
    registration: '',
    userClass: '',
    shift: 0,
    campusId: 0,
    courseId: 0
  })) {
    rejects(schemas.newUserSchema, { ...student, [field]: value }, field);
  }
});
test('todos os perfis de servidor respeitam SIAPE e titulação exigidos', () => {
  for (const profile of [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'BANKING',
    'LIBRARY'
  ]) {
    const data = { ...student, profile, siape: '1234567', titration: 'Mestre' };
    accepts(schemas.newUserSchema, data);
    rejects(schemas.newUserSchema, { ...data, siape: '' }, 'siape');
    if (profile !== 'LIBRARY')
      rejects(schemas.newUserSchema, { ...data, titration: '' }, 'titration');
  }
  accepts(schemas.newUserSchema, {
    ...student,
    profile: 'BANKING',
    email: 'banca@example.org',
    siape: '123',
    titration: 'Mestre'
  });
});
test('edição de usuário aplica os campos condicionais existentes', () => {
  const data = { ...student, id: 1, profile: ['STUDENT'], status: 'ACTIVE' };
  accepts(schemas.editUserSchema, data);
  rejects(
    schemas.editUserSchema,
    { ...data, registration: '' },
    'registration'
  );
  rejects(schemas.editUserSchema, { ...data, userClass: '' }, 'userClass');
  rejects(schemas.editUserSchema, { ...data, profile: [] }, 'profile');
  rejects(schemas.editUserSchema, { ...data, phone: 'abcdefghijk' }, 'phone');
  rejects(schemas.editUserSchema, { ...data, name: '   ' }, 'name');
  rejects(schemas.editUserSchema, { ...data, profile: ['ADVISOR'] }, 'siape');
});
test('proposta discente exige orientador inteiro, texto real e limites de 255', () => {
  accepts(schemas.newTccSchema, proposal);
  for (const [field, value] of [
    ['advisorId', 0],
    ['advisorId', 1.5],
    ['title', '  '],
    ['summary', '\n '],
    ['title', 'x'.repeat(256)],
    ['summary', 'x'.repeat(256)]
  ]) {
    rejects(schemas.newTccSchema, { ...proposal, [field]: value }, field);
  }
  accepts(schemas.newTccSchema, {
    ...proposal,
    title: 'x'.repeat(255),
    summary: 'x'.repeat(255)
  });
});
test('proposta da gestão exige 1–4 estudantes válidos, curso e ausência de duplicação', () => {
  const entry = { studentEmail: student.email, courseId: 1 };
  accepts(schemas.newTccManagementSchema, { ...proposal, students: [entry] });
  rejects(schemas.newTccManagementSchema, proposal, 'students');
  rejects(
    schemas.newTccManagementSchema,
    { ...proposal, students: [{ ...entry, studentEmail: '' }] },
    'students.0.studentEmail'
  );
  rejects(
    schemas.newTccManagementSchema,
    { ...proposal, students: [{ ...entry, courseId: 0 }] },
    'students.0.courseId'
  );
  rejects(
    schemas.newTccManagementSchema,
    {
      ...proposal,
      students: [
        entry,
        { ...entry, studentEmail: entry.studentEmail.toUpperCase() }
      ]
    },
    'students.1.studentEmail'
  );
  rejects(
    schemas.newTccManagementSchema,
    {
      ...proposal,
      students: Array.from({ length: 5 }, (_, i) => ({
        ...entry,
        studentEmail: `teste${i}@discente.ifpe.edu.br`
      }))
    },
    'students'
  );
});
test('edição de TCC exige título e resumo com mínimo existente', () => {
  accepts(schemas.editTccSchema, proposal);
  rejects(schemas.editTccSchema, { title: '', summary: 'curto' });
  rejects(schemas.editTccSchema, { title: ' ', summary: ' '.repeat(10) });
});
test('primeiro acesso e mudança de senha rejeitam confirmação divergente', () => {
  const first = {
    email: student.email,
    inviteCode: 'TESTE',
    password: 'teste1234',
    confirmPassword: 'teste1234'
  };
  accepts(schemas.newPasswordSchema, first);
  rejects(
    schemas.newPasswordSchema,
    { ...first, inviteCode: '' },
    'inviteCode'
  );
  rejects(
    schemas.newPasswordSchema,
    { ...first, confirmPassword: 'outra' },
    'confirmPassword'
  );
  const update = {
    userEmail: student.email,
    userPassword: 'teste1234',
    confirmPassword: 'teste1234'
  };
  accepts(schemas.updatePasswordSchema, update);
  rejects(
    schemas.updatePasswordSchema,
    { ...update, confirmPassword: 'outra' },
    'confirmPassword'
  );
  rejects(schemas.updatePasswordSchema, {
    ...update,
    userPassword: '',
    confirmPassword: ''
  });
});
test('recuperação e verificação de códigos exigem e-mail válido e código', () => {
  accepts(schemas.resendAccessCodeSchema, { userEmail: student.email });
  rejects(
    schemas.resendAccessCodeSchema,
    { userEmail: 'inválido' },
    'userEmail'
  );
  for (const [schema, field] of [
    [schemas.verifyAccessCodeSchema, 'accessCode'],
    [schemas.verifyTccCodeSchema, 'code']
  ]) {
    accepts(schema, { userEmail: student.email, [field]: 'TESTE' });
    rejects(schema, { userEmail: student.email, [field]: '' }, field);
  }
});
test('agendamento valida campos obrigatórios e e-mail dos examinadores', () => {
  const data = {
    scheduleDate: '2026-11-20',
    scheduleTime: '14:00',
    scheduleLocation: 'Sala de teste'
  };
  accepts(schemas.scheduleSchema, data);
  for (const field of ['scheduleDate', 'scheduleTime', 'scheduleLocation'])
    rejects(schemas.scheduleSchema, { ...data, [field]: '' }, field);
  rejects(
    schemas.scheduleSchema,
    {
      ...data,
      bankingMembers: [{ name: 'Teste', email: 'inválido', role: 'Banca' }]
    },
    'bankingMembers.0.email'
  );
});
test('banca exige dois membros positivos e cancelamento exige motivo mínimo', () => {
  accepts(schemas.registerBankingSchema, {
    idInternalBanking: 1,
    idExternalBanking: 2
  });
  rejects(
    schemas.registerBankingSchema,
    { idInternalBanking: 0, idExternalBanking: 2 },
    'idInternalBanking'
  );
  rejects(
    schemas.registerBankingSchema,
    { idInternalBanking: 1, idExternalBanking: 0 },
    'idExternalBanking'
  );
  accepts(schemas.cancellationSchema, {
    reason: 'Motivo fictício de cancelamento'
  });
  rejects(schemas.cancellationSchema, { reason: 'curto' }, 'reason');
  rejects(schemas.cancellationSchema, { reason: ' '.repeat(10) }, 'reason');
});
