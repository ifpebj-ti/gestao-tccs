'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  cancellationSchema,
  CancellationSchemaType
} from '@/app/schemas/tccCancellationSchema';
import {
  registerBankingSchema,
  RegisterBankingSchemaType
} from '@/app/schemas/registerBankingSchema';
import {
  scheduleSchema,
  ScheduleSchemaType
} from '@/app/schemas/scheduleSchema';
import { editTccSchema, EditTccSchemaType } from '@/app/schemas/editTccSchema';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

interface Member {
  id: number;
  name: string;
}

interface TccDetailsResponse {
  id: number;
  infoTcc: {
    title: string;
    summary: string;
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
  };
  infoStudent: {
    name: string;
    registration: string;
    cpf: string;
    course: string;
    email: string;
  }[];
  infoAdvisor: { name: string; email: string };
  infoBanking: {
    nameInternal: string;
    emailInternal: string;
    nameExternal: string;
    emailExternal: string;
  };
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

export function useTccDetails() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const [tccData, setTccData] = useState<TccDetailsResponse | null>(null);
  const [cancellationDetails, setCancellationDetails] =
    useState<CancellationDetailsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [profile, setProfile] = useState<string | string[] | null>(null);
  const [resendingInviteTo, setResendingInviteTo] = useState<string | null>(
    null
  );

  const [isCancellationModalOpen, setIsCancellationModalOpen] = useState(false);
  const [isBankingFormVisible, setIsBankingFormVisible] = useState(false);
  const [isScheduleFormVisible, setIsScheduleFormVisible] = useState(false);
  const [isEditingTccInfo, setIsEditingTccInfo] = useState(false);

  const [allBankingMembers, setAllBankingMembers] = useState<Member[]>([]);

  const searchParams = useSearchParams();
  const tccId = Number(searchParams.get('id'));

  const cancellationForm = useForm<CancellationSchemaType>({
    resolver: zodResolver(cancellationSchema),
    defaultValues: { reason: '' }
  });

  const bankingForm = useForm<RegisterBankingSchemaType>({
    resolver: zodResolver(registerBankingSchema)
  });

  const scheduleForm = useForm<ScheduleSchemaType>({
    resolver: zodResolver(scheduleSchema)
  });

  const editTccForm = useForm<EditTccSchemaType>({
    resolver: zodResolver(editTccSchema),
    defaultValues: { title: '', summary: '' }
  });

