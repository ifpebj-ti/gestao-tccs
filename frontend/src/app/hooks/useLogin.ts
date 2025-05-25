import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema, LoginSchemaType } from '@/app/schemas/loginSchema';
import { toast } from 'react-toastify';

export function useLogin() {
  const form = useForm<LoginSchemaType>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: ''
    }
  });

  const submitForm: SubmitHandler<LoginSchemaType> = async (data) => {
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: data.email,
          password: data.password
        })
      });

      if (response.ok) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const result = await response.json();
          toast.success('Login realizado com sucesso!');
          window.location.href = '/homePage';
          console.log('Resposta JSON do servidor:', result);
        } else {
          toast.success('Login realizado com sucesso!');
        }
      } else {
        toast.error(`Erro na requisição: ${response.status} ${response.statusText}`);
      }
    } catch (error) {
      toast.error('Erro ao enviar o login. Tente novamente mais tarde.');
      console.error('Erro ao enviar o login:', error);
    }
  };

  return { form, submitForm };
}
