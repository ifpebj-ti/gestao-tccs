'use client';

import { useTccDetails } from '@/app/hooks/useTccDetails';
import { FormProvider } from 'react-hook-form';
import React from 'react';

import { TccOverview } from '@/components/tcc-details/TccOverview';
import { TccInfoSection } from '@/components/tcc-details/TccInfoSection';
import { StudentInfoSection } from '@/components/tcc-details/StudentInfoSection';
import { AdvisorInfoSection } from '@/components/tcc-details/AdvisorInfoSection';
import { BankingInfoSection } from '@/components/tcc-details/BankingInfoSection';
import { ScheduleSection } from '@/components/tcc-details/ScheduleSection';
import { ActionPanel } from '@/components/tcc-details/ActionPanel';
import { CancellationModal } from '@/components/tcc-details/CancellationModal';
import { AdvisorChangeModal } from '@/components/tcc-details/AdvisorChangeModal';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';
import { FinalSubmissionBanner } from '@/components/tcc-details/FinalSubmissionBanner';
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
    handleRequestCancellation,
    handleApproveCancellation,
    handleScheduleSubmit,
    handleSendScheduleEmail,
    handleResendInvite,
    handleEditTccInfo,
    handleConcludePresentation,
    handleUploadTccFile,
    handleUploadFinalTccFile,
    handleConfirmHardCover,
    handleDownloadFinalTccFile,
    handleForwardToLibrary,
    handleConfirmLibrary,
    isUploadingFinalFile,
    isConfirmingHardCover,
    isDownloadingFinalFile,
    isForwardingToLibrary,
    isConfirmingLibrary,
    isConcluding,
    orientadorToken,
    setOrientadorToken,
    // Alteração / Afastamento de Orientador
    pendingAdvisorChange,
    isAdvisorChangeModalOpen,
    setIsAdvisorChangeModalOpen,
    advisorChangeType,
    availableAdvisors,
    advisorChangeForm,
    handleRequestAdvisorChange,
    openAdvisorChangeModal
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
        <TccOverview
          title={tccData.infoTcc.title}
          cancellationRequested={tccData.cancellationRequest}
        />
        <div className="mt-4">
          <FinalSubmissionBanner
            step={tccData.infoTcc.step}
            finalSubmissionDeadline={tccData.infoTcc.finalSubmissionDeadline}
            finalTccFile={tccData.infoTcc.finalTccFile}
            hardCoverReceivedAt={tccData.infoTcc.hardCoverReceivedAt}
            forwardedToLibraryAt={tccData.infoTcc.forwardedToLibraryAt}
            libraryReceivedAt={tccData.infoTcc.libraryReceivedAt}
            libraryCatalogCode={tccData.infoTcc.libraryCatalogCode}
            isCompleted={tccData.infoTcc.status === 'COMPLETED'}
            profile={profile}
            onUploadFinalFile={handleUploadFinalTccFile}
            onConfirmHardCover={handleConfirmHardCover}
            onDownloadFinalFile={handleDownloadFinalTccFile}
            onForwardToLibrary={handleForwardToLibrary}
            onConfirmLibrary={handleConfirmLibrary}
            isUploadingFinalFile={isUploadingFinalFile}
            isConfirmingHardCover={isConfirmingHardCover}
            isDownloadingFinalFile={isDownloadingFinalFile}
            isForwardingToLibrary={isForwardingToLibrary}
            isConfirmingLibrary={isConfirmingLibrary}
          />
        </div>
        <input
          id="tccUploadInput"
          type="file"
          accept=".pdf"
          className="hidden"
          onChange={handleUploadTccFile}
        />
        <div className="flex flex-col gap-8 mt-6">
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
            <AdvisorInfoSection
              advisor={tccData.infoAdvisor}
              profile={profile}
              pendingAdvisorChange={pendingAdvisorChange}
              onRequestAdvisorChange={openAdvisorChangeModal}
              isTccActive={
                tccData.infoTcc.status !== 'COMPLETED' &&
                tccData.infoTcc.status !== 'CANCELLED' &&
                !tccData.cancellationRequest
              }
            />
          )}
          <BankingInfoSection
            bankingData={tccData.infoBanking}
            isCompleted={tccData.infoTcc.status === 'COMPLETED'}
          />
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

      <AdvisorChangeModal
        isOpen={isAdvisorChangeModalOpen}
        onOpenChange={setIsAdvisorChangeModalOpen}
        form={advisorChangeForm}
        onSubmit={handleRequestAdvisorChange}
        type={advisorChangeType}
        availableAdvisors={availableAdvisors}
        currentAdvisorName={tccData.infoAdvisor.name}
      />

      <Dialog
        open={!!orientadorToken}
        onOpenChange={(open) => {
          if (!open) setOrientadorToken(null);
        }}
      >
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
