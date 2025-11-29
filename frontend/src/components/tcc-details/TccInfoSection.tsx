'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { ScheduleSchemaType } from '@/app/schemas/scheduleSchema';
import { EditTccSchemaType } from '@/app/schemas/editTccSchema';
import { useEffect } from 'react';
import { Pencil, X, Check, Calendar } from 'lucide-react';

interface TccInfoSectionProps {
  infoTcc: {
    title: string;
    summary: string;
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
  };

  isEditingInfo?: boolean;
  onToggleEditInfo?: (val: boolean) => void;
  editForm?: UseFormReturn<EditTccSchemaType>;
  onEditSubmit?: SubmitHandler<EditTccSchemaType>;

  isScheduleFormVisible?: boolean;
  onOpenSchedule?: () => void;
  onScheduleCancel?: () => void;
  scheduleForm?: UseFormReturn<ScheduleSchemaType>;
  onScheduleSubmit?: SubmitHandler<ScheduleSchemaType>;
}

export function TccInfoSection({
  infoTcc,
  isEditingInfo = false,
  onToggleEditInfo,
  editForm,
  onEditSubmit,
  isScheduleFormVisible = false,
  onOpenSchedule,
  onScheduleCancel = () => {},
  scheduleForm,
  onScheduleSubmit
}: TccInfoSectionProps) {
  const canEdit = !!editForm && !!onToggleEditInfo && !!onEditSubmit;

  const registerEdit = editForm?.register;
  const handleSubmitEdit = editForm?.handleSubmit;
  const formStateEdit = editForm?.formState;
  const resetEdit = editForm?.reset;

  const isSubmittingEdit = formStateEdit?.isSubmitting;
  const errorsEdit = formStateEdit?.errors;

  const {
    register: registerSchedule,
    handleSubmit: handleSubmitSchedule,
    formState: formStateSchedule,
    reset: resetSchedule
  } = scheduleForm || {};
  const { errors: errorsSchedule, isSubmitting: isSubmittingSchedule } =
    formStateSchedule || {};

  const hasSchedule = !!infoTcc.presentationDate;

  useEffect(() => {
    if (hasSchedule && scheduleForm && resetSchedule) {
      resetSchedule({
        scheduleDate: infoTcc.presentationDate ?? '',
        scheduleTime: infoTcc.presentationTime ?? '',
        scheduleLocation: infoTcc.presentationLocation
      });
    }
  }, [hasSchedule, infoTcc, resetSchedule, scheduleForm]);

  const handleCancelEdit = () => {
    if (resetEdit && onToggleEditInfo) {
      resetEdit({ title: infoTcc.title ?? '', summary: infoTcc.summary ?? '' });
      onToggleEditInfo(false);
    }
  };

  return (
    <section>
      <div className="flex justify-between items-center">
        <h2 className="text-lg font-extrabold uppercase">Informações do TCC</h2>

        {canEdit &&
          (!isEditingInfo ? (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onToggleEditInfo && onToggleEditInfo(true)}
            >
              <Pencil className="w-4 h-4 mr-2" />
              Editar
            </Button>
          ) : (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleCancelEdit}
              className="text-red-500 hover:text-red-700 hover:bg-red-50"
            >
              <X className="w-4 h-4 mr-2" />
              Cancelar Edição
            </Button>
          ))}
      </div>

      <form
        id="edit-tcc-form"
        onSubmit={
          handleSubmitEdit && onEditSubmit
            ? handleSubmitEdit(onEditSubmit)
            : undefined
        }
        className="grid md:grid-cols-2 gap-4 mt-4"
      >
        <div className="grid items-center gap-1.5 md:col-span-2">
          <Label className="font-semibold" htmlFor="tcc-title">
            Título da proposta
            {isEditingInfo && <span className="text-red-500">*</span>}
          </Label>

          {isEditingInfo && registerEdit ? (
            <div className="flex flex-col gap-1">
              <Input
                key="title-edit"
                id="tcc-title"
                errorText={errorsEdit?.title?.message || ''}
                {...registerEdit('title')}
              />
            </div>
          ) : (
            <Input
              key="title-view"
              id="tcc-title-view"
              readOnly
              value={infoTcc.title || ''}
            />
          )}
        </div>

        <div className="grid items-center gap-1.5 md:col-span-2">
          <Label className="font-semibold" htmlFor="tcc-summary">
            Resumo da proposta
            {isEditingInfo && <span className="text-red-500">*</span>}
          </Label>
          <div className="relative">
            {isEditingInfo && registerEdit ? (
              <div className="flex flex-col gap-1">
                <Input
                  key="summary-edit"
                  id="tcc-summary"
                  errorText={errorsEdit?.summary?.message || ''}
                  {...registerEdit('summary')}
                />
              </div>
            ) : (
              <Input
                key="summary-view"
                id="tcc-summary-view"
                readOnly
                value={infoTcc.summary || ''}
              />
            )}
          </div>
        </div>

        {isEditingInfo && (
          <div className="md:col-span-2 flex justify-end mt-2">
            <Button type="submit" disabled={isSubmittingEdit}>
              {isSubmittingEdit ? (
                'Salvando...'
              ) : (
                <>
                  <Check className="w-4 h-4 mr-2" /> Salvar Alterações
                </>
              )}
            </Button>
          </div>
        )}
      </form>

      {/* Área de Agendamento */}
      <div className="mt-8 border-t pt-4">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-lg font-extrabold uppercase">Apresentação</h2>

          {!isScheduleFormVisible && onOpenSchedule && (
            <Button variant="ghost" size="sm" onClick={onOpenSchedule}>
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
            <div className="flex gap-2 self-end">
              <Button type="button" variant="ghost" onClick={onScheduleCancel}>
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
          <p className="text-sm text-gray-500">
            Nenhuma apresentação agendada para este TCC.
          </p>
        )}
      </div>
    </section>
  );
}
