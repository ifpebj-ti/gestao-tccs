'use client';

import {
  usePendingSignatures,
  PendingTcc,
  GroupedByUserAndTcc
} from '@/app/hooks/usePendingSignatures';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { CollapseCard } from '@/components/CollapseCard';
import {
  faUser,
  faGraduationCap,
  faFileSignature,
  faInfoCircle
} from '@fortawesome/free-solid-svg-icons';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useRouter } from 'next/navigation';

export default function PendingSignaturesPage() {
  const { push } = useRouter();
  const {
    pendingData,
    groupedByUser,
    isLoading,
    profile,
    showAll,
    setShowAll
  } = usePendingSignatures();

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
      profile.some((r) => ['ADMIN', 'SUPERVISOR'].includes(r))) ||
      (typeof profile === 'string' &&
        ['ADMIN', 'SUPERVISOR'].includes(profile)));

  // Função de renderização para a visão TCC -> Documentos
  const renderTccCard = (tcc: PendingTcc) => {
    const totalPending = tcc.pendingDetails.reduce(
      (total, doc) => total + doc.userDetails.length,
      0
    );
    return (
      <CollapseCard
        key={tcc.tccId}
        title={
          'TCC - ' + (tcc.studentNames.join(', ') || 'TCC sem aluno definido')
        }
        icon={faGraduationCap}
        indicatorNumber={totalPending}
        indicatorColor="bg-red-600"
      >
        <div className="space-y-2">
          {tcc.pendingDetails.map((doc) => (
            <div
              key={doc.documentId}
              className="p-3 bg-gray-50 rounded-md border hover:bg-gray-100 transition cursor-pointer"
              onClick={() => push(`/assinatura/${doc.documentId}`)}
            >
              <p className="font-semibold text-gray-700 flex items-center gap-2">
                <FontAwesomeIcon
                  icon={faFileSignature}
                  className="text-gray-500"
                />
                {doc.documentName}
              </p>
            </div>
          ))}
        </div>
      </CollapseCard>
    );
  };

  // Função de renderização para a visão Usuário -> TCC -> Documentos
  const renderUserCard = (userGroup: GroupedByUserAndTcc) => {
    const totalTasks = userGroup.tccGroups.reduce(
      (acc, group) => acc + group.documents.length,
      0
    );
    return (
      <CollapseCard
        key={userGroup.userId}
        title={`${userGroup.userName} (${profileLabels[userGroup.userProfile] || userGroup.userProfile})`}
        icon={faUser}
        indicatorNumber={totalTasks}
        indicatorColor="bg-red-600"
      >
        <div className="space-y-2">
          {userGroup.tccGroups.map((tccGroup) => (
            <CollapseCard
              key={tccGroup.tccId}
              title={`TCC de: ${tccGroup.studentNames.join(', ')}`}
              icon={faGraduationCap}
              indicatorNumber={tccGroup.documents.length}
              indicatorColor="bg-blue-600"
            >
              <div className="space-y-3 pl-2">
                {tccGroup.documents.map((doc) => (
                  <div
                    key={doc.documentId}
                    className="p-3 bg-gray-50 rounded-md border hover:bg-gray-100 transition cursor-pointer"
                    onClick={() => push(`/assinatura/${doc.documentId}`)}
                  >
                    <p className="font-semibold text-gray-700 flex items-center gap-2">
                      <FontAwesomeIcon
                        icon={faFileSignature}
                        className="text-gray-500"
                      />
                      {doc.documentName}
                    </p>
                  </div>
                ))}
              </div>
            </CollapseCard>
          ))}
        </div>
      </CollapseCard>
    );
  };

  // As variáveis 'dataToRender' e 'renderFunction' foram removidas.

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <div className="flex flex-col md:flex-row md:items-center justify-between mb-10">
        <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800">
          Assinaturas Pendentes
        </h1>
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
          <div className="flex items-center gap-3 p-3 text-sm text-blue-800 rounded-lg bg-blue-50 mb-6">
            <FontAwesomeIcon icon={faInfoCircle} className="h-5 w-5" />
            <div>
              <span className="font-medium">Nota:</span> As assinaturas são
              liberadas em uma ordem específica. Uma pendência pode não ser
              exibida aqui se uma etapa anterior ainda não foi concluída.
            </div>
          </div>
        )}

      {isLoading ? (
        <p className="text-center text-gray-500 mt-4">Carregando...</p>
      ) : (showAll ? groupedByUser.length === 0 : pendingData.length === 0) ? (
        <p className="text-center text-gray-500 mt-4">
          Nenhuma assinatura pendente encontrada.
        </p>
      ) : (
        <>
          <div className="md:hidden flex flex-col gap-2">
            {showAll
              ? groupedByUser.map((userGroup) => renderUserCard(userGroup))
              : pendingData.map((tcc) => renderTccCard(tcc))}
          </div>
          <div className="hidden md:grid grid-cols-2 lg:grid-cols-3 gap-4">
            {showAll
              ? groupedByUser.map((userGroup) => renderUserCard(userGroup))
              : pendingData.map((tcc) => renderTccCard(tcc))}
          </div>
        </>
      )}
    </div>
  );
}
