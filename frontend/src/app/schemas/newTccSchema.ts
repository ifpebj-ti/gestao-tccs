import { z } from 'zod';

export const newTccSchema = z.object({
  students: z
    .array(
      z.object({
        studentEmail: z
          .string()
          .min(1, 'Email é obrigatório')
          .email('Email inválido')
          .refine(email => email.endsWith('.ifpe.edu.br'), {
            message: 'O email deve ser do domínio @*.ifpe.edu.br',
          }),
        courseId: z.coerce
          .number({ invalid_type_error: 'Selecione um curso' })
          .min(1, 'Selecione um curso para o estudante'),
      })
    )
    .min(1, 'Informe ao menos um estudante')
    .max(4, 'Máximo de 4 estudantes'),
  
  advisorId: z.coerce.number().min(1, 'Selecione um orientador'),
  title: z.string().min(1, 'Título é obrigatório'),
  summary: z.string().min(1, 'Resumo é obrigatório'),
});

export type NewTccSchemaType = z.infer<typeof newTccSchema>;
