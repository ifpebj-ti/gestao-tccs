import { z } from 'zod';

export const firstAccessSchema = z.object({
  userEmail: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
    .email('Email Inválido'),
  accessCode: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
}).required();

export type FirstAccessSchemaType = z.infer<typeof firstAccessSchema>;
