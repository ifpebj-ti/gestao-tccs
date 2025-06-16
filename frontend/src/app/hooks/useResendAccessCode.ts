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
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/AccessCode/resend', {
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
          toast.success('Código de acesso enviado com sucesso!');
        } else {
          toast.success('Código de acesso enviado com sucesso!');
        }
      } else {
        toast.error(`Erro na requisição: ${response.status} ${response.statusText}`);
      }
    } catch {
      toast.error('Erro ao enviar o código de acesso. Tente novamente mais tarde.');
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
