'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import {
  Calendar,
  Check,
  Clock,
  Landmark,
  Mail,
  MapPin,
  Pencil,
  Presentation,
  Send,
  User,
  Users
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useTccDetails } from '@/app/hooks/useTccDetails';
import { PageState } from '@/components/layout/PageState';
import { PageTitle } from '@/components/layout/PageTitle';
import { Button } from '@/components/ui/button';
import { ScheduleDialog } from '@/components/tcc-details/ScheduleDialog';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';
import { AddToCalendarButton } from '@/components/calendar/AddToCalendarButton';

export default function BankingManagementClient() {
  const router = useRouter();
  const {
    tccData,
    loading,
    profile,
    isScheduleFormVisible,
    setIsScheduleFormVisible,
    scheduleForm,
    internalExaminers,
    handleScheduleSubmit,
    handleSendScheduleEmail,
    handleConcludePresentation,
    isConcluding,
    orientadorToken,
    setOrientadorToken
  } = useTccDetails();

  const [isConcludedLocally, setIsConcludedLocally] = useState(false);

  const isPresentationConcluded =
    isConcludedLocally ||
    tccData?.infoTcc?.status === 'COMPLETED' ||
    (tccData?.infoTcc?.finalGrade !== undefined && tccData?.infoTcc?.finalGrade !== null);

  const handleConclude = async () => {
    await handleConcludePresentation();
    setIsConcludedLocally(true);
  };

  const hasExaminers = (tccData?.infoBanking?.members || []).some(
    (m) => !m.role?.toLowerCase().includes('orientador')
  );

  if (loading) return <PageState loading>Carregando dados do TCC...</PageState>;
  if (!tccData) return <PageState>TCC não encontrado.</PageState>;

  const roles = Array.isArray(profile) ? profile : [profile];
  let currentUserId: number | null = null;
  try {
    const token = Cookies.get('token');
    if (token) currentUserId = Number(jwtDecode<{ userId: string }>(token).userId);
  } catch {
    currentUserId = null;
  }
  const canManage =
    roles.includes('ADVISOR') &&
    currentUserId !== null &&
    currentUserId === Number(tccData.advisorUserId);

  if (!canManage) {
    return (
      <div className="space-y-5">
        <div className="flex justify-end">
          <Button variant="outline" onClick={() => router.push('/banking')}>
            Voltar para Banca
          </Button>
        </div>
        <PageState>
          Somente o orientador responsável por este TCC pode gerenciar a banca.
          Examinadores podem acessar as avaliações pendentes na área Banca.
        </PageState>
      </div>
    );
  }

  const hasSchedule = !!tccData.infoTcc.presentationDate;

  // Unifica os membros da banca garantindo que o orientador esteja presente
  const members = [...(tccData.infoBanking?.members || [])];
  const hasAdvisorInBanking = members.some(
    (m) =>
      m.role?.toLowerCase().includes('orientador') ||
      (tccData.infoAdvisor?.email &&
        m.email?.toLowerCase() === tccData.infoAdvisor.email.toLowerCase())
  );
  if (!hasAdvisorInBanking && tccData.infoAdvisor?.name) {
    members.push({
      name: tccData.infoAdvisor.name,
      email: tccData.infoAdvisor.email,
      role: 'Orientador(a)'
    });
  }

  return (
    <div className="space-y-6 pb-10">
      {/* Top Header */}
      <div>
        <PageTitle>Gerenciar banca</PageTitle>
        <p className="mt-1 text-sm text-[#556987]">
          Gerencie a data, local e os examinadores da banca do TCC.
        </p>
      </div>

      {/* Card 1: Apresentação */}
      <div className="rounded-2xl border border-[#ECEAE2] bg-white/40 p-6 md:p-8">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3.5">
            <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[#002B18] text-white">
              <Presentation className="size-5 stroke-[1.75]" />
            </div>
            <h2 className="text-xl font-bold tracking-tight text-[#002B18]">
              Apresentação
            </h2>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {hasSchedule ? (
              <>
                <button
                  type="button"
                  onClick={handleConclude}
                  disabled={isConcluding || isPresentationConcluded}
                  className={cn(
                    "inline-flex cursor-pointer items-center gap-2 rounded-full border px-5 py-2 text-sm font-semibold transition-colors",
                    isPresentationConcluded
                      ? "border-green-600 bg-green-50 text-green-700 cursor-default"
                      : "border-[#002B18] bg-transparent text-[#002B18] hover:bg-[#002B18]/5 disabled:cursor-not-allowed disabled:opacity-50"
                  )}
                >
                  <Check className="size-4" />
                  {isConcluding
                    ? 'Concluindo...'
                    : isPresentationConcluded
                      ? 'Apresentação Concluída'
                      : 'Concluir Apresentação'}
                </button>

                <AddToCalendarButton
                  eventData={{
                    tccId: tccData.id,
                    title: tccData.infoTcc.title || 'TCC',
                    summary: tccData.infoTcc.summary,
                    location: tccData.infoTcc.presentationLocation,
                    presentationDate: tccData.infoTcc.presentationDate,
                    presentationTime: tccData.infoTcc.presentationTime
                  }}
                  variant="outline"
                  className="rounded-full border-[#002B18] text-[#002B18] px-5 py-2 hover:bg-[#002B18]/5 font-semibold text-sm"
                />

                <button
                  type="button"
                  onClick={() => setIsScheduleFormVisible(true)}
                  className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5"
                >
                  <Pencil className="size-4 text-[#002B18]" />
                  Editar Apresentação
                </button>
              </>
            ) : (
              <button
                type="button"
                onClick={() => setIsScheduleFormVisible(true)}
                className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5"
              >
                <Calendar className="size-4 text-[#002B18]" />
                Agendar Apresentação
              </button>
            )}
          </div>
        </div>

        <div className="mt-6 border-t border-[#ECEAE2] pt-6">
          <div className="grid grid-cols-1 divide-y divide-[#ECEAE2] md:grid-cols-3 md:divide-x md:divide-y-0">
            {/* Data */}
            <div className="flex items-center gap-3.5 py-3 md:py-0 md:pr-6">
              <Calendar className="size-5 shrink-0 text-[#002B18]" />
              <div>
                <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
                  DATA DA APRESENTAÇÃO
                </span>
                <p className="mt-0.5 text-base font-bold text-[#002B18]">
                  {tccData.infoTcc.presentationDate || 'Não informada'}
                </p>
              </div>
            </div>

            {/* Hora */}
            <div className="flex items-center gap-3.5 py-3 md:py-0 md:px-6">
              <Clock className="size-5 shrink-0 text-[#002B18]" />
              <div>
                <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
                  HORA DA APRESENTAÇÃO
                </span>
                <p className="mt-0.5 text-base font-bold text-[#002B18]">
                  {tccData.infoTcc.presentationTime || 'Não informada'}
                </p>
              </div>
            </div>

            {/* Local */}
            <div className="flex items-center gap-3.5 py-3 md:py-0 md:pl-6">
              <MapPin className="size-5 shrink-0 text-[#002B18]" />
              <div>
                <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
                  LOCAL DA APRESENTAÇÃO
                </span>
                <p className="mt-0.5 text-base font-bold text-[#002B18]">
                  {tccData.infoTcc.presentationLocation || 'Não informado'}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Card 2: Membros da Banca */}
      <div className="rounded-2xl border border-[#ECEAE2] bg-white/40 p-6 md:p-8">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-3.5">
            <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[#002B18] text-white">
              <Users className="size-5 stroke-[1.75]" />
            </div>
            <div>
              <h2 className="text-xl font-bold tracking-tight text-[#002B18]">
                Membros da Banca
              </h2>
              <p className="mt-0.5 text-sm text-[#556987]">
                Informações dos participantes da banca.
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {hasSchedule && (
              <button
                type="button"
                onClick={handleSendScheduleEmail}
                className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5"
              >
                <Send className="size-4 text-[#002B18]" />
                Reenviar Comunicados
              </button>
            )}

            <button
              type="button"
              onClick={() => setIsScheduleFormVisible(true)}
              className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5"
            >
              <Landmark className="size-4 text-[#002B18]" />
              {hasExaminers ? 'Editar Banca' : 'Inserir Banca'}
            </button>
          </div>
        </div>

        <div className="mt-6 border-t border-[#ECEAE2]">
          {members.length > 0 ? (
            <div className="divide-y divide-[#ECEAE2]">
              {members.map((member, index) => (
                <div
                  key={index}
                  className="flex items-start gap-4 py-5"
                >
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[#E8F3EC] text-[#002B18] mt-1">
                    <User className="size-5 stroke-[1.75]" />
                  </div>

                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2 md:gap-8 flex-1">
                    {/* Coluna Membro */}
                    <div>
                      <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
                        MEMBRO DA BANCA
                      </span>
                      <p className="mt-1 text-base font-bold text-[#002B18]">
                        {member.name}
                      </p>
                      <div className="mt-1.5 flex items-center gap-2 text-sm text-[#002B18]">
                        <Mail className="size-4 shrink-0 text-[#002B18]" />
                        <span>{member.email}</span>
                      </div>
                    </div>

                    {/* Coluna Instituição/Papel */}
                    <div>
                      <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
                        INSTITUIÇÃO/PAPEL
                      </span>
                      <div className="mt-2 flex items-center gap-2 text-sm font-semibold text-[#002B18] md:mt-3">
                        <Landmark className="size-4.5 shrink-0 text-[#002B18]" />
                        <span>{member.role}</span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="py-6 text-sm italic text-gray-500">
              Nenhum membro cadastrado na banca.
            </p>
          )}
        </div>
      </div>

      {/* Modal de Agendamento/Examinadores */}
      {canManage && (
        <ScheduleDialog
          isOpen={isScheduleFormVisible}
          hasSchedule={!!tccData.infoTcc.presentationDate}
          onOpenChange={setIsScheduleFormVisible}
          form={scheduleForm}
          onSubmit={handleScheduleSubmit}
          internalExaminers={internalExaminers}
        />
      )}

      {/* Modal de Avaliação do Orientador (se acionado ao concluir) */}
      <Dialog
        open={!!orientadorToken}
        onOpenChange={(open) => !open && setOrientadorToken(null)}
      >
        <DialogContent className="max-h-[90vh] max-w-[95vw] overflow-y-auto sm:max-w-4xl">
          <AvaliacaoForm
            token={orientadorToken}
            onSuccessCallback={() => setOrientadorToken(null)}
            hideLogoAndMinHeight
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}

