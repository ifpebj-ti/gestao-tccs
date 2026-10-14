const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');
const { changeOwnPasswordSchema } = loadSource(
  'src/app/schemas/changeOwnPasswordSchema.ts'
);
const data = {
  currentPassword: 'atual-de-teste',
  newPassword: 'nova-de-teste',
  confirmPassword: 'nova-de-teste'
};
test('alteração interna exige senha atual, mínimo, confirmação e senha diferente', () => {
  assert.equal(changeOwnPasswordSchema.safeParse(data).success, true);
  for (const invalid of [
    { currentPassword: '' },
    { newPassword: 'curta' },
    { confirmPassword: 'outra' },
    {
      newPassword: data.currentPassword,
      confirmPassword: data.currentPassword
    },
    { userId: 999 },
    { email: 'outro@example.org' }
  ]) {
    assert.equal(
      changeOwnPasswordSchema.safeParse({ ...data, ...invalid }).success,
      false
    );
  }
});
test('limite do bcrypt considera bytes UTF-8 e preserva espaços da senha', () => {
  assert.equal(
    changeOwnPasswordSchema.safeParse({
      ...data,
      newPassword: 'á'.repeat(37),
      confirmPassword: 'á'.repeat(37)
    }).success,
    false
  );
  assert.equal(
    changeOwnPasswordSchema.safeParse({
      ...data,
      newPassword: 'x'.repeat(72),
      confirmPassword: 'x'.repeat(72)
    }).success,
    true
  );
  assert.equal(
    changeOwnPasswordSchema.parse({
      ...data,
      newPassword: ' senha longa ',
      confirmPassword: ' senha longa '
    }).newPassword,
    ' senha longa '
  );
});
