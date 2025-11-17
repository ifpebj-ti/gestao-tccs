import { z } from 'zod';

export const editUserSchema = z
  .object({
    id: z.number(),
    name: z.string().min(1, { message: 'Nome é obrigatório' }),
    email: z.string().email({ message: 'Email inválido' }),

    phone: z.string().min(1, { message: 'Telefone é obrigatório.' }).min(10, {
      message:
        'Telefone inválido, deve conter DDD e número (mínimo 10 dígitos).'
    }),

    // --- Campos que serão validados condicionalmente ---
    registration: z.string().nullable().optional(),
    cpf: z.string().nullable().optional(),
    siape: z.string().nullable().optional(),
    userClass: z.string().nullable().optional(),
    titration: z.string().nullable().optional(),

    status: z.enum(['ACTIVE', 'INACTIVE']),
    profile: z
      .array(z.string())
      .min(1, { message: 'Pelo menos um perfil é obrigatório' }),
    shift: z.number().nullable().optional(),
    campiId: z.number().nullable().optional(),
    courseId: z.number().nullable().optional()
  })
  // --- Validação Condicional ---
  .refine(
    (data) => {
      // Se o perfil principal for STUDENT
      if (data.profile[0] === 'STUDENT') {
        // Matrícula é obrigatória
        return data.registration && data.registration.length > 0;
      }
      return true;
    },
    { message: 'A matrícula é obrigatória.', path: ['registration'] }
  )
  .refine(
    (data) => {
      // Se o perfil principal for STUDENT
      if (data.profile[0] === 'STUDENT') {
        // Turma é obrigatória
        return data.userClass && data.userClass.length > 0;
      }
      return true;
    },
    { message: 'Turma não pode ser vazia.', path: ['userClass'] }
  )
  .refine(
    (data) => {
      // Se o perfil principal NÃO for STUDENT
      if (data.profile[0] !== 'STUDENT') {
        // SIAPE é obrigatório
        return data.siape && data.siape.length > 0;
      }
      return true;
    },
    { message: 'SIAPE é obrigatório.', path: ['siape'] }
  )
  .refine(
    (data) => {
      // Se o perfil principal NÃO for STUDENT
      if (data.profile[0] !== 'STUDENT') {
        // Titulação é obrigatória
        return data.titration && data.titration.length > 0;
      }
      return true;
    },
    { message: 'Titulação não pode ser vazia.', path: ['titration'] }
  );

export type EditUserSchemaType = z.infer<typeof editUserSchema>;