  const fetchTccDetails = useCallback(async () => {
    setLoading(true);
    const token = Cookies.get('token');
    if (!token || !tccId) {
      setLoading(false);
      return;
    }

    try {
      setProfile(jwtDecode<DecodedToken>(token).role);
    } catch {}

    try {
      const res = await fetch(`${API_URL}/Tcc?tccId=${tccId}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) throw new Error('Erro ao buscar dados do TCC.');
      const result: TccDetailsResponse = await res.json();
      setTccData(result);

      editTccForm.reset({
        title: result.infoTcc.title ?? '',
        summary: result.infoTcc.summary ?? ''
      });

      if (result.cancellationRequest) {
        const detailsRes = await fetch(
          `${API_URL}/Tcc/cancellation?tccId=${tccId}`,
          {
            headers: { Authorization: `Bearer ${token}` }
          }
        );
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
  }, [tccId, API_URL, editTccForm]);

  useEffect(() => {
    fetchTccDetails();
  }, [fetchTccDetails]);

  useEffect(() => {
    const fetchMembers = async () => {
      const token = Cookies.get('token');
      if (!token) return;
      try {
        const res = await fetch(`${API_URL}/User/filter?Profile=BANKING`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (res.ok) {
          const data = await res.json();
          setAllBankingMembers(data);
        }
      } catch {
        toast.error('Erro ao carregar lista de membros da banca.');
      }
    };
    fetchMembers();
  }, [API_URL]);

  const handleRequestCancellation: SubmitHandler<
    CancellationSchemaType
  > = async (data) => {
    try {
      const token = Cookies.get('token');
      const res = await fetch(`${API_URL}/Tcc/cancellation/request`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ idTcc: tccId, reason: data.reason })
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
    const token = Cookies.get('token');
    if (!token) {
      toast.error('Autenticação necessária.');
      return;
    }
    try {
      const res = await fetch(
        `${API_URL}/Tcc/cancellation/approve?tccId=${tccId}`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`
          }
        }
      );
      if (!res.ok) throw new Error('Falha ao aprovar o cancelamento.');
      push('/ongoingTCCs');
      toast.success('Cancelamento aprovado com sucesso!');
      fetchTccDetails();
    } catch {
      toast.error('Erro ao aprovar o cancelamento.');
    }
  };

  const handleRegisterBanking: SubmitHandler<
    RegisterBankingSchemaType
  > = async (data) => {
    try {
      const token = Cookies.get('token');
      const res = await fetch(`${API_URL}/Tcc/banking`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ idTcc: tccId, ...data })
      });
      if (!res.ok) throw new Error('Falha ao cadastrar banca.');
      toast.success('Banca cadastrada com sucesso!');
      setIsBankingFormVisible(false);
      fetchTccDetails();
    } catch {
      toast.error('Erro ao cadastrar banca.');
    }
  };

  const handleScheduleSubmit: SubmitHandler<ScheduleSchemaType> = async (
    data
  ) => {
    const method = tccData?.infoTcc.presentationDate ? 'PUT' : 'POST';
    try {
      const token = Cookies.get('token');
      const res = await fetch(`${API_URL}/Tcc/schedule`, {
        method: method,
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ ...data, idTcc: tccId })
      });
      if (!res.ok)
        throw new Error(
          `Falha ao ${method === 'POST' ? 'marcar' : 'editar'} apresentação.`
        );
      toast.success(
        `Apresentação ${method === 'POST' ? 'marcada' : 'editada'} com sucesso!`
      );
      setIsScheduleFormVisible(false);
      fetchTccDetails();
    } catch {
      toast.error(
        `Erro ao ${method === 'POST' ? 'marcar' : 'editar'} apresentação.`
      );
    }
  };

  const handleSendScheduleEmail = async () => {
    const token = Cookies.get('token');
    if (!token) {
      toast.error('Autenticação necessária.');
      return;
    }
    try {
      const res = await fetch(`${API_URL}/Tcc/schedule/email?tccId=${tccId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        }
      });
      if (!res.ok) {
        throw new Error('Falha ao enviar email de agendamento.');
      }
      toast.success('Agenda enviada por email para todos os envolvidos!');
    } catch {
      toast.error('Erro ao tentar enviar o email.');
    }
  };

  const handleResendInvite = async (studentEmail: string) => {
    setResendingInviteTo(studentEmail);
    const token = Cookies.get('token');
    if (!token) {
      toast.error('Autenticação necessária.');
      setResendingInviteTo(null);
      return;
    }

    try {
      const res = await fetch(`${API_URL}/Tcc/invite/code/${tccId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(studentEmail)
      });

      if (!res.ok) {
        throw new Error('Falha ao reenviar o convite.');
      }

      toast.success(`Convite reenviado com sucesso para ${studentEmail}!`);
    } catch {
      toast.error('Erro ao tentar reenviar o convite.');
    } finally {
      setResendingInviteTo(null);
    }
  };

  const handleDownloadAllDocuments = async () => {
    const token = Cookies.get('token');
    if (!tccId || !token) {
      toast.error('Não foi possível identificar o TCC para download.');
      return;
    }

    try {
      const res = await fetch(
        `${API_URL}/Signature/all/documents/download/${tccId}`,
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );

      if (!res.ok) throw new Error('Erro ao baixar os documentos.');

      const contentDisposition = res.headers.get('Content-Disposition');
      let filename = `TCC_${tccId}_documentos.zip`;

      if (contentDisposition) {
        const filenameMatch = contentDisposition.match(/filename="?([^"]+)"?/);
        if (filenameMatch && filenameMatch[1]) {
          filename = filenameMatch[1];
        }
      }

      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', filename);
      document.body.appendChild(link);
      link.click();
      link.parentNode?.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch {
      toast.error('Não foi possível baixar o pacote de documentos.');
    }
  };

  const handleEditTccInfo: SubmitHandler<EditTccSchemaType> = async (data) => {
    try {
      const token = Cookies.get('token');
      const payload = {
        Title: data.title,
        Summary: data.summary
      };

      const res = await fetch(`${API_URL}/Tcc/${tccId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (!res.ok) {
        throw new Error('Erro ao atualizar TCC.');
      }

      toast.success('Informações atualizadas com sucesso!');
      setIsEditingTccInfo(false);
      fetchTccDetails();
    } catch {
      toast.error('Erro ao atualizar informações do TCC.');
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
    isScheduleFormVisible,
    setIsScheduleFormVisible,
    isEditingTccInfo,
    setIsEditingTccInfo,
    cancellationForm,
    bankingForm,
    scheduleForm,
    editTccForm,
    allBankingMembers,
    handleRequestCancellation,
    handleApproveCancellation,
    handleRegisterBanking,
    handleScheduleSubmit,
    handleSendScheduleEmail,
    resendingInviteTo,
    handleResendInvite,
    handleDownloadAllDocuments,
    handleEditTccInfo
  };
}
