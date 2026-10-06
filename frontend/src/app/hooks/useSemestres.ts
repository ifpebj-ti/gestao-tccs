import { useState, useCallback } from 'react';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { toast } from 'react-toastify';

export interface Semester {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export const useSemestres = () => {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [semesters, setSemesters] = useState<Semester[]>([]);
  const [loading, setLoading] = useState(false);

  const getHeaders = () => {
    const token = Cookies.get('token');
    return {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    };
  };

  const fetchSemesters = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`${API_URL}/Semester/all`, {
        headers: getHeaders()
      });
      if (res.ok) {
        const data = await res.json();
        setSemesters(data);
      } else {
        toast.error('Erro ao buscar os semestres.');
      }
    } catch {
      toast.error('Erro de conexão ao buscar semestres.');
    } finally {
      setLoading(false);
    }
  }, [API_URL]);

  const createSemester = async (name: string, startDate: string, endDate: string, isActive: boolean) => {
    try {
      const res = await fetch(`${API_URL}/Semester`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ name, startDate, endDate, isActive })
      });
      if (res.ok) {
        toast.success('Semestre criado com sucesso!');
        await fetchSemesters();
        return true;
      }
      toast.error('Erro ao criar Semestre.');
      return false;
    } catch {
      toast.error('Erro de conexão ao criar Semestre.');
      return false;
    }
  };

  const updateSemester = async (id: number, name: string, startDate: string, endDate: string, isActive: boolean) => {
    try {
      const res = await fetch(`${API_URL}/Semester/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify({ name, startDate, endDate, isActive })
      });
      if (res.ok) {
        toast.success('Semestre atualizado com sucesso!');
        await fetchSemesters();
        return true;
      }
      toast.error('Erro ao atualizar Semestre.');
      return false;
    } catch {
      toast.error('Erro de conexão ao atualizar Semestre.');
      return false;
    }
  };

  const deleteSemester = async (id: number) => {
    try {
      const res = await fetch(`${API_URL}/Semester/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      });
      if (res.ok) {
        toast.success('Semestre removido com sucesso!');
        await fetchSemesters();
        return true;
      }
      toast.error('Erro ao remover Semestre.');
      return false;
    } catch {
      toast.error('Erro de conexão ao remover Semestre.');
      return false;
    }
  };

  return {
    semesters,
    loading,
    fetchSemesters,
    createSemester,
    updateSemester,
    deleteSemester
  };
};
