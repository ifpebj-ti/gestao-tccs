import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newUserSchema, NewUserSchemaSchemaType } from '@/app/schemas/newUserSchema';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';

export function useNewUserForm() {
  const { push } = useRouter();
  const {handleSubmit, register, formState: {errors, isSubmitting}, reset} = useForm<NewUserSchemaSchemaType>({
    resolver: zodResolver(newUserSchema),
    defaultValues: {
      name: '',
      email: '',
      registration: '',
      cpf: '',
      profile: undefined,
      siape: '',
      course: 'ENGENHARIA DE SOFTWARE'
    }
  });

  const submitForm: SubmitHandler<NewUserSchemaSchemaType> = async (data) => {
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/api/User', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({...data, profile: [data.profile]})
      });

      if (response.ok) {
        toast.success('Usuário registrado com sucesso!');
        reset();
        const result = await response.json();
        Cookies.set('access_token_temp', result.token, { expires: 5 / 1440 }); // 5 minutes
        if (data.profile === 'STUDENT') {
          push('/newPassword');
        }
      } else {
        toast.error("Erro ao registrar usuário. Verifique os dados e tente novamente.");
      }
    } catch {
      toast.error('Erro ao registrar usuário. Tente novamente mais tarde.');
    }
  };

  return { register, submitForm, handleSubmit, errors, isSubmitting};
}
