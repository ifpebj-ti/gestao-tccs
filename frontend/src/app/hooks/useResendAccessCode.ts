import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { resendAccessCodeSchema, ResendAccessCodeSchemaType } from '@/app/schemas/resendAccessCodeSchema';
import { toast } from 'react-toastify';

export function useResendAccessCode() {
  const form = useForm<ResendAccessCodeSchemaType>({
    resolver: zodResolver(resendAccessCodeSchema),
    defaultValues: {
      userEmail: '',
    }
  });

  const submitForm: SubmitHandler<ResendAccessCodeSchemaType> = async (data) => {
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/api/AccessCode/resend', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail,
        })
      });

      if (response.ok) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const result = await response.json();
          toast.success('Código de acesso enviado com sucesso!');
          console.log('Resposta JSON do servidor:', result);
        } else {
          toast.success('Código de acesso enviado com sucesso!');
        }
      } else {
        toast.error(`Erro na requisição: ${response.status} ${response.statusText}`);
      }
    } catch (error) {
      toast.error('Erro ao enviar o código de acesso. Tente novamente mais tarde.');
      console.error('Erro ao enviar o código de acesso:', error);
    }
  };

  return { form, submitForm };
}
