'use client';

import { useCompletedTCCs } from '@/app/hooks/useCompletedTCCs';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { faGraduationCap } from '@fortawesome/free-solid-svg-icons';
import { CollapseCard } from '@/components/CollapseCard';
import { useRouter } from 'next/navigation';

export default function CompletedTCCsPage() {
  const { completedTCCs, isLoading } = useCompletedTCCs();
  const { push } = useRouter();

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        TCCs Concluídos
      </h1>

      {isLoading ? (
        <p className="text-center text-gray-500 mt-4">
          Carregando TCCs concluídos...
        </p>
      ) : completedTCCs.length === 0 ? (
        <p className="text-center text-gray-500 mt-4">
          Nenhum TCC concluído foi encontrado.
        </p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {completedTCCs.map((tcc) => (
            <CollapseCard
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
