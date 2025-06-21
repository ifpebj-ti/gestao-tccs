'use client';
import TccTabs from '@/components/TccTabs';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Suspense, useEffect } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import React from 'react';
import { Button } from '@/components/ui/button';

export default function TCCDetails() {
  interface TCCFromApi {
    tccId: number;
    studanteNames: string[];
  }
  const [tccs, setTccs] = React.useState<TCCFromApi[]>([]);

  useEffect(() => {
    const fetchTccs = async () => {
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          return;
        }

        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/Tcc/filter?filter=IN_PROGRESS`,
          {
            headers: {
              Authorization: `Bearer ${token}`
            }
          }
        );

        if (!res.ok) {
          throw new Error('Erro ao buscar TCCs');
        }

        const data: TCCFromApi[] = await res.json();
        setTccs(data);
      } catch {
        toast.error('Erro ao carregar TCCs em andamento.');
      }
    };

    fetchTccs();
  }, []);

  return (
    <div className="flex flex-col">
      <Suspense fallback={null}>
        <TccTabs />
      </Suspense>{' '}
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        TCC -{' '}
        {tccs.length > 0 ? tccs[0].studanteNames.join(', ') : 'Carregando...'}
      </h1>
      <div className="flex flex-col gap-5">
        <h2 className="text-lg font-extrabold uppercase">Informações do TCC</h2>
        <div className="grid md:grid-cols-2 gap-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="name">
              Título da proposta
            </Label>
            <Input
              id="name"
              type="text"
              placeholder="Digite o nome do usuário"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="name">
              Resumo da proposta
            </Label>
            <Input
              id="name"
              type="text"
              placeholder="Digite o nome do usuário"
            />
          </div>
        </div>
        <h2 className="text-lg font-extrabold uppercase">
          Informações do(s) estudante(s)
        </h2>
        <div className="grid md:grid-cols-2 gap-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="name">
              Nome do estudante
            </Label>
            <Input
              id="name"
              type="text"
              placeholder="Digite o nome do estudante"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="matricula">
              Matrícula
            </Label>
            <Input
              id="matricula"
              type="text"
              placeholder="Digite a matrícula do estudante"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="cpf">
              CPF
            </Label>
            <Input
              id="cpf"
              type="text"
              placeholder="Digite o CPF do estudante"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="curso">
              Curso
            </Label>
            <Input
              id="curso"
              type="text"
              placeholder="Digite o curso do estudante"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="email">
              Email
            </Label>
            <Input
              id="email"
              type="email"
              placeholder="Digite o email do estudante"
            />
          </div>
        </div>
        <h2 className="text-lg font-extrabold uppercase">
          Informações do orientador
        </h2>
        <div className="grid md:grid-cols-2 gap-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="orientadorNome">
              Nome do orientador
            </Label>
            <Input
              id="orientadorNome"
              type="text"
              placeholder="Digite o nome do orientador"
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="orientadorEmail">
              Email do orientador
            </Label>
            <Input
              id="orientadorEmail"
              type="email"
              placeholder="Digite o email do orientador"
            />
          </div>
        </div>
      </div>
      <Button
        className="md:w-fit w-full mt-6 md:self-end"
        variant="destructive"
      >
        Solicitar cancelamento de proposta
      </Button>
    </div>
  );
}
