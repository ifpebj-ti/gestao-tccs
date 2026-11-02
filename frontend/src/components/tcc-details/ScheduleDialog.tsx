'use client';

import { Controller, SubmitHandler, UseFormReturn } from 'react-hook-form';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog';
import { ScheduleSchemaType } from '@/app/schemas/scheduleSchema';

interface InternalExaminer {
  id: number;
  name: string;
  email: string;
  profile: string;
}

interface ScheduleDialogProps {
  isOpen: boolean;
  hasSchedule: boolean;
  onOpenChange: (open: boolean) => void;
  form: UseFormReturn<ScheduleSchemaType>;
  onSubmit: SubmitHandler<ScheduleSchemaType>;
  internalExaminers: InternalExaminer[];
}

export function ScheduleDialog({
  isOpen,
  hasSchedule,
  onOpenChange,
  form,
  onSubmit,
  internalExaminers
}: ScheduleDialogProps) {
  const { register, control, handleSubmit, watch, setValue, getValues, formState } = form;
  const members = watch('bankingMembers') ?? [];

  const addMember = () => {
    setValue('bankingMembers', [
      ...(getValues('bankingMembers') ?? []),
      { name: '', email: '', role: 'Examinador Externo', userId: null, memberType: 'EXTERNAL' }
    ], { shouldDirty: true });
  };

  const removeMember = (index: number) => {
    setValue('bankingMembers', members.filter((_, memberIndex) => memberIndex !== index), {
      shouldDirty: true,
      shouldValidate: true
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] max-w-[95vw] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>{hasSchedule ? 'Editar apresentação' : 'Agendar apresentação'}</DialogTitle>
          <DialogDescription>
            Informe os dados da apresentação e os membros da banca examinadora.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-1.5">
              <Label htmlFor="scheduleDate">Data</Label>
              <Input id="scheduleDate" type="date" {...register('scheduleDate')} />
              {formState.errors.scheduleDate && <p className="text-sm text-red-600">{formState.errors.scheduleDate.message}</p>}
            </div>
            <div className="grid gap-1.5">
              <Label htmlFor="scheduleTime">Hora</Label>
              <Input id="scheduleTime" type="time" {...register('scheduleTime')} />
              {formState.errors.scheduleTime && <p className="text-sm text-red-600">{formState.errors.scheduleTime.message}</p>}
            </div>
            <div className="grid gap-1.5 md:col-span-2">
              <Label htmlFor="scheduleLocation">Local ou link</Label>
              <Input id="scheduleLocation" placeholder="Ex.: Sala 20 ou link do Meet" {...register('scheduleLocation')} />
              {formState.errors.scheduleLocation && <p className="text-sm text-red-600">{formState.errors.scheduleLocation.message}</p>}
            </div>
          </div>

          <div className="border-t pt-4">
            <div className="mb-3 flex items-center justify-between gap-3">
              <Label className="text-base font-semibold">Membros da banca examinadora</Label>
              <Button type="button" variant="outline" size="sm" onClick={addMember}>+ Adicionar examinador</Button>
            </div>

            {members.length === 0 ? (
              <p className="rounded-md border border-dashed p-4 text-center text-sm text-gray-500">
                Nenhum examinador adicional. O orientador será incluído automaticamente.
              </p>
            ) : (
              <div className="flex flex-col gap-3">
                {members.map((member, index) => (
                  <div key={`${index}-${member.userId ?? member.email}`} className="relative grid gap-3 rounded-md border bg-gray-50 p-4 md:grid-cols-2">
                    <Button
                      type="button"
                      variant="destructive"
                      size="sm"
                      className="absolute right-2 top-2 h-7 w-7 p-0"
                      aria-label="Remover examinador"
                      onClick={() => removeMember(index)}
                    >
                      <X className="h-4 w-4" />
                    </Button>

                    <div className="grid gap-1.5 pr-8 md:col-span-2">
                      <Label htmlFor={`member-type-${index}`}>Tipo de examinador</Label>
                      <Controller
                        control={control}
                        name={`bankingMembers.${index}.memberType`}
                        render={({ field }) => (
                          <select
                            {...field}
                            id={`member-type-${index}`}
                            className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm"
                            onChange={event => {
                              const nextType = event.target.value as 'INTERNAL' | 'EXTERNAL';
                              field.onChange(nextType);
                              setValue(`bankingMembers.${index}.userId`, null, { shouldDirty: true });
                              setValue(`bankingMembers.${index}.name`, '', { shouldDirty: true, shouldValidate: true });
                              setValue(`bankingMembers.${index}.email`, '', { shouldDirty: true, shouldValidate: true });
                              setValue(`bankingMembers.${index}.role`, nextType === 'INTERNAL' ? 'Examinador Interno' : 'Examinador Externo', { shouldDirty: true, shouldValidate: true });
                            }}
                          >
                            <option value="EXTERNAL">Examinador externo</option>
                            <option value="INTERNAL">Examinador interno cadastrado</option>
                          </select>
                        )}
                      />
                    </div>

                        {(member.memberType ?? 'EXTERNAL') === 'INTERNAL' ? (
                      <div className="grid gap-1.5 md:col-span-2">
                        <Label htmlFor={`internal-examiner-${index}`}>Orientador cadastrado</Label>
                        <Controller
                          control={control}
                          name={`bankingMembers.${index}.userId`}
                          render={({ field }) => (
                            <select
                              id={`internal-examiner-${index}`}
                              value={field.value ?? ''}
                              onBlur={field.onBlur}
                              onChange={event => {
                                const selectedId = Number(event.target.value);
                                const selected = internalExaminers.find(user => user.id === selectedId);
                                field.onChange(selected?.id ?? null);
                                setValue(`bankingMembers.${index}.name`, selected?.name ?? '', { shouldDirty: true, shouldValidate: true });
                                setValue(`bankingMembers.${index}.email`, selected?.email ?? '', { shouldDirty: true, shouldValidate: true });
                                setValue(`bankingMembers.${index}.role`, 'Examinador Interno', { shouldDirty: true, shouldValidate: true });
                              }}
                              className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm"
                            >
                              <option value="">Selecione um orientador</option>
                              {internalExaminers.map(user => (
                                <option key={user.id} value={user.id}>
                                  {user.name} ({user.email})
                                </option>
                              ))}
                            </select>
                          )}
                        />
                        {formState.errors.bankingMembers?.[index]?.userId && <p className="text-sm text-red-600">Selecione um usuário cadastrado.</p>}
                        {internalExaminers.length === 0 && <p className="text-sm text-amber-700">Nenhum orientador cadastrado foi encontrado.</p>}
                      </div>
                    ) : (
                      <>
                        <div className="grid gap-1.5">
                          <Label htmlFor={`member-name-${index}`}>Nome</Label>
                          <Input id={`member-name-${index}`} placeholder="Nome do examinador" {...register(`bankingMembers.${index}.name`)} />
                          {formState.errors.bankingMembers?.[index]?.name && <p className="text-sm text-red-600">{formState.errors.bankingMembers[index]?.name?.message}</p>}
                        </div>
                        <div className="grid gap-1.5">
                          <Label htmlFor={`member-email-${index}`}>E-mail</Label>
                          <Input id={`member-email-${index}`} type="email" placeholder="E-mail" {...register(`bankingMembers.${index}.email`)} />
                          {formState.errors.bankingMembers?.[index]?.email && <p className="text-sm text-red-600">{formState.errors.bankingMembers[index]?.email?.message}</p>}
                        </div>
                      </>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={formState.isSubmitting}>
              {formState.isSubmitting ? 'Salvando...' : 'Salvar agendamento'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
