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
  ClipboardList
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { PageTitle } from '@/components/layout/PageTitle';

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
    <div className="flex items-center gap-2 rounded-lg border border-dashed border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-400">
      <CheckCircle2 className="size-4 shrink-0 text-emerald-400" />
      {message}
    </div>
  );
}

interface PendingSectionProps {
  title: string;
  subtitle: string;
  icon: React.ReactNode;
  iconBg: string;
  count: number;
  items: PendingTccItem[];
  emptyMessage: string;
  actionLabel: string;
  onAction: (tccId: number) => void;
  badgeColor: string;
  showDeadline?: boolean;
}

function PendingSection({
  title,
  subtitle,
  icon,
  iconBg,
  count,
  items,
  emptyMessage,
  actionLabel,
  onAction,
  badgeColor,
  showDeadline = false
}: PendingSectionProps) {
  return (
    <section className="rounded-xl border border-gray-200 bg-white shadow-sm overflow-hidden">
      {/* Header da seção */}
      <div className={`flex items-center gap-3 px-5 py-4 ${iconBg} border-b border-gray-100`}>
        <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-white/80 shadow-xs">
          {icon}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h2 className="text-sm font-bold text-[#002B18] truncate">{title}</h2>
            {count > 0 && (
              <span className={`inline-flex items-center justify-center rounded-full px-2 py-0.5 text-xs font-bold ${badgeColor}`}>
                {count}
              </span>
            )}
          </div>
          <p className="text-xs text-gray-500 mt-0.5">{subtitle}</p>
        </div>
      </div>

      {/* Lista de itens */}
      <div className="divide-y divide-gray-50 px-5 py-3 flex flex-col gap-2">
        {items.length === 0 ? (
          <EmptyState message={emptyMessage} />
        ) : (
          items.map((item) => {
            const deadline = showDeadline ? formatDeadline(item.finalSubmissionDeadline) : null;
            return (
              <div
                key={item.tccId}
                className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-lg px-1 py-2.5"
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
                <Button
                  size="sm"
                  variant="outline"
                  className="shrink-0 self-start sm:self-center text-xs font-semibold border-[#002B18]/20 text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47] cursor-pointer"
                  onClick={() => onAction(item.tccId)}
                >
                  {actionLabel}
                  <ArrowRight className="ml-1 size-3" />
                </Button>
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

  const fetchPending = useCallback(async () => {
    const token = Cookies.get('token');
    if (!token) {
      push('/');
      return;
    }

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
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

  const totalPending =
    (data?.awaitingHardCoverCount ?? 0) +
    (data?.awaitingLibraryForwardCount ?? 0) +
    (data?.awaitingCancellationCount ?? 0);

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
    <div className="flex flex-col w-full gap-6">
      {/* Cabeçalho */}
      <div className="mb-2">
        <PageTitle>
          Pendências de{' '}
          <span className="text-[#00A355]">Coordenação</span>
        </PageTitle>
        <p className="mt-2 text-sm text-gray-500 max-w-2xl">
          Centralize e resolva rapidamente todas as ações que aguardam a intervenção da Coordenação.
        </p>
      </div>

      {/* Badge total */}
      {!loading && data && totalPending > 0 && (
        <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2.5 text-sm font-medium text-amber-800 self-start">
          <ClipboardList className="size-4 shrink-0" />
          {totalPending} pendência{totalPending !== 1 ? 's' : ''} no total
        </div>
      )}
      {!loading && data && totalPending === 0 && (
        <div className="flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2.5 text-sm font-medium text-emerald-800 self-start">
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
        <div className="flex flex-col gap-5">
          {/* 1 — Capa Dura */}
          <PendingSection
            title="Aguardando Capa Dura"
            subtitle="TCCs aprovados na defesa que ainda não tiveram a capa dura entregue à coordenação"
            icon={<BookOpen className="size-5 text-amber-600" />}
            iconBg="bg-amber-50"
            badgeColor="bg-amber-100 text-amber-800"
            count={data.awaitingHardCoverCount}
            items={data.awaitingHardCover}
            emptyMessage="Nenhum TCC aguardando entrega da capa dura."
            actionLabel="Ver TCC"
            showDeadline
            onAction={(tccId) => push(`/ongoingTCCs/details?id=${tccId}`)}
          />

          {/* 2 — Encaminhamento à Biblioteca */}
          <PendingSection
            title="Aguardando Envio à Biblioteca"
            subtitle="TCCs com capa dura confirmada que ainda não foram encaminhados à biblioteca"
            icon={<Library className="size-5 text-blue-600" />}
            iconBg="bg-blue-50"
            badgeColor="bg-blue-100 text-blue-800"
            count={data.awaitingLibraryForwardCount}
            items={data.awaitingLibraryForward}
            emptyMessage="Nenhum TCC aguardando encaminhamento à biblioteca."
            actionLabel="Ver TCC"
            onAction={(tccId) => push(`/ongoingTCCs/details?id=${tccId}`)}
          />

          {/* 3 — Cancelamentos */}
          <PendingSection
            title="Solicitações de Cancelamento"
            subtitle="Solicitações de cancelamento enviadas por estudantes aguardando análise"
            icon={<XCircle className="size-5 text-red-500" />}
            iconBg="bg-red-50"
            badgeColor="bg-red-100 text-red-700"
            count={data.awaitingCancellationCount}
            items={data.awaitingCancellation}
            emptyMessage="Nenhuma solicitação de cancelamento pendente."
            actionLabel="Analisar"
            onAction={(tccId) => push(`/ongoingTCCs/details?id=${tccId}`)}
          />
        </div>
      )}
    </div>
  );
}
