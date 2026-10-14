export interface ReceivedProposal {
  tccId: number;
  title: string;
  summary: string;
  studanteNames: string[];
  pendingParticipantCount: number;
}

async function request(
  base: string,
  token: string,
  path: string,
  init?: RequestInit
) {
  const response = await fetch(`${base}/Tcc${path}`, {
    ...init,
    cache: 'no-store',
    headers: { Authorization: `Bearer ${token}`, ...init?.headers }
  });
  const data = await response.json().catch(() => null);
  if (!response.ok)
    throw new Error(
      data?.detail ||
        data?.message ||
        'Não foi possível concluir a solicitação. Tente novamente.'
    );
  return data;
}

export async function loadReceivedProposals(
  base: string,
  token: string,
  userId: string
): Promise<ReceivedProposal[]> {
  const items: ReceivedProposal[] = await request(
    base,
    token,
    `/filter?userId=${encodeURIComponent(userId)}&StatusTcc=PENDING_APPROVAL`
  );
  return Promise.all(
    items.map(async (item) => {
      const detail = await request(base, token, `?tccId=${item.tccId}`);
      return {
        ...item,
        title: detail.infoTcc.title,
        summary: detail.infoTcc.summary,
        pendingParticipantCount: detail.infoTcc.pendingParticipantCount ?? 0
      };
    })
  );
}

export async function decideReceivedProposal(
  base: string,
  token: string,
  id: number,
  reason?: string
) {
  if (reason !== undefined && (!reason.trim() || reason.trim().length > 1000))
    throw new Error('Informe uma justificativa de até 1000 caracteres.');
  return request(
    base,
    token,
    `/${id}/${reason === undefined ? 'approve' : 'reject'}-proposal`,
    {
      method: 'PATCH',
      ...(reason !== undefined
        ? {
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ reason: reason.trim() })
          }
        : {})
    }
  );
}
