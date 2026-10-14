import { z } from 'zod';
import { isValidDate } from '@/lib/date';

const required = (label: string) =>
  z.string().trim().min(1, `${label} é obrigatório.`);
export const campusSchema = z.object({
  name: required('Nome'),
  city: required('Cidade').max(100)
});
export const courseSchema = z.object({
  name: required('Nome').max(50),
  level: required('Nível').max(1000)
});
export const semesterSchema = z
  .object({
    name: required('Nome').max(50),
    startDate: z
      .string()
      .refine(isValidDate, 'Informe uma data inicial válida.'),
    endDate: z.string().refine(isValidDate, 'Informe uma data final válida.')
  })
  .refine((data) => data.endDate >= data.startDate, {
    path: ['endDate'],
    message: 'A data final deve ser igual ou posterior à inicial.'
  });
