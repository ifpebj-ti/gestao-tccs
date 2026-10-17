'use client';

import { useCompletedTccDetails } from '@/app/hooks/useCompletedTCCsDetails';
import { TccOverview } from '@/components/tcc-details/TccOverview';
import { useParams } from 'next/navigation';
import { TccInfoSection } from '@/components/tcc-details/TccInfoSection';
import { StudentInfoSection } from '@/components/tcc-details/StudentInfoSection';
import { AdvisorInfoSection } from '@/components/tcc-details/AdvisorInfoSection';
import { BankingInfoSection } from '@/components/tcc-details/BankingInfoSection';
import { ScheduleSection } from '@/components/tcc-details/ScheduleSection';
import { FinalSubmissionBanner } from '@/components/tcc-details/FinalSubmissionBanner';

export default function CompletedTccDetailsClient() {
  const { id } = useParams<{ id: string }>();
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
      <div className="mt-6">
        <TccOverview title={tccDetails.infoTcc.title} tccId={id} step={6} />
      </div>

      <div className="mt-4">
        <FinalSubmissionBanner
          step={tccDetails.infoTcc.step}
          finalSubmissionDeadline={tccDetails.infoTcc.finalSubmissionDeadline}
          finalTccFile={tccDetails.infoTcc.finalTccFile}
          hardCoverReceivedAt={tccDetails.infoTcc.hardCoverReceivedAt}
          forwardedToLibraryAt={tccDetails.infoTcc.forwardedToLibraryAt}
          libraryReceivedAt={tccDetails.infoTcc.libraryReceivedAt}
          libraryCatalogCode={tccDetails.infoTcc.libraryCatalogCode}
          isCompleted={true}
        />
      </div>

      <div className="flex flex-col gap-8 mt-10">
        <TccInfoSection infoTcc={tccDetails.infoTcc} />
        <StudentInfoSection students={tccDetails.infoStudent} />

        {tccDetails.infoAdvisor?.name && (
          <AdvisorInfoSection advisor={tccDetails.infoAdvisor} />
        )}

        <BankingInfoSection
          bankingData={tccDetails.infoBanking}
          isCompleted={true}
        />

        <ScheduleSection
          infoTcc={tccDetails.infoTcc}
          isScheduleFormVisible={false}
          onScheduleCancel={() => {}}
          canSchedule={false}
          isCompleted={true}
        />
      </div>
    </div>
  );
}
