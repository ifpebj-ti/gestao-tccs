'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { ScheduleSchemaType } from '@/app/schemas/scheduleSchema';
import { useEffect } from 'react';

interface TccInfoSectionProps {
  infoTcc: {
    title: string;
    summary: string;
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
  };
  isScheduleFormVisible: boolean;
  onScheduleCancel: () => void;
  scheduleForm: UseFormReturn<ScheduleSchemaType>;
  onScheduleSubmit: SubmitHandler<ScheduleSchemaType>;
}

export function TccInfoSection({
  infoTcc,
  isScheduleFormVisible,
  onScheduleCancel,
  scheduleForm,
  onScheduleSubmit
}: TccInfoSectionProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset
  } = scheduleForm;
  const hasSchedule = infoTcc.presentationDate;

  useEffect(() => {
    if (hasSchedule) {
      reset({
        scheduleDate: infoTcc.presentationDate ?? '',
        scheduleTime: infoTcc.presentationTime ?? '',
        scheduleLocation: infoTcc.presentationLocation ?? ''
      });
    }
  }, [hasSchedule, infoTcc, reset]);

  return (
    <section>
      <h2 className="text-lg font-extrabold uppercase">Informações do TCC</h2>
      <div className="grid md:grid-cols-2 gap-4 mt-4">
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Título da proposta</Label>
          <Input value={infoTcc.title} readOnly />
        </div>
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Resumo da proposta</Label>
          <Input value={infoTcc.summary} readOnly />
        </div>

        {isScheduleFormVisible ? (
          <form
            onSubmit={handleSubmit(onScheduleSubmit)}
            className="md:col-span-2 flex flex-col gap-4 mt-4 border-t pt-4"
          >
            <h3 className="font-semibold text-md text-gray-800">
              Agendar Apresentação
            </h3>
            <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="grid gap-1.5">
                <Label htmlFor="scheduleDate">Data</Label>
                <Input
                  id="scheduleDate"
                  type="date"
                  {...register('scheduleDate')}
                />
                {errors.scheduleDate && (
                  <p className="text-sm text-red-600">
                    {errors.scheduleDate.message}
                  </p>
                )}
              </div>
              <div className="grid gap-1.5">
                <Label htmlFor="scheduleTime">Hora</Label>
                <Input
                  id="scheduleTime"
                  type="time"
                  {...register('scheduleTime')}
                />
                {errors.scheduleTime && (
                  <p className="text-sm text-red-600">
                    {errors.scheduleTime.message}
                  </p>
                )}
              </div>
              <div className="grid gap-1.5 lg:col-span-1">
                <Label htmlFor="scheduleLocation">Local/Link</Label>
                <Input
                  id="scheduleLocation"
                  placeholder="Ex: Sala 20 ou Link do Meet"
                  {...register('scheduleLocation')}
                />
                {errors.scheduleLocation && (
                  <p className="text-sm text-red-600">
                    {errors.scheduleLocation.message}
                  </p>
                )}
              </div>
            </div>
            <div className="flex gap-2 self-end">
              <Button type="button" variant="ghost" onClick={onScheduleCancel}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Salvando...' : 'Salvar Agendamento'}
              </Button>
            </div>
          </form>
        ) : hasSchedule ? (
          <>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Data da apresentação</Label>
              <Input value={infoTcc.presentationDate || ''} readOnly />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Hora da apresentação</Label>
              <Input value={infoTcc.presentationTime || ''} readOnly />
            </div>
            <div className="grid items-center gap-1.5 md:col-span-2">
              <Label className="font-semibold">Local da apresentação</Label>
              <Input value={infoTcc.presentationLocation} readOnly />
            </div>
          </>
        ) : null}
      </div>
    </section>
  );
}
