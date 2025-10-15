import { z } from 'zod';

export const autoRegisterSchema = z.object({
  name: z.string().min(3, 'O nome completo é obrigatório'),
  email: z
    .string()
    .min(1, 'O email é obrigatório')
    .email('Formato de email inválido')
    .refine(email => email.endsWith('.ifpe.edu.br'), {
      message: 'Por favor, use seu email institucional (@*.ifpe.edu.br)',
    }),
  registration: z.string().min(1, 'A matrícula é obrigatória'),
  cpf: z.string().min(11, 'CPF inválido').max(14, 'CPF inválido').refine(cpf => {
    const numericCpf = cpf.replace(/\D/g, '');
    return /^\d{11}$/.test(numericCpf);
  }, {
    message: 'CPF inválido',
  }),
});

export type AutoRegisterSchemaType = z.infer<typeof autoRegisterSchema>;
