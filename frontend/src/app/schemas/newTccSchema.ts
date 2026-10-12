import { z } from 'zod';

export const newTccSchema = z.object({
  students: z.array(
    z.object({
      studentEmail: z.string(),
      courseId: z.coerce.number()
    })
  ),
  advisorId: z.coerce.number().min(1, 'Selecione um orientador'),
  title: z
    .string()
    .min(1, 'O título é obrigatório')
    .max(255, 'O título deve ter no máximo 255 caracteres'),
  summary: z
    .string()
    .min(1, 'O resumo é obrigatório')
    .max(255, 'O resumo da proposta deve ter no máximo 255 caracteres (limite do sistema)')
});

export type NewTccSchemaType = z.infer<typeof newTccSchema>;
