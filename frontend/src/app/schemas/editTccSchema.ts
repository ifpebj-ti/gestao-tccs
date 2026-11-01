import { z } from 'zod';

export const editTccSchema = z.object({
  title: z
    .string({ required_error: 'O título é obrigatório' })
    .trim()
    .min(1, 'O título não pode ficar vazio'),

  summary: z
    .string({ required_error: 'O resumo é obrigatório' })
    .trim()
    .min(10, 'O resumo deve ter pelo menos 10 caracteres')
});

export type EditTccSchemaType = z.infer<typeof editTccSchema>;
