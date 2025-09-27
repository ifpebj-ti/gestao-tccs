import { z } from 'zod';

export const newTccSchema = z.object({
  studentEmails: z
    .array(z.string().email('Email inválido'))
    .min(1, 'Informe ao menos um estudante')
    .max(4, 'Máximo de 4 estudantes')
    .refine(emails => emails.every(email => email.endsWith('.ifpe.edu.br')), {
      message: 'O email deve ser do domínio @*ifpe.edu.br',
    }),
  advisorId: z.coerce.number().min(1, 'ID do orientador é obrigatório'),
  title: z.string().min(1, 'Título é obrigatório'),
  summary: z.string().min(1, 'Resumo é obrigatório'),
});

export type NewTccSchemaType = z.infer<typeof newTccSchema>;
