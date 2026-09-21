'use client';

import { Suspense } from 'react';
import { useTccDetails } from '@/app/hooks/useTccDetails';
import { FormProvider } from 'react-hook-form';
import React from 'react';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import TccTabs from '@/components/TccTabs';
import { Button } from '@/components/ui/button';
import { faFileArchive } from '@fortawesome/free-solid-svg-icons';
import { TccHeader } from '@/components/tcc-details/TccHeader';
import { TccInfoSection } from '@/components/tcc-details/TccInfoSection';
import { StudentInfoSection } from '@/components/tcc-details/StudentInfoSection';
import { AdvisorInfoSection } from '@/components/tcc-details/AdvisorInfoSection';
import { BankingInfoSection } from '@/components/tcc-details/BankingInfoSection';
import { ScheduleSection } from '@/components/tcc-details/ScheduleSection';
import { ActionPanel } from '@/components/tcc-details/ActionPanel';
import { CancellationModal } from '@/components/tcc-details/CancellationModal';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';
export default function DetailsClient() {
  const {
    tccData,
    cancellationDetails,
    loading,
    profile,
    // Modais e Visibilidade
    isCancellationModalOpen,
    setIsCancellationModalOpen,
    isScheduleFormVisible,
    setIsScheduleFormVisible,
    isEditingTccInfo,
    setIsEditingTccInfo,
    // Forms
    cancellationForm,
    scheduleForm,
    editTccForm,
    // Handlers
    // Handlers
    handleRequestCancellation,
    handleApproveCancellation,
    handleScheduleSubmit,
    handleSendScheduleEmail,
    handleResendInvite,
    handleDownloadAllDocuments,
    handleEditTccInfo,
    handleConcludePresentation,
    isConcluding,
    orientadorToken,
    setOrientadorToken
  } = useTccDetails();

  if (loading) {
    return <div className="p-4">Carregando...</div>;
  }

  if (!tccData) {
    return <div className="p-4">TCC não encontrado.</div>;
  }

  const canManageTcc = !!(
    profile &&
    ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR'].some((role) =>
      Array.isArray(profile) ? profile.includes(role) : profile === role
    )
  );

  return (
    <FormProvider {...cancellationForm}>
      <div className="flex flex-col">
        <BreadcrumbAuto />
        <Suspense fallback={null}>
          <TccTabs />
        </Suspense>

        <div className="mt-6 flex flex-col md:flex-row justify-between gap-4">
          <TccHeader
            infoStudent={tccData.infoStudent}
            cancellationRequested={tccData.cancellationRequest}
          />
          <Button
            variant="outline"
            onClick={handleDownloadAllDocuments}
            className="w-full md:w-fit"
            icon={faFileArchive}
          >
            Baixar Todos os Documentos
          </Button>
        </div>

        <div className="flex flex-col gap-8 mt-10">
          <TccInfoSection
            infoTcc={tccData.infoTcc}
            isEditingInfo={isEditingTccInfo}
            onToggleEditInfo={setIsEditingTccInfo}
            editForm={editTccForm}
            onEditSubmit={handleEditTccInfo}
          />
          <StudentInfoSection
            students={tccData.infoStudent}
            canResendInvite={canManageTcc && !tccData.cancellationRequest}
            onResendInvite={handleResendInvite}
            resendingInviteTo={null}
          />
          {tccData.infoAdvisor.name && (
            <AdvisorInfoSection advisor={tccData.infoAdvisor} />
          )}
          <BankingInfoSection bankingData={tccData.infoBanking} />
        </div>

        <ScheduleSection 
          infoTcc={tccData.infoTcc}
          isScheduleFormVisible={isScheduleFormVisible}
          onOpenSchedule={() => setIsScheduleFormVisible((prev) => !prev)}
          onScheduleCancel={() => setIsScheduleFormVisible(false)}
          scheduleForm={scheduleForm}
          onScheduleSubmit={handleScheduleSubmit}
          canSchedule={canManageTcc && !tccData.cancellationRequest}
          onSendScheduleEmail={handleSendScheduleEmail}
          isCompleted={tccData.infoTcc.status === 'COMPLETED'}
          onConcludePresentation={handleConcludePresentation}
          isConcluding={isConcluding}
        />

        <ActionPanel
          profile={profile}
          cancellationRequested={tccData.cancellationRequest}
          cancellationDetails={cancellationDetails}
          onApprove={handleApproveCancellation}
          onRequest={() => setIsCancellationModalOpen(true)}
        />
      </div>

      <CancellationModal
        isOpen={isCancellationModalOpen}
        onOpenChange={setIsCancellationModalOpen}
        form={cancellationForm}
        onSubmit={handleRequestCancellation}
      />

      <Dialog open={!!orientadorToken} onOpenChange={(open) => { if (!open) setOrientadorToken(null); }}>
        <DialogContent className="max-w-[95vw] sm:max-w-4xl md:max-w-5xl max-h-[90vh] overflow-y-auto">
          <AvaliacaoForm 
            token={orientadorToken} 
            onSuccessCallback={() => setOrientadorToken(null)}
            hideLogoAndMinHeight={true}
          />
        </DialogContent>
      </Dialog>
    </FormProvider>
  );
}
