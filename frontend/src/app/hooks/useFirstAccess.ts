import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { firstAccessSchema, FirstAccessSchemaType } from '@/app/schemas/firstAccessSchema';
import { toast } from 'react-toastify';

export function useFirstAccess() {
  const form = useForm<FirstAccessSchemaType>({
    resolver: zodResolver(firstAccessSchema),
    defaultValues: {
      userEmail: '',
      accessCode: ''
    }
  });

  const submitForm: SubmitHandler<FirstAccessSchemaType> = async (data) => {
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/api/AccessCode/verify', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail,
          accessCode: data.accessCode
        })
      });

      console.log('Dados enviados:', {
        userEmail: data.userEmail,
        accessCode: data.accessCode
        });

        console.log('Resposta do servidor:', response);

      if (response.ok) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const result = await response.json();
          toast.success('Código de acesso verificado com sucesso!');
            window.location.href = '/autoRegister';
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

  return { form, submitForm };
}
