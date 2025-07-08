import { z } from 'zod';

export const cancellationSchema = z.object({
  reason: z.string().min(10, {
    message: 'O motivo deve ter pelo menos 10 caracteres.'
  })
});

export type CancellationSchemaType = z.infer<typeof cancellationSchema>;