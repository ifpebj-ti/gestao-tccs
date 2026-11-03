'use client';

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
  Users
} from 'lucide-react';
import { useTccDetails } from '@/app/hooks/useTccDetails';
import { PageState } from '@/components/layout/PageState';
import { PageTitle } from '@/components/layout/PageTitle';
import { Button } from '@/components/ui/button';
import { ScheduleDialog } from '@/components/tcc-details/ScheduleDialog';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';

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
    <div className="space-y-8 pb-10">
      {/* Top Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <PageTitle>Gerenciar banca</PageTitle>
          <p className="mt-1 text-sm text-[#556987]">
            Gerencie a data, local e os examinadores da banca do TCC.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {hasSchedule && (
            <button
              type="button"
              onClick={handleConcludePresentation}
              disabled={isConcluding}
              className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Check className="size-4 text-[#002B18]" />
              {isConcluding ? 'Concluindo...' : 'Concluir Apresentação'}
            </button>
          )}

          {hasSchedule && (
            <button
              type="button"
              onClick={handleSendScheduleEmail}
              className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5"
            >
              <Mail className="size-4 text-[#002B18]" />
              Reenviar Comunicados
            </button>
          )}

          <button
            type="button"
            onClick={() => setIsScheduleFormVisible(true)}
            className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-[#002B18] bg-transparent px-5 py-2 text-sm font-semibold text-[#002B18] transition-colors hover:bg-[#002B18]/5"
          >
            <Pencil className="size-4 text-[#002B18]" />
            {hasSchedule ? 'Inserir Examinadores' : 'Agendar Apresentação'}
          </button>
        </div>
      </div>

      {/* Seção 1: Apresentação */}
      <section className="pt-2">
        <div className="flex items-center gap-3.5">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-full bg-[#E8F3EC] text-[#002B18]">
            <Calendar className="size-5.5 stroke-[1.75]" />
          </div>
          <h2 className="text-xl font-bold tracking-tight text-[#002B18]">
            Apresentação
          </h2>
        </div>

        <div className="mt-6 grid grid-cols-1 divide-y divide-[#ECEAE2] md:grid-cols-3 md:divide-x md:divide-y-0">
          {/* Data */}
          <div className="py-2 md:py-0 md:pr-6">
            <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
              DATA DA APRESENTAÇÃO
            </span>
            <div className="mt-2.5 flex items-center gap-2.5 text-base font-semibold text-[#002B18]">
              <Calendar className="size-5 shrink-0 stroke-[1.75]" />
              <span>{tccData.infoTcc.presentationDate || 'Não informada'}</span>
            </div>
          </div>

          {/* Hora */}
          <div className="py-2 md:py-0 md:px-6">
            <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
              HORA DA APRESENTAÇÃO
            </span>
            <div className="mt-2.5 flex items-center gap-2.5 text-base font-semibold text-[#002B18]">
              <Clock className="size-5 shrink-0 stroke-[1.75]" />
              <span>{tccData.infoTcc.presentationTime || 'Não informada'}</span>
            </div>
          </div>

          {/* Local */}
          <div className="py-2 md:py-0 md:pl-6">
            <span className="block text-[11px] font-bold tracking-wider text-[#556987] uppercase">
              LOCAL DA APRESENTAÇÃO
            </span>
            <div className="mt-2.5 flex items-center gap-2.5 text-base font-semibold text-[#002B18]">
              <MapPin className="size-5 shrink-0 fill-[#002B18] text-[#002B18]" />
              <span>
                {tccData.infoTcc.presentationLocation || 'Não informado'}
              </span>
            </div>
          </div>
        </div>
      </section>

      {/* Linha divisória horizontal entre Apresentação e Membros */}
      <hr className="border-[#ECEAE2]" />

      {/* Seção 2: Membros da Banca */}
      <section>
        <div className="flex items-start gap-3.5">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-full bg-[#E8F3EC] text-[#002B18]">
            <Users className="size-5.5 stroke-[1.75]" />
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

        {members.length > 0 ? (
          <div className="mt-6 divide-y divide-[#ECEAE2]">
            {members.map((member, index) => (
              <div
                key={index}
                className="grid grid-cols-1 gap-4 py-5 md:grid-cols-2 md:gap-8"
              >
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
                  <div className="mt-2 flex items-center gap-2 text-sm font-semibold text-[#002B18] md:mt-4">
                    <Landmark className="size-4.5 shrink-0 text-[#002B18]" />
                    <span>{member.role}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="mt-6 text-sm italic text-gray-500">
            Nenhum membro cadastrado na banca.
          </p>
        )}
      </section>

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

