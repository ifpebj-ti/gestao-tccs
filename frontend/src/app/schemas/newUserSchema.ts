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
  }),
  // siape, caso profile = 'COORDINATOR' | 'SUPERVISOR' | 'ADVISOR'
  siape: z.string().optional().refine(value => value === undefined || value.length >= 3, {
    message: 'SIAPE inválido',
  }),
  course: z.string().min(1, 'Curso é obrigatório').refine(value => value !== 'Selecione um curso', {
    message: 'Selecione um curso válido',
  }),
});

export type NewUserSchemaSchemaType = z.infer<typeof newUserSchema>;
