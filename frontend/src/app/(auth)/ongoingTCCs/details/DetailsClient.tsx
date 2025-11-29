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
import { ActionPanel } from '@/components/tcc-details/ActionPanel';
import { CancellationModal } from '@/components/tcc-details/CancellationModal';

export default function DetailsClient() {
  const {
    tccData,
    cancellationDetails,
    loading,
    profile,
    // Modais e Visibilidade
    isCancellationModalOpen,
    setIsCancellationModalOpen,
    isBankingFormVisible,
    setIsBankingFormVisible,
    isScheduleFormVisible,
    setIsScheduleFormVisible,
    isEditingTccInfo,
    setIsEditingTccInfo,
    // Forms
    cancellationForm,
    bankingForm,
    allBankingMembers,
    scheduleForm,
    editTccForm,
    // Handlers
    handleRequestCancellation,
    handleApproveCancellation,
    handleRegisterBanking,
    handleScheduleSubmit,
    handleSendScheduleEmail,
    handleResendInvite,
    handleDownloadAllDocuments,
    handleEditTccInfo
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
            isScheduleFormVisible={isScheduleFormVisible}
            onScheduleCancel={() => setIsScheduleFormVisible(false)}
            scheduleForm={scheduleForm}
            onScheduleSubmit={handleScheduleSubmit}
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
          <BankingInfoSection
            bankingData={tccData.infoBanking}
            canRegister={canManageTcc && !tccData.cancellationRequest}
            isFormVisible={isBankingFormVisible}
            onCancel={() => setIsBankingFormVisible(false)}
            form={bankingForm}
            onSubmit={handleRegisterBanking}
            allBankingMembers={allBankingMembers}
          />
        </div>

        <ActionPanel
          profile={profile}
          cancellationRequested={tccData.cancellationRequest}
          cancellationDetails={cancellationDetails}
          hasBanking={
            !!tccData.infoBanking?.nameInternal ||
            !!tccData.infoBanking?.nameExternal
          }
          isBankingFormVisible={isBankingFormVisible}
          hasSchedule={!!tccData.infoTcc.presentationDate}
          isScheduleFormVisible={isScheduleFormVisible}
          onApprove={handleApproveCancellation}
          onRequest={() => setIsCancellationModalOpen(true)}
          onRegisterBankingClick={() => setIsBankingFormVisible(true)}
          onScheduleClick={() => setIsScheduleFormVisible((prev) => !prev)}
          onSendScheduleEmail={handleSendScheduleEmail}
        />
      </div>

      <CancellationModal
        isOpen={isCancellationModalOpen}
        onOpenChange={setIsCancellationModalOpen}
        form={cancellationForm}
        onSubmit={handleRequestCancellation}
      />
    </FormProvider>
  );
}
