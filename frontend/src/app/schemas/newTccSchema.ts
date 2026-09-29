import { z } from 'zod';

export const newTccSchema = z.object({
  students: z.array(
    z.object({
      studentEmail: z.string(),
      courseId: z.coerce.number()
    })
  ),
  advisorId: z.coerce.number().min(1, 'Selecione um orientador'),
  title: z.string().min(1, 'O título é obrigatório'),
  summary: z.string().min(1, 'O resumo é obrigatório')
});

export type NewTccSchemaType = z.infer<typeof newTccSchema>;
