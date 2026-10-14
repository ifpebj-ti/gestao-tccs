'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';

import { useCompletedTCCs } from '@/app/hooks/useCompletedTCCs';
import { faGraduationCap } from '@fortawesome/free-solid-svg-icons';
import { CollapseCard } from '@/components/CollapseCard';
import { useRouter } from 'next/navigation';

export default function CompletedTCCsPage() {
  const { completedTCCs, isLoading } = useCompletedTCCs();
  const { push } = useRouter();

  return (
    <div className="flex flex-col">
      <PageTitle className="mb-10">
        TCCs Concluídos
      </PageTitle>

      {isLoading ? (
        <PageState loading>Carregando TCCs concluídos...</PageState>
      ) : completedTCCs.length === 0 ? (
        <PageState>Nenhum TCC concluído foi encontrado.</PageState>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {completedTCCs.map((tcc) => (
            <CollapseCard variant="flat"
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
