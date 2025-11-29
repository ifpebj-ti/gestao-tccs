'use client';

import { useForm, SubmitHandler, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newTccSchema, NewTccSchemaType } from '@/app/schemas/newTccSchema';
import { toast } from 'react-toastify';
import { useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { useRouter } from 'next/navigation';
import { env } from 'next-runtime-env';

interface Course {
  id: number;
  name: string;
}

interface Advisor {
  id: number;
  name: string;
}

export function useNewTccForm() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const [advisors, setAdvisors] = useState<Advisor[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);

  const form = useForm<NewTccSchemaType>({
    resolver: zodResolver(newTccSchema),
    defaultValues: {
      students: [{ studentEmail: '', courseId: 0 }],
      advisorId: 0,
      title: null,
      summary: null
    }
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'students'
  });

  useEffect(() => {
    const token = Cookies.get('token');

    const fetchData = async <T>(
      endpoint: string,
      setData: (data: T) => void
    ) => {
      try {
        const res = await fetch(`${API_URL}/${endpoint}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (!res.ok) throw new Error(`Erro ao buscar ${endpoint}`);
        const data = (await res.json()) as T;
        setData(data);
      } catch {
        toast.error(`Erro ao carregar dados de ${endpoint.toLowerCase()}.`);
      }
    };

    fetchData<Advisor[]>('User/filter?Profile=ADVISOR', setAdvisors);
    fetchData<Course[]>('Campi/all/courses', setCourses);
  }, [API_URL]);

  const submitForm: SubmitHandler<NewTccSchemaType> = async (data) => {
    try {
      const payload = {
        students: data.students,
        title: data.title || null,
        summary: data.summary || null,
        advisorId: data.advisorId
      };

      const response = await fetch(`${API_URL}/Tcc`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${Cookies.get('token')}`
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        push('/ongoingTCCs');
        toast.success('Proposta de TCC enviada com sucesso!');
      } else {
        toast.error(
          'Erro ao enviar a proposta de TCC. Verifique os dados e tente novamente.'
        );
      }
    } catch {
      toast.error('Erro ao enviar a proposta de TCC.');
    }
  };

  return {
    form,
    submitForm,
    advisors,
    courses,
    fields,
    append,
    remove,
    isSubmitting: form.formState.isSubmitting
  };
}
