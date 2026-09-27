'use client';

import { CardHome } from '@/components/CardHome';
import { CollapseCard } from '@/components/CollapseCard';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog';
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
  faCheck,
  faClock,
  faExclamationTriangle,
  faFileCircleCheck,
  faFileCirclePlus,
  faFileSignature,
  faGraduationCap,
  faInbox,
  faTimes,
  faUserPlus,
  faUsers
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
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
}

interface AdvisorProposalItem {
  tccId: number;
  studanteNames: string[];
  title: string;
  status: string;
  summary: string;
}

export default function HomePage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const [profile, setProfile] = useState<string | string[] | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);
  const [tccs, setTccs] = useState<TCCsInformation | null>(null);
  const [userTCCs, setUserTCCs] = useState<UserTCCs[]>([]);
  const [completedUserTCC, setCompletedUserTCC] = useState<UserTCCs[]>([]);

  // Discente: propostas pendentes ou recusadas
  const [pendingProposal, setPendingProposal] = useState<ProposalInfo | null>(null);
  const [rejectedProposal, setRejectedProposal] = useState<ProposalInfo | null>(null);

  // Docente/Orientador: propostas recebidas para aprovação
  const [advisorPendingProposals, setAdvisorPendingProposals] = useState<AdvisorProposalItem[]>([]);
  const [isAdvisorModalOpen, setIsAdvisorModalOpen] = useState(false);
  const [selectedProposalForReject, setSelectedProposalForReject] = useState<AdvisorProposalItem | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const [isRejectDialogOpen, setIsRejectDialogOpen] = useState(false);

  // Modal de cancelamento de proposta (discente)
  const [isCancelProposalDialogOpen, setIsCancelProposalDialogOpen] = useState(false);
  const [proposalToCancelId, setProposalToCancelId] = useState<number | null>(null);
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
        const pendingRes = await fetch(
          `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=PENDING_APPROVAL`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (pendingRes.ok) {
          const pendingData = await pendingRes.json();
          if (pendingData && pendingData.length > 0) {
            const tccId = pendingData[0].tccId;
            const detailRes = await fetch(`${API_URL}/Tcc?tccId=${tccId}`, {
              headers: { Authorization: `Bearer ${token}` }
            });
            if (detailRes.ok) {
              const detailData = await detailRes.json();
              setPendingProposal({
                tccId,
                title: detailData.infoTcc?.title || pendingData[0].title || 'Proposta de TCC',
                summary: detailData.infoTcc?.summary || '',
                advisorName: detailData.infoAdvisor?.name || ''
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
                title: detailData.infoTcc?.title || rejectedData[0].title || 'Proposta de TCC',
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
            data.map(async (item: { tccId: number; studanteNames?: string[]; title?: string }) => {
              try {
                const detailRes = await fetch(`${API_URL}/Tcc?tccId=${item.tccId}`, {
                  headers: { Authorization: `Bearer ${token}` }
                });
                if (detailRes.ok) {
                  const detailData = await detailRes.json();
                  return {
                    tccId: item.tccId,
                    studanteNames: item.studanteNames || [],
                    title: detailData.infoTcc?.title || item.title || 'Proposta de TCC',
                    status: 'PENDING_APPROVAL',
                    summary: detailData.infoTcc?.summary || ''
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
                summary: ''
              };
            })
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

      const userHasStudentRole = Array.isArray(decodedToken.role)
        ? decodedToken.role.includes('STUDENT')
        : decodedToken.role === 'STUDENT';

      if (userHasStudentRole) {
        checkUserTccStatus(decodedToken.userId);
      }

      const userHasAdvisorRole = Array.isArray(decodedToken.role)
        ? decodedToken.role.some((r) => ['ADVISOR', 'COORDINATOR', 'SUPERVISOR'].includes(r))
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

  const isLimitedView = () => {
    if (!profile) return false;
    if (typeof profile === 'string')
      return profile === 'STUDENT' || profile === 'BANKING';
    if (Array.isArray(profile)) return false;
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
    return canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY', 'BANKING']);
  };

  const handleMyTccClick = () => {
    if (completedUserTCC.length > 0) {
      push(`/completedTCCs/${completedUserTCC[0].tccId}`);
    } else if (userTCCs.length > 0) {
      push(`/myTCC/signatures?id=${userTCCs[0].tccId}`);
    }
  };

  // Cancelar proposta pelo discente
  const handleCancelProposal = async () => {
    if (!proposalToCancelId) return;
    const token = Cookies.get('token');
    if (!token) return;

    setActionLoading(true);
    try {
      const res = await fetch(`${API_URL}/Tcc/${proposalToCancelId}/cancel-proposal`, {
        method: 'PATCH',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.message || 'Falha ao cancelar proposta.');
      }

      toast.success('Proposta de TCC cancelada com sucesso.');
      setIsCancelProposalDialogOpen(false);
      setProposalToCancelId(null);
      if (currentUserId) {
        await checkUserTccStatus(currentUserId);
      }
    } catch (error: unknown) {
      toast.error(error instanceof Error ? error.message : 'Erro ao cancelar proposta.');
    } finally {
      setActionLoading(false);
    }
  };

  // Aceitar proposta pelo docente
  const handleApproveProposal = async (tccId: number) => {
    const token = Cookies.get('token');
    if (!token) return;

    setActionLoading(true);
    try {
      const res = await fetch(`${API_URL}/Tcc/${tccId}/approve-proposal`, {
        method: 'PATCH',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.message || 'Falha ao aprovar proposta.');
      }

      toast.success('Proposta de orientação aceita com sucesso! O TCC foi iniciado.');
      if (currentUserId) {
        await fetchAdvisorPendingProposals(currentUserId);
        await fetchTccs();
      }
    } catch (error: unknown) {
      toast.error(error instanceof Error ? error.message : 'Erro ao aprovar proposta.');
    } finally {
      setActionLoading(false);
    }
  };

  // Recusar proposta pelo docente
  const handleRejectProposal = async () => {
    if (!selectedProposalForReject) return;
    if (!rejectReason.trim()) {
      toast.error('Informe o motivo da recusa da proposta.');
      return;
    }

    const token = Cookies.get('token');
    if (!token) return;

    setActionLoading(true);
    try {
      const res = await fetch(`${API_URL}/Tcc/${selectedProposalForReject.tccId}/reject-proposal`, {
        method: 'PATCH',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ reason: rejectReason.trim() })
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.message || 'Falha ao recusar proposta.');
      }

      toast.success('Proposta recusada com sucesso. O discente foi notificado.');
      setIsRejectDialogOpen(false);
      setSelectedProposalForReject(null);
      setRejectReason('');
      if (currentUserId) {
        await fetchAdvisorPendingProposals(currentUserId);
        await fetchTccs();
      }
    } catch (error: unknown) {
      toast.error(error instanceof Error ? error.message : 'Erro ao recusar proposta.');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-8">
        Página Inicial
      </h1>

      {/* BANNER DISCENTE: PROPOSTA PENDENTE DE APROVAÇÃO */}
      {isStudent() && pendingProposal && (
        <div className="mb-8 flex items-center justify-between border-b pb-4">
          <p className="text-gray-700">
            <FontAwesomeIcon icon={faClock} className="text-amber-500 mr-2" />
            Sua proposta <strong>{pendingProposal.title}</strong> está em análise por <strong>{pendingProposal.advisorName || 'seu orientador'}</strong>.
          </p>
          <Button
            variant="link"
            className="text-red-600 p-0 h-auto font-normal"
            onClick={() => {
              setProposalToCancelId(pendingProposal.tccId);
              setIsCancelProposalDialogOpen(true);
            }}
            disabled={actionLoading}
          >
            Cancelar proposta
          </Button>
        </div>
      )}

      {/* BANNER DISCENTE: PROPOSTA RECUSADA / REFORMULAR */}
      {isStudent() && rejectedProposal && (
        <div className="mb-8 border-b pb-4">
          <div className="flex items-center justify-between mb-2">
            <p className="text-gray-700">
              <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mr-2" />
              Sua proposta <strong>{rejectedProposal.title}</strong> foi devolvida por <strong>{rejectedProposal.advisorName}</strong>.
            </p>
            <div className="flex gap-4">
              <Button
                variant="link"
                className="text-[#1351B4] p-0 h-auto font-normal"
                onClick={() => push(`/newTCC?reformulate=${rejectedProposal.tccId}`)}
                disabled={actionLoading}
              >
                Reformular
              </Button>
              <Button
                variant="link"
                className="text-red-600 p-0 h-auto font-normal"
                onClick={() => {
                  setProposalToCancelId(rejectedProposal.tccId);
                  setIsCancelProposalDialogOpen(true);
                }}
                disabled={actionLoading}
              >
                Cancelar
              </Button>
            </div>
          </div>
          <p className="text-sm text-gray-500 italic">
            Motivo: &ldquo;{rejectedProposal.rejectionReason || 'Sem justificativa informada.'}&rdquo;
          </p>
        </div>
      )}

      {/* MOBILE (Collapse) */}
      <div className="md:hidden">
        {canShowPendingSignatures() && (
          <CollapseCard
            title="Assinaturas pendentes"
            icon={faFileSignature}
            indicatorNumber={tccs?.pendingSignature || 0}
            indicatorColor="bg-red-600"
            onClick={() => push('/pendingSignatures')}
          />
        )}

        {canView([
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING'
        ]) && (
          <CollapseCard
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={tccs?.tccInprogress || 0}
            indicatorColor="bg-blue-600"
            onClick={() => push('/ongoingTCCs')}
          />
        )}

        {canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY']) && (
          <CollapseCard
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          />
        )}

        {/* DOCENTE / COORDENADOR: PROPOSTAS PENDENTES DE ORIENTAÇÃO */}
        {canView(['ADVISOR', 'COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCard
            title="Propostas de TCC pendentes"
            icon={faInbox}
            indicatorNumber={advisorPendingProposals.length}
            indicatorColor="bg-amber-600"
            onClick={() => setIsAdvisorModalOpen(true)}
          />
        )}

        {/* DISCENTE: MEU TCC (quando em andamento ou concluído) */}
        {canView(['STUDENT']) &&
          (userTCCs.length > 0 || completedUserTCC.length > 0) && (
            <CollapseCard
              title="Meu TCC"
              icon={faGraduationCap}
              onClick={handleMyTccClick}
            />
          )}

        {/* DISCENTE: CADASTRAR NOVA PROPOSTA (quando não possui TCC nem proposta ativa) */}
        {isStudent() &&
          userTCCs.length === 0 &&
          completedUserTCC.length === 0 &&
          !pendingProposal &&
          !rejectedProposal && (
            <CollapseCard
              title="Cadastrar nova proposta de TCC"
              icon={faFileCirclePlus}
              onClick={() => push('/newTCC')}
            />
          )}

        {/* COORDENADOR/SUPERVISOR: CADASTRO MANUAL SE NECESSÁRIO */}
        {canView(['COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCard
            title="Cadastrar nova proposta"
            icon={faFileCirclePlus}
            onClick={() => push('/newTCC')}
          />
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCard
            title="Cadastrar novo usuário"
            icon={faUserPlus}
            onClick={() => push('/newUser')}
          />
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCard
            title="Usuários"
            icon={faUsers}
            onClick={() => push('/users')}
          />
        )}
      </div>

      {/* DESKTOP (Grid Cards) */}
      <div
        className={`hidden md:grid gap-6 ${
          isLimitedView() ? 'grid-cols-2' : 'md:grid-cols-2 lg:grid-cols-3'
        }`}
      >
        {canShowPendingSignatures() && (
          <CardHome
            title="Assinaturas pendentes"
            icon={faFileSignature}
            indicatorNumber={tccs?.pendingSignature || 0}
            indicatorColor="bg-red-600"
            onClick={() => push('/pendingSignatures')}
          />
        )}

        {canView([
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING'
        ]) && (
          <CardHome
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={tccs?.tccInprogress || 0}
            indicatorColor="bg-blue-600"
            onClick={() => push('/ongoingTCCs')}
          />
        )}

        {canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY']) && (
          <CardHome
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          />
        )}

        {/* DOCENTE / COORDENADOR: PROPOSTAS PENDENTES DE ORIENTAÇÃO */}
        {canView(['ADVISOR', 'COORDINATOR', 'SUPERVISOR']) && (
          <CardHome
            title="Propostas de TCC pendentes"
            icon={faInbox}
            indicatorNumber={advisorPendingProposals.length}
            indicatorColor="bg-amber-600"
            onClick={() => setIsAdvisorModalOpen(true)}
          />
        )}

        {/* DISCENTE: MEU TCC (quando em andamento ou concluído) */}
        {canView(['STUDENT']) &&
          (userTCCs.length > 0 || completedUserTCC.length > 0) && (
            <CardHome
              title="Meu TCC"
              icon={faGraduationCap}
              onClick={handleMyTccClick}
            />
          )}

        {/* DISCENTE: CADASTRAR NOVA PROPOSTA (quando não possui TCC nem proposta ativa) */}
        {isStudent() &&
          userTCCs.length === 0 &&
          completedUserTCC.length === 0 &&
          !pendingProposal &&
          !rejectedProposal && (
            <Link href="/newTCC">
              <CardHome
                title="Cadastrar nova proposta de TCC"
                icon={faFileCirclePlus}
                onClick={() => push('/newTCC')}
              />
            </Link>
          )}

        {/* COORDENADOR/SUPERVISOR: CADASTRO MANUAL ADMINISTRATIVO */}
        {canView(['COORDINATOR', 'SUPERVISOR']) && (
          <Link href="/newTCC">
            <CardHome
              title="Cadastrar nova proposta"
              icon={faFileCirclePlus}
              onClick={() => push('/newTCC')}
            />
          </Link>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <Link href="/newUser">
            <CardHome
              title="Cadastrar novo usuário"
              icon={faUserPlus}
              onClick={() => push('/newUser')}
            />
          </Link>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <Link href="/users">
            <CardHome
              title="Usuários"
              icon={faUsers}
              onClick={() => push('/users')}
            />
          </Link>
        )}
      </div>

      {/* MODAL DO ORIENTADOR: LISTA DE PROPOSTAS PENDENTES */}
      <Dialog open={isAdvisorModalOpen} onOpenChange={setIsAdvisorModalOpen}>
        <DialogContent className="sm:max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="text-xl font-bold text-[#1351B4]">
              Propostas de TCC Pendentes de Orientação
            </DialogTitle>
            <DialogDescription>
              Avalie as propostas de TCC enviadas pelos discentes para sua orientação.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {advisorPendingProposals.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <FontAwesomeIcon icon={faInbox} className="text-4xl text-gray-300 mb-2" />
                <p className="text-base font-medium">Nenhuma proposta pendente no momento.</p>
              </div>
            ) : (
              advisorPendingProposals.map((prop) => (
                <div
                  key={prop.tccId}
                  className="p-5 border border-gray-200 rounded-xl bg-gray-50/60 shadow-xs flex flex-col gap-3"
                >
                  <div className="flex justify-between items-start flex-wrap gap-2">
                    <h3 className="font-semibold text-lg text-gray-900">{prop.title}</h3>
                    <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800">
                      Pendente de Aprovação
                    </span>
                  </div>

                  <p className="text-sm text-gray-600">
                    <strong>Discente(s):</strong>{' '}
                    {prop.studanteNames && prop.studanteNames.length > 0
                      ? prop.studanteNames.join(', ')
                      : 'Não informado'}
                  </p>

                  {prop.summary && (
                    <div className="bg-white p-3 rounded-md border border-gray-200 text-sm text-gray-700 whitespace-pre-line">
                      <p className="font-semibold text-xs text-gray-500 uppercase mb-1">
                        Resumo:
                      </p>
                      {prop.summary}
                    </div>
                  )}

                  <div className="flex justify-end gap-2 pt-2 border-t border-gray-200">
                    <Button
                      variant="destructive"
                      size="sm"
                      className="cursor-pointer"
                      onClick={() => {
                        setSelectedProposalForReject(prop);
                        setRejectReason('');
                        setIsRejectDialogOpen(true);
                      }}
                      disabled={actionLoading}
                    >
                      <FontAwesomeIcon icon={faTimes} className="mr-1.5" />
                      Recusar
                    </Button>
                    <Button
                      size="sm"
                      className="bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer"
                      onClick={() => handleApproveProposal(prop.tccId)}
                      disabled={actionLoading}
                    >
                      <FontAwesomeIcon icon={faCheck} className="mr-1.5" />
                      Aceitar Proposta
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* MODAL DE RECUSA COM JUSTIFICATIVA (DOCENTE) */}
      <Dialog open={isRejectDialogOpen} onOpenChange={setIsRejectDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-bold text-red-600 flex items-center gap-2">
              <FontAwesomeIcon icon={faExclamationTriangle} />
              Recusar Proposta de TCC
            </DialogTitle>
            <DialogDescription>
              Informe ao estudante o motivo ou os pontos de melhoria para a proposta:
            </DialogDescription>
          </DialogHeader>

          <div className="py-2">
            <p className="text-sm font-semibold text-gray-800 mb-2">
              Título: {selectedProposalForReject?.title}
            </p>
            <Textarea
              placeholder="Descreva detalhadamente o motivo da recusa ou instruções para reformulação da proposta..."
              rows={4}
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              className="w-full text-sm"
              disabled={actionLoading}
            />
          </div>

          <DialogFooter className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setIsRejectDialogOpen(false);
                setSelectedProposalForReject(null);
              }}
              disabled={actionLoading}
            >
              Voltar
            </Button>
            <Button
              variant="destructive"
              onClick={handleRejectProposal}
              disabled={actionLoading || !rejectReason.trim()}
            >
              Confirmar Recusa
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* CONFIRMAÇÃO DE CANCELAMENTO DE PROPOSTA (DISCENTE) */}
      <AlertDialog
        open={isCancelProposalDialogOpen}
        onOpenChange={setIsCancelProposalDialogOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="text-lg font-bold text-red-600">
              Deseja cancelar esta proposta?
            </AlertDialogTitle>
            <AlertDialogDescription>
              Ao cancelar, a proposta será descartada definitivamente e você poderá cadastrar uma nova proposta com outro tema ou orientador.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={actionLoading}>
              Não, manter
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleCancelProposal}
              disabled={actionLoading}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              Sim, cancelar proposta
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
