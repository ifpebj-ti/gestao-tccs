import { z } from 'zod';

export const newUserSchema = z
  .object({
    name: z
      .string()
      .min(2, { message: 'Nome deve ter pelo menos 2 caracteres' }),

    email: z
      .string()
      .min(1, { message: 'O campo Email é obrigatório.' })
      .email({
        message: 'O campo Email deve conter um endereço de e-mail válido.'
      }),

    phone: z
      .string()
      .min(1, { message: 'Telefone é obrigatório.' })
      .regex(/^\(?\d{2}\)?[\s-]?9?\d{4}-?\d{4}$/, {
        message:
          'Telefone inválido (Ex: XX 9XXXX-XXXX ou XXXXXXXXXX). Mínimo 10 dígitos.'
      }),

    cpf: z.string().regex(/^\d{3}\.\d{3}\.\d{3}-\d{2}$/, {
      message: 'O CPF deve estar no formato XXX.XXX.XXX-XX.'
    }),

    // Campos Condicionais
    registration: z.string().optional(),
    siape: z.string().optional(),
    userClass: z.string().optional(),
    titration: z.string().optional(),

    profile: z.string().refine(
      (val) => {
        const allowed = [
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'STUDENT',
          'BANKING',
          'LIBRARY'
        ];
        return allowed.includes(val);
      },
      { message: 'A seleção de um perfil é obrigatória.' }
    ),

    shift: z.coerce.number().optional(),

    campusId: z.coerce
      .number({ invalid_type_error: 'Selecione um campus' })
      .min(1, 'Campus é obrigatório'),
    courseId: z.coerce
      .number({ invalid_type_error: 'Selecione um curso' })
      .min(1, 'Curso é obrigatório')
  })
  // --- Validações Condicionais ---
  .superRefine((data, ctx) => {
    // Validação de Email (Exceto Banca/Banking)
    if (data.profile !== 'BANKING' && !data.email.endsWith('.ifpe.edu.br')) {
      ctx.addIssue({
        path: ['email'],
        code: z.ZodIssueCode.custom,
        message: 'O email deve pertencer ao domínio do IFPE (@*.ifpe.edu.br)'
      });
    }

    // Validação Específica para ESTUDANTE
    if (data.profile === 'STUDENT') {
      if (!data.registration || data.registration.trim().length < 1) {
        ctx.addIssue({
          path: ['registration'],
          code: z.ZodIssueCode.custom,
          message: 'Matrícula é obrigatória para estudantes.'
        });
      }
      if (!data.userClass || data.userClass.trim().length < 1) {
        ctx.addIssue({
          path: ['userClass'],
          code: z.ZodIssueCode.custom,
          message: 'Turma é obrigatória para estudantes.'
        });
      }
      if (!data.shift || data.shift < 1) {
        ctx.addIssue({
          path: ['shift'],
          code: z.ZodIssueCode.custom,
          message: 'Selecione um turno válido.'
        });
      }
    }

    // Perfis que EXIGEM SIAPE
    const profilesThatNeedSiape = [
      'COORDINATOR',
      'SUPERVISOR',
      'ADVISOR',
      'LIBRARY',
      'ADMIN',
      'BANKING'
    ];
    if (profilesThatNeedSiape.includes(data.profile)) {
      if (!data.siape || data.siape.trim().length < 3) {
        ctx.addIssue({
          path: ['siape'],
          code: z.ZodIssueCode.custom,
          message: 'SIAPE é obrigatório para o perfil selecionado.'
        });
      }
    }

    // Perfis que EXIGEM TITULAÇÃO
    const profilesThatNeedTitration = [
      'COORDINATOR',
      'SUPERVISOR',
      'ADVISOR',
      'ADMIN',
      'BANKING'
    ];
    if (profilesThatNeedTitration.includes(data.profile)) {
      if (!data.titration || data.titration.trim().length < 2) {
        ctx.addIssue({
          path: ['titration'],
          code: z.ZodIssueCode.custom,
          message: 'Titulação é obrigatória.'
        });
      }
    }
  });

export type NewUserSchemaSchemaType = z.infer<typeof newUserSchema>;
