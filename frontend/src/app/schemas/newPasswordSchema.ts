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
    .min(1, 'A senha é obrigatória'),
  confirmPassword: z
    .string()
    .min(1, 'A confirmação de senha é obrigatória'),
})
.refine(data => data.password === data.confirmPassword, {
  message: 'As senhas não coincidem',
  path: ['confirmPassword'], 
});

export type NewPasswordSchemaType = z.infer<typeof newPasswordSchema>;
