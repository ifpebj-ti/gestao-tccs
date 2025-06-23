'use client';

import { CollapseCard } from '@/components/CollapseCard';
import Step from '@/components/Steps';
import {
  faCheckCircle,
  faFileAlt,
  faClock,
  faInfoCircle,
  faArrowRight
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
    otherSignatures?: SignatureDetail[];
  }

  interface Signature {
    documentId: number;
    attachmentName: string;
    detailsOnlyDocs: SignatureDetail[];
    detailsNotOnlyDocs: SignatureDetail[];
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

      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 pb-10 truncate max-w-full">
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
              indicatorNumber={
                (doc.detailsOnlyDocs?.length ?? 0) +
                (doc.detailsNotOnlyDocs?.reduce((acc, detail) => {
                  return acc + 1 + (detail.otherSignatures?.length ?? 0);
                }, 0) ?? 0)
              }
            >
              <ul className="text-gray-700 space-y-2">
                {/* detailsOnlyDocs → normal */}
                {(doc.detailsOnlyDocs ?? []).map((assinante, idx) => (
                  <li
                    key={`only-${idx}`}
                    className="flex items-center justify-between"
                  >
                    <span className="flex items-center gap-1">
                      {assinante.userName}{' '}
                      <span className="text-gray-500">
                        (
                        {profileLabels[assinante.userProfile] ||
                          assinante.userProfile}
                        )
                      </span>
                    </span>
                    <span
                      className={`flex items-center font-semibold gap-1 ${
                        assinante.isSigned
                          ? 'text-green-600'
                          : 'text-yellow-600'
                      }`}
                    >
                      <FontAwesomeIcon
                        icon={assinante.isSigned ? faCheckCircle : faClock}
                        className="h-4 w-4"
                      />
                      {assinante.isSigned ? 'Assinado' : 'Pendente'}
                    </span>
                  </li>
                ))}

                {/* detailsNotOnlyDocs → normal */}
                {(doc.detailsNotOnlyDocs ?? []).map((assinante, idx) => (
                  <React.Fragment key={`notOnly-${idx}`}>
                    <li className="flex items-center justify-between">
                      <span className="flex items-center gap-2">
                        {assinante.userName}{' '}
                        <span className="text-gray-500">
                          (
                          {profileLabels[assinante.userProfile] ||
                            assinante.userProfile}
                          )
                        </span>
                        {/* Ícone de informação se tiver filhos */}
                        {assinante.otherSignatures &&
                          assinante.otherSignatures.length > 0 && (
                            <div className="group relative">
                              <FontAwesomeIcon
                                icon={faInfoCircle}
                                className="text-blue-500 ml-1"
                              />
                              <div className="absolute left-1/2 transform -translate-x-1/2 mt-2 w-64 bg-white p-2 text-sm text-gray-700 rounded shadow-lg z-10 hidden group-hover:block">
                                Esse anexo precisa ser separado em documentos
                                individuais.
                              </div>
                            </div>
                          )}
                      </span>
                      <span
                        className={`flex items-center font-semibold gap-1 ${
                          assinante.isSigned
                            ? 'text-green-600'
                            : 'text-yellow-600'
                        }`}
                      >
                        <FontAwesomeIcon
                          icon={assinante.isSigned ? faCheckCircle : faClock}
                          className="h-4 w-4"
                        />
                        {assinante.isSigned ? 'Assinado' : 'Pendente'}
                      </span>
                    </li>

                    {/* Assinaturas filhas */}
                    {(assinante.otherSignatures ?? []).map((sub, subIdx) => (
                      <li
                        key={`sub-${idx}-${subIdx}`}
                        className="flex items-center justify-between pl-8 border-l-2 border-[#1351B4]/30 ml-2"
                      >
                        <span className="flex items-center gap-1">
                          <FontAwesomeIcon
                            icon={faArrowRight}
                            className="text-xs text-[#1351B4]"
                          />
                          {sub.userName}{' '}
                          <span className="text-gray-500">
                            ({profileLabels[sub.userProfile] || sub.userProfile}
                            )
                          </span>
                        </span>
                        <span
                          className={`flex items-center font-semibold gap-1 ${
                            sub.isSigned ? 'text-green-600' : 'text-yellow-600'
                          }`}
                        >
                          <FontAwesomeIcon
                            icon={sub.isSigned ? faCheckCircle : faClock}
                            className="h-4 w-4"
                          />
                          {sub.isSigned ? 'Assinado' : 'Pendente'}
                        </span>
                      </li>
                    ))}
                  </React.Fragment>
                ))}
              </ul>
            </CollapseCard>
          ))}
        </div>
      )}
    </div>
  );
}
