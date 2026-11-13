'use client';

import { useCallback, useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';

export interface PendingInternalBankingEvaluation {
  tccId: number;
  tccTitle: string | null;
  scheduledDate: string;
  location: string;
  studentNames: string[];
}

const UPDATED_EVENT = 'banking-evaluations-updated';

export function notifyBankingEvaluationsUpdated() {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event(UPDATED_EVENT));
  }
}

export function usePendingInternalBankingEvaluations(enabled = true) {
  const apiUrl = env('NEXT_PUBLIC_API_URL');
  const [evaluations, setEvaluations] = useState<PendingInternalBankingEvaluation[]>([]);
  const [isLoading, setIsLoading] = useState(enabled);

  const refresh = useCallback(async () => {
    if (!enabled) {
      setEvaluations([]);
      setIsLoading(false);
      return;
    }

    const token = Cookies.get('token');
    if (!token) {
      setEvaluations([]);
      setIsLoading(false);
      return;
    }

    try {
      const response = await fetch(`${apiUrl}/Tcc/banking/pending-evaluations`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: 'no-store'
      });
      if (!response.ok) throw new Error('Falha ao carregar avaliações pendentes.');
      setEvaluations(await response.json());
    } catch {
      setEvaluations([]);
    } finally {
      setIsLoading(false);
    }
  }, [apiUrl, enabled]);

  useEffect(() => {
    void refresh();
    if (!enabled) return;

    const interval = window.setInterval(() => void refresh(), 60_000);
    const refreshOnFocus = () => void refresh();
    const refreshOnUpdate = () => void refresh();
    window.addEventListener('focus', refreshOnFocus);
    window.addEventListener(UPDATED_EVENT, refreshOnUpdate);

    return () => {
      window.clearInterval(interval);
      window.removeEventListener('focus', refreshOnFocus);
      window.removeEventListener(UPDATED_EVENT, refreshOnUpdate);
    };
  }, [enabled, refresh]);

  return { evaluations, isLoading, refresh };
}
