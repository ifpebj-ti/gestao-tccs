import { z } from 'zod';

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
    .email('Email Inválido'),
  password: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
}).required();

export type LoginSchemaType = z.infer<typeof loginSchema>;
