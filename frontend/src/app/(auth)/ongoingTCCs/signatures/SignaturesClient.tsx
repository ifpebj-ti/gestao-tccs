'use client';

import { PageState } from '@/components/layout/PageState';

import { SectionTitle } from '@/components/layout/SectionTitle';

import { CollapseCard } from '@/components/CollapseCard';
import { TccOverview } from '@/components/tcc-details/TccOverview';
import {
  faCheckCircle,
  faFileAlt,
  faClock,
  faInfoCircle,
  faArrowRight
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React, { useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';

import { useSearchParams } from 'next/navigation';

import { env } from 'next-runtime-env';

export default function SignaturesClient() {
  const API_URL = env('NEXT_PUBLIC_API_URL');

  const profileLabels: Record<string, string> = {
    STUDENT: 'Aluno',
    ADVISOR: 'Orientador',
    COORDINATOR: 'Coordenador',
    SUPERVISOR: 'Supervisor',
    LIBRARY: 'Bibliotecária'
  };

  interface TccDetailsResponse {
    infoTcc: { title: string };
    cancellationRequest: boolean;
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

  const [title, setTitle] = useState('');
  const [data, setData] = useState<WorkflowResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [cancellationRequested, setCancellationRequested] = useState(false);
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
        const res = await fetch(`${API_URL}/Tcc/workflow?tccId=${tccId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
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
        const res = await fetch(`${API_URL}/Tcc?tccId=${tccId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (!res.ok) throw new Error('Erro ao buscar detalhes do TCC.');
        const result: TccDetailsResponse = await res.json();
        setTitle(result.infoTcc.title);
        setCancellationRequested(result.cancellationRequest);
      } catch {
        toast.error('Erro ao carregar dados dos alunos.');
      }
    };

    if (tccId) {
      fetchWorkflow();
      fetchTccDetails();
    }
  }, [tccId, API_URL]);

  return (
    <div>
      <TccOverview
        title={title}
        cancellationRequested={cancellationRequested}
        step={data?.step ?? null}
      />

      <SectionTitle className="md:mt-6 mb-4">
        Documentos e Assinaturas
      </SectionTitle>

      {loading ? (
        <PageState loading>Carregando documentos...</PageState>
      ) : (
        <div className="flex flex-col">
          {data?.signatures.map((doc, index) => (
            <CollapseCard
              variant="flat"
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

                    {(assinante.otherSignatures ?? []).map((sub, subIdx) => (
                      <li
                        key={`sub-${idx}-${subIdx}`}
                        className="flex items-center justify-between pl-8 border-l-2 border-[#006633]/30 ml-2"
                      >
                        <span className="flex items-center gap-1">
                          <FontAwesomeIcon
                            icon={faArrowRight}
                            className="text-xs text-[#006633]"
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
