import { z } from 'zod';

export const updatePasswordSchema = z.object({
  userEmail: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
    .email('Email Inválido'),
  userPassword: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
}).required();

export type UpdatePasswordSchemaType = z.infer<typeof updatePasswordSchema>;
