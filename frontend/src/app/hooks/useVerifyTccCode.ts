import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { VerifyTccCodeSchemaType, verifyTccCodeSchema } from '@/app/schemas/verifyTccCodeSchema';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

export function useVerifyTccCode() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const form = useForm<VerifyTccCodeSchemaType>({
    resolver: zodResolver(verifyTccCodeSchema),
    defaultValues: {
      userEmail: '',
      code: ''
    }
  });

  const submitForm: SubmitHandler<VerifyTccCodeSchemaType> = async (data) => {
    try {
      const response = await fetch(`${API_URL}/Tcc/code/verify`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail,
          code: data.code
        })
      });

      if (response.ok) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const result = await response.json();

          // Cookie que expira em 5 minutos
          const expires = new Date(Date.now() + 5 * 60 * 1000).toUTCString();
          document.cookie = `access_token_temp=${result.token}; expires=${expires}; path=/; secure; samesite=Strict`;

          toast.success('Código de acesso verificado com sucesso!');

          if (window.location.pathname === '/firstAccess') {
            window.location.href = '/autoRegister';
          } else if (window.location.pathname === '/forgotPassword') {
            window.location.href = '/newPassword';
          }

        } else {
          toast.success('Código de acesso verificado com sucesso!');
        }
      } else {
        toast.error('Código de acesso inválido ou expirado. Tente novamente.');
      }
    } catch (error) {
      toast.error('Erro ao enviar o código de acesso. Tente novamente mais tarde.');
      console.error('Erro ao enviar o código de acesso:', error);
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
