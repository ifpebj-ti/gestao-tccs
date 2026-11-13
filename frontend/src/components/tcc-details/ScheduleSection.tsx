'use client';

import { SectionTitle } from '@/components/layout/SectionTitle';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import {
  Pencil,
  Calendar,
  Check,
  Award,
  CheckCircle2,
  XCircle
} from 'lucide-react';
import { faEnvelope } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { AddToCalendarButton } from '@/components/calendar/AddToCalendarButton';

interface ScheduleSectionProps {
  infoTcc: {
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
    finalGrade?: number | null;
    finalOpinion?: string | null;
  };
  tccId?: number | string;
  tccTitle?: string;
  tccSummary?: string | null;
  isScheduleFormVisible?: boolean;
  onOpenSchedule?: () => void;
  canSchedule?: boolean;
  onSendScheduleEmail?: () => void;
  isCompleted?: boolean;
  onConcludePresentation?: () => void;
  isConcluding?: boolean;
}

export function ScheduleSection({
  infoTcc,
  tccId,
  tccTitle,
  tccSummary,
  isScheduleFormVisible = false,
  onOpenSchedule,
  canSchedule = false,
  onSendScheduleEmail,
  isCompleted = false,
  onConcludePresentation,
  isConcluding
}: ScheduleSectionProps) {
  const hasSchedule = !!infoTcc.presentationDate;
  const isApproved =
    infoTcc.finalOpinion?.toLowerCase().includes('aprovado') ||
    (infoTcc.finalGrade !== undefined &&
      infoTcc.finalGrade !== null &&
      infoTcc.finalGrade >= 7);

  return (
    <section className="mt-4 border-t border-[#ECEAE2] py-6">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
        <div>
          <div className="flex items-center gap-3">
            <SectionTitle>
              {isCompleted
                ? 'Apresentação & Resultado da Banca'
                : 'Apresentação'}
            </SectionTitle>
            {isCompleted && (
              <span className="flex items-center text-sm font-semibold bg-green-100 text-green-800 px-3 py-1 rounded-full">
                <CheckCircle2 className="w-4 h-4 mr-1 text-green-600" />{' '}
                Apresentação Concluída
              </span>
            )}
          </div>
          <p className="text-sm text-gray-500 mt-1">
            {isCompleted
              ? 'Consulte o parecer final, nota da banca e informações da defesa.'
              : canSchedule
                ? 'Gerencie a data, local e os examinadores da banca do TCC.'
                : 'Consulte os dados da apresentação. O gerenciamento da banca fica na área Banca.'}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2 w-full md:w-auto">
          {hasSchedule && tccId && !isCompleted && (
            <AddToCalendarButton
              eventData={{
                tccId,
                title: tccTitle || 'TCC',
                summary: tccSummary,
                location: infoTcc.presentationLocation,
                presentationDate: infoTcc.presentationDate,
                presentationTime: infoTcc.presentationTime
              }}
              variant="outline"
              size="default"
            />
          )}

          {!isCompleted &&
            canSchedule &&
            !isScheduleFormVisible &&
            onOpenSchedule && (
              <>
                {hasSchedule && onConcludePresentation && (
                  <Button
                    variant="default"
                    className="w-full md:w-auto bg-green-600 hover:bg-green-700 text-white"
                    onClick={onConcludePresentation}
                    disabled={isConcluding}
                  >
                    <Check className="w-4 h-4 mr-2" />{' '}
                    {isConcluding ? 'Concluindo...' : 'Concluir Apresentação'}
                  </Button>
                )}
                {hasSchedule && onSendScheduleEmail && (
                  <Button
                    variant="outline"
                    size="default"
                    className="w-full md:w-auto"
                    onClick={onSendScheduleEmail}
                  >
                    <FontAwesomeIcon icon={faEnvelope} className="mr-2" /> Reenviar
                    Comunicados
                  </Button>
                )}
                <Button
                  variant="default"
                  size="default"
                  className="w-full md:w-auto"
                  onClick={onOpenSchedule}
                >
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
              </>
            )}
        </div>
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
                isApproved
                  ? 'bg-emerald-100 text-emerald-600'
                  : 'bg-red-100 text-red-600'
              }`}
            >
              {isApproved ? (
                <Award className="w-8 h-8" />
              ) : (
                <XCircle className="w-8 h-8" />
              )}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span
                  className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${
                    isApproved
                      ? 'bg-emerald-200 text-emerald-800'
                      : 'bg-red-200 text-red-800'
                  }`}
                >
                  {infoTcc.finalOpinion ||
                    (isApproved ? 'Aprovado' : 'Reprovado')}
                </span>
                <span className="text-xs text-gray-500 font-medium">
                  Parecer Final
                </span>
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
              {infoTcc.finalGrade !== undefined && infoTcc.finalGrade !== null
                ? 'Média da Banca'
                : 'Status'}
            </span>
            <span
              className={`text-3xl font-black mt-1 ${
                isApproved ? 'text-emerald-600' : 'text-red-600'
              }`}
            >
              {infoTcc.finalGrade !== undefined && infoTcc.finalGrade !== null
                ? infoTcc.finalGrade.toFixed(1).replace('.', ',')
                : isApproved
                  ? 'Aprovado'
                  : 'Reprovado'}
            </span>
          </div>
        </div>
      )}

      {hasSchedule ? (
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
            {isCompleted
              ? 'Apresentação concluída.'
              : 'Nenhuma apresentação agendada para este TCC ainda.'}
          </p>
        </div>
      )}
    </section>
  );
}
