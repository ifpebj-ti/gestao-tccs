import { z } from 'zod';

export const resendAccessCodeSchema = z.object({
  userEmail: z
    .string()
    .min(1, { message: 'Campo obrigatório' })
    .email('Email Inválido'),
}).required();

export type ResendAccessCodeSchemaType = z.infer<typeof resendAccessCodeSchema>;
