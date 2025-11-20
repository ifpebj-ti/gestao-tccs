'use client';

import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  newUserSchema,
  NewUserSchemaSchemaType
} from '@/app/schemas/newUserSchema';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { useEffect, useState } from 'react';

interface Course {
  id: number;
  name: string;
}

interface CampusWithCourses {
  id: number;
  name: string;
  courses: Course[];
}

export function useNewUserForm() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const token = Cookies.get('token');

  const [campusData, setCampusData] = useState<CampusWithCourses[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);

  const defaultValues: Partial<NewUserSchemaSchemaType> = {
    name: '',
    email: '',
    registration: '',
    cpf: '',
    phone: '',
    userClass: '',
    shift: undefined,
    titration: '',
    profile: undefined,
    siape: '',
    campusId: undefined,
    courseId: undefined
  };

  const {
    handleSubmit,
    register,
    formState: { errors, isSubmitting },
    reset,
    watch,
    setValue
  } = useForm<NewUserSchemaSchemaType>({
    resolver: zodResolver(newUserSchema),
    defaultValues: defaultValues
  });

  const watchedCampusId = watch('campusId');

  // Busca de Campi e Cursos
  useEffect(() => {
    const fetchCampusData = async () => {
      try {
        const response = await fetch(`${API_URL}/Campi/all`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`
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
  }, [API_URL, token]);

  // Lógica de cascata Campus -> Cursos
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

  const submitForm: SubmitHandler<NewUserSchemaSchemaType> = async (data) => {
    try {
      const payload = {
        name: data.name,
        email: data.email,
        registration: data.registration || '',
        cpf: data.cpf,
        siape: data.siape || '',
        profile: [data.profile],
        phone: data.phone,
        userClass: data.userClass || '',
        shift: data.shift ? Number(data.shift) : 0,
        titration: data.titration || '',
        courseId: Number(data.courseId),
        campusId: Number(data.campusId)
      };

      const response = await fetch(`${API_URL}/User`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        toast.success('Usuário registrado com sucesso!');
        reset();
        push('/homePage');
      } else {
        const errorData = await response.json().catch(() => null);
        if (errorData && errorData.errors) {
          const firstError = Object.values(errorData.errors)[0];
          toast.error(`Erro: ${firstError}`);
        } else {
          toast.error(
            'Erro ao registrar usuário. Verifique os dados e tente novamente.'
          );
        }
      }
    } catch {
      toast.error('Erro ao registrar usuário. Tente novamente mais tarde.');
    }
  };

  return {
    register,
    submitForm,
    handleSubmit,
    errors,
    isSubmitting,
    watch,
    campus: campusData,
    courses,
    setValue
  };
}
