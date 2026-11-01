const test = require('node:test');
const assert = require('node:assert/strict');
const { loadSource } = require('./load-source.cjs');
const { campusSchema, courseSchema, semesterSchema } = loadSource(
  'src/app/schemas/managementSchema.ts'
);
const { scheduleSchema } = loadSource('src/app/schemas/scheduleSchema.ts');
test('campus e curso exigem textos reais e respeitam limites do banco', () => {
  assert.equal(
    campusSchema.safeParse({ name: 'Campus teste', city: 'Cidade teste' })
      .success,
    true
  );
  assert.equal(
    courseSchema.safeParse({ name: 'Curso teste', level: 'Superior' }).success,
    true
  );
  for (const data of [
    { name: ' ', city: 'Teste' },
    { name: 'Teste', city: ' ' },
    { name: 'Teste', city: 'x'.repeat(101) }
  ])
    assert.equal(campusSchema.safeParse(data).success, false);
  for (const data of [
    { name: ' ', level: 'Superior' },
    { name: 'x'.repeat(51), level: 'Superior' },
    { name: 'Teste', level: ' ' }
  ])
    assert.equal(courseSchema.safeParse(data).success, false);
});
test('semestre rejeita datas vazias, impossíveis e intervalo invertido', () => {
  const data = {
    name: '2026.2',
    startDate: '2026-07-01',
    endDate: '2026-11-30'
  };
  assert.equal(semesterSchema.safeParse(data).success, true);
  for (const invalid of [
    { name: ' ' },
    { startDate: '' },
    { startDate: '2026-02-30' },
    { endDate: '2026-01-01' }
  ])
    assert.equal(
      semesterSchema.safeParse({ ...data, ...invalid }).success,
      false
    );
  assert.equal(
    semesterSchema.safeParse({
      ...data,
      startDate: '2028-02-29',
      endDate: '2028-03-01'
    }).success,
    true
  );
});
test('agendamento rejeita datas impossíveis, horas inválidas e local em branco', () => {
  const data = {
    scheduleDate: '2026-11-20',
    scheduleTime: '14:00',
    scheduleLocation: 'Sala teste'
  };
  for (const invalid of [
    { scheduleDate: '2026-02-29' },
    { scheduleTime: '25:00' },
    { scheduleTime: '14:60' },
    { scheduleLocation: '   ' }
  ])
    assert.equal(
      scheduleSchema.safeParse({ ...data, ...invalid }).success,
      false
    );
});
