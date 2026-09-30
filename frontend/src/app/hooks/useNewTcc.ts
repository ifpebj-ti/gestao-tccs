'use client';

import { useForm, SubmitHandler, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newTccSchema, NewTccSchemaType } from '@/app/schemas/newTccSchema';
import { toast } from 'react-toastify';
import { useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { useRouter, useSearchParams } from 'next/navigation';
import { env } from 'next-runtime-env';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  userId: string;
  role: string | string[];
}

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
  const searchParams = useSearchParams();
  const reformulateId = searchParams.get('reformulate');

  const [advisors, setAdvisors] = useState<Advisor[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [isStudent, setIsStudent] = useState(false);

  const form = useForm<NewTccSchemaType>({
    resolver: zodResolver(newTccSchema),
    defaultValues: {
      students: [{ studentEmail: '', courseId: 0 }],
      advisorId: 0,
      title: '',
      summary: ''
    }
  });

  const { setValue } = form;

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'students'
  });

  useEffect(() => {
    const token = Cookies.get('token');
    if (token) {
      const decoded = jwtDecode<DecodedToken>(token);
      const studentRole = Array.isArray(decoded.role)
        ? decoded.role.includes('STUDENT')
        : decoded.role === 'STUDENT';
      setIsStudent(studentRole);
    }

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

    // Se estiver reformulando, busca dados do TCC recusado
    if (reformulateId && token) {
      fetch(`${API_URL}/Tcc?tccId=${reformulateId}`, {
        headers: { Authorization: `Bearer ${token}` }
      })
        .then((res) => (res.ok ? res.json() : null))
        .then((data) => {
          if (data?.infoTcc) {
            setValue('title', data.infoTcc.title || '');
            setValue('summary', data.infoTcc.summary || '');
          }
        })
        .catch(() => {
          toast.error('Erro ao carregar dados da proposta para reformulação.');
        });
    }
  }, [API_URL, reformulateId, setValue]);

  const submitForm: SubmitHandler<NewTccSchemaType> = async (data) => {
    const token = Cookies.get('token');
    try {
      if (isStudent) {
        if (reformulateId) {
          // Reformulação da proposta recusada
          const response = await fetch(
            `${API_URL}/Tcc/${reformulateId}/reformulate-proposal`,
            {
              method: 'PATCH',
              headers: {
                'Content-Type': 'application/json',
                Authorization: `Bearer ${token}`
              },
              body: JSON.stringify({
                title: data.title,
                summary: data.summary,
                advisorId: data.advisorId > 0 ? data.advisorId : null
              })
            }
          );

          if (response.ok) {
            toast.success('Proposta reformulada e enviada com sucesso!');
            push('/homePage');
          } else {
            const err = await response.json();
            toast.error(
              err.message || 'Erro ao reformular proposta. Tente novamente.'
            );
          }
        } else {
          // Submissão inicial da proposta pelo discente
          const response = await fetch(`${API_URL}/Tcc/proposal`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({
              title: data.title,
              summary: data.summary,
              advisorId: data.advisorId
            })
          });

          if (response.ok) {
            toast.success('Proposta de TCC enviada ao orientador com sucesso!');
            push('/homePage');
          } else {
            const err = await response.json();
            toast.error(
              err.message ||
                'Erro ao enviar proposta de TCC. Verifique os dados e tente novamente.'
            );
          }
        }
      } else {
        // Fluxo existente para Docente / Coordenador
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
            Authorization: `Bearer ${token}`
          },
          body: JSON.stringify(payload)
        });

        if (response.ok) {
          push('/ongoingTCCs');
          toast.success('Proposta de TCC criada com sucesso!');
        } else {
          toast.error(
            'Erro ao criar o TCC. Verifique os dados e tente novamente.'
          );
        }
      }
    } catch {
      toast.error('Erro de conexão ao enviar a proposta.');
    }
  };

  return {
    form,
    register: form.register,
    control: form.control,
    errors: form.formState.errors,
    onSubmit: form.handleSubmit(submitForm),
    submitForm,
    advisors,
    courses,
    fields,
    append,
    remove,
    isStudent,
    isReformulating: !!reformulateId,
    isSubmitting: form.formState.isSubmitting
  };
}
