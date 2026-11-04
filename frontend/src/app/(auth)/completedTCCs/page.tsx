'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';
import { SemesterFilter } from '@/components/SemesterFilter';
import { TccSearch } from '@/components/TccSearch';
import { useCoordinatorTccSearch } from '@/app/hooks/useCoordinatorTccSearch';

import { useCompletedTCCs } from '@/app/hooks/useCompletedTCCs';
import { useSemestres } from '@/app/hooks/useSemestres';
import { faGraduationCap } from '@fortawesome/free-solid-svg-icons';
import { CollapseCard } from '@/components/CollapseCard';
import { useRouter } from 'next/navigation';
import { useState, useEffect } from 'react';

export default function CompletedTCCsPage() {
  const { semesters, fetchSemesters } = useSemestres();
  const [selectedSemesterId, setSelectedSemesterId] = useState<number | null>(
    null
  );
  const { completedTCCs, isLoading } = useCompletedTCCs(
    null,
    selectedSemesterId
  );
  const { push } = useRouter();
  const search = useCoordinatorTccSearch(completedTCCs);

  useEffect(() => {
    fetchSemesters();
  }, [fetchSemesters]);

  return (
    <div className="flex flex-col">
      <div className="mb-10 flex flex-col items-start gap-8">
        <PageTitle>TCCs Concluídos</PageTitle>
        <div className="grid w-full max-w-6xl grid-cols-1 gap-y-7 lg:grid-cols-[minmax(0,1fr)_16rem] lg:items-start lg:gap-x-16 xl:gap-x-24">
          {search.enabled && (
            <TccSearch
              value={search.query}
              onChange={search.setQuery}
              includeAdvisor={search.includeAdvisor}
              advisorSearchUnavailable={search.advisorSearchUnavailable}
            />
          )}
          <SemesterFilter
            semesters={semesters}
            value={selectedSemesterId}
            onChange={setSelectedSemesterId}
          />
        </div>
      </div>

      {isLoading ? (
        <PageState loading>Carregando TCCs concluídos...</PageState>
      ) : completedTCCs.length === 0 ? (
        <PageState>Nenhum TCC concluído foi encontrado.</PageState>
      ) : search.filteredTccs.length === 0 ? (
        <PageState>Nenhum TCC encontrado para esta busca.</PageState>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {search.filteredTccs.map((tcc) => (
            <CollapseCard
              variant="flat"
              key={tcc.tccId}
              title={
                (tcc.studanteNames || []).join(', ') || 'TCC sem aluno definido'
              }
              icon={faGraduationCap}
              onClick={() => push(`/completedTCCs/${tcc.tccId}`)}
            />
          ))}
        </div>
      )}
    </div>
  );
}
