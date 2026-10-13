'use client';

import { useCompletedTCCs } from '@/app/hooks/useCompletedTCCs';
import { useSemestres, Semester } from '@/app/hooks/useSemestres';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { faGraduationCap, faFilter } from '@fortawesome/free-solid-svg-icons';
import { CollapseCard } from '@/components/CollapseCard';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useState, useEffect } from 'react';

export default function CompletedTCCsPage() {
  const { semesters, fetchSemesters } = useSemestres();
  const [selectedSemesterId, setSelectedSemesterId] = useState<number | null>(null);
  const { completedTCCs, isLoading } = useCompletedTCCs(null, selectedSemesterId);
  const { push } = useRouter();

  useEffect(() => {
    fetchSemesters();
  }, [fetchSemesters]);

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800">
          TCCs Concluídos
        </h1>
        <div className="flex items-center gap-2">
          <FontAwesomeIcon icon={faFilter} className="text-gray-500" />
          <select
            value={selectedSemesterId || ''}
            onChange={(e) => setSelectedSemesterId(e.target.value ? Number(e.target.value) : null)}
            className="px-3 py-2 border border-gray-300 rounded-md bg-white text-sm focus:outline-none focus:ring-2 focus:ring-[#1351B4]"
          >
            <option value="">Todos os semestres</option>
            {semesters.map((semester: Semester) => (
              <option key={semester.id} value={semester.id}>
                {semester.name} {semester.isActive && '(Ativo)'}
              </option>
            ))}
          </select>
        </div>
      </div>

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
