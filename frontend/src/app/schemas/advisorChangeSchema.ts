import { z } from 'zod';

export const advisorChangeSchema = z.object({
  reason: z.string().trim().min(20, {
    message: 'A justificativa deve ter pelo menos 20 caracteres.'
  }),
  suggestedAdvisorId: z
    .number({ error: 'Selecione um orientador substituto.' })
    .positive({ message: 'Selecione um orientador substituto.' })
    .optional()
});

export type AdvisorChangeSchemaType = z.infer<typeof advisorChangeSchema>;
