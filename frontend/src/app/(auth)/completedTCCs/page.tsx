'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';
import { SemesterFilter } from '@/components/SemesterFilter';

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

  useEffect(() => {
    fetchSemesters();
  }, [fetchSemesters]);

  return (
    <div className="flex flex-col">
      <div className="mb-6 flex flex-col items-start gap-5">
        <PageTitle>TCCs Concluídos</PageTitle>
        <SemesterFilter
          semesters={semesters}
          value={selectedSemesterId}
          onChange={setSelectedSemesterId}
        />
      </div>

      {isLoading ? (
        <PageState loading>Carregando TCCs concluídos...</PageState>
      ) : completedTCCs.length === 0 ? (
        <PageState>Nenhum TCC concluído foi encontrado.</PageState>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {completedTCCs.map((tcc) => (
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
