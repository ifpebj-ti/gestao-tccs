import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { verifyAccessCodeSchema, VerifyAccessCodeSchemaType } from '@/app/schemas/verifyAccessCodeSchema';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import Cookies from 'js-cookie';

export function useVerifyAccessCode() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const form = useForm<VerifyAccessCodeSchemaType>({
    resolver: zodResolver(verifyAccessCodeSchema),
    defaultValues: {
      userEmail: '',
      accessCode: ''
    }
  });

  const submitForm: SubmitHandler<VerifyAccessCodeSchemaType> = async (data) => {
    try {
      const response = await fetch(`${API_URL}/AccessCode/verify`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail,
          accessCode: data.accessCode
        })
      });

      if (response.ok) {
        const isHttps = typeof window !== 'undefined' && window.location.protocol === 'https:';
        Cookies.set('access_token_temp', `verified_${encodeURIComponent(data.userEmail.trim())}`, {
          expires: 15 / (24 * 60),
          path: '/',
          sameSite: 'lax',
          secure: isHttps
        });

        toast.success('Código de acesso verificado com sucesso!');

        if (window.location.pathname === '/firstAccess') {
          window.location.href = '/autoRegister';
        } else if (window.location.pathname === '/forgotPassword') {
          window.location.href = '/updatePassword';
        }
      } else {
        toast.error('Código de acesso inválido ou expirado. Tente novamente.');
      }
    } catch {
      toast.error('Erro ao enviar o código de acesso. Tente novamente mais tarde.');
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
