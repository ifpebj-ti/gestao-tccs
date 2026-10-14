'use client';

import { useCallback, useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import type { StudentNavigation } from '@/components/Sidebar/menuConfig';
import { resolveStudentNavigation } from '@/lib/studentNavigation';

export function useStudentNavigation(
  userId: string | null,
  isStudent: boolean,
  pathname: string
): StudentNavigation {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [result, setResult] = useState<StudentNavigation>({
    status: 'loading'
  });

  const refresh = useCallback(
    async (signal: AbortSignal) => {
      if (!userId || !isStudent) return;
      const token = Cookies.get('token');
      if (!token) return;
      try {
        const responses = await Promise.all(
          ['COMPLETED', 'IN_PROGRESS'].map((status) =>
            fetch(
              `${API_URL}/Tcc/filter?userId=${encodeURIComponent(userId)}&StatusTcc=${status}`,
              {
                headers: { Authorization: `Bearer ${token}` },
                signal
              }
            )
          )
        );
        if (responses.some((response) => !response.ok))
          throw new Error('Consulta indisponível');
        const [completed, active] = await Promise.all(
          responses.map((response) => response.json())
        );
        if (signal.aborted) return;
        setResult(resolveStudentNavigation(completed, active));
      } catch {
        if (!signal.aborted) setResult({ status: 'error' });
      }
    },
    [API_URL, userId, isStudent]
  );

  useEffect(() => {
    const controller = new AbortController();
    void refresh(controller.signal);
    const handleFocus = () => {
      void refresh(controller.signal);
    };
    window.addEventListener('focus', handleFocus);
    return () => {
      controller.abort();
      window.removeEventListener('focus', handleFocus);
    };
  }, [refresh, pathname]);
  return result;
}
