import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { resendAccessCodeSchema, ResendAccessCodeSchemaType } from '@/app/schemas/resendAccessCodeSchema';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

export function useResendAccessCode() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const form = useForm<ResendAccessCodeSchemaType>({
    resolver: zodResolver(resendAccessCodeSchema),
    defaultValues: {
      userEmail: '',
    }
  });

  const submitForm: SubmitHandler<ResendAccessCodeSchemaType> = async (data) => {
    try {
      const response = await fetch(`${API_URL}/AccessCode/resend`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail,
        })
      });

      if (response.ok) {
        toast.success('Código de acesso enviado com sucesso!');
        return true;
      } else {
        toast.error('Erro ao enviar o código de acesso. Verifique se o e-mail está correto.');
        return false;
      }
    } catch {
      toast.error('Erro de conexão. Tente novamente mais tarde.');
      return false;
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
