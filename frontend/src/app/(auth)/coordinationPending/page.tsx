'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import {
  BookOpen,
  Library,
  XCircle,
  ArrowRight,
  LoaderCircle,
  CheckCircle2,
  ClipboardList,
  Send
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { PageTitle } from '@/components/layout/PageTitle';
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

interface DecodedToken {
  role: string | string[];
  userId: string;
}

interface PendingTccItem {
  tccId: number;
  title: string;
  studentNames: string[];
  finalSubmissionDeadline: string | null;
}

interface CoordinationPending {
  awaitingHardCoverCount: number;
  awaitingLibraryForwardCount: number;
  awaitingCancellationCount: number;
  awaitingHardCover: PendingTccItem[];
  awaitingLibraryForward: PendingTccItem[];
  awaitingCancellation: PendingTccItem[];
  awaitingAdvisorChangesCount: number;
  awaitingAdvisorChanges: PendingAdvisorChange[];
}

interface PendingAdvisorChange {
  tccId: number; advisorChangeId: number; title: string; studentNames: string[];
  requesterName: string; requesterRole: string; currentAdvisorName: string;
  suggestedAdvisorName: string | null; reason: string; createdAt: string; documentId: number | null;
}

function formatDeadline(deadline: string | null): {
  text: string;
  urgent: boolean;
  expired: boolean;
} {
  if (!deadline) return { text: '', urgent: false, expired: false };
  const date = new Date(deadline);
  if (isNaN(date.getTime())) return { text: '', urgent: false, expired: false };
  const now = new Date();
  date.setHours(23, 59, 59, 999);
  const diff = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
  const formatted = new Date(deadline).toLocaleDateString('pt-BR');
  if (diff < 0) return { text: `Prazo expirou em ${formatted}`, urgent: false, expired: true };
  if (diff === 0) return { text: `Prazo: hoje (${formatted})`, urgent: true, expired: false };
  return { text: `${diff} dia${diff !== 1 ? 's' : ''} restante${diff !== 1 ? 's' : ''} (${formatted})`, urgent: diff <= 5, expired: false };
}

function EmptyState({ message }: { message: string }) {
  return (
    <div role="status" className="flex items-start gap-2 py-2 text-sm leading-relaxed text-[#426650]">
      <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-[#426650]" />
      {message}
    </div>
  );
}

function PendingIcon({ children }: { children: React.ReactNode }) {
  return (
    <span className="relative -mt-2 flex size-10 shrink-0 items-center justify-center text-[#006633] transition-colors duration-300 ease-out group-hover/pending:text-white group-focus-within/pending:text-white motion-reduce:transition-none">
      <span
        aria-hidden="true"
        className="absolute inset-0 scale-75 rounded-full bg-[#002B18] opacity-0 transition-[opacity,transform,scale] duration-300 ease-out group-hover/pending:scale-100 group-hover/pending:opacity-100 group-focus-within/pending:scale-100 group-focus-within/pending:opacity-100 motion-reduce:transition-none"
      />
      <span className="relative">{children}</span>
    </span>
  );
}

interface PendingSectionProps {
  title: string;
  subtitle: string;
  icon: React.ReactNode;
  count: number;
  items: PendingTccItem[];
  emptyMessage: string;
  actionLabel: string;
  onAction: (tccId: number) => void;
  primaryActionLabel?: string;
  onPrimaryAction?: (tccId: number) => void;
  primaryActionIcon?: React.ReactNode;
  primaryLoadingId?: number | null;
  showDeadline?: boolean;
}

function PendingSection({
  title,
  subtitle,
  icon,
  count,
  items,
  emptyMessage,
  actionLabel,
  onAction,
  primaryActionLabel,
  onPrimaryAction,
  primaryActionIcon,
  primaryLoadingId,
  showDeadline = false
}: PendingSectionProps) {
  return (
    <section className="group/pending border-b border-[#DDD9CD] pb-7">
      {/* Header da seção */}
      <div className="flex items-start gap-3 pb-4">
        <PendingIcon>{icon}</PendingIcon>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h2 className="text-base font-semibold text-[#002B18]">{title}</h2>
            {count > 0 && (
              <span className="text-xs font-medium tabular-nums text-[#617265]">
                {count}
              </span>
            )}
          </div>
          <p className="mt-1 max-w-3xl text-sm leading-relaxed text-[#617265]">{subtitle}</p>
        </div>
      </div>

      {/* Lista de itens */}
      <div className="flex flex-col divide-y divide-[#ECEAE2] pl-13">
        {items.length === 0 ? (
          <EmptyState message={emptyMessage} />
        ) : (
          items.map((item) => {
            const deadline = showDeadline ? formatDeadline(item.finalSubmissionDeadline) : null;
            const isItemLoading = primaryLoadingId === item.tccId;

            return (
              <div
                key={item.tccId}
                className="flex flex-col justify-between gap-4 py-4 sm:flex-row sm:items-center"
              >
                <div className="min-w-0">
                  <p className="text-sm font-semibold text-[#002B18] truncate">{item.title}</p>
                  <p className="text-xs text-gray-500 mt-0.5 truncate">
                    {item.studentNames.join(', ')}
                  </p>
                  {deadline && deadline.text && (
                    <span
                      className={`mt-1 inline-block text-xs font-medium ${
                        deadline.expired
                          ? 'text-red-600'
                          : deadline.urgent
                            ? 'text-amber-600'
                            : 'text-gray-400'
                      }`}
                    >
                      {deadline.text}
                    </span>
                  )}
                </div>

                <div className="flex items-center gap-2 shrink-0 self-start sm:self-center">
                  {primaryActionLabel && onPrimaryAction && (
                    <Button
                      size="sm"
                      disabled={isItemLoading}
                      className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] text-xs font-semibold shadow-xs cursor-pointer"
                      onClick={() => onPrimaryAction(item.tccId)}
                    >
                      {isItemLoading ? (
                        <LoaderCircle className="size-3.5 animate-spin motion-reduce:animate-none" />
                      ) : (
                        primaryActionIcon
                      )}
                      {primaryActionLabel}
                    </Button>
                  )}

                  <Button
                    size="sm"
                    variant="outline"
                    className="text-xs font-semibold border-[#002B18]/20 text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47] cursor-pointer"
                    onClick={() => onAction(item.tccId)}
                  >
                    {actionLabel}
                    <ArrowRight className="ml-1 size-3" />
                  </Button>
                </div>
              </div>
            );
          })
        )}
      </div>
    </section>
  );
}

