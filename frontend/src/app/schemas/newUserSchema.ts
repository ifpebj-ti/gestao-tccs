import { z } from 'zod';

export const newUserSchema = z.object({
  name: z.string().min(3, 'Nome muito curto'),
  email: z.string().min(1, 'Email é obrigatório').email('Email inválido').refine(email => email.endsWith('@discente.ifpe.edu.br'), {
    message: 'O email deve ser do domínio @discente.ifpe.edu.br',
  }),
  registration: z.string().min(3, 'Matrícula inválida'),
  cpf: z.string().min(11, 'CPF inválido').max(14, 'CPF inválido').refine(cpf => /^\d{11}$/.test(cpf) || /^\d{3}\.\d{3}\.\d{3}-\d{2}$/.test(cpf), {
    message: 'CPF inválido',
  }),
  profile: z.enum(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'STUDENT', 'BANKING', 'LIBRARY'], {
    errorMap: (issue, ctx) => {
      if (issue.code === 'invalid_type') {
        return { message: 'Perfil inválido' };
      }
      return { message: ctx.defaultError };
    }
  }).array().min(1, 'Selecione pelo menos um perfil'),
  // siape obrigatorio caso profile = 'COORDINATOR' | 'SUPERVISOR' | 'ADVISOR'
  siape: z.string().optional(),
  course: z.string().min(1, 'Curso é obrigatório').refine(value => value !== 'Selecione um curso', {
    message: 'Selecione um curso válido',
  }),
}).superRefine((data, ctx) => {
  const profilesThatNeedSiape = ['COORDINATOR', 'SUPERVISOR', 'ADVISOR'];

  if (profilesThatNeedSiape.includes(data.profile[0]) && (!data.siape || data.siape.trim().length < 3)) {
    ctx.addIssue({
      path: ['siape'],
      code: z.ZodIssueCode.custom,
      message: 'SIAPE é obrigatório para esse perfil',
    });
  }
});

export type NewUserSchemaSchemaType = z.infer<typeof newUserSchema>;
