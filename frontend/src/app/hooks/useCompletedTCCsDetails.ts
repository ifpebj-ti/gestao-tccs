'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

interface InfoTcc {
  title: string;
  summary: string;
  presentationDate: string | null;
  presentationTime: string | null;
  presentationLocation: string;
}
interface InfoStudent { name: string; registration: string; cpf: string; course: string; email: string; }
interface InfoAdvisor { name: string; email: string; }
interface InfoBanking { nameInternal: string; emailInternal: string; nameExternal: string; emailExternal: string; }

export interface CompletedTccDetailsResponse {
  infoTcc: InfoTcc;
  infoStudent: InfoStudent[];
  infoAdvisor: InfoAdvisor;
  infoBanking: InfoBanking;
}

export function useCompletedTccDetails() {
  const [tccDetails, setTccDetails] = useState<CompletedTccDetailsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const params = useParams();
  const tccId = params.id as string;
  const API_URL = env('NEXT_PUBLIC_API_URL');

  const fetchDetails = useCallback(async () => {
    if (!tccId) return;
    setIsLoading(true);
    const token = Cookies.get('token');

    try {
      const res = await fetch(`${API_URL}/Tcc?tccId=${tccId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error('Falha ao buscar detalhes do TCC.');
      const data = await res.json();
      setTccDetails(data);
    } catch {
      toast.error('Erro ao carregar detalhes do TCC.');
    } finally {
      setIsLoading(false);
    }
  }, [tccId, API_URL]);

  useEffect(() => {
    fetchDetails();
  }, [fetchDetails]);

  return { tccDetails, isLoading };
}