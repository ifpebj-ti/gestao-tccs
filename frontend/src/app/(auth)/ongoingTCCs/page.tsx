'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';
import { SemesterFilter } from '@/components/SemesterFilter';

import { ResponsiveRecordCard } from '@/components/ResponsiveRecordCard';
import {
  faGraduationCap,
  faPen,
  faFile
} from '@fortawesome/free-solid-svg-icons';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';
import { useSemestres } from '@/app/hooks/useSemestres';

interface DecodedToken {
  role: string | string[];
  userId: string;
}

interface TCCFromApi {
  tccId: number;
  studanteNames: string[];
}

export default function OngoingTCCsPage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const { semesters, fetchSemesters } = useSemestres();
  const [tccs, setTccs] = useState<TCCFromApi[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedSemesterId, setSelectedSemesterId] = useState<number | null>(
    null
  );

  useEffect(() => {
    fetchSemesters();
  }, [fetchSemesters]);

  useEffect(() => {
    const fetchTccs = async () => {
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          return;
        }

        const decodedToken = jwtDecode<DecodedToken>(token);
        const { role, userId } = decodedToken;

        const wideViewRoles = ['ADMIN', 'COORDINATOR', 'LIBRARY', 'SUPERVISOR'];
        let endpoint = `${API_URL}/Tcc/filter`;

        // Variável para determinar o tipo de visão (ampla ou restrita)
        let userHasWideView = false;
        if (typeof role === 'string') {
          userHasWideView = wideViewRoles.includes(role);
        } else if (Array.isArray(role)) {
          userHasWideView = role.some((r) => wideViewRoles.includes(r));
        }

        if (userHasWideView) {
          endpoint += '?StatusTcc=IN_PROGRESS';
        } else {
          endpoint += `?UserId=${userId}&StatusTcc=IN_PROGRESS`;
        }

        // Adicionar filtro de semestre se selecionado
        if (selectedSemesterId) {
          endpoint += `&SemesterId=${selectedSemesterId}`;
        }

        const res = await fetch(endpoint, {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });

        if (!res.ok) {
          throw new Error('Erro ao buscar TCCs');
        }

        const data: TCCFromApi[] = await res.json();
        setTccs(data);
      } catch {
        toast.error('Erro ao carregar TCCs em andamento.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchTccs();
  }, [API_URL, selectedSemesterId]);

  return (
    <div className="flex flex-col">
      <div className="mb-6 flex flex-col items-start gap-5">
        <PageTitle>TCCs em andamento</PageTitle>
        <SemesterFilter
          semesters={semesters}
          value={selectedSemesterId}
          onChange={setSelectedSemesterId}
        />
      </div>

      {isLoading ? (
        <PageState loading>Carregando...</PageState>
      ) : tccs.length === 0 ? (
        <PageState>Nenhum TCC em andamento encontrado.</PageState>
      ) : (
        <div className="grid grid-cols-1 gap-x-8 md:grid-cols-2 lg:grid-cols-3">
          {tccs.map((tcc) => (
            <ResponsiveRecordCard
              key={tcc.tccId}
              onClick={() => push(`/ongoingTCCs/signatures?id=${tcc.tccId}`)}
              title={
                tcc.studanteNames.join(', ') ||
                'Aguardando cadastro do(s) estudante(s)'
              }
              icon={faGraduationCap}
              indicatorColor="bg-red-600"
            >
              <div className="flex flex-col gap-2">
                <button
                  onClick={() =>
                    push(`/ongoingTCCs/signatures?id=${tcc.tccId}`)
                  }
                  className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                >
                  <FontAwesomeIcon icon={faPen} className="text-[#006633]" />
                  <span>Assinaturas</span>
                </button>
                <button
                  onClick={() => push(`/ongoingTCCs/details?id=${tcc.tccId}`)}
                  className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                >
                  <FontAwesomeIcon icon={faFile} className="text-[#006633]" />
                  <span>TCC</span>
                </button>
              </div>
            </ResponsiveRecordCard>
          ))}
        </div>
      )}
    </div>
  );
}
