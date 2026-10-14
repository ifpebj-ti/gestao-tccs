'use client';

import {
  HomeShortcuts,
  type HomeShortcut
} from '@/components/home/HomeShortcuts';
import { PageTitle } from '@/components/layout/PageTitle';
import { NoticeCard } from '@/components/layout/NoticeCard';
import {
  ProposalInvitations,
  ProposalConsentStatus
} from '@/components/home/ProposalInvitations';
import { Button } from '@/components/ui/button';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from '@/components/ui/alert-dialog';
import {
  FileSignature,
  GraduationCap,
  CheckCircle2,
  Inbox,
  FilePlus2,
  UserPlus,
  Users,
  Building2,
  Calendar,
  Clock,
  AlertTriangle,
  ArrowRight,
  FileText
} from 'lucide-react';
import Link from 'next/link';
import { jwtDecode } from 'jwt-decode';
import Cookies from 'js-cookie';
import { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string | string[];
}

interface TCCsInformation {
  pendingSignature: number;
  tccInprogress: number;
}

interface UserTCCs {
  tccId: number;
  studanteNames: string[];
}

interface ProposalInfo {
  tccId: number;
  title: string;
  summary: string;
  advisorName?: string;
  rejectionReason?: string | null;
  status?: string;
}

interface AdvisorProposalItem {
  tccId: number;
  studanteNames: string[];
  title: string;
  status: string;
  summary: string;
  pendingParticipantCount: number;
}

