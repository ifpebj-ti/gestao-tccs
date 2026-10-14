'use client';

import { useCallback, useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { Button, buttonVariants } from '@/components/ui/button';
import { toast } from 'react-toastify';
import Link from 'next/link';
import { NoticeCard } from '@/components/layout/NoticeCard';

export interface ProposalInvitation {
  id: number;
  tccId: number;
  title: string;
  summary: string;
  requestedBy: string;
}

export function ProposalDeclinedNotification({
  notification,
  busy,
  onDismiss
}: {
  notification: { title: string; declinedBy: string };
  busy: boolean;
  onDismiss: () => void;
}) {
  return (
    <NoticeCard title={`Participação recusada: ${notification.title}`}>
      <p className="mt-2 text-sm">
        {notification.declinedBy} recusou o convite. A proposta não foi enviada
        ao professor. Revise os integrantes e submeta novamente.
      </p>
      <div className="mt-3 flex flex-wrap gap-3">
        <Link href="/newTCC" className={buttonVariants()}>
          Submeter novamente
        </Link>
        <Button
          type="button"
          variant="accent"
          disabled={busy}
          onClick={onDismiss}
        >
          Marcar como lida
        </Button>
      </div>
    </NoticeCard>
  );
}

export function ProposalInvitationCard({
  invitation,
  busy,
  onRespond
}: {
  invitation: ProposalInvitation;
  busy: boolean;
  onRespond: (accept: boolean) => void;
}) {
  return (
    <NoticeCard title={invitation.title}>
      <p className="mt-1 text-sm">
        {invitation.requestedBy} convidou você para participar desta proposta.
      </p>
      <p className="mt-2 whitespace-pre-wrap text-sm">{invitation.summary}</p>
      <p className="mt-2 text-sm">
        Ao aceitar, você será vinculado ao mesmo TCC. Recusar cancela a proposta
        para que o solicitante possa revisá-la.
      </p>
      <div className="mt-3 flex flex-wrap gap-3">
        <Button type="button" disabled={busy} onClick={() => onRespond(true)}>
          Aceitar participação
        </Button>
        <Button
          type="button"
          variant="destructive"
          disabled={busy}
          onClick={() => onRespond(false)}
        >
          Recusar convite
        </Button>
      </div>
    </NoticeCard>
  );
}

export function ProposalInvitations() {
  const api = env('NEXT_PUBLIC_API_URL');
  const [invitations, setInvitations] = useState<ProposalInvitation[]>([]);
  const [notifications, setNotifications] = useState<
    { id: number; tccId: number; title: string; declinedBy: string }[]
  >([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const load = useCallback(async () => {
    try {
      const responses = await Promise.all(
        ['', '/notifications'].map((path) =>
          fetch(`${api}/Tcc/participant-invitations${path}`, {
            headers: { Authorization: `Bearer ${Cookies.get('token')}` },
            cache: 'no-store'
          })
        )
      );
      if (responses.some((response) => !response.ok))
        throw new Error('Não foi possível carregar os convites.');
      const [invitations, notifications] = await Promise.all(
        responses.map((response) => response.json())
      );
      setInvitations(invitations);
      setNotifications(notifications);
      setError('');
    } catch {
      setError('Não foi possível carregar os convites.');
    }
  }, [api]);
  useEffect(() => {
    void load();
  }, [load]);
  const respond = async (id: number, accept: boolean) => {
    if (busy) return;
    setBusy(true);
    try {
      const response = await fetch(`${api}/Tcc/participant-invitations/${id}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${Cookies.get('token')}`
        },
        body: JSON.stringify({ accept })
      });
      const data = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error(
          data?.detail || 'Não foi possível responder ao convite.'
        );
      toast.success(typeof data === 'string' ? data : 'Resposta registrada.');
      // Refreshes the home and student navigation after the new binding.
      window.location.reload();
    } catch (failure) {
      toast.error(
        failure instanceof Error ? failure.message : 'Falha ao responder.'
      );
    } finally {
      setBusy(false);
    }
  };
  const dismiss = async (id: number) => {
    setBusy(true);
    try {
      const response = await fetch(
        `${api}/Tcc/participant-invitations/${id}/notification`,
        {
          method: 'PATCH',
          headers: { Authorization: `Bearer ${Cookies.get('token')}` }
        }
      );
      if (!response.ok) throw new Error();
      setNotifications((current) => current.filter((item) => item.id !== id));
    } catch {
      toast.error('Não foi possível marcar a notificação como lida.');
    } finally {
      setBusy(false);
    }
  };
  if (error)
    return (
      <div role="alert" className="mb-6 text-sm text-red-700">
        {error}{' '}
        <Button variant="outline" onClick={() => void load()}>
          Tentar novamente
        </Button>
      </div>
    );
  if (!invitations.length && !notifications.length) return null;
  return (
    <section
      className="mb-8 space-y-4"
      aria-label="Convites para propostas em grupo"
    >
      {notifications.map((notification) => (
        <ProposalDeclinedNotification
          key={notification.id}
          notification={notification}
          busy={busy}
          onDismiss={() => void dismiss(notification.id)}
        />
      ))}
      {invitations.length > 0 && (
        <h2 className="text-lg font-semibold text-[#002B18]">
          Convites para participar de TCC
        </h2>
      )}
      {invitations.map((invitation) => (
        <ProposalInvitationCard
          key={invitation.id}
          invitation={invitation}
          busy={busy}
          onRespond={(accept) => void respond(invitation.id, accept)}
        />
      ))}
    </section>
  );
}

export function ProposalConsentStatus({ tccId }: { tccId: number }) {
  const api = env('NEXT_PUBLIC_API_URL');
  const [status, setStatus] = useState<{ name: string; status: string }[]>([]);
  const [error, setError] = useState(false);
  const load = useCallback(async () => {
    try {
      const response = await fetch(
        `${api}/Tcc/participant-invitations/proposal/${tccId}`,
        {
          headers: { Authorization: `Bearer ${Cookies.get('token')}` },
          cache: 'no-store'
        }
      );
      if (!response.ok) throw new Error();
      setStatus(await response.json());
      setError(false);
    } catch {
      setError(true);
    }
  }, [api, tccId]);
  useEffect(() => {
    void load();
  }, [load]);
  if (error)
    return (
      <p role="alert" className="mt-2 text-sm text-red-700">
        Não foi possível consultar os aceites.{' '}
        <Button variant="outline" onClick={() => void load()}>
          Tentar novamente
        </Button>
      </p>
    );
  if (!status.length) return null;
  return (
    <ul className="mt-3 space-y-1 text-sm" aria-label="Aceites dos integrantes">
      {status.map((item, index) => (
        <li key={index}>
          {item.name}:{' '}
          {item.status === 'ACCEPTED'
            ? 'participação aceita'
            : item.status === 'PENDING'
              ? 'aguardando aceite'
              : 'convite encerrado'}
        </li>
      ))}
    </ul>
  );
}
