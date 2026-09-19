import { z } from 'zod';

export const scheduleSchema = z.object({
  scheduleDate: z.string().min(1, { message: 'A data é obrigatória.' }),
  scheduleTime: z.string().min(1, { message: 'A hora é obrigatória.' }),
  scheduleLocation: z.string().min(3, { message: 'O local é obrigatório.' }),
  bankingMembers: z.array(
    z.object({
      name: z.string().min(1, { message: 'Nome do examinador é obrigatório.' }),
      email: z.string().email({ message: 'E-mail inválido.' }),
      role: z.string().min(1, { message: 'Papel ou Instituição é obrigatório.' })
    })
  ).optional(),
});

export type ScheduleSchemaType = z.infer<typeof scheduleSchema>;