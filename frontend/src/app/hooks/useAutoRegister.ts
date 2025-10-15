'use client';

import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { autoRegisterSchema, AutoRegisterSchemaType } from '@/app/schemas/autoRegisterSchema';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';
import { env } from 'next-runtime-env';

export function useAutoRegister() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const form = useForm<AutoRegisterSchemaType>({
    resolver: zodResolver(autoRegisterSchema),
    defaultValues: {
      name: '',
      email: '',
      registration: '',
      cpf: '',
    },
  });

  const { formState: { isSubmitting }, reset } = form;

  const submitForm: SubmitHandler<AutoRegisterSchemaType> = async (data) => {
    try {
      const payload = {
        name: data.name,
        email: data.email,
        registration: data.registration,
        cpf: data.cpf,
      };

      const response = await fetch(`${API_URL}/User/autoregister`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        toast.success('Cadastro realizado com sucesso! Agora, crie sua senha.');
        reset();
        push('/newPassword');
      } else {
        const errorData = await response.json();
        toast.error(errorData.message || "Erro ao realizar cadastro. Verifique os dados e tente novamente.");
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
