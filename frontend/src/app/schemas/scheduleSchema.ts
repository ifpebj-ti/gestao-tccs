import { z } from 'zod';

export const scheduleSchema = z.object({
  scheduleDate: z.string().min(1, { message: 'A data é obrigatória.' }),
  scheduleTime: z.string().min(1, { message: 'A hora é obrigatória.' }),
  scheduleLocation: z.string().min(3, { message: 'O local é obrigatório.' }),
});

export type ScheduleSchemaType = z.infer<typeof scheduleSchema>;