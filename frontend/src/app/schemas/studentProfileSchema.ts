import { z } from 'zod';
import { isValidCpf } from '@/lib/cpf';
import { isValidProfilePhone } from '@/lib/phone';

const text = (label: string, min: number, max: number) =>
  z
    .string()
    .trim()
    .min(min, `${label}: informe pelo menos ${min} caracteres.`)
    .max(max, `${label}: limite de ${max} caracteres.`)
    .refine(
      (value) => !/[\x00-\x1f\x7f]/.test(value),
      'Remova os caracteres de controle.'
    );

export const studentProfileSchema = z
  .object({
    name: text('Nome', 3, 100),
    phone: z
      .string()
      .refine(isValidProfilePhone, 'Informe DDD e 10 ou 11 dígitos.'),
    cpf: z.string().refine(isValidCpf, 'Informe um CPF válido.'),
    registration: text('Matrícula', 1, 50),
    userClass: text('Turma', 1, 50),
    shift: z.coerce
      .number()
      .int()
      .min(1, 'Selecione um turno.')
      .max(4, 'Selecione um turno válido.')
  })
  .strict();

export type StudentProfileValues = z.infer<typeof studentProfileSchema>;
