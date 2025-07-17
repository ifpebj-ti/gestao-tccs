'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { RegisterBankingSchemaType } from '@/app/schemas/registerBankingSchema';
import { faInfoCircle } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

interface Member {
  id: number;
  name: string;
}

interface BankingInfo {
  nameInternal: string;
  emailInternal: string;
  nameExternal: string;
  emailExternal: string;
}

interface BankingInfoSectionProps {
  bankingData: BankingInfo | null;
  canRegister?: boolean;
  isFormVisible?: boolean;
  onCancel?: () => void;
  form?: UseFormReturn<RegisterBankingSchemaType>;
  onSubmit?: SubmitHandler<RegisterBankingSchemaType>;
  allBankingMembers?: Member[];
}

export function BankingInfoSection({
  bankingData,
  canRegister = false,
  isFormVisible = false,
  onCancel = () => {},
  form,
  onSubmit,
  allBankingMembers = []
}: BankingInfoSectionProps) {
  const hasBanking =
    bankingData && (bankingData.nameInternal || bankingData.nameExternal);

  // Desestruturação segura, caso 'form' seja undefined
  const { register, handleSubmit, watch, formState } = form || {};
  const { errors, isSubmitting } = formState || {};

  const selectedInternalId = watch?.('idInternalBanking');
  const selectedExternalId = watch?.('idExternalBanking');

  const internalOptions = allBankingMembers.filter(
    (member) => member.id !== selectedExternalId
  );
  const externalOptions = allBankingMembers.filter(
    (member) => member.id !== selectedInternalId
  );

  return (
    <section>
      <h2 className="text-lg font-extrabold uppercase">Informações da Banca</h2>

      {hasBanking ? (
        <div className="grid md:grid-cols-2 gap-4 mt-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Membro Interno</Label>
            <Input
              value={bankingData.nameInternal || 'Não definido'}
              readOnly
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Email Interno</Label>
            <Input
              value={bankingData.emailInternal || 'Não definido'}
              readOnly
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Membro Externo</Label>
            <Input
              value={bankingData.nameExternal || 'Não definido'}
              readOnly
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Email Externo</Label>
            <Input
              value={bankingData.emailExternal || 'Não definido'}
              readOnly
            />
          </div>
        </div>
      ) : isFormVisible && canRegister && form && onSubmit && handleSubmit ? (
        <form
          onSubmit={handleSubmit(onSubmit)}
          className="flex flex-col gap-4 mt-4"
        >
          <div className="flex items-center gap-2 p-3 text-sm text-blue-800 rounded-lg bg-blue-50">
            <FontAwesomeIcon icon={faInfoCircle} className="h-5 w-5" />
            <div>
              <span className="font-semibold">Atenção:</span> Para que um
              usuário participe de uma banca, ele deve ter realizado o primeiro
              acesso ao sistema.
            </div>
          </div>
          <div className="grid md:grid-cols-2 gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="idInternalBanking">
                Membro Interno
              </Label>
              <select
                id="idInternalBanking"
                className="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                {...register?.('idInternalBanking', { valueAsNumber: true })}
              >
                <option value={0}>Selecione um membro interno</option>
                {internalOptions.map((member) => (
                  <option key={member.id} value={member.id}>
                    {member.name}
                  </option>
                ))}
              </select>
              {errors?.idInternalBanking && (
                <p className="text-sm text-red-600">
                  {errors.idInternalBanking.message}
                </p>
              )}
            </div>

            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="idExternalBanking">
                Membro Externo
              </Label>
              <select
                id="idExternalBanking"
                className="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                {...register?.('idExternalBanking', { valueAsNumber: true })}
              >
                <option value={0}>Selecione um membro externo</option>
                {externalOptions.map((member) => (
                  <option key={member.id} value={member.id}>
                    {member.name}
                  </option>
                ))}
              </select>
              {errors?.idExternalBanking && (
                <p className="text-sm text-red-600">
                  {errors.idExternalBanking.message}
                </p>
              )}
            </div>
          </div>
          <div className="flex gap-2 self-end">
            <Button type="button" variant="ghost" onClick={onCancel}>
              Cancelar
            </Button>
            <Button type="submit" className="md:w-fit" disabled={isSubmitting}>
              {isSubmitting ? 'Salvando...' : 'Salvar Banca'}
            </Button>
          </div>
        </form>
      ) : (
        <p className="text-gray-600 italic mt-4">
          Aguardando cadastro da banca.
        </p>
      )}
    </section>
  );
}
