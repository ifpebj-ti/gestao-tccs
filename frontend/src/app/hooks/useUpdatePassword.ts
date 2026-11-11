import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { updatePasswordSchema, UpdatePasswordSchemaType } from '@/app/schemas/updatePasswordSchema';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

export function useUpdatePassword() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const form = useForm<UpdatePasswordSchemaType>({
    resolver: zodResolver(updatePasswordSchema),
    defaultValues: {
      userEmail: '',
      userPassword: '',
      confirmPassword: ''
    }
  });

  const submitForm: SubmitHandler<UpdatePasswordSchemaType> = async (data) => {
    try {
      const response = await fetch(`${API_URL}/Auth/update-password`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userEmail: data.userEmail,
          userPassword: data.userPassword
        })
      });
      if (response.ok) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          toast.success('Senha atualizada com sucesso!');
          window.location.href = '/';
        } else {
          toast.success('Senha atualizada com sucesso!');
        }
      } else {
        toast.error(response.status === 401
          ? 'Valide novamente o código de recuperação neste navegador.'
          : 'Não foi possível atualizar a senha. Confira os dados e a validade do código.');
      }
    } catch {
      toast.error('Erro ao enviar a senha. Tente novamente mais tarde.');
    }
  };

  return { form, submitForm, isSubmitting: form.formState.isSubmitting};
}
