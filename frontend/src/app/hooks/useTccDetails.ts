'use client';

import { useEffect, useState, useCallback } from 'react';
import { useSearchParams } from 'next/navigation';
import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { cancellationSchema, CancellationSchemaType } from '@/app/schemas/tccCancellationSchema';
import { registerBankingSchema, RegisterBankingSchemaType } from '@/app/schemas/registerBankingSchema';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { toast } from 'react-toastify';

interface TccDetailsResponse {
  infoTcc: { title: string; summary: string; presentationDate: string; presentationTime: string; presentationLocation: string; };
  infoStudent: { name: string; registration: string; cpf: string; course: string; email: string; }[];
  infoAdvisor: { name: string; email: string; };
  infoBanking: { nameInternal: string; emailInternal: string; nameExternal: string; emailExternal: string; };
  cancellationRequest: boolean;
}

interface CancellationDetailsResponse {
  titleTCC: string;
  studentName: string[];
  advisorName: string;
  reasonCancellation: string;
}

interface DecodedToken {
  role: string | string[];
  userId: string;
}

interface Member {
  id: number;
  name: string;
}

export function useTccDetails() {
  const [tccData, setTccData] = useState<TccDetailsResponse | null>(null);
  const [cancellationDetails, setCancellationDetails] = useState<CancellationDetailsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [profile, setProfile] = useState<string | string[] | null>(null);
  const [isCancellationModalOpen, setIsCancellationModalOpen] = useState(false);
  const [isBankingFormVisible, setIsBankingFormVisible] = useState(false);
  const [allBankingMembers, setAllBankingMembers] = useState<Member[]>([]);

  const searchParams = useSearchParams();
  const tccId = Number(searchParams.get('id'));

  const cancellationForm = useForm<CancellationSchemaType>({
    resolver: zodResolver(cancellationSchema),
    defaultValues: { reason: '' },
  });

  const bankingForm = useForm<RegisterBankingSchemaType>({
    resolver: zodResolver(registerBankingSchema),
  });

  const fetchTccDetails = useCallback(async () => {
    setLoading(true);
    const token = Cookies.get('token');
    if (!token || !tccId) {
      setLoading(false);
      return;
    }
    setProfile(jwtDecode<DecodedToken>(token).role);

    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Tcc?tccId=${tccId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error('Erro ao buscar dados do TCC.');
      const result: TccDetailsResponse = await res.json();
      setTccData(result);

      if (result.cancellationRequest) {
        const detailsRes = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Tcc/cancellation?tccId=${tccId}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        if (detailsRes.ok) {
            const detailsData = await detailsRes.json();
            setCancellationDetails(detailsData);
        }
      } else {
        setCancellationDetails(null);
      }
    } catch {
      toast.error('Erro ao carregar dados do TCC.');
    } finally {
      setLoading(false);
    }
  }, [tccId]);

  useEffect(() => {
    fetchTccDetails();
  }, [fetchTccDetails]);

  useEffect(() => {
    const fetchMembers = async () => {
        const token = Cookies.get('token');
        if (!token) return;
        try {
          const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/User/filter?Profile=BANKING`, { headers: { Authorization: `Bearer ${token}` } });

          if(res.ok) {
            const data = await res.json();
            setAllBankingMembers(data);
          }
        } catch {
            toast.error("Erro ao carregar lista de membros da banca.");
        }
    };
    fetchMembers();
  }, []);

  const handleRequestCancellation: SubmitHandler<CancellationSchemaType> = async (data) => {
    try {
      const token = Cookies.get('token');
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Tcc/cancellation/request`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ idTcc: tccId, reason: data.reason }),
      });
      if (!res.ok) throw new Error('Falha ao enviar solicitação.');
      toast.success('Solicitação de cancelamento enviada com sucesso!');
      setIsCancellationModalOpen(false);
      cancellationForm.reset();
      fetchTccDetails();
    } catch {
      toast.error('Erro ao enviar solicitação de cancelamento.');
    }
  };

  const handleApproveCancellation = async () => {
    try {
      const token = Cookies.get('token');
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Tcc/cancellation/approve?tccId=${tccId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error('Falha ao aprovar cancelamento.');
      toast.success('Cancelamento aprovado com sucesso!');
      fetchTccDetails();
    } catch {
      toast.error('Erro ao aprovar cancelamento.');
    }
  };

  const handleRegisterBanking: SubmitHandler<RegisterBankingSchemaType> = async (data) => {
    try {
        const token = Cookies.get('token');
        const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Tcc/banking`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
            body: JSON.stringify({ idTcc: tccId, ...data }),
        });
        if (!res.ok) throw new Error('Falha ao cadastrar banca.');
        toast.success('Banca cadastrada com sucesso!');
        setIsBankingFormVisible(false);
        fetchTccDetails();
    } catch {
        toast.error('Erro ao cadastrar banca.');
    }
  };

  return {
    tccData,
    cancellationDetails,
    loading,
    profile,
    isCancellationModalOpen,
    setIsCancellationModalOpen,
    isBankingFormVisible,
    setIsBankingFormVisible,
    cancellationForm,
    bankingForm,
    allBankingMembers,
    handleRequestCancellation,
    handleApproveCancellation,
    handleRegisterBanking,
  };
}