'use client';

import { CollapseCard } from '@/components/CollapseCard';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
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

interface TCCFromApi {
  tccId: number;
  studanteNames: string[];
}

export default function OngoingTCCsPage() {
  const { push } = useRouter();
  const [tccs, setTccs] = useState<TCCFromApi[]>([]);

  useEffect(() => {
    const fetchTccs = async () => {
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          return;
        }

        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/Tcc/filter?filter=IN_PROGRESS`,
          {
            headers: {
              Authorization: `Bearer ${token}`
            }
          }
        );

        if (!res.ok) {
          throw new Error('Erro ao buscar TCCs');
        }

        const data: TCCFromApi[] = await res.json();
        setTccs(data);
      } catch {
        toast.error('Erro ao carregar TCCs em andamento.');
      }
    };

    fetchTccs();
  }, []);

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        TCCs em andamento
      </h1>

      {tccs.length === 0 ? (
        <p className="text-center text-gray-500 mt-4">
          Nenhum TCC em andamento encontrado.
        </p>
      ) : (
        <>
          {/* Mobile - com filhos */}
          <div className="md:hidden flex flex-col gap-2">
            {tccs.map((tcc) => (
              <div key={tcc.tccId}>
                <CollapseCard
                  title={tcc.studanteNames.join(', ')}
                  icon={faGraduationCap}
                  indicatorColor="bg-red-600"
                >
                  <div className="flex flex-col gap-2">
                    <button
                      onClick={() =>
                        push(`/ongoingTCCs/${tcc.tccId}/signatures`)
                      }
                      className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                    >
                      <FontAwesomeIcon
                        icon={faPen}
                        className="text-[#1351B4]"
                      />
                      <span>Assinaturas</span>
                    </button>
                    <button
                      onClick={() => push(`/ongoingTCCs/${tcc.tccId}/details`)}
                      className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                    >
                      <FontAwesomeIcon
                        icon={faFile}
                        className="text-[#1351B4]"
                      />
                      <span>TCC</span>
                    </button>
                  </div>
                </CollapseCard>
              </div>
            ))}
          </div>

          {/* Desktop - igual ao mobile, mas sem filhos */}
          <div className="hidden md:grid grid-cols-2 lg:grid-cols-3 gap-4">
            {tccs.map((tcc) => (
              <div key={tcc.tccId}>
                <CollapseCard
                  title={tcc.studanteNames.join(', ')}
                  icon={faGraduationCap}
                  indicatorColor="bg-red-600"
                  onClick={() => push(`/ongoingTCCs/${tcc.tccId}/signatures`)}
                />
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
