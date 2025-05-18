import { z } from 'zod';

export const verifyAccessCodeSchema = z.object({
  userEmail: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
    .email('Email Inválido'),
  accessCode: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
}).required();

export type VerifyAccessCodeSchemaType = z.infer<typeof verifyAccessCodeSchema>;
