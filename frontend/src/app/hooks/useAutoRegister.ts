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
import { useState, useEffect } from 'react';

interface Course {
  id: number;
  name: string;
}

interface CampusWithCourses {
  id: number;
  name: string;
  courses: Course[];
}

export function useAutoRegister() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const [campusData, setCampusData] = useState<CampusWithCourses[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);

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
      campusId: undefined,
      courseId: undefined,
      password: '',
      confirmPassword: ''
    }
  });

  const {
    formState: { isSubmitting },
    reset,
    watch,
    setValue
  } = form;

  const watchedCampusId = watch('campusId');

  useEffect(() => {
    const fetchCampusData = async () => {
      try {
        const response = await fetch(`${API_URL}/Campi/public/all`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json'
          }
        });

        if (response.ok) {
          const data = await response.json();
          setCampusData(data);
        } else {
          toast.error('Não foi possível carregar os dados de campi e cursos.');
        }
      } catch {
        toast.error('Erro de conexão ao buscar dados de campi e cursos.');
      }
    };
    fetchCampusData();
  }, [API_URL]);

  useEffect(() => {
    setValue('courseId', 0);

    if (watchedCampusId && watchedCampusId > 0) {
      const selectedCampus = campusData.find(
        (campus) => campus.id === Number(watchedCampusId)
      );
      setCourses(selectedCampus ? selectedCampus.courses : []);
    } else {
      setCourses([]);
    }
  }, [watchedCampusId, campusData, setValue]);

  const submitForm: SubmitHandler<AutoRegisterSchemaType> = async (data) => {
    try {
      const payload = {
        name: data.name,
        email: data.email,
        registration: data.registration,
        cpf: data.cpf,
        phone: data.phone,
        userClass: data.userClass,
        shift: Number(data.shift),
        campiId: Number(data.campusId),
        courseId: Number(data.courseId),
        password: data.password
      };

      // Passo 1: Autocadastro do estudante com senha
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

      // Passo 2: Login automático
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
        Cookies.remove('access_token_temp');

        toast.success('Cadastro e senha configurados com sucesso! Bem-vindo(a).');
        reset();
        push('/homePage');
      } else {
        // Se falhar apenas o login automático, redireciona para a tela de login
        if (typeof window !== 'undefined') {
          sessionStorage.removeItem('first_access_email');
          sessionStorage.removeItem('first_access_code');
        }
        Cookies.remove('access_token_temp');
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
    isSubmitting,
    campus: campusData,
    courses
  };
}
