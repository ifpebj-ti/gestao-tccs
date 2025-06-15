'use client';

import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newTccSchema, NewTccSchemaType } from '@/app/schemas/newTccSchema';
import { toast } from 'react-toastify';
import { useEffect, useState } from 'react';
import Cookies from 'js-cookie';

export function useNewTccForm() {
  const [advisors, setAdvisors] = useState<{ id: number; name: string }[]>([]);

  const form = useForm<NewTccSchemaType>({
    resolver: zodResolver(newTccSchema),
    defaultValues: {
      studentEmails: [''],
      advisorId: 0,
      title: '',
      summary: '',
    },
  });

  useEffect(() => {
    const fetchAdvisors = async () => {
      const token = Cookies.get('token');
      try {
        const res = await fetch(
          process.env.NEXT_PUBLIC_API_URL + '/api/User/filter?Profile=ADVISOR',
          {
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          }
        );
        if (!res.ok) throw new Error('Erro ao buscar orientadores');
        const data = await res.json();
        setAdvisors(data);
      } catch (error) {
        console.error(error);
        toast.error('Erro ao carregar orientadores.');
      }
    };

    fetchAdvisors();
  }, []);

  const submitForm: SubmitHandler<NewTccSchemaType> = async (data) => {
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + '/api/Tcc', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${Cookies.get('token')}`,
        },
        body: JSON.stringify({
          studentEmails: data.studentEmails,
          title: data.title,
          summary: data.summary,
          advisorId: data.advisorId,
        }),
      });

      if (response.ok) {
        toast.success('Proposta de TCC enviada com sucesso!');
      } else {
        toast.error(`Erro na requisição: ${response.status} ${response.statusText}`);
      }
    } catch {
      toast.error('Erro ao enviar a proposta de TCC.');
    }
  };

  return { form, submitForm, advisors };
}
