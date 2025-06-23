'use client';
import { CollapseCard } from '@/components/CollapseCard';
import Step from '@/components/Steps';
import {
  faCheckCircle,
  faFileAlt,
  faClock
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React, { Suspense, useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import TccTabs from '@/components/TccTabs';
import { useSearchParams } from 'next/navigation';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';

export default function Signatures() {
  const profileLabels: Record<string, string> = {
    STUDENT: 'Aluno',
    ADVISOR: 'Orientador',
    COORDINATOR: 'Coordenador',
    SUPERVISOR: 'Supervisor',
    LIBRARY: 'Bibliotecária'
  };

  interface Student {
    name: string;
    registration: string;
    cpf: string;
    course: string;
    email: string;
  }

  interface TccDetailsResponse {
    infoStudent: Student[];
  }

  interface SignatureDetail {
    userId: number;
    userProfile: string;
    userName: string;
    isSigned: boolean;
  }

  interface Signature {
    documentId: number;
    attachmentName: string;
    details: SignatureDetail[];
  }

  interface WorkflowResponse {
    tccId: number;
    step: number;
    signatures: Signature[];
  }

  const formatStudentName = (fullName: string) => {
    const [first, second] = fullName.trim().split(' ');
    return `${first ?? ''} ${second ?? ''}`;
  };

  const [students, setStudents] = useState<string[]>([]);
  const [data, setData] = useState<WorkflowResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const searchParams = useSearchParams();

  const tccId = Number(searchParams.get('id'));

  useEffect(() => {
    const token = Cookies.get('token');
    if (!token) {
      toast.error('Token de autenticação não encontrado.');
      return;
    }

    const fetchWorkflow = async () => {
      try {
        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/Tcc/workflow?tccId=${tccId}`,
          {
            headers: { Authorization: `Bearer ${token}` }
          }
        );

        if (!res.ok) throw new Error('Erro ao buscar workflow do TCC.');
        const result: WorkflowResponse = await res.json();
        setData(result);
      } catch {
        toast.error('Erro ao carregar workflow do TCC.');
      } finally {
        setLoading(false);
      }
    };

    // Função para buscar detalhes do TCC para obter os nomes dos alunos
    const fetchTccDetails = async () => {
      try {
        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/Tcc?tccId=${tccId}`,
          {
            headers: { Authorization: `Bearer ${token}` }
          }
        );

        if (!res.ok) throw new Error('Erro ao buscar detalhes do TCC.');
        const result: TccDetailsResponse = await res.json();
        const names = result.infoStudent.map((s) => s.name);
        setStudents(names);
      } catch {
        toast.error('Erro ao carregar dados dos alunos.');
      }
    };

    if (tccId) {
      fetchWorkflow();
      fetchTccDetails();
    }
  }, [tccId]);

  return (
    <div>
      <BreadcrumbAuto />
      <Suspense fallback={null}>
        <TccTabs />
      </Suspense>

      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10 truncate max-w-full">
        {data?.step === 1 ? (
          <>Aguardando cadastro do estudante</>
        ) : (
          <>
            TCC -{' '}
            <span title={students.join(', ')}>
              {students.length > 0
                ? students.map(formatStudentName).join(', ')
                : 'Carregando...'}
            </span>
          </>
        )}
      </h1>

      {data && <Step currentStep={data.step} />}

      <h2 className="text-lg font-extrabold uppercase mt-6 mb-4">
        Documentos e Assinaturas
      </h2>

      {loading ? (
        <p className="text-gray-500">Carregando documentos...</p>
      ) : (
        <div className="flex flex-col">
          {data?.signatures.map((doc, index) => (
            <CollapseCard
              key={index}
              title={doc.attachmentName}
              icon={faFileAlt}
              indicatorNumber={doc.details.length}
            >
              <ul className="text-gray-700 space-y-2">
                {doc.details.map((assinante, idx) => (
                  <li key={idx} className="flex items-center justify-between">
                    <span>
                      {assinante.userName}{' '}
                      <span className="text-gray-500">
                        (
                        {profileLabels[assinante.userProfile] ||
                          assinante.userProfile}
                        )
                      </span>
                    </span>
                    <span
                      className={`flex items-center font-semibold gap-1 ${assinante.isSigned ? 'text-green-600' : 'text-yellow-600'}`}
                    >
                      <FontAwesomeIcon
                        icon={assinante.isSigned ? faCheckCircle : faClock}
                        className="h-4 w-4"
                      />
                      {assinante.isSigned ? 'Assinado' : 'Pendente'}
                    </span>
                  </li>
                ))}
              </ul>
            </CollapseCard>
          ))}
        </div>
      )}
    </div>
  );
}
