'use client';

import { Suspense } from 'react';
import { useTccDetails } from '@/app/hooks/useTccDetails';
import { FormProvider } from 'react-hook-form';
import React from 'react';

// Imports da UI e dos componentes de seção
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import TccTabs from '@/components/TccTabs';
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
    isCancellationModalOpen,
    setIsCancellationModalOpen,
    isBankingFormVisible,
    setIsBankingFormVisible,
    cancellationForm,
    bankingForm,
    allBankingMembers,
    handleRequestCancellation,
    handleApproveCancellation,
    handleRegisterBanking
  } = useTccDetails();

  if (loading) {
    return <div className="p-4">Carregando...</div>;
  }

  if (!tccData) {
    return <div className="p-4">TCC não encontrado.</div>;
  }

  // CORREÇÃO 1: Garante que a variável seja sempre um booleano.
  const canRegisterBanking = !!(
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

        <TccHeader
          infoStudent={tccData.infoStudent}
          cancellationRequested={tccData.cancellationRequest}
        />

        <div className="flex flex-col gap-8 mt-10">
          <TccInfoSection infoTcc={tccData.infoTcc} />
          <StudentInfoSection students={tccData.infoStudent} />

          {tccData.infoAdvisor.name && (
            <AdvisorInfoSection advisor={tccData.infoAdvisor} />
          )}

          <BankingInfoSection
            bankingData={tccData.infoBanking}
            canRegister={canRegisterBanking && !tccData.cancellationRequest}
            isFormVisible={isBankingFormVisible}
            onCancel={() => setIsBankingFormVisible(false)}
            form={bankingForm}
            onSubmit={handleRegisterBanking}
            allBankingMembers={allBankingMembers}
          />
        </div>

        {/* CORREÇÃO 2: Adiciona as props que estavam faltando. */}
        <ActionPanel
          profile={profile}
          cancellationRequested={tccData.cancellationRequest}
          cancellationDetails={cancellationDetails}
          onApprove={handleApproveCancellation}
          onRequest={() => setIsCancellationModalOpen(true)}
          hasBanking={!!tccData.infoBanking?.nameInternal}
          isBankingFormVisible={isBankingFormVisible}
          onRegisterBankingClick={() => setIsBankingFormVisible(true)}
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
