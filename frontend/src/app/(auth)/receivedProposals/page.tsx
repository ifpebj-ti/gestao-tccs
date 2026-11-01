'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';
import { toast } from 'react-toastify';
import { Check, Inbox, X } from 'lucide-react';
import { PageTitle } from '@/components/layout/PageTitle';
import { PageState } from '@/components/layout/PageState';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import {
  loadReceivedProposals,
  decideReceivedProposal,
  type ReceivedProposal
} from '@/lib/receivedProposals';

export default function ReceivedProposalsPage() {
  const base = env('NEXT_PUBLIC_API_URL') || '';
  const [items, setItems] = useState<ReceivedProposal[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [rejecting, setRejecting] = useState<number | null>(null);
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState<number | null>(null);
  const lock = useRef(false);
  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const token = Cookies.get('token');
      if (!token) throw new Error('Sua sessão expirou. Entre novamente.');
      const { userId } = jwtDecode<{ userId: string }>(token);
      setItems(await loadReceivedProposals(base, token, userId));
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : 'Não foi possível carregar as propostas.'
      );
    } finally {
      setLoading(false);
    }
  }, [base]);
  useEffect(() => {
    void load();
  }, [load]);

  async function decide(id: number, rejection?: string) {
    if (lock.current) return;
    lock.current = true;
    setBusy(id);
    setError('');
    try {
      const token = Cookies.get('token');
      if (!token) throw new Error('Sua sessão expirou. Entre novamente.');
      await decideReceivedProposal(base, token, id, rejection);
      setItems((current) => current.filter((item) => item.tccId !== id));
      setRejecting(null);
      setReason('');
      toast.success(
        rejection === undefined
          ? 'Proposta aceita. O TCC está em andamento.'
          : 'Proposta recusada. A justificativa foi enviada aos estudantes.'
      );
    } catch (e) {
      setError(
        e instanceof Error ? e.message : 'Não foi possível avaliar a proposta.'
      );
    } finally {
      lock.current = false;
      setBusy(null);
    }
  }

  return (
    <div className="w-full space-y-6">
      <header className="space-y-3">
        <PageTitle>Propostas recebidas</PageTitle>
        <p className="max-w-2xl text-sm leading-relaxed text-slate-600">
          Avalie os temas enviados para sua orientação. Ao aceitar, o trabalho
          passa para os TCCs em andamento. Ao recusar, informe o que precisa ser
          revisto.
        </p>
      </header>
      {error && (
        <div
          role="alert"
          className="space-y-3 rounded-xl border border-red-200 p-5 text-sm text-red-800"
        >
          <p>{error}</p>
          <Button
            variant="outline"
            disabled={busy !== null}
            onClick={() => void load()}
          >
            Atualizar propostas
          </Button>
        </div>
      )}
      {loading ? (
        <PageState loading>Carregando propostas recebidas…</PageState>
      ) : (
        <>
          <div className="flex items-center gap-3 border-b border-[#DDD9CD] pb-4 text-sm font-semibold text-[#002B18]">
            <Inbox className="size-5" />
            {items.length}{' '}
            {items.length === 1
              ? 'proposta aguardando avaliação'
              : 'propostas aguardando avaliação'}
          </div>
          {!items.length && !error && (
            <PageState>
              Nenhuma proposta aguardando sua avaliação. Novas propostas
              aparecerão aqui após o aceite de todos os integrantes.
            </PageState>
          )}
          <div className="space-y-5">
            {items.map((item) => (
              <article
                key={item.tccId}
                className="overflow-hidden rounded-xl border border-[#E2E5DC] border-l-4 border-l-[#183E2D] bg-white shadow-[0_3px_16px_rgba(24,62,45,0.04)]"
              >
                <div className="space-y-4 p-5 md:p-6">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <h2 className="max-w-3xl break-words text-lg font-bold leading-snug text-[#002B18] md:text-xl">
                      {item.title}
                    </h2>
                  </div>
                  <p className="text-sm text-slate-600">
                    <strong className="text-[#002B18]">Estudantes: </strong>
                    {item.studanteNames.join(', ') || 'Não informado'}
                  </p>
                  <div className="space-y-2 border-l-2 border-[#DCE8DF] pl-4">
                    <h3 className="text-xs font-bold uppercase tracking-wider text-slate-600">
                      Resumo da proposta
                    </h3>
                    <p className="whitespace-pre-wrap break-words text-sm leading-7 text-slate-700">
                      {item.summary || 'Resumo não informado.'}
                    </p>
                  </div>
                  {item.pendingParticipantCount > 0 && (
                    <p className="text-sm text-amber-800">
                      Aguardando aceite de todos os integrantes para liberar a
                      avaliação.
                    </p>
                  )}
                  {rejecting === item.tccId && (
                    <form
                      className="space-y-4 border-t border-[#DDD9CD] pt-5"
                      onSubmit={(e) => {
                        e.preventDefault();
                        void decide(item.tccId, reason);
                      }}
                    >
                      <label
                        htmlFor={`reason-${item.tccId}`}
                        className="block text-sm font-semibold text-[#002B18]"
                      >
                        Justificativa da recusa
                      </label>
                      <Textarea
                        id={`reason-${item.tccId}`}
                        rows={1}
                        className="min-h-0 resize-none overflow-hidden"
                        autoFocus
                        required
                        maxLength={1000}
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        placeholder="Explique aos estudantes o que precisa ser revisto."
                        disabled={busy !== null}
                      />
                      <div className="flex flex-wrap gap-3">
                        <Button
                          type="submit"
                          variant="destructive"
                          disabled={busy !== null || !reason.trim()}
                        >
                          {busy === item.tccId
                            ? 'Enviando…'
                            : 'Confirmar recusa'}
                        </Button>
                        <Button
                          type="button"
                          variant="cancel"
                          disabled={busy !== null}
                          onClick={() => setRejecting(null)}
                        >
                          Voltar
                        </Button>
                      </div>
                    </form>
                  )}
                </div>
                {rejecting !== item.tccId && (
                  <footer className="mx-5 flex flex-wrap items-center justify-end gap-3 border-t border-[#ECEAE2] py-4 md:mx-6">
                    <Button
                      variant="destructive"
                      disabled={
                        busy !== null || item.pendingParticipantCount > 0
                      }
                      onClick={() => {
                        setRejecting(item.tccId);
                        setReason('');
                      }}
                    >
                      <X className="size-4" />
                      Recusar
                    </Button>
                    <Button
                      variant="brand"
                      disabled={
                        busy !== null || item.pendingParticipantCount > 0
                      }
                      onClick={() => void decide(item.tccId)}
                    >
                      <Check className="size-4" />
                      {busy === item.tccId ? 'Aceitando…' : 'Aceitar proposta'}
                    </Button>
                  </footer>
                )}
              </article>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
