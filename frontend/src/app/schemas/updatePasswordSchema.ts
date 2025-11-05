import { z } from 'zod';

export const updatePasswordSchema = z
  .object({
    userEmail: z
      .string()
      .min(1, { message: 'Campo obrigatório' })
      .email('Email Inválido'),
    userPassword: z
      .string(),
    confirmPassword: z
      .string()
      .min(1, { message: 'Campo obrigatório' })
  })
  .refine((data) => data.userPassword === data.confirmPassword, {
    message: 'As senhas não conferem',
    path: ['confirmPassword']
  });

export type UpdatePasswordSchemaType = z.infer<typeof updatePasswordSchema>;
