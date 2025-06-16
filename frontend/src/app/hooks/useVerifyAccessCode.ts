import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { verifyAccessCodeSchema, VerifyAccessCodeSchemaType } from '@/app/schemas/verifyAccessCodeSchema';
import { toast } from 'react-toastify';

export function useVerifyAccessCode() {
  const form = useForm<VerifyAccessCodeSchemaType>({
    resolver: zodResolver(verifyAccessCodeSchema),
    defaultValues: {
      userEmail: '',
      accessCode: ''
    }
  });

  const submitForm: SubmitHandler<VerifyAccessCodeSchemaType> = async (data) => {
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/AccessCode/verify', {
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

          console.log('Resposta JSON do servidor:', result);
        } else {
          toast.success('Código de acesso verificado com sucesso!');
        }
      } else {
        toast.error(`Erro na requisição: ${response.status} ${response.statusText}`);
      }
    } catch (error) {
      toast.error('Erro ao enviar o código de acesso. Tente novamente mais tarde.');
      console.error('Erro ao enviar o código de acesso:', error);
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
