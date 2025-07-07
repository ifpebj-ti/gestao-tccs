import { z } from 'zod';

export const registerBankingSchema = z.object({
  idInternalBanking: z.number().positive({ message: 'Selecione um membro interno.' }),
  idExternalBanking: z.number().positive({ message: 'Selecione um membro externo.' }),
});

export type RegisterBankingSchemaType = z.infer<typeof registerBankingSchema>;