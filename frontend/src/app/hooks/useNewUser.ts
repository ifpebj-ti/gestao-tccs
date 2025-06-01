import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newUserSchema, NewUserSchemaSchemaType } from '@/app/schemas/newUserSchema';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';

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
        const expires = new Date(Date.now() + 5 * 60 * 1000).toUTCString();
        document.cookie = `access_token_temp=${result.token}; expires=${expires}; path=/; secure; samesite=Strict`;
        if (data.profile === 'STUDENT') {
          push('/newPassword');
        }
      } else {
        const errorText = await response.text();
        toast.error(`Erro: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      toast.error('Erro ao registrar usuário. Tente novamente mais tarde.');
      console.error('Erro no cadastro:', error);
    }
  };

  return { register, submitForm, handleSubmit, errors, isSubmitting};
}
