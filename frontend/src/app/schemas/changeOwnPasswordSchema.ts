import { z } from 'zod';

const bcryptLength = (value: string) =>
  new TextEncoder().encode(value).length <= 72;
export const changeOwnPasswordSchema = z
  .object({
    currentPassword: z
      .string()
      .min(1, 'Informe sua senha atual.')
      .refine(bcryptLength, 'A senha ultrapassa o limite de 72 bytes.'),
    newPassword: z
      .string()
      .min(8, 'Use pelo menos 8 caracteres.')
      .refine(bcryptLength, 'A senha ultrapassa o limite de 72 bytes.'),
    confirmPassword: z.string().min(1, 'Confirme a nova senha.')
  })
  .strict()
  .refine((data) => data.newPassword === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'As senhas não coincidem.'
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    path: ['newPassword'],
    message: 'Escolha uma senha diferente da atual.'
  });
export type ChangeOwnPasswordValues = z.infer<typeof changeOwnPasswordSchema>;