export default function HomePage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const [profile, setProfile] = useState<string | string[] | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [userName, setUserName] = useState<string>('');
  const [tccs, setTccs] = useState<TCCsInformation | null>(null);
  const [userTCCs, setUserTCCs] = useState<UserTCCs[]>([]);
  const [completedUserTCC, setCompletedUserTCC] = useState<UserTCCs[]>([]);

  // Discente: propostas pendentes ou recusadas
  const [pendingProposal, setPendingProposal] = useState<ProposalInfo | null>(
    null
  );
  const [rejectedProposal, setRejectedProposal] = useState<ProposalInfo | null>(
    null
  );

  // Docente/Orientador: propostas recebidas para aprovação
  const [advisorPendingProposals, setAdvisorPendingProposals] = useState<
    AdvisorProposalItem[]
  >([]);
  // Modal de cancelamento de proposta (discente)
  const [isCancelProposalDialogOpen, setIsCancelProposalDialogOpen] =
    useState(false);
  const [proposalToCancelId, setProposalToCancelId] = useState<number | null>(
    null
  );
  const [actionLoading, setActionLoading] = useState(false);

  const checkUserTccStatus = useCallback(
    async (userId: string) => {
      const token = Cookies.get('token');
      if (!token) return;

      try {
        // 1. TCCs concluídos
        const completedRes = await fetch(
          `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=COMPLETED`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (completedRes.ok) {
          const completedData = await completedRes.json();
          if (completedData && completedData.length > 0) {
            setCompletedUserTCC(completedData);
            setPendingProposal(null);
            setRejectedProposal(null);
            return;
          }
        }

        // 2. TCCs em andamento
        const inProgressRes = await fetch(
          `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=IN_PROGRESS`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (inProgressRes.ok) {
          const inProgressData = await inProgressRes.json();
          if (inProgressData && inProgressData.length > 0) {
            setUserTCCs(inProgressData);
            setPendingProposal(null);
            setRejectedProposal(null);
            return;
          }
        }

        // 3. Proposta pendente de aprovação
        const pendingResponses = await Promise.all(
          ['AWAITING_PARTICIPANTS', 'PENDING_APPROVAL'].map((status) =>
            fetch(
              `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=${status}`,
              { headers: { Authorization: `Bearer ${token}` } }
            )
          )
        );
        if (pendingResponses.every((response) => response.ok)) {
          const pendingData = (
            await Promise.all(
              pendingResponses.map((response) => response.json())
            )
          ).flat();
          if (pendingData && pendingData.length > 0) {
            const tccId = pendingData[0].tccId;
            const detailRes = await fetch(`${API_URL}/Tcc?tccId=${tccId}`, {
              headers: { Authorization: `Bearer ${token}` }
            });
            if (detailRes.ok) {
              const detailData = await detailRes.json();
              setPendingProposal({
                tccId,
                title:
                  detailData.infoTcc?.title ||
                  pendingData[0].title ||
                  'Proposta de TCC',
                summary: detailData.infoTcc?.summary || '',
                advisorName: detailData.infoAdvisor?.name || '',
                status: detailData.infoTcc?.status
              });
              setRejectedProposal(null);
              return;
            }
          }
        }

        // 4. Proposta recusada (aguardando reformulação ou cancelamento)
        const rejectedRes = await fetch(
          `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=REJECTED`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (rejectedRes.ok) {
          const rejectedData = await rejectedRes.json();
          if (rejectedData && rejectedData.length > 0) {
            const tccId = rejectedData[0].tccId;
            const detailRes = await fetch(`${API_URL}/Tcc?tccId=${tccId}`, {
              headers: { Authorization: `Bearer ${token}` }
            });
            if (detailRes.ok) {
              const detailData = await detailRes.json();
              setRejectedProposal({
                tccId,
                title:
                  detailData.infoTcc?.title ||
                  rejectedData[0].title ||
                  'Proposta de TCC',
                summary: detailData.infoTcc?.summary || '',
                advisorName: detailData.infoAdvisor?.name || '',
                rejectionReason: detailData.infoTcc?.rejectionReason || ''
              });
              setPendingProposal(null);
              return;
            }
          }
        }

        // Discente sem TCC e sem proposta ativa
        setPendingProposal(null);
        setRejectedProposal(null);
        setUserTCCs([]);
        setCompletedUserTCC([]);
      } catch {
        toast.error('Erro ao verificar o status do seu TCC.');
      }
    },
    [API_URL]
  );

  const fetchAdvisorPendingProposals = useCallback(
    async (userId: string) => {
      const token = Cookies.get('token');
      if (!token) return;

      try {
        const res = await fetch(
          `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=PENDING_APPROVAL`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (!res.ok) return;

        const data = await res.json();
        if (Array.isArray(data)) {
          const detailedList = await Promise.all(
            data.map(
              async (item: {
                tccId: number;
                studanteNames?: string[];
                title?: string;
              }) => {
                try {
                  const detailRes = await fetch(
                    `${API_URL}/Tcc?tccId=${item.tccId}`,
                    {
                      headers: { Authorization: `Bearer ${token}` }
                    }
                  );
                  if (detailRes.ok) {
                    const detailData = await detailRes.json();
                    return {
                      tccId: item.tccId,
                      studanteNames: item.studanteNames || [],
                      title:
                        detailData.infoTcc?.title ||
                        item.title ||
                        'Proposta de TCC',
                      status: 'PENDING_APPROVAL',
                      summary: detailData.infoTcc?.summary || '',
                      pendingParticipantCount:
                        detailData.infoTcc?.pendingParticipantCount ?? 0
                    };
                  }
                } catch {
                  // fallback
                }
                return {
                  tccId: item.tccId,
                  studanteNames: item.studanteNames || [],
                  title: item.title || 'Proposta de TCC',
                  status: 'PENDING_APPROVAL',
                  summary: '',
                  pendingParticipantCount: 1
                };
              }
            )
          );
          setAdvisorPendingProposals(detailedList);
        }
      } catch {
        // silencioso
      }
    },
    [API_URL]
  );

  const fetchTccs = useCallback(async () => {
    try {
      const token = Cookies.get('token');
      if (!token) {
        toast.error('Token de autenticação não encontrado.');
        return;
      }

      const res = await fetch(`${API_URL}/Home`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      if (!res.ok) {
        throw new Error('Erro ao buscar informações dos TCCs.');
      }

      const data: TCCsInformation = await res.json();
      setTccs(data);
    } catch {
      toast.error('Erro ao carregar informações dos TCCs.');
    }
  }, [API_URL]);

  useEffect(() => {
    const token = Cookies.get('token');
    if (token) {
      const decodedToken = jwtDecode<DecodedToken>(token);
      setProfile(decodedToken.role);
      setCurrentUserId(decodedToken.userId);
      setUserName(decodedToken.unique_name || '');

      const userHasStudentRole = Array.isArray(decodedToken.role)
        ? decodedToken.role.includes('STUDENT')
        : decodedToken.role === 'STUDENT';

      if (userHasStudentRole) {
        checkUserTccStatus(decodedToken.userId);
      }

      const userHasAdvisorRole = Array.isArray(decodedToken.role)
        ? decodedToken.role.some((r) =>
            ['ADVISOR', 'COORDINATOR', 'SUPERVISOR'].includes(r)
          )
        : ['ADVISOR', 'COORDINATOR', 'SUPERVISOR'].includes(decodedToken.role);

      if (userHasAdvisorRole) {
        fetchAdvisorPendingProposals(decodedToken.userId);
      }
    }
  }, [checkUserTccStatus, fetchAdvisorPendingProposals]);

  useEffect(() => {
    fetchTccs();
  }, [fetchTccs]);

  const canView = (allowedRoles: string[]) => {
    if (!profile) return false;
    if (typeof profile === 'string') return allowedRoles.includes(profile);
    if (Array.isArray(profile))
      return profile.some((userRole) => allowedRoles.includes(userRole));
    return false;
  };

  const isStudent = () => {
    if (!profile) return false;
    if (typeof profile === 'string') return profile === 'STUDENT';
    if (Array.isArray(profile)) return profile.includes('STUDENT');
    return false;
  };

  const canShowPendingSignatures = () => {
    if (isStudent()) {
      return userTCCs.length > 0 || completedUserTCC.length > 0;
    }
    return canView([
      'COORDINATOR',
      'SUPERVISOR',
      'ADVISOR',
      'LIBRARY',
      'BANKING'
    ]);
  };

  const handleMyTccClick = () => {
    if (completedUserTCC.length > 0) {
      push(`/completedTCCs/${completedUserTCC[0].tccId}`);
    } else if (userTCCs.length > 0) {
      push(`/myTCC/signatures?id=${userTCCs[0].tccId}`);
    }
  };

  // Cancelar proposta (discente)
  const handleCancelProposalSubmit = async () => {
    if (!proposalToCancelId) return;
    const token = Cookies.get('token');
    if (!token) return;

    setActionLoading(true);
    try {
      const res = await fetch(
        `${API_URL}/Tcc/${proposalToCancelId}/cancel-proposal`,
        {
          method: 'PATCH',
          headers: { Authorization: `Bearer ${token}` }
        }
      );

      if (!res.ok) {
        const errorData = await res.json().catch(() => null);
        throw new Error(
          errorData?.detail ||
            errorData?.message ||
            'Falha ao cancelar a proposta.'
        );
      }

      toast.success('Proposta cancelada com sucesso.');
      setIsCancelProposalDialogOpen(false);
      setProposalToCancelId(null);
      if (currentUserId) {
        await checkUserTccStatus(currentUserId);
      }
    } catch (error: unknown) {
      toast.error(
        error instanceof Error ? error.message : 'Erro ao cancelar proposta.'
      );
    } finally {
      setActionLoading(false);
    }
  };

  const isStudentWithoutProposal =
    isStudent() &&
    userTCCs.length === 0 &&
    completedUserTCC.length === 0 &&
    !pendingProposal &&
    !rejectedProposal;

  const supportText = isStudent()
    ? 'Confira o que precisa da sua atenção.'
    : canView(['ADMIN'])
      ? 'Gerencie usuários, instituições, cursos e períodos letivos em um só lugar.'
      : canView(['ADVISOR', 'COORDINATOR', 'SUPERVISOR'])
        ? 'Avalie propostas, acompanhe suas orientações e assine os documentos de cada etapa acadêmica.'
        : canView(['LIBRARY', 'BANKING'])
          ? 'Acompanhe os trabalhos de conclusão de curso e os documentos que aguardam sua assinatura.'
          : 'Acompanhe as atividades acadêmicas e acesse as ferramentas disponíveis para seu perfil.';

  const shortcuts: HomeShortcut[] = [
    {
      title: 'Assinaturas pendentes',
      icon: FileSignature,
      group: 'attention',
      visible: canShowPendingSignatures(),
      description:
        tccs === null
          ? 'Documentos que aguardam sua validação digital'
          : tccs.pendingSignature > 0
            ? `${tccs.pendingSignature} aguardando sua assinatura`
            : 'Nenhuma pendência',
      indicatorNumber: tccs?.pendingSignature || 0,
      indicatorColor: 'bg-red-600 text-white',
      onClick: () => push('/pendingSignatures')
    },
    {
      title: 'Propostas de TCC pendentes',
      icon: Inbox,
      group: 'attention',
      visible: canView(['ADVISOR', 'COORDINATOR', 'SUPERVISOR']),
      description:
        advisorPendingProposals.length > 0
          ? `${advisorPendingProposals.length} aguardando seu aceite`
          : 'Propostas submetidas por estudantes aguardando aceite',
      indicatorNumber: advisorPendingProposals.length,
      indicatorColor: 'bg-amber-600 text-white',
      onClick: () => push('/receivedProposals')
    },
    {
      title: 'TCCs em andamento',
      icon: GraduationCap,
      group: 'tracking',
      visible: canView([
        'COORDINATOR',
        'SUPERVISOR',
        'ADVISOR',
        'LIBRARY',
        'BANKING'
      ]),
      description: 'Trabalhos acadêmicos ativos no período',
      indicatorNumber: tccs?.tccInprogress || 0,
      indicatorColor: 'bg-[#002B18] text-[#C8FF47]',
      onClick: () => push('/ongoingTCCs')
    },
    {
      title: 'TCCs concluídos',
      icon: CheckCircle2,
      group: 'tracking',
      visible: canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY']),
      description: 'Histórico de trabalhos finalizados e defendidos',
      onClick: () => push('/completedTCCs')
    },
    {
      title: 'Meu TCC',
      icon: GraduationCap,
      group: 'tracking',
      visible:
        canView(['STUDENT']) &&
        (userTCCs.length > 0 || completedUserTCC.length > 0),
      description: 'Acesse o fluxo do seu projeto, assinaturas e etapas',
      onClick: handleMyTccClick
    },
    {
      title: 'Cadastrar nova proposta',
      icon: FilePlus2,
      group: 'management',
      visible: canView(['COORDINATOR', 'SUPERVISOR']),
      description: 'Inclusão direta de propostas e vínculos',
      onClick: () => push('/newTCC')
    },
    {
      title: 'Cadastrar novo usuário',
      icon: UserPlus,
      group: 'management',
      visible: canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']),
      description: 'Adicione docentes, coordenadores ou estudantes',
      onClick: () => push('/newUser')
    },
    {
      title: 'Usuários',
      icon: Users,
      group: 'management',
      visible: canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']),
      description: 'Gerenciamento de acessos e permissões',
      onClick: () => push('/users')
    },
    {
      title: 'Instituições e Cursos',
      icon: Building2,
      group: 'management',
      visible: canView(['ADMIN']),
      description: 'Configuração de campi e matrizes curriculares',
      onClick: () => push('/instituicoes')
    },
    {
      title: 'Semestres Letivos',
      icon: Calendar,
      group: 'management',
      visible: canView(['ADMIN']),
      description: 'Períodos e prazos institucionais',
      onClick: () => push('/semestres')
    }
  ];

  return (
    <div className="flex flex-col w-full">
      {/* CABEÇALHO EDITORIAL MINIMALISTA (SEM BREADCRUMB E SEM TAG) */}
      <div className="mb-8">
        <PageTitle>
          {userName ? (
            <>
              Olá, <span className="text-[#00A355]">{userName}</span>!
            </>
          ) : (
            'Página Inicial'
          )}
        </PageTitle>
        <p className="mt-2 text-sm md:text-base text-gray-600 max-w-2xl leading-relaxed">
          {supportText}
        </p>
      </div>

      {/* BANNER DISCENTE: PROPOSTA PENDENTE DE APROVAÇÃO (SEAMLESS FLAT) */}
      {isStudent() && <ProposalInvitations />}
      {isStudent() && pendingProposal && (
        <NoticeCard title="Acompanhamento da proposta" className="mb-8">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-start gap-3">
              <Clock
                className="w-5 h-5 text-[#C8FF47] flex-shrink-0 mt-0.5"
                strokeWidth={1.75}
              />
              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-[#C8FF47]">
                  {pendingProposal.status === 'AWAITING_PARTICIPANTS'
                    ? 'Aguardando participantes'
                    : 'Proposta em análise'}
                </span>
                <h3 className="text-base font-bold mt-0.5">
                  {pendingProposal.title}
                </h3>
                <p className="text-sm mt-0.5">
                  {pendingProposal.status === 'AWAITING_PARTICIPANTS' ? (
                    'A proposta será enviada ao professor somente quando todos os integrantes aceitarem.'
                  ) : (
                    <>
                      Aguardando avaliação de{' '}
                      <strong>
                        {pendingProposal.advisorName || 'seu orientador'}
                      </strong>
                      .
                    </>
                  )}
                </p>
                <ProposalConsentStatus tccId={pendingProposal.tccId} />
              </div>
            </div>

            <Button
              variant="destructive"
              size="sm"
              className="self-start md:self-center font-medium"
              onClick={() => {
                setProposalToCancelId(pendingProposal.tccId);
                setIsCancelProposalDialogOpen(true);
              }}
              disabled={actionLoading}
            >
              Cancelar proposta
            </Button>
          </div>
        </NoticeCard>
      )}

      {/* BANNER DISCENTE: PROPOSTA RECUSADA / REFORMULAR (SEAMLESS FLAT) */}
      {isStudent() && rejectedProposal && (
        <NoticeCard title="Proposta devolvida para ajustes" className="mb-8">
          <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
            <div className="flex items-start gap-3">
              <AlertTriangle
                className="w-5 h-5 text-[#C8FF47] flex-shrink-0 mt-0.5"
                strokeWidth={1.75}
              />
              <div>
                <h3 className="text-base font-bold mt-0.5">
                  {rejectedProposal.title}
                </h3>
                <p className="text-sm mt-0.5">
                  Devolvida por <strong>{rejectedProposal.advisorName}</strong>.
                </p>
                {rejectedProposal.rejectionReason && (
                  <p className="mt-2 text-sm italic">
                    Motivo: &ldquo;{rejectedProposal.rejectionReason}&rdquo;
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2 self-start md:self-center">
              <Button
                variant="destructive"
                size="sm"
                className=""
                onClick={() => {
                  setProposalToCancelId(rejectedProposal.tccId);
                  setIsCancelProposalDialogOpen(true);
                }}
                disabled={actionLoading}
              >
                Cancelar
              </Button>
              <Button
                size="sm"
                className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold"
                onClick={() =>
                  push(`/newTCC?reformulate=${rejectedProposal.tccId}`)
                }
                disabled={actionLoading}
              >
                Reformular Proposta
              </Button>
            </div>
          </div>
        </NoticeCard>
      )}

      {/* ESTADO DO ESTUDANTE: SEM PROPOSTA ATIVA (TOTALMENTE FLAT, SEM CARD) */}
      {isStudentWithoutProposal && (
        <div className="pt-2 pb-10">
          <div className="max-w-2xl">
            <h2 className="text-xl md:text-2xl font-bold text-[#002B18] tracking-tight mb-2">
              Cadastrar Nova Proposta de TCC
            </h2>
            <p className="text-sm md:text-base text-gray-600 mb-6 leading-relaxed">
              Você ainda não possui um trabalho de conclusão em andamento.
              Defina seu tema, redija o resumo da proposta e selecione um
              professor orientador para submeter à avaliação.
            </p>

            <div className="flex flex-wrap items-center gap-6 mb-8 text-xs text-gray-600 font-medium">
              <div className="flex items-center gap-2">
                <GraduationCap
                  className="w-4 h-4 text-[#002B18]"
                  strokeWidth={1.75}
                />
                <span>Escolha seu orientador</span>
              </div>
              <div className="flex items-center gap-2">
                <FileText
                  className="w-4 h-4 text-[#002B18]"
                  strokeWidth={1.75}
                />
                <span>Defina tema e resumo</span>
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle2
                  className="w-4 h-4 text-[#002B18]"
                  strokeWidth={1.75}
                />
                <span>Submissão 100% digital</span>
              </div>
            </div>

            <Link
              href="/newTCC"
              className="inline-flex items-center gap-2.5 px-7 py-3.5 bg-[#002B18] text-[#C8FF47] hover:bg-[#00A355] hover:text-white font-bold text-sm rounded-full transition-all duration-300 shadow-sm hover:shadow-md hover:scale-[1.01] active:scale-[0.98]"
            >
              <FilePlus2 className="w-4 h-4" strokeWidth={1.75} />
              Cadastrar Proposta de TCC
              <ArrowRight className="w-4 h-4" strokeWidth={1.75} />
            </Link>
          </div>
        </div>
      )}

      <HomeShortcuts
        cards={shortcuts}
        attentionOnly={
          isStudent() || canView(['ADVISOR', 'COORDINATOR', 'SUPERVISOR'])
        }
        loading={tccs === null}
      />

      {/* CONFIRMAÇÃO DE CANCELAMENTO (DISCENTE) */}
      <AlertDialog
        open={isCancelProposalDialogOpen}
        onOpenChange={setIsCancelProposalDialogOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="text-[#002B18]">
              Deseja realmente cancelar esta proposta?
            </AlertDialogTitle>
            <AlertDialogDescription>
              Esta ação cancelará permanentemente a submissão. Você poderá
              cadastrar uma nova proposta quando desejar.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={actionLoading}>
              Voltar
            </AlertDialogCancel>
            <AlertDialogAction
              className="bg-red-600 hover:bg-red-700 text-white"
              onClick={handleCancelProposalSubmit}
              disabled={actionLoading}
            >
              {actionLoading ? 'Cancelando...' : 'Sim, cancelar proposta'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
