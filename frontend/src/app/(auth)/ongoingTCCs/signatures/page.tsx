'use client';
import { CollapseCard } from '@/components/CollapseCard';
import Step from '@/components/Steps';
import {
  faCheckCircle,
  faFileAlt,
  faClock
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React, { Suspense } from 'react';
import { useEffect } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import TccTabs from '@/components/TccTabs';

export default function Signatures() {
  interface TCCFromApi {
    tccId: number;
    studanteNames: string[];
  }
  const [tccs, setTccs] = React.useState<TCCFromApi[]>([]);
  const currentStep = 4;

  const documentos = [
    {
      nome: 'Formulário de Proposta',
      assinaturas: [
        { nome: 'Aluno', assinou: true },
        { nome: 'Orientador', assinou: false }
      ]
    },
    {
      nome: 'Plano de Trabalho',
      assinaturas: [
        { nome: 'Aluno', assinou: true },
        { nome: 'Orientador', assinou: true },
        { nome: 'Coordenador', assinou: false }
      ]
    }
  ];

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
    <div>
      <Suspense fallback={null}>
        <TccTabs />
      </Suspense>{' '}
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        TCC -{' '}
        {tccs.length > 0 ? tccs[0].studanteNames.join(', ') : 'Carregando...'}
      </h1>
      <Step currentStep={currentStep} />
      <div className="flex flex-col">
        {documentos.map((doc, index) => (
          <CollapseCard
            key={index}
            title={doc.nome}
            icon={faFileAlt}
            indicatorNumber={doc.assinaturas.length}
          >
            <ul className="text-gray-700 space-y-2">
              {doc.assinaturas.map((assinante, idx) => (
                <li key={idx} className="flex items-center justify-between">
                  <span>{assinante.nome}</span>
                  <span
                    className={`flex items-center gap-1 ${assinante.assinou ? 'text-green-600' : 'text-yellow-600'}`}
                  >
                    <FontAwesomeIcon
                      icon={assinante.assinou ? faCheckCircle : faClock}
                      className="h-4 w-4"
                    />
                    {assinante.assinou ? 'Assinado' : 'Pendente'}
                  </span>
                </li>
              ))}
            </ul>
          </CollapseCard>
        ))}
      </div>
    </div>
  );
}
