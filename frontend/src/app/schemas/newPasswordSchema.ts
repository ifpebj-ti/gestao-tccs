import { z } from 'zod';

export const newPasswordSchema = z.object({
  email: z
    .string()
    .min(1, 'O email é obrigatório')
    .email('Formato de email inválido'),
  inviteCode: z
    .string()
    .min(1, 'O código de convite é obrigatório'),
  password: z
    .string()
    .min(8, 'A senha deve ter pelo menos 8 caracteres.')
    .refine((value) => new TextEncoder().encode(value).length <= 72, 'A senha deve ter até 72 bytes.'),
  confirmPassword: z
    .string()
    .min(1, 'A confirmação de senha é obrigatória'),
})
.refine(data => data.password === data.confirmPassword, {
  message: 'As senhas não coincidem',
  path: ['confirmPassword'], 
});

export type NewPasswordSchemaType = z.infer<typeof newPasswordSchema>;
