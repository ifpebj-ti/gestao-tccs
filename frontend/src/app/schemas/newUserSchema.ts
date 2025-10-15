import { z } from 'zod';

export const newUserSchema = z.object({
  name: z.string().min(3, 'Nome muito curto'),
  email: z.string().min(1, 'Email é obrigatório').email('Email inválido'),
  registration: z.string().optional(),
  cpf: z.string().min(11, 'CPF inválido').max(14, 'CPF inválido').refine(cpf => {
    const numericCpf = cpf.replace(/\D/g, '');
    return /^\d{11}$/.test(numericCpf);
  }, {
    message: 'CPF inválido',
  }),
  profile: z.enum(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'STUDENT', 'BANKING', 'LIBRARY'], {
    errorMap: () => ({ message: 'A seleção de um perfil é obrigatória.' })
  }),  siape: z.string().optional(),
  campusId: z.coerce.number({ invalid_type_error: 'Selecione um campus' }).min(1, 'Campus é obrigatório'),
  courseId: z.coerce.number({ invalid_type_error: 'Selecione um curso' }).min(1, 'Curso é obrigatório'),
}).superRefine((data, ctx) => {
  if (!data.profile) {
    ctx.addIssue({
      path: ['profile'],
      code: z.ZodIssueCode.custom,
      message: 'A seleção de um perfil é obrigatória.'
    });
    return;
  }
  
  if (data.profile !== 'BANKING' && !data.email.endsWith('.ifpe.edu.br')) {
    ctx.addIssue({
      path: ['email'],
      code: z.ZodIssueCode.custom,
      message: 'O email deve pertencer ao domínio do IFPE (@*.ifpe.edu.br)',
    });
  }

  const profilesThatNeedSiape = ['COORDINATOR', 'SUPERVISOR', 'ADVISOR'];
  if (profilesThatNeedSiape.includes(data.profile) && (!data.siape || data.siape.trim().length < 3)) {
    ctx.addIssue({
      path: ['siape'],
      code: z.ZodIssueCode.custom,
      message: 'SIAPE é obrigatório para o perfil selecionado.'
    });
  }
});

export type NewUserSchemaSchemaType = z.infer<typeof newUserSchema>;
