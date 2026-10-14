import { z } from 'zod';

export const newTccSchema = z.object({
  participants: z
    .array(
      z.object({
        email: z.string().trim().email('Informe um e-mail válido').max(254)
      })
    )
    .max(3, 'Adicione no máximo três outros integrantes')
    .superRefine((participants, ctx) => {
      const emails = new Set<string>();
      participants.forEach((participant, index) => {
        const email = participant.email.toLowerCase();
        if (emails.has(email))
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: [index, 'email'],
            message: 'Este e-mail já foi informado'
          });
        emails.add(email);
      });
    })
    .optional(),
  students: z.array(
    z.object({
      studentEmail: z.string(),
      courseId: z.coerce.number()
    })
  ),
  advisorId: z.coerce
    .number()
    .int('Selecione um orientador válido')
    .min(1, 'Selecione um orientador'),
  title: z
    .string()
    .trim()
    .min(1, 'O título é obrigatório')
    .max(255, 'O título deve ter no máximo 255 caracteres'),
  summary: z
    .string()
    .trim()
    .min(1, 'O resumo é obrigatório')
    .max(
      255,
      'O resumo da proposta deve ter no máximo 255 caracteres (limite do sistema)'
    )
});

// O discente envia apenas a própria proposta; a gestão informa os estudantes.
export const newTccManagementSchema = newTccSchema.extend({
  students: z
    .array(
      z.object({
        studentEmail: z.string().trim().email('Informe um e-mail válido'),
        courseId: z.coerce.number().int().min(1, 'Selecione um curso')
      })
    )
    .min(1, 'Informe pelo menos um estudante')
    .max(4, 'Informe no máximo quatro estudantes')
    .superRefine((students, ctx) => {
      const seen = new Set<string>();
      students.forEach((student, index) => {
        const email = student.studentEmail.toLowerCase();
        if (seen.has(email))
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: [index, 'studentEmail'],
            message: 'Este estudante já foi informado'
          });
        seen.add(email);
      });
    })
});

export type NewTccSchemaType = z.infer<typeof newTccSchema>;