export default function CoordinationPendingPage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const [data, setData] = useState<CoordinationPending | null>(null);
  const [loading, setLoading] = useState(true);
  const [authorized, setAuthorized] = useState(true);
  const [canReviewAdvisorChanges, setCanReviewAdvisorChanges] = useState(false);

  // Estados dos modais de ação rápida
  const [selectedTccId, setSelectedTccId] = useState<number | null>(null);
  const [isHardCoverModalOpen, setIsHardCoverModalOpen] = useState(false);
  const [isForwardModalOpen, setIsForwardModalOpen] = useState(false);
  const [actionLoadingId, setActionLoadingId] = useState<number | null>(null);
  const [rejectItem, setRejectItem] = useState<PendingAdvisorChange | null>(null);
  const [reviewNotes, setReviewNotes] = useState('');

  const fetchPending = useCallback(async () => {
    const token = Cookies.get('token');
    if (!token) {
      push('/');
      return;
    }

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
      setCanReviewAdvisorChanges(roles.includes('COORDINATOR'));
      const isCoordination = roles.some((r) => ['COORDINATOR', 'SUPERVISOR'].includes(r));
      if (!isCoordination) {
        setAuthorized(false);
        setLoading(false);
        return;
      }
    } catch {
      push('/');
      return;
    }

    setLoading(true);
    try {
      const res = await fetch(`${API_URL}/Tcc/coordination-pending`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) throw new Error('Erro ao buscar pendências.');
      const result: CoordinationPending = await res.json();
      setData(result);
    } catch {
      toast.error('Erro ao carregar pendências de coordenação.');
    } finally {
      setLoading(false);
    }
  }, [API_URL, push]);

  useEffect(() => {
    fetchPending();
  }, [fetchPending]);

  // Ação rápida: Confirmar Capa Dura
  const handleConfirmHardCoverSubmit = async () => {
    if (!selectedTccId) return;
    const token = Cookies.get('token');
    if (!token) return;

    setActionLoadingId(selectedTccId);
    try {
      const res = await fetch(`${API_URL}/Tcc/${selectedTccId}/confirm-hardcover`, {
        method: 'PATCH',
        headers: { Authorization: `Bearer ${token}` }
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.detail || err?.message || 'Falha ao confirmar capa dura.');
      }

      toast.success('Recebimento da versão em capa dura confirmado com sucesso!');
      await fetchPending();
    } catch (error: unknown) {
      toast.error(error instanceof Error ? error.message : 'Erro ao confirmar capa dura.');
    } finally {
      setActionLoadingId(null);
      setIsHardCoverModalOpen(false);
      setSelectedTccId(null);
    }
  };

  // Ação rápida: Encaminhar à Biblioteca
  const handleForwardToLibrarySubmit = async () => {
    if (!selectedTccId) return;
    const token = Cookies.get('token');
    if (!token) return;

    setActionLoadingId(selectedTccId);
    try {
      const res = await fetch(`${API_URL}/Tcc/${selectedTccId}/forward-library`, {
        method: 'PATCH',
        headers: { Authorization: `Bearer ${token}` }
      });

      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.detail || err?.message || 'Falha ao encaminhar para a biblioteca.');
      }

      toast.success('TCC encaminhado com sucesso para a Biblioteca do Campus!');
      await fetchPending();
    } catch (error: unknown) {
      toast.error(error instanceof Error ? error.message : 'Erro ao encaminhar para a biblioteca.');
    } finally {
      setActionLoadingId(null);
      setIsForwardModalOpen(false);
      setSelectedTccId(null);
    }
  };

  const totalPending =
    (data?.awaitingHardCoverCount ?? 0) +
    (data?.awaitingLibraryForwardCount ?? 0) +
    (data?.awaitingCancellationCount ?? 0) +
    (data?.awaitingAdvisorChangesCount ?? 0);

  const reviewAdvisorChange = async (item: PendingAdvisorChange, approve: boolean) => {
    const token = Cookies.get('token');
    if (!token) return;
    setActionLoadingId(item.tccId);
    try {
      const res = await fetch(`${API_URL}/Tcc/${item.tccId}/advisor-change/${approve ? 'approve' : 'reject'}`, {
        method: 'PATCH',
        headers: { Authorization: `Bearer ${token}`, ...(approve ? {} : { 'Content-Type': 'application/json' }) },
        ...(approve ? {} : { body: JSON.stringify({ reviewNotes }) })
      });
      if (!res.ok) {
        const err = await res.json().catch(() => null);
        throw new Error(err?.detail || err?.message || 'Não foi possível registrar a decisão.');
      }
      toast.success(approve ? 'Substituição homologada.' : 'Solicitação recusada.');
      setRejectItem(null); setReviewNotes('');
      await fetchPending();
    } catch (error) { toast.error(error instanceof Error ? error.message : 'Erro ao registrar decisão.'); }
    finally { setActionLoadingId(null); }
  };

  const openSignedDocument = async (item: PendingAdvisorChange) => {
    if (!item.documentId) return;
    const token = Cookies.get('token');
    if (!token) return;
    const tab = window.open('about:blank', '_blank');
    if (!tab) { toast.error('Permita pop-ups para abrir o PDF assinado.'); return; }
    try {
      const res = await fetch(`${API_URL}/Tcc/${item.tccId}/files/${item.documentId}/preview`, { headers: { Authorization: `Bearer ${token}` } });
      if (!res.ok) {
        const problem = await res.json().catch(() => null);
        throw new Error(problem?.detail || problem?.message || `Falha ao carregar o documento (HTTP ${res.status}).`);
      }
      const url = URL.createObjectURL(await res.blob());
      tab.location.href = url;
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (error) { tab.close(); toast.error(error instanceof Error ? error.message : 'Erro ao abrir documento.'); }
  };

  if (!authorized) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center">
        <XCircle className="mb-4 size-12 text-red-400" />
        <h2 className="text-lg font-bold text-[#002B18]">Acesso não autorizado</h2>
        <p className="mt-1 text-sm text-gray-500">
          Esta página é restrita à Coordenação e Supervisão.
        </p>
      </div>
    );
  }

  return (
    <div className="flex w-full max-w-6xl flex-col gap-7">
      {/* Cabeçalho */}
      <div>
        <PageTitle>Pendências</PageTitle>
        <p className="mt-3 max-w-2xl text-sm leading-relaxed text-[#617265]">
          Acompanhe as pendências de biblioteca, substituições e cancelamentos.
        </p>
      </div>

      {/* Badge total */}
      {!loading && data && totalPending > 0 && (
        <div role="status" className="flex items-center gap-2 text-sm font-medium text-[#426650]">
          <ClipboardList className="size-4 shrink-0" />
          {totalPending} pendência{totalPending !== 1 ? 's' : ''} no total
        </div>
      )}
      {!loading && data && totalPending === 0 && (
        <div role="status" className="flex items-center gap-2 text-sm font-medium text-[#426650]">
          <CheckCircle2 className="size-4 shrink-0" />
          Nenhuma pendência no momento. Tudo em dia!
        </div>
      )}

      {/* Loading */}
      {loading && (
        <div className="flex items-center justify-center py-20">
          <LoaderCircle className="size-8 animate-spin text-[#002B18]" />
        </div>
      )}

      {/* Seções */}
      {!loading && data && (
        <Tabs defaultValue="biblioteca" className="gap-7">
          <TabsList
            aria-label="Tipos de pendências"
            className="grid h-auto w-full grid-cols-3 items-stretch gap-2 sm:gap-8 lg:gap-16"
          >
            <TabsTrigger
              value="biblioteca"
              className="min-w-0 flex-wrap whitespace-normal px-2 text-center text-xs sm:px-3 sm:text-sm"
            >
              Biblioteca{' '}
              <span className="ml-1 text-xs font-medium tabular-nums text-[#617265]">
                {data.awaitingHardCoverCount + data.awaitingLibraryForwardCount}
              </span>
            </TabsTrigger>
            <TabsTrigger
              value="substituicoes"
              className="min-w-0 flex-wrap whitespace-normal px-2 text-center text-xs sm:px-3 sm:text-sm"
            >
              Substituições{' '}
              <span className="ml-1 text-xs font-medium tabular-nums text-[#617265]">
                {data.awaitingAdvisorChangesCount}
              </span>
            </TabsTrigger>
            <TabsTrigger
              value="cancelamentos"
              className="min-w-0 flex-wrap whitespace-normal px-2 text-center text-xs sm:px-3 sm:text-sm"
            >
              Solicitações de cancelamento{' '}
              <span className="ml-1 text-xs font-medium tabular-nums text-[#617265]">
                {data.awaitingCancellationCount}
              </span>
            </TabsTrigger>
          </TabsList>
          <TabsContent value="biblioteca" className="space-y-10">
          <PendingSection
            title="Aguardando Capa Dura"
            subtitle="TCCs aprovados na defesa que ainda não tiveram a capa dura entregue à coordenação"
            icon={<BookOpen className="size-5" />}
            count={data.awaitingHardCoverCount}
            items={data.awaitingHardCover}
            emptyMessage="Nenhum TCC aguardando entrega da capa dura."
            actionLabel="Ver TCC"
            primaryActionLabel="Confirmar Entrega"
            primaryActionIcon={<CheckCircle2 className="size-3.5 mr-1" />}
            primaryLoadingId={actionLoadingId}
            showDeadline
            onPrimaryAction={(tccId) => {
              setSelectedTccId(tccId);
              setIsHardCoverModalOpen(true);
            }}
            onAction={(tccId) => push(`/ongoingTCCs/details?id=${tccId}`)}
          />

          <PendingSection
            title="Aguardando Envio à Biblioteca"
            subtitle="TCCs com capa dura confirmada que ainda não foram encaminhados à biblioteca"
            icon={<Library className="size-5" />}
            count={data.awaitingLibraryForwardCount}
            items={data.awaitingLibraryForward}
            emptyMessage="Nenhum TCC aguardando encaminhamento à biblioteca."
            actionLabel="Ver TCC"
            primaryActionLabel="Encaminhar à Biblioteca"
            primaryActionIcon={<Send className="size-3.5 mr-1" />}
            primaryLoadingId={actionLoadingId}
            onPrimaryAction={(tccId) => {
              setSelectedTccId(tccId);
              setIsForwardModalOpen(true);
            }}
            onAction={(tccId) => push(`/ongoingTCCs/details?id=${tccId}`)}
          />
          </TabsContent>
          <TabsContent value="substituicoes">
          <section className="group/pending border-b border-[#DDD9CD] pb-7">
            <div className="flex items-start gap-3 pb-4">
              <PendingIcon><ClipboardList className="size-5" /></PendingIcon>
              <div className="flex-1"><h2 className="text-base font-semibold text-[#002B18]">Substituições de Orientador</h2><p className="mt-1 text-sm leading-relaxed text-[#617265]">Solicitações assinadas aguardando homologação da Coordenação</p></div>
              {data.awaitingAdvisorChangesCount > 0 && <span className="text-xs font-medium tabular-nums text-[#617265]">{data.awaitingAdvisorChangesCount}</span>}
            </div>
            <div className="divide-y divide-[#ECEAE2] pl-13">
              {data.awaitingAdvisorChanges.length === 0 ? <EmptyState message="Nenhuma substituição de orientador aguardando análise." /> : data.awaitingAdvisorChanges.map(item => (
                <div key={item.advisorChangeId} className="flex flex-col gap-3 py-4">
                  <div><p className="text-sm font-semibold text-[#002B18]">{item.title}</p><p className="text-xs text-gray-500">Estudantes: {item.studentNames.join(', ')} · Solicitante: {item.requesterName} ({item.requesterRole})</p><p className="text-xs text-gray-500">Orientador atual: {item.currentAdvisorName} · Novo: {item.suggestedAdvisorName || 'Não informado'}</p><p className="mt-2 text-sm text-gray-700">Justificativa: {item.reason}</p></div>
                  <div className="flex flex-wrap gap-2">
                    {item.documentId && <Button size="sm" variant="outline" onClick={() => openSignedDocument(item)}>Abrir PDF assinado</Button>}
                    {canReviewAdvisorChanges && <Button size="sm" disabled={actionLoadingId === item.tccId} className="bg-emerald-700 text-white" onClick={() => reviewAdvisorChange(item, true)}>Homologar</Button>}
                    {canReviewAdvisorChanges && <Button size="sm" variant="destructive" disabled={actionLoadingId === item.tccId} onClick={() => { setRejectItem(item); setReviewNotes(''); }}>Recusar</Button>}
                  </div>
                </div>
              ))}
            </div>
          </section>
          </TabsContent>
          <TabsContent value="cancelamentos">
          <PendingSection
            title="Solicitações de Cancelamento"
            subtitle="Solicitações de cancelamento enviadas por estudantes aguardando análise"
            icon={<XCircle className="size-5" />}
            count={data.awaitingCancellationCount}
            items={data.awaitingCancellation}
            emptyMessage="Nenhuma solicitação de cancelamento pendente."
            actionLabel="Analisar"
            onAction={(tccId) => push(`/ongoingTCCs/details?id=${tccId}`)}
          />
          </TabsContent>
        </Tabs>
      )}

      {/* Modal de Confirmação Rápida de Capa Dura */}
      <AlertDialog
        open={isHardCoverModalOpen}
        onOpenChange={setIsHardCoverModalOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirmar Recebimento da Capa Dura</AlertDialogTitle>
            <AlertDialogDescription>
              Você confirma que o estudante entregou a versão física encadernada
              em capa dura à Coordenação do Curso?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={actionLoadingId !== null}>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold cursor-pointer"
              disabled={actionLoadingId !== null}
              onClick={handleConfirmHardCoverSubmit}
            >
              {actionLoadingId !== null ? 'Confirmando...' : 'Confirmar Recebimento'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={rejectItem !== null} onOpenChange={(open) => { if (!open) setRejectItem(null); }}>
        <AlertDialogContent>
          <AlertDialogHeader><AlertDialogTitle>Recusar solicitação</AlertDialogTitle><AlertDialogDescription>Registre o parecer que será armazenado no processo.</AlertDialogDescription></AlertDialogHeader>
          <textarea className="min-h-28 w-full rounded-md border border-gray-300 p-3 text-sm" maxLength={2000} value={reviewNotes} onChange={e => setReviewNotes(e.target.value)} placeholder="Parecer da Coordenação (obrigatório)" />
          <AlertDialogFooter><AlertDialogCancel disabled={actionLoadingId !== null}>Cancelar</AlertDialogCancel><AlertDialogAction disabled={!reviewNotes.trim() || actionLoadingId !== null} className="bg-red-700 text-white" onClick={() => rejectItem && reviewAdvisorChange(rejectItem, false)}>Confirmar recusa</AlertDialogAction></AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Modal de Encaminhamento Rápido à Biblioteca */}
      <AlertDialog
        open={isForwardModalOpen}
        onOpenChange={setIsForwardModalOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Encaminhar TCC à Biblioteca</AlertDialogTitle>
            <AlertDialogDescription>
              Deseja encaminhar a versão física deste TCC para a Biblioteca do Campus?
              A equipe da Biblioteca será notificada automaticamente para incluir a obra no acervo.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={actionLoadingId !== null}>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold cursor-pointer"
              disabled={actionLoadingId !== null}
              onClick={handleForwardToLibrarySubmit}
            >
              {actionLoadingId !== null ? 'Encaminhando...' : 'Sim, Encaminhar à Biblioteca'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
