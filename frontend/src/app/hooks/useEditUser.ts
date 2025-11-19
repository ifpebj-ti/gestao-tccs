'use client';

import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { editUserSchema, EditUserSchemaType } from '../schemas/editUserSchema';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import Cookies from 'js-cookie';

/**
 * @param onSuccess - Callback para ser executado após salvar com sucesso.
 */
export function useEditUser(onSuccess: () => void) {
  const API_URL = env('NEXT_PUBLIC_API_URL');

  const form = useForm<EditUserSchemaType>({
    resolver: zodResolver(editUserSchema),
    defaultValues: {
      id: 0,
      name: '',
      email: '',
      registration: '',
      cpf: '',
      siape: '',
      phone: '',
      userClass: '',
      titration: '',
      status: 'INACTIVE',
      profile: [],
      shift: null,
      campiId: null,
      courseId: null
    }
  });

  const {
    formState: { isSubmitting }
  } = form;

  const submitForm: SubmitHandler<EditUserSchemaType> = async (data) => {
    const payload = data;

    const token = Cookies.get('token');
    if (!token) {
      toast.error('Sessão expirada. Faça login novamente.');
      return;
    }

    try {
      const response = await fetch(`${API_URL}/User`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        toast.success('Usuário atualizado com sucesso!');
        onSuccess();
      } else {
        toast.error('Não foi possível atualizar o usuário.');
      }
    } catch {
      toast.error(
        'Não foi possível conectar ao servidor. Tente novamente mais tarde.'
      );
    }
  };

  return {
    form,
    submitForm,
    isSubmitting
  };
}
