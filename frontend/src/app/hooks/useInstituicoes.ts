import { useState, useCallback } from 'react';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { toast } from 'react-toastify';

export interface CampiCourse {
  course: Course;
}

export interface Campi {
  id: number;
  name: string;
  courses: Course[];
  city?: string;
}

export interface Course {
  id: number;
  name: string;
  level?: string; // Incluindo level
}

export const useInstituicoes = () => {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [campis, setCampis] = useState<Campi[]>([]);
  const [loading, setLoading] = useState(false);

  const getHeaders = () => {
    const token = Cookies.get('token');
    return {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    };
  };

  const fetchCampis = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`${API_URL}/Campi/all`, {
        headers: getHeaders()
      });
      if (res.ok) {
        const data = await res.json();
        setCampis(data);
      } else {
        toast.error('Erro ao buscar as instituições.');
      }
    } catch {
      toast.error('Erro de conexão ao buscar instituições.');
    } finally {
      setLoading(false);
    }
  }, [API_URL]);

  const createCampi = async (name: string, city: string) => {
    try {
      const res = await fetch(`${API_URL}/Campi`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ name, city })
      });
      if (res.ok) {
        toast.success('Campus criado com sucesso!');
        await fetchCampis();
        return true;
      }
      toast.error('Erro ao criar Campus.');
      return false;
    } catch {
      toast.error('Erro de conexão ao criar Campus.');
      return false;
    }
  };

  const updateCampi = async (id: number, name: string, city: string) => {
    try {
      const res = await fetch(`${API_URL}/Campi/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify({ name, city })
      });
      if (res.ok) {
        toast.success('Campus atualizado com sucesso!');
        await fetchCampis();
        return true;
      }
      toast.error('Erro ao atualizar Campus.');
      return false;
    } catch {
      toast.error('Erro de conexão ao atualizar Campus.');
      return false;
    }
  };

  const deleteCampi = async (id: number) => {
    try {
      const res = await fetch(`${API_URL}/Campi/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      });
      if (res.ok) {
        toast.success('Campus removido com sucesso!');
        await fetchCampis();
        return true;
      }
      toast.error('Erro ao remover Campus.');
      return false;
    } catch {
      toast.error('Erro de conexão ao remover Campus.');
      return false;
    }
  };

  const createCourse = async (name: string, level: string, campiId: number) => {
    try {
      const res = await fetch(`${API_URL}/Course`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ name, level, campiId })
      });
      if (res.ok) {
        toast.success('Curso criado com sucesso!');
        await fetchCampis(); // Refresh to get the new course in the campi
        return true;
      }
      toast.error('Erro ao criar Curso.');
      return false;
    } catch {
      toast.error('Erro de conexão ao criar Curso.');
      return false;
    }
  };

  const updateCourse = async (id: number, name: string, level: string) => {
    try {
      const res = await fetch(`${API_URL}/Course/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify({ name, level })
      });
      if (res.ok) {
        toast.success('Curso atualizado com sucesso!');
        await fetchCampis();
        return true;
      }
      toast.error('Erro ao atualizar Curso.');
      return false;
    } catch {
      toast.error('Erro de conexão ao atualizar Curso.');
      return false;
    }
  };

  const deleteCourse = async (id: number) => {
    try {
      const res = await fetch(`${API_URL}/Course/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      });
      if (res.ok) {
        toast.success('Curso removido com sucesso!');
        await fetchCampis();
        return true;
      }
      toast.error('Erro ao remover Curso.');
      return false;
    } catch {
      toast.error('Erro de conexão ao remover Curso.');
      return false;
    }
  };

  return {
    campis,
    loading,
    fetchCampis,
    createCampi,
    updateCampi,
    deleteCampi,
    createCourse,
    updateCourse,
    deleteCourse
  };
};
