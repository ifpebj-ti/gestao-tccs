import { useState } from 'react';
import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { VerifyTccCodeSchemaType, verifyTccCodeSchema } from '@/app/schemas/verifyTccCodeSchema';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

export function useVerifyTccCode() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [isSendingCode, setIsSendingCode] = useState(false);

  const form = useForm<VerifyTccCodeSchemaType>({
    resolver: zodResolver(verifyTccCodeSchema),
    defaultValues: {
      userEmail: '',
      code: ''
    }
  });

  const sendCode = async (email: string): Promise<boolean> => {
    if (!email || !email.trim()) {
      toast.error('Por favor, informe seu e-mail institucional.');
      return false;
    }

    const trimmedEmail = email.trim().toLowerCase();
    if (!trimmedEmail.endsWith('.ifpe.edu.br')) {
      toast.error('O e-mail deve pertencer ao domínio institucional (@discente.ifpe.edu.br).');
      return false;
    }

    setIsSendingCode(true);
    try {
      const response = await fetch(`${API_URL}/Tcc/code/send`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ userEmail: trimmedEmail })
      });

      if (response.ok) {
        toast.success('Código de acesso enviado com sucesso para seu e-mail!');
        return true;
      } else {
        let msg = 'Erro ao enviar código de acesso.';
        try {
          const err = await response.json();
          if (err.message) msg = err.message;
          else if (err.detail) msg = err.detail;
          else if (err.title) msg = err.title;
        } catch { }
        toast.error(msg);
        return false;
      }
    } catch {
      toast.error('Erro de conexão com o servidor. Tente novamente mais tarde.');
      return false;
    } finally {
      setIsSendingCode(false);
    }
  };

  const submitForm: SubmitHandler<VerifyTccCodeSchemaType> = async (data) => {
    try {
      const response = await fetch(`${API_URL}/Tcc/code/verify`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail.trim(),
          code: data.code.trim().toUpperCase()
        })
      });

      if (response.ok) {
        // Armazena temporariamente para evitar redigitação no cadastro e senha
        if (typeof window !== 'undefined') {
          sessionStorage.setItem('first_access_email', data.userEmail.trim());
          sessionStorage.setItem('first_access_code', data.code.trim().toUpperCase());
        }

        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const result = await response.json();
          if (result.token) {
            const expires = new Date(Date.now() + 5 * 60 * 1000).toUTCString();
            document.cookie = `access_token_temp=${result.token}; expires=${expires}; path=/; secure; samesite=Strict`;
          }
        }

        toast.success('Código de acesso verificado com sucesso!');

        if (window.location.pathname === '/firstAccess') {
          window.location.href = '/autoRegister';
        } else if (window.location.pathname === '/forgotPassword') {
          window.location.href = '/newPassword';
        }
      } else {
        let errorMessage = 'Código de acesso inválido ou expirado. Tente novamente.';
        try {
          const errorData = await response.json();
          if (errorData.message) errorMessage = errorData.message;
          else if (errorData.detail) errorMessage = errorData.detail;
          else if (errorData.errors?.Code?.[0]) errorMessage = errorData.errors.Code[0];
          else if (errorData.errors?.UserEmail?.[0]) errorMessage = errorData.errors.UserEmail[0];
        } catch { }
        toast.error(errorMessage);
      }
    } catch {
      toast.error('Erro de conexão ao verificar o código. Tente novamente mais tarde.');
    }
  };

  return { form, submitForm, sendCode, isSendingCode, isSubmitting: form.formState.isSubmitting };
}

