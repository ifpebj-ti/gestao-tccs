'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { ScheduleSchemaType } from '@/app/schemas/scheduleSchema';
import { Pencil, X, Calendar, Check, Award, CheckCircle2, XCircle } from 'lucide-react';
import { faEnvelope } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

interface ScheduleSectionProps {
  infoTcc: {
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
    finalGrade?: number | null;
    finalOpinion?: string | null;
  };
  isScheduleFormVisible?: boolean;
  onOpenSchedule?: () => void;
  onScheduleCancel?: () => void;
  scheduleForm?: UseFormReturn<ScheduleSchemaType>;
  onScheduleSubmit?: SubmitHandler<ScheduleSchemaType>;
  canSchedule?: boolean;
  onSendScheduleEmail?: () => void;
  isCompleted?: boolean;
  onConcludePresentation?: () => void;
  isConcluding?: boolean;
}

export function ScheduleSection({
  infoTcc,
  isScheduleFormVisible = false,
  onOpenSchedule,
  onScheduleCancel = () => {},
  scheduleForm,
  onScheduleSubmit,
  canSchedule = false,
  onSendScheduleEmail,
  isCompleted = false,
  onConcludePresentation,
  isConcluding
}: ScheduleSectionProps) {
  const hasSchedule = !!infoTcc.presentationDate;
  const isApproved =
    infoTcc.finalOpinion?.toLowerCase().includes('aprovado') ||
    (infoTcc.finalGrade !== undefined && infoTcc.finalGrade !== null && infoTcc.finalGrade >= 7);
  
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
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-extrabold uppercase text-gray-800">
              {isCompleted ? 'Apresentação & Resultado da Banca' : 'Apresentação'}
            </h2>
            {isCompleted && (
              <span className="flex items-center text-sm font-semibold bg-green-100 text-green-800 px-3 py-1 rounded-full">
                <CheckCircle2 className="w-4 h-4 mr-1 text-green-600" /> Apresentação Concluída
              </span>
            )}
          </div>
          <p className="text-sm text-gray-500 mt-1">
            {isCompleted 
              ? 'Consulte o parecer final, nota da banca e informações da defesa.' 
              : 'Gerencie a data, local e os examinadores da banca do TCC.'}
          </p>
        </div>

        {!isCompleted && canSchedule && !isScheduleFormVisible && onOpenSchedule && (
          <div className="flex flex-col md:flex-row gap-2 w-full md:w-auto">
            {hasSchedule && onConcludePresentation && (
              <Button variant="default" className="w-full md:w-auto bg-green-600 hover:bg-green-700 text-white" onClick={onConcludePresentation} disabled={isConcluding}>
                <Check className="w-4 h-4 mr-2" /> {isConcluding ? 'Concluindo...' : 'Concluir Apresentação'}
              </Button>
            )}
            {hasSchedule && onSendScheduleEmail && (
              <Button variant="outline" size="default" className="w-full md:w-auto" onClick={onSendScheduleEmail}>
                <FontAwesomeIcon icon={faEnvelope} className="mr-2" /> Enviar Agenda
              </Button>
            )}
            <Button variant="default" size="default" className="w-full md:w-auto" onClick={onOpenSchedule}>
              {hasSchedule ? (
                <>
                  <Pencil className="w-4 h-4 mr-2" /> Inserir Examinadores
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

      {/* Card de Parecer e Nota Final da Banca quando concluído */}
      {isCompleted && (
        <div
          className={`mb-6 p-6 rounded-xl border flex flex-col md:flex-row items-start md:items-center justify-between gap-6 ${
            isApproved
              ? 'bg-gradient-to-r from-emerald-50 via-teal-50 to-green-50 border-emerald-200 text-emerald-950 shadow-sm'
              : 'bg-gradient-to-r from-red-50 via-rose-50 to-orange-50 border-red-200 text-red-950 shadow-sm'
          }`}
        >
          <div className="flex items-start gap-4">
            <div
              className={`p-3 rounded-full flex-shrink-0 ${
                isApproved ? 'bg-emerald-100 text-emerald-600' : 'bg-red-100 text-red-600'
              }`}
            >
              {isApproved ? <Award className="w-8 h-8" /> : <XCircle className="w-8 h-8" />}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span
                  className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${
                    isApproved ? 'bg-emerald-200 text-emerald-800' : 'bg-red-200 text-red-800'
                  }`}
                >
                  {infoTcc.finalOpinion || (isApproved ? 'Aprovado' : 'Reprovado')}
                </span>
                <span className="text-xs text-gray-500 font-medium">Parecer Final</span>
              </div>
              <h3 className="text-lg font-bold mt-2">
                {isApproved
                  ? 'Trabalho Aprovado pela Comissão Examinadora'
                  : 'Trabalho Reprovado pela Comissão Examinadora'}
              </h3>
              <p className="text-sm text-gray-600 mt-1 max-w-xl">
                {isApproved
                  ? 'Parabéns! Todas as avaliações da banca e do(a) orientador(a) foram registradas com sucesso e o trabalho obteve aprovação.'
                  : 'O trabalho não atingiu a média mínima institucional (7,0) para aprovação.'}
              </p>
            </div>
          </div>

          <div className="flex flex-col items-center justify-center p-4 rounded-xl bg-white border border-gray-100 shadow-sm min-w-[140px] self-stretch md:self-auto">
            <span className="text-xs font-semibold uppercase text-gray-400">
              {infoTcc.finalGrade !== undefined && infoTcc.finalGrade !== null ? 'Média da Banca' : 'Status'}
            </span>
            <span
              className={`text-3xl font-black mt-1 ${
                isApproved ? 'text-emerald-600' : 'text-red-600'
              }`}
            >
              {infoTcc.finalGrade !== undefined && infoTcc.finalGrade !== null 
                ? infoTcc.finalGrade.toFixed(1).replace('.', ',')
                : (isApproved ? 'Aprovado' : 'Reprovado')}
            </span>
          </div>
        </div>
      )}

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
            {isCompleted ? 'Apresentação concluída.' : 'Nenhuma apresentação agendada para este TCC ainda.'}
          </p>
        </div>
      )}
    </section>
  );
}
