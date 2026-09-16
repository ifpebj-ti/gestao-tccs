'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { ScheduleSchemaType } from '@/app/schemas/scheduleSchema';
import { Pencil, X, Calendar } from 'lucide-react';
import { faEnvelope } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

interface ScheduleSectionProps {
  infoTcc: {
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
  };
  isScheduleFormVisible: boolean;
  onOpenSchedule?: () => void;
  onScheduleCancel: () => void;
  scheduleForm?: UseFormReturn<ScheduleSchemaType>;
  onScheduleSubmit?: SubmitHandler<ScheduleSchemaType>;
  canSchedule: boolean;
  onSendScheduleEmail?: () => void;
}

export function ScheduleSection({
  infoTcc,
  isScheduleFormVisible,
  onOpenSchedule,
  onScheduleCancel,
  scheduleForm,
  onScheduleSubmit,
  canSchedule,
  onSendScheduleEmail
}: ScheduleSectionProps) {
  const hasSchedule = !!infoTcc.presentationDate;
  
  const {
    register: registerSchedule,
    handleSubmit: handleSubmitSchedule,
    formState: formStateSchedule,
  } = scheduleForm || {};
  
  const { errors: errorsSchedule, isSubmitting: isSubmittingSchedule } =
    formStateSchedule || {};

  return (
    <section className="mt-4 border p-6 rounded-lg bg-white shadow-sm">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
        <div>
          <h2 className="text-xl font-extrabold uppercase text-gray-800">Apresentação</h2>
          <p className="text-sm text-gray-500 mt-1">Gerencie a data, local e os examinadores da banca do TCC.</p>
        </div>

        {canSchedule && !isScheduleFormVisible && onOpenSchedule && (
          <div className="flex gap-2 w-full md:w-auto">
            {hasSchedule && onSendScheduleEmail && (
              <Button variant="outline" size="default" className="w-full md:w-auto" onClick={onSendScheduleEmail}>
                <FontAwesomeIcon icon={faEnvelope} className="mr-2" /> Enviar Agenda
              </Button>
            )}
            <Button variant="default" size="default" className="w-full md:w-auto" onClick={onOpenSchedule}>
              {hasSchedule ? (
                <>
                  <Pencil className="w-4 h-4 mr-2" /> Editar Agendamento
                </>
              ) : (
                <>
                  <Calendar className="w-4 h-4 mr-2" /> Agendar Apresentação
                </>
              )}
            </Button>
          </div>
        )}
      </div>

      {isScheduleFormVisible &&
      scheduleForm &&
      handleSubmitSchedule &&
      onScheduleSubmit ? (
        <form
          onSubmit={handleSubmitSchedule(onScheduleSubmit)}
          className="flex flex-col gap-4"
        >
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div className="grid gap-1.5">
              <Label htmlFor="scheduleDate">Data</Label>
              <Input
                id="scheduleDate"
                type="date"
                {...registerSchedule?.('scheduleDate')}
              />
              {errorsSchedule?.scheduleDate && (
                <p className="text-sm text-red-600">
                  {errorsSchedule.scheduleDate.message}
                </p>
              )}
            </div>
            <div className="grid gap-1.5">
              <Label htmlFor="scheduleTime">Hora</Label>
              <Input
                id="scheduleTime"
                type="time"
                {...registerSchedule?.('scheduleTime')}
              />
              {errorsSchedule?.scheduleTime && (
                <p className="text-sm text-red-600">
                  {errorsSchedule.scheduleTime.message}
                </p>
              )}
            </div>
            <div className="grid gap-1.5 lg:col-span-1">
              <Label htmlFor="scheduleLocation">Local/Link</Label>
              <Input
                id="scheduleLocation"
                placeholder="Ex: Sala 20 ou Link do Meet"
                {...registerSchedule?.('scheduleLocation')}
              />
              {errorsSchedule?.scheduleLocation && (
                <p className="text-sm text-red-600">
                  {errorsSchedule.scheduleLocation.message}
                </p>
              )}
            </div>
          </div>

          {/* Início Membros da Banca */}
          <div className="mt-4 border-t pt-4">
            <div className="flex justify-between items-center mb-2">
              <Label className="text-base font-semibold">Membros da Banca (Convidados)</Label>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  const current = scheduleForm.getValues('bankingMembers') || [];
                  scheduleForm.setValue('bankingMembers', [
                    ...current,
                    { name: '', email: '', role: '' }
                  ]);
                }}
              >
                + Adicionar Membro
              </Button>
            </div>
            
            <div className="flex flex-col gap-4">
              {scheduleForm.watch('bankingMembers')?.map((member, index) => (
                <div key={index} className="grid md:grid-cols-12 gap-2 items-end border p-3 rounded-md bg-gray-50 relative">
                  <Button 
                    type="button" 
                    variant="ghost" 
                    size="sm" 
                    className="absolute top-2 right-2 text-red-500 hover:text-red-700 h-6 w-6 p-0"
                    onClick={() => {
                      const current = scheduleForm.getValues('bankingMembers') || [];
                      scheduleForm.setValue('bankingMembers', current.filter((_, i) => i !== index));
                    }}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                  <div className="grid gap-1.5 md:col-span-4">
                    <Label>Nome</Label>
                    <Input
                      placeholder="Nome do avaliador"
                      {...registerSchedule?.(`bankingMembers.${index}.name` as const)}
                    />
                    {errorsSchedule?.bankingMembers?.[index]?.name && (
                      <p className="text-sm text-red-600">{errorsSchedule.bankingMembers[index]?.name?.message}</p>
                    )}
                  </div>
                  <div className="grid gap-1.5 md:col-span-4">
                    <Label>E-mail</Label>
                    <Input
                      type="email"
                      placeholder="E-mail"
                      {...registerSchedule?.(`bankingMembers.${index}.email` as const)}
                    />
                    {errorsSchedule?.bankingMembers?.[index]?.email && (
                      <p className="text-sm text-red-600">{errorsSchedule.bankingMembers[index]?.email?.message}</p>
                    )}
                  </div>
                  <div className="grid gap-1.5 md:col-span-4">
                    <Label>Instituição/Papel</Label>
                    <Input
                      placeholder="Ex: IFPE - Avaliador Externo"
                      {...registerSchedule?.(`bankingMembers.${index}.role` as const)}
                    />
                    {errorsSchedule?.bankingMembers?.[index]?.role && (
                      <p className="text-sm text-red-600">{errorsSchedule.bankingMembers[index]?.role?.message}</p>
                    )}
                  </div>
                </div>
              ))}
              {(!scheduleForm.watch('bankingMembers') || scheduleForm.watch('bankingMembers')?.length === 0) && (
                <p className="text-sm text-gray-500 italic">Nenhum membro adicionado. Apenas o orientador estará na banca por padrão se ninguém for adicionado.</p>
              )}
            </div>
          </div>
          {/* Fim Membros da Banca */}

          <div className="flex gap-2 self-end mt-4">
            <Button type="button" variant="outline" onClick={onScheduleCancel}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmittingSchedule}>
              {isSubmittingSchedule ? 'Salvando...' : 'Salvar Agendamento'}
            </Button>
          </div>
        </form>
      ) : hasSchedule ? (
        <div className="grid md:grid-cols-2 gap-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Data da apresentação</Label>
            <Input
              value={infoTcc.presentationDate || ''}
              readOnly
              className="bg-gray-50"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Hora da apresentação</Label>
            <Input
              value={infoTcc.presentationTime || ''}
              readOnly
              className="bg-gray-50"
            />
          </div>
          <div className="grid items-center gap-1.5 md:col-span-2">
            <Label className="font-semibold">Local da apresentação</Label>
            <Input
              value={infoTcc.presentationLocation}
              readOnly
              className="bg-gray-50"
            />
          </div>
        </div>
      ) : (
        <div className="flex items-center justify-center p-6 bg-gray-50 rounded-lg border border-dashed">
          <p className="text-gray-500 text-center">
            Nenhuma apresentação agendada para este TCC ainda.
          </p>
        </div>
      )}
    </section>
  );
}
