const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');
const { studentProfileSchema } = loadSource(
  'src/app/schemas/studentProfileSchema.ts'
);
const { isValidCpf } = loadSource('src/lib/cpf.ts');
const data = {
  name: 'Estudante fictício',
  phone: '81999991234',
  cpf: '123.456.789-09',
  registration: 'TESTE',
  userClass: 'Turma de teste',
  shift: 3
};

test('cadastro próprio aceita somente os seis campos autorizados', () => {
  assert.equal(studentProfileSchema.safeParse(data).success, true);
  for (const field of [
    'email',
    'id',
    'roles',
    'profile',
    'status',
    'password',
    'campusId',
    'courseId'
  ]) {
    assert.equal(
      studentProfileSchema.safeParse({ ...data, [field]: 'alteração' }).success,
      false
    );
  }
});
test('cadastro próprio rejeita campos vazios, excesso de tamanho e caracteres de controle', () => {
  for (const [field, value] of [
    ['name', ' '],
    ['name', 'x'.repeat(101)],
    ['name', 'Nome\x00'],
    ['registration', ''],
    ['registration', 'x'.repeat(51)],
    ['userClass', ' '],
    ['userClass', 'x'.repeat(51)],
    ['phone', 'abc81999991234'],
    ['shift', 0],
    ['shift', 5],
    ['shift', 1.5]
  ]) {
    assert.equal(
      studentProfileSchema.safeParse({ ...data, [field]: value }).success,
      false,
      field
    );
  }
});
test('turnos respeitam os quatro valores existentes e textos são normalizados', () => {
  for (const shift of [1, 2, 3, 4])
    assert.equal(
      studentProfileSchema.safeParse({ ...data, shift }).success,
      true
    );
  assert.equal(
    studentProfileSchema.parse({ ...data, name: ' Nome corrigido ' }).name,
    'Nome corrigido'
  );
});
test('CPF valida formato e dígitos verificadores, sem aceitar números repetidos', () => {
  for (const value of ['12345678909', '123.456.789-09'])
    assert.equal(isValidCpf(value), true);
  for (const value of [
    '',
    '00000000000',
    '11111111111',
    '12345678900',
    'abc12345678909',
    '123'
  ])
    assert.equal(isValidCpf(value), false);
});
