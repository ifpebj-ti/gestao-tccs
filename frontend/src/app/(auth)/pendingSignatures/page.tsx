'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';
import { NoticeCard } from '@/components/layout/NoticeCard';

import { useCallback, useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import {
  usePendingSignatures,
  PendingTcc,
  GroupedByUserAndTcc
} from '@/app/hooks/usePendingSignatures';
import { CollapseCard } from '@/components/CollapseCard';
import {
  faUser,
  faGraduationCap,
  faFileSignature
} from '@fortawesome/free-solid-svg-icons';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';
import { env } from 'next-runtime-env';

interface PendingInternalEvaluation {
  tccId: number;
  tccTitle: string | null;
  scheduledDate: string;
  location: string;
  studentNames: string[];
}

export default function PendingSignaturesPage() {
  const { push } = useRouter();
  const {
    groupedByUser,
    isLoading,
    profile,
    showAll,
    setShowAll,
    pendingData,
    userId: loggedInUserId,
    fetchPendingSignatures
  } = usePendingSignatures();

  const [openUserCards, setOpenUserCards] = useState<Set<number>>(new Set());
  const [openTccGroups, setOpenTccGroups] = useState<Set<string>>(new Set());
  const [pendingEvaluations, setPendingEvaluations] = useState<PendingInternalEvaluation[]>([]);
  const [selectedEvaluation, setSelectedEvaluation] = useState<PendingInternalEvaluation | null>(null);

  const loadPendingEvaluations = useCallback(async () => {
    const token = Cookies.get('token');
    if (!token) return;
    try {
      const response = await fetch(`${env('NEXT_PUBLIC_API_URL')}/Tcc/banking/pending-evaluations`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!response.ok) throw new Error();
      setPendingEvaluations(await response.json());
    } catch {
      toast.error('Erro ao carregar avaliações de banca pendentes.');
    }
  }, []);

  useEffect(() => {
    loadPendingEvaluations();
  }, [loadPendingEvaluations]);

  const profileLabels: Record<string, string> = {
    STUDENT: 'Aluno',
    ADVISOR: 'Orientador',
    COORDINATOR: 'Coordenador',
    SUPERVISOR: 'Supervisor',
    LIBRARY: 'Bibliotecária'
  };

  const canToggleView =
    profile &&
    ((Array.isArray(profile) &&
      profile.some((r) =>
        ['ADMIN', 'COORDINATOR', 'SUPERVISOR'].includes(r)
      )) ||
      (typeof profile === 'string' &&
        ['ADMIN', 'COORDINATOR', 'SUPERVISOR'].includes(profile)));

  const toggleUserCard = (userId: number) => {
    setOpenUserCards((prevOpen) => {
      const newOpen = new Set(prevOpen);
      if (newOpen.has(userId)) {
        newOpen.delete(userId);
      } else {
        newOpen.add(userId);
      }
      return newOpen;
    });
  };

  const toggleTccGroup = (tccGroupId: string) => {
    setOpenTccGroups((prevOpen) => {
      const newOpen = new Set(prevOpen);
      if (newOpen.has(tccGroupId)) {
        newOpen.delete(tccGroupId);
      } else {
        newOpen.add(tccGroupId);
      }
      return newOpen;
    });
  };

  // Visão "Minhas Pendências" (TCC -> Documentos)
  const renderTccCard = (tcc: PendingTcc) => {
    const totalPending = tcc.pendingDetails.reduce(
      (total, doc) => total + doc.userDetails.length,
      0
    );
    const cardId = `tcc-${tcc.tccId}`;

    return (
      <CollapseCard
        variant="flat"
        key={cardId}
        title={
          'TCC - ' + (tcc.studentNames.join(', ') || 'TCC sem aluno definido')
        }
        icon={faGraduationCap}
        indicatorNumber={totalPending}
        indicatorColor="bg-red-600"
        onToggle={() => toggleTccGroup(cardId)}
      >
        <div className="space-y-2">
          {tcc.pendingDetails.map((doc) => {
            const studentId = doc.userDetails[0]?.idDocumentOwner;

            return (
              <div
                key={doc.documentId}
                className="p-3 bg-gray-50 rounded-md border hover:bg-gray-100 transition cursor-pointer"
                onClick={() =>
                  push(
                    `/pendingSignatures/signature/${doc.documentId}?tccId=${
                      tcc.tccId
                    }&docName=${encodeURIComponent(doc.documentName)}&studentId=${studentId}`
                  )
                }
              >
                <p className="font-semibold text-gray-700 flex items-center gap-2">
                  <FontAwesomeIcon
                    icon={faFileSignature}
                    className="text-gray-500"
                  />
                  {doc.documentName}
                </p>
              </div>
            );
          })}
        </div>
      </CollapseCard>
    );
  };

  // Visão "Todas as Pendências" (Usuário -> TCC -> Documentos)
  const renderUserCard = (userGroup: GroupedByUserAndTcc) => {
    const totalTasks = userGroup.tccGroups.reduce(
      (acc, group) => acc + group.documents.length,
      0
    );
    return (
      <CollapseCard
        variant="flat"
        key={userGroup.userId}
        title={`${userGroup.userName} (${
          profileLabels[userGroup.userProfile] || userGroup.userProfile
        })`}
        icon={faUser}
        indicatorNumber={totalTasks}
        indicatorColor="bg-red-600"
        isOpen={openUserCards.has(userGroup.userId)}
        onToggle={() => toggleUserCard(userGroup.userId)}
      >
        <div className="space-y-2">
          {userGroup.tccGroups.map((tccGroup) => {
            const tccGroupId = `${userGroup.userId}-${tccGroup.tccId}`;
            return (
              <CollapseCard
                variant="flat"
                key={tccGroupId}
                title={`TCC de: ${tccGroup.studentNames.join(', ')}`}
                icon={faGraduationCap}
                indicatorNumber={tccGroup.documents.length}
                indicatorColor="bg-blue-600"
                isOpen={openTccGroups.has(tccGroupId)}
                onToggle={() => toggleTccGroup(tccGroupId)}
              >
                <div className="space-y-3 pl-2">
                  {tccGroup.documents.map((doc) => {
                    const isActionable =
                      String(userGroup.userId) === loggedInUserId;
                    return (
                      <div
                        key={doc.documentId}
                        className={`p-3 bg-gray-50 rounded-md border transition ${
                          isActionable
                            ? 'hover:bg-gray-100 cursor-pointer'
                            : 'cursor-not-allowed opacity-70'
                        }`}
                        onClick={
                          isActionable
                            ? () =>
                                push(
                                  `/pendingSignatures/signature/${
                                    doc.documentId
                                  }?tccId=${
                                    tccGroup.tccId
                                  }&docName=${encodeURIComponent(
                                    doc.documentName
                                  )}&studentId=${doc.studentId}`
                                )
                            : undefined
                        }
                      >
                        <p className="font-semibold text-gray-700 flex items-center gap-2">
                          <FontAwesomeIcon
                            icon={faFileSignature}
                            className="text-gray-500"
                          />
                          {doc.documentName}
                        </p>
                      </div>
                    );
                  })}
                </div>
              </CollapseCard>
            );
          })}
        </div>
      </CollapseCard>
    );
  };

  return (
    <div className="flex flex-col">
      <div className="flex flex-col md:flex-row md:items-center justify-between mb-10">
        <div className="space-y-3">
          <PageTitle className="text-2xl md:text-3xl">
            Assinaturas pendentes
          </PageTitle>
          <p className="text-sm text-slate-600">
            Selecione um documento para conferir e concluir sua assinatura.
          </p>
        </div>
        {canToggleView && (
          <Tabs
            value={showAll ? 'all' : 'mine'}
            onValueChange={(value) => setShowAll(value === 'all')}
            className="mt-4 md:mt-0"
          >
            <TabsList>
              <TabsTrigger value="mine">Minhas Pendências</TabsTrigger>
              <TabsTrigger value="all">Todas as Pendências</TabsTrigger>
            </TabsList>
          </Tabs>
        )}
      </div>

      {pendingEvaluations.length > 0 && (
        <section className="mb-8 rounded-lg border border-[#DDD9CD] bg-white p-4">
          <h2 className="mb-3 text-lg font-semibold text-[#002B18]">Avaliações de banca pendentes</h2>
          <div className="grid gap-3 md:grid-cols-2">
            {pendingEvaluations.map((evaluation) => (
              <div key={evaluation.tccId} className="rounded-md border bg-[#FAF9F5] p-4">
                <p className="font-semibold">{evaluation.tccTitle || `TCC ${evaluation.tccId}`}</p>
                <p className="mt-1 text-sm text-slate-600">{evaluation.studentNames.join(', ') || 'TCC sem aluno definido'}</p>
                <p className="mt-1 text-sm text-slate-600">
                  {new Date(evaluation.scheduledDate).toLocaleString('pt-BR')} · {evaluation.location}
                </p>
                <Button className="mt-3" onClick={() => setSelectedEvaluation(evaluation)}>Avaliar TCC</Button>
              </div>
            ))}
          </div>
        </section>
      )}

      {!isLoading &&
        (showAll ? groupedByUser.length > 0 : pendingData.length > 0) && (
          <NoticeCard title="Sobre as pendências" className="mb-6">
            As assinaturas são liberadas em uma ordem específica. Uma pendência
            pode não ser exibida aqui se uma etapa anterior ainda não foi
            concluída.
          </NoticeCard>
        )}

      {isLoading ? (
        <PageState loading>Carregando...</PageState>
      ) : (showAll ? groupedByUser.length === 0 : pendingData.length === 0) ? (
        <PageState>Nenhuma assinatura pendente encontrada.</PageState>
      ) : (
        <div className="grid grid-cols-1 gap-x-8 md:grid-cols-2 lg:grid-cols-3">
          {showAll
            ? groupedByUser.map((userGroup) => renderUserCard(userGroup))
            : pendingData.map((tcc) => renderTccCard(tcc))}
        </div>
      )}

      <Dialog open={selectedEvaluation !== null} onOpenChange={(open) => !open && setSelectedEvaluation(null)}>
        <DialogContent className="max-h-[90vh] max-w-[95vw] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Avaliar apresentação</DialogTitle>
          </DialogHeader>
          {selectedEvaluation && (
            <AvaliacaoForm
              internalTccId={selectedEvaluation.tccId}
              onSuccessCallback={() => {
                setSelectedEvaluation(null);
                void loadPendingEvaluations();
                void fetchPendingSignatures();
              }}
              hideLogoAndMinHeight
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
