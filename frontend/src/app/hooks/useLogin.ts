import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema, LoginSchemaType } from '@/app/schemas/loginSchema';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { useRouter } from 'next/navigation';
import { env } from 'next-runtime-env';

export function useLogin() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const form = useForm<LoginSchemaType>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: ''
    }
  });

  const submitForm: SubmitHandler<LoginSchemaType> = async (data) => {
    try {
      const response = await fetch(`${API_URL}/Auth/login`, {
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
          const token = result.accessToken;

          if (token) {
            Cookies.set('token', token, { expires: 1 }); // Define o cookie para expirar em 1 dia
            push('/homePage');
          }
        } else {
          toast.success('Login realizado com sucesso!');
        }
      } else {
        toast.error('Senha ou email incorretos. Tente novamente.');
      }
    } catch {
      toast.error('Erro ao enviar o login. Tente novamente mais tarde.');
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
