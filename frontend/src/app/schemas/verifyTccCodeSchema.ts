import { z } from 'zod';

export const verifyTccCodeSchema = z.object({
  userEmail: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
    .email('Email Inválido'),
  code: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
}).required();

export type VerifyTccCodeSchemaType = z.infer<typeof verifyTccCodeSchema>;
