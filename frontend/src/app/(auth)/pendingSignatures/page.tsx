'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';
import { NoticeCard } from '@/components/layout/NoticeCard';

import { useState } from 'react';
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

export default function PendingSignaturesPage() {
  const { push } = useRouter();
  const {
    groupedByUser,
    isLoading,
    profile,
    showAll,
    setShowAll,
    pendingData,
    userId: loggedInUserId
  } = usePendingSignatures();

  const [openUserCards, setOpenUserCards] = useState<Set<number>>(new Set());
  const [openTccGroups, setOpenTccGroups] = useState<Set<string>>(new Set());

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
    </div>
  );
}
