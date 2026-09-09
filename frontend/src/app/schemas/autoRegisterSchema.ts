import { z } from 'zod';

export const autoRegisterSchema = z.object({
  name: z.string().min(3, 'O nome completo é obrigatório'),

  email: z
    .string()
    .min(1, 'O email é obrigatório')
    .email('Formato de email inválido')
    .refine((email) => email.endsWith('.ifpe.edu.br'), {
      message: 'Por favor, use seu email institucional (@*.ifpe.edu.br)'
    }),

  registration: z.string().min(1, 'A matrícula é obrigatória'),

  cpf: z.string().regex(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/, {
    message: 'O CPF deve estar no formato XXX.XXX.XXX-XX.'
  }),

  phone: z
    .string()
    .min(1, { message: 'Telefone é obrigatório.' })
    .regex(/^\(?\d{2}\)?[\s-]?9?\d{4}-?\d{4}$/, {
      message:
        'Telefone inválido (Ex: XX 9XXXX-XXXX ou XXXXXXXXXX). Mínimo 10 dígitos.'
    }),

  userClass: z.string().min(1, { message: 'A turma é obrigatória.' }),

  shift: z.coerce
    .number({ invalid_type_error: 'Selecione um turno válido' })
    .min(1, 'O turno é obrigatório.'),

  password: z
    .string()
    .min(8, 'A senha deve ter no mínimo 8 caracteres'),

  confirmPassword: z
    .string()
    .min(1, 'A confirmação de senha é obrigatória')
}).refine((data) => data.password === data.confirmPassword, {
  message: 'As senhas não coincidem',
  path: ['confirmPassword']
});

export type AutoRegisterSchemaType = z.infer<typeof autoRegisterSchema>;
