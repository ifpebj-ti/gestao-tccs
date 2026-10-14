import { z } from 'zod';
import { isValidDate } from '@/lib/date';

export const scheduleSchema = z.object({
  scheduleDate: z.string().refine(isValidDate, 'Informe uma data válida.'),
  scheduleTime: z
    .string()
    .regex(/^([01][0-9]|2[0-3]):[0-5][0-9]$/, 'Informe uma hora válida.'),
  scheduleLocation: z
    .string()
    .trim()
    .min(3, { message: 'O local é obrigatório.' }),
  bankingMembers: z
    .array(
      z.object({
        name: z
          .string()
          .min(1, { message: 'Nome do examinador é obrigatório.' }),
        email: z.string().email({ message: 'E-mail inválido.' }),
        role: z
          .string()
          .min(1, { message: 'Papel ou Instituição é obrigatório.' })
      })
    )
    .optional()
});

export type ScheduleSchemaType = z.infer<typeof scheduleSchema>;
