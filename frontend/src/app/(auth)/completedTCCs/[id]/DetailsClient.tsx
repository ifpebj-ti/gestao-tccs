'use client';

import { useCompletedTccDetails } from '@/app/hooks/useCompletedTCCsDetails';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { TccHeader } from '@/components/tcc-details/TccHeader';
import { TccInfoSection } from '@/components/tcc-details/TccInfoSection';
import { StudentInfoSection } from '@/components/tcc-details/StudentInfoSection';
import { AdvisorInfoSection } from '@/components/tcc-details/AdvisorInfoSection';
import { BankingInfoSection } from '@/components/tcc-details/BankingInfoSection';

export default function CompletedTccDetailsClient() {
  const { tccDetails, isLoading } = useCompletedTccDetails();

  if (isLoading) {
    return <div className="p-4 text-center">Carregando...</div>;
  }

  if (!tccDetails) {
    return (
      <div className="p-4 text-center">
        Não foi possível encontrar os detalhes deste TCC.
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />

      <div className="mt-6">
        <TccHeader
          infoStudent={tccDetails.infoStudent}
          cancellationRequested={false}
        />
      </div>

      <div className="flex flex-col gap-8 mt-10">
        <TccInfoSection infoTcc={tccDetails.infoTcc} />
        <StudentInfoSection students={tccDetails.infoStudent} />

        {tccDetails.infoAdvisor?.name && (
          <AdvisorInfoSection advisor={tccDetails.infoAdvisor} />
        )}

        <BankingInfoSection bankingData={tccDetails.infoBanking} />
      </div>
    </div>
  );
}
