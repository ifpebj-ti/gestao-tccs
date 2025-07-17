'use client';

import { useState, useEffect } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

export interface CompletedTccItem {
  tccId: number;
  studanteNames: string[];
}

export interface CompletedTccDetails {
  infoTcc: { title: string; summary: string; presentationDate: string | null; presentationTime: string | null; presentationLocation: string; };
  infoStudent: { name: string; registration: string; cpf: string; course: string; email: string; }[];
  infoAdvisor: { name: string; email: string; };
  infoBanking: { nameInternal: string; emailInternal: string; nameExternal: string; emailExternal: string; };
}

export function useCompletedTCCs(tccId?: string | null) {
  const [list, setList] = useState<CompletedTccItem[]>([]);
  const [details, setDetails] = useState<CompletedTccDetails | null>(null);
  
  const [isLoading, setIsLoading] = useState(true);
  const API_URL = env('NEXT_PUBLIC_API_URL');

  useEffect(() => {
    const token = Cookies.get('token');
    if (!token) {
      toast.error("Token de autenticação não encontrado.");
      setIsLoading(false);
      return;
    }

    const fetchList = async () => {
      try {
        const res = await fetch(`${API_URL}/Tcc/filter?StatusTcc=COMPLETED`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) throw new Error('Falha ao buscar TCCs concluídos.');
        const data = await res.json();
        setList(data);
      } catch {
        toast.error('Erro ao carregar os TCCs concluídos.');
      }
    };
    
    const fetchDetails = async (id: string) => {
      try {
        const res = await fetch(`${API_URL}/Tcc?tccId=${id}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) throw new Error('Falha ao buscar detalhes do TCC.');
        const data = await res.json();
        setDetails(data);
      } catch {
        toast.error('Erro ao carregar detalhes do TCC.');
      }
    };

    const runFetch = async () => {
        setIsLoading(true);
        if (tccId) {
            await fetchDetails(tccId);
        } else {
            await fetchList();
        }
        setIsLoading(false);
    };

    runFetch();
  }, [tccId, API_URL]);

  return { completedTCCs: list, tccDetails: details, isLoading };
}