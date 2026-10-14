'use client';

import { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { toast } from 'react-toastify';
import { isValidProfilePhone, phoneDigits } from '@/lib/phone';
import {
  studentProfileSchema,
  type StudentProfileValues
} from '@/app/schemas/studentProfileSchema';

export interface MyProfile {
  id: number;
  name: string;
  email: string;
  phone: string;
  registration: string | null;
  siape: string | null;
  campus: string | null;
  course: string | null;
  roles: string[];
  cpf: string | null;
  userClass: string | null;
  shift: string | null;
}

export function useMyProfile() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const router = useRouter();
  const [profile, setProfile] = useState<MyProfile | null>(null);
  const [status, setStatus] = useState<'loading' | 'ready' | 'error'>(
    'loading'
  );
  const [isSaving, setIsSaving] = useState(false);

  const loadProfile = useCallback(
    async (signal?: AbortSignal) => {
      const token = Cookies.get('token');
      if (!token) {
        router.replace('/');
        return;
      }
      setStatus('loading');
      try {
        const response = await fetch(`${API_URL}/User/me`, {
          cache: 'no-store',
          headers: { Authorization: `Bearer ${token}` },
          signal
        });
        if (response.status === 401) {
          router.replace('/');
          return;
        }
        if (!response.ok) throw new Error('Consulta indisponível');
        const data: MyProfile = await response.json();
        if (signal?.aborted) return;
        setProfile(data);
        setStatus('ready');
      } catch {
        if (!signal?.aborted) setStatus('error');
      }
    },
    [API_URL, router]
  );

  useEffect(() => {
    const controller = new AbortController();
    void loadProfile(controller.signal);
    return () => controller.abort();
  }, [loadProfile]);

  const save = async (
    method: 'PUT' | 'PATCH',
    data: { phone: string },
    message: string
  ): Promise<boolean> => {
    const token = Cookies.get('token');
    if (!token) {
      router.replace('/');
      return false;
    }
    setIsSaving(true);
    try {
      const response = await fetch(`${API_URL}/User/me`, {
        method,
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ ...data, phone: phoneDigits(data.phone) })
      });
      if (response.status === 401) {
        router.replace('/');
        return false;
      }
      if (!response.ok) throw new Error('Atualização indisponível');
      const updated: MyProfile = await response.json();
      setProfile(updated);
      window.dispatchEvent(
        new CustomEvent('profile-updated', {
          detail: { userId: String(updated.id), name: updated.name }
        })
      );
      toast.success(message);
      return true;
    } catch {
      toast.error('Não foi possível salvar suas informações. Tente novamente.');
      return false;
    } finally {
      setIsSaving(false);
    }
  };

  const savePhone = (phone: string) => {
    if (!isValidProfilePhone(phone)) {
      toast.error('Informe um telefone válido com DDD.');
      return Promise.resolve(false);
    }
    return save('PUT', { phone }, 'Telefone atualizado com sucesso!');
  };
  const saveStudent = (data: StudentProfileValues) => {
    const parsed = studentProfileSchema.safeParse(data);
    if (!parsed.success) return Promise.resolve(false);
    return save('PATCH', parsed.data, 'Informações atualizadas com sucesso!');
  };
  return { profile, status, isSaving, loadProfile, savePhone, saveStudent };
}
