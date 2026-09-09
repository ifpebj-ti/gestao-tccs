'use client';

import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  autoRegisterSchema,
  AutoRegisterSchemaType
} from '@/app/schemas/autoRegisterSchema';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';
import { env } from 'next-runtime-env';

import Cookies from 'js-cookie';

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
      phone: '',
      userClass: '',
      shift: undefined,
      password: '',
      confirmPassword: ''
    }
  });

  const {
    formState: { isSubmitting },
    reset
  } = form;

  const submitForm: SubmitHandler<AutoRegisterSchemaType> = async (data) => {
    try {
      const payload = {
        name: data.name,
        email: data.email,
        registration: data.registration,
        cpf: data.cpf,
        phone: data.phone,
        userClass: data.userClass,
        shift: Number(data.shift)
      };

      // Passo 1: Autocadastro do estudante
      const autoRegisterResponse = await fetch(`${API_URL}/User/autoregister`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (!autoRegisterResponse.ok) {
        const errorData = await autoRegisterResponse.json();
        const errorMessage =
          errorData.message ||
          errorData.errors?.message ||
          'Erro ao realizar cadastro. Verifique os dados e tente novamente.';
        toast.error(errorMessage);
        return;
      }

      // Passo 2: Definição da senha
      const inviteCode =
        typeof window !== 'undefined'
          ? sessionStorage.getItem('first_access_code') || ''
          : '';

      const passwordResponse = await fetch(`${API_URL}/Auth/new-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: data.email,
          password: data.password,
          inviteCode: inviteCode
        })
      });

      if (!passwordResponse.ok) {
        const passwordError = await passwordResponse.json();
        toast.error(
          passwordError.message ||
            'Cadastro realizado, mas ocorreu um erro ao definir sua senha. Você será redirecionado para a tela de definição de senha.'
        );
        push('/newPassword');
        return;
      }

      // Passo 3: Login automático
      const loginResponse = await fetch(`${API_URL}/Auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: data.email,
          password: data.password
        })
      });

      if (loginResponse.ok) {
        const result = await loginResponse.json();
        const token = result.accessToken;

        if (token) {
          Cookies.set('token', token, { expires: 1 });
        }

        if (typeof window !== 'undefined') {
          sessionStorage.removeItem('first_access_email');
          sessionStorage.removeItem('first_access_code');
        }

        toast.success('Cadastro e senha configurados com sucesso! Bem-vindo(a).');
        reset();
        push('/homePage');
      } else {
        // Se falhar apenas o login automático, redireciona para a tela de login
        if (typeof window !== 'undefined') {
          sessionStorage.removeItem('first_access_email');
          sessionStorage.removeItem('first_access_code');
        }
        toast.success('Cadastro finalizado com sucesso! Faça login para continuar.');
        reset();
        push('/');
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
