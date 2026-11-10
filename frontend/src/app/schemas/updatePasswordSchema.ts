import { z } from 'zod';

export const updatePasswordSchema = z
  .object({
    userEmail: z
      .string()
      .min(1, { message: 'Campo obrigatório' })
      .email('Email Inválido'),
    userPassword: z
      .string()
      .min(8, 'A senha deve ter pelo menos 8 caracteres.')
      .refine((value) => new TextEncoder().encode(value).length <= 72, 'A senha deve ter até 72 bytes.'),
    confirmPassword: z
      .string()
      .min(1, { message: 'Campo obrigatório' })
  })
  .refine((data) => data.userPassword === data.confirmPassword, {
    message: 'As senhas não conferem',
    path: ['confirmPassword']
  });

export type UpdatePasswordSchemaType = z.infer<typeof updatePasswordSchema>;
