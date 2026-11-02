'use client';

import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { FormProvider } from 'react-hook-form';
import { useTccDetails } from '@/app/hooks/useTccDetails';
import { PageState } from '@/components/layout/PageState';
import { PageTitle } from '@/components/layout/PageTitle';
import { Button } from '@/components/ui/button';
import { ScheduleSection } from '@/components/tcc-details/ScheduleSection';
import { ScheduleDialog } from '@/components/tcc-details/ScheduleDialog';
import { BankingInfoSection } from '@/components/tcc-details/BankingInfoSection';
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
  const canManage = roles.includes('ADVISOR') &&
    currentUserId !== null && currentUserId === Number(tccData.advisorUserId);

  if (!canManage) {
    return (
      <div className="space-y-5">
        <div className="flex justify-end">
          <Button variant="outline" onClick={() => router.push('/banking')}>
            Voltar para Banca
          </Button>
        </div>
        <PageState>Somente o orientador responsável por este TCC pode gerenciar a banca. Examinadores podem acessar as avaliações pendentes na área Banca.</PageState>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <PageTitle>Gerenciar banca</PageTitle>
          <p className="mt-2 text-sm text-slate-600">{tccData.infoTcc.title}</p>
        </div>
        <Button variant="outline" onClick={() => router.push('/banking')}>
          Voltar para Banca
        </Button>
      </div>

      <BankingInfoSection
        bankingData={tccData.infoBanking}
        isCompleted={tccData.infoTcc.status === 'COMPLETED'}
      />

      <ScheduleSection
        infoTcc={tccData.infoTcc}
        isScheduleFormVisible={isScheduleFormVisible}
        onOpenSchedule={() => setIsScheduleFormVisible(true)}
        canSchedule={canManage && !tccData.cancellationRequest}
        onSendScheduleEmail={handleSendScheduleEmail}
        isCompleted={tccData.infoTcc.status === 'COMPLETED'}
        onConcludePresentation={handleConcludePresentation}
        isConcluding={isConcluding}
      />

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
