import { z } from 'zod';

export const newUserSchema = z.object({
  name: z.string().min(3, 'Nome muito curto'),
  email: z.string().min(1, 'Email é obrigatório').email('Email inválido'),
  registration: z.string().optional(),
  cpf: z.string().min(11, 'CPF inválido').max(14, 'CPF inválido').refine(cpf => /^\d{11}$/.test(cpf) || /^\d{3}\.\d{3}\.\d{3}-\d{2}$/.test(cpf), {
    message: 'CPF inválido',
  }),
  profile: z.enum(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'STUDENT', 'BANKING', 'LIBRARY']).optional(),
  siape: z.string().optional(),
  course: z.string().min(1, 'Curso é obrigatório').refine(value => value !== 'Selecione um curso', {
    message: 'Selecione um curso válido',
  }),
}).superRefine((data, ctx) => {
  if (!data.profile) {
    ctx.addIssue({
      path: ['profile'],
      code: z.ZodIssueCode.custom,
      message: 'A seleção de um perfil é obrigatória.'
    });
    return;
  }

  // Se o perfil NÃO FOR 'BANKING' E o email NÃO terminar com '.ifpe.edu.br'
  if (data.profile !== 'BANKING' && !data.email.endsWith('.ifpe.edu.br')) {
    ctx.addIssue({
      path: ['email'],
      code: z.ZodIssueCode.custom,
      message: 'O email deve pertencer ao domínio do IFPE (@*.ifpe.edu.br)',
    });
  }

  // Se o perfil for 'COORDINATOR', 'SUPERVISOR' ou 'ADVISOR', o campo siape é obrigatório
  const profilesThatNeedSiape = ['COORDINATOR', 'SUPERVISOR', 'ADVISOR'];
  if (profilesThatNeedSiape.includes(data.profile) && (!data.siape || data.siape.trim().length < 3)) {
    ctx.addIssue({
      path: ['siape'],
      code: z.ZodIssueCode.custom,
      message: 'SIAPE é obrigatório para o perfil selecionado.'
    });
  }

  // Se o perfil for 'STUDENT', o campo matrícula é obrigatório
  if (data.profile === 'STUDENT' && !data.registration) {
    ctx.addIssue({
      path: ['registration'],
      code: z.ZodIssueCode.custom,
      message: 'Matrícula é obrigatória'
    });
  }
});

export type NewUserSchemaSchemaType = z.infer<typeof newUserSchema>;