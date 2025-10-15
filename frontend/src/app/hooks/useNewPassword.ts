'use client';

import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newPasswordSchema, NewPasswordSchemaType } from '@/app/schemas/newPasswordSchema';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';
import { env } from 'next-runtime-env';

export function useNewPassword() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const form = useForm<NewPasswordSchemaType>({
    resolver: zodResolver(newPasswordSchema),
    defaultValues: {
      email: '',
      inviteCode: '',
      password: '',
      confirmPassword: '',
    },
  });

  const { formState: { isSubmitting } } = form;

  const submitForm: SubmitHandler<NewPasswordSchemaType> = async (data) => {

    try {
      const payload = {
        email: data.email,
        password: data.password,
        inviteCode: data.inviteCode,
      };

      const response = await fetch(`${API_URL}/Auth/new-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        toast.success('Senha definida com sucesso! Você já pode fazer login.');
        push('/');
      } else {
        const errorData = await response.json();
        toast.error(errorData.message || "Não foi possível definir a senha. Verifique os dados e tente novamente.");
      }
    } catch {
      toast.error('Não foi possível conectar ao servidor. Tente novamente mais tarde.');
    }
  };

  return { 
    form, 
    submitForm, 
    isSubmitting 
  };
}

