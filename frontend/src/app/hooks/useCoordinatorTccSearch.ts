'use client';

import { useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';
import { filterTccs, type SearchableTcc } from '@/lib/tccSearch';

export function useCoordinatorTccSearch<T extends SearchableTcc>(tccs: T[]) {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [enabled, setEnabled] = useState(false);
  const [includeAdvisor, setIncludeAdvisor] = useState(false);
  const [query, setQuery] = useState('');
  const [advisorNames, setAdvisorNames] = useState<Record<number, string>>({});
  const [advisorSearchUnavailable, setAdvisorSearchUnavailable] =
    useState(false);

  useEffect(() => {
    const token = Cookies.get('token');
    if (!token) return;
    try {
      const { role } = jwtDecode<{ role: string | string[] }>(token);
      const roles = Array.isArray(role) ? role : [role];
      if (roles.includes('ADMIN')) return;
      if (roles.includes('SUPERVISOR') && !roles.includes('COORDINATOR'))
        return;
      if (!roles.includes('COORDINATOR') && !roles.includes('ADVISOR')) return;
      setEnabled(true);
      if (!roles.includes('COORDINATOR')) return;
    } catch {
      return;
    }
    setIncludeAdvisor(true);
    const controller = new AbortController();
    setAdvisorSearchUnavailable(false);
    fetch(`${API_URL}/User/filter?Profile=ADVISOR`, {
      headers: { Authorization: `Bearer ${token}` },
      signal: controller.signal
    })
      .then(async (response) => {
        if (!response.ok) throw new Error();
        const advisors: { id: number; name: string }[] = await response.json();
        if (!controller.signal.aborted) {
          setAdvisorNames(
            Object.fromEntries(advisors.map(({ id, name }) => [id, name]))
          );
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) setAdvisorSearchUnavailable(true);
      });
    return () => controller.abort();
  }, [API_URL]);

  return {
    enabled,
    includeAdvisor,
    query,
    setQuery,
    advisorSearchUnavailable,
    filteredTccs: enabled ? filterTccs(tccs, query, advisorNames) : tccs
  };
}
