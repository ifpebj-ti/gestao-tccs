'use client';
import TccTabs from '@/components/TccTabs';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Suspense, useEffect, useState } from 'react';
import Cookies from 'js-cookie';
import { toast } from 'react-toastify';
import React from 'react';
import { Button } from '@/components/ui/button';
import { useSearchParams } from 'next/navigation';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';

export default function ReusableTccDetailsPage() {
  interface InfoTcc {
    title: string;
    summary: string;
    presentationDate: string;
    presentationTime: string;
    presentationLocation: string;
  }

  interface InfoStudent {
    name: string;
    registration: string;
    cpf: string;
    course: string;
    email: string;
  }

  interface InfoAdvisor {
    name: string;
    email: string;
  }

  interface InfoBanking {
    nameInternal: string;
    emailInternal: string;
    nameExternal: string;
    emailExternal: string;
  }

  interface TccDetailsResponse {
    infoTcc: InfoTcc;
    infoStudent: InfoStudent[];
    infoAdvisor: InfoAdvisor;
    infoBanking: InfoBanking;
    cancellationRequest: boolean;
  }

  const formatStudentName = (fullName: string) => {
    const [first, second] = fullName.trim().split(' ');
    return `${first ?? ''} ${second ?? ''}`;
  };

  const [tccData, setTccData] = useState<TccDetailsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const searchParams = useSearchParams();
  const tccId = Number(searchParams.get('id'));

  useEffect(() => {
    const fetchTccDetails = async () => {
      const token = Cookies.get('token');
      if (!token) {
        toast.error('Token de autenticação não encontrado.');
        return;
      }

      try {
        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/Tcc?tccId=${tccId}`,
          {
            headers: {
              Authorization: `Bearer ${token}`
            }
          }
        );

        if (!res.ok) throw new Error('Erro ao buscar dados do TCC.');

        const result = await res.json();
        setTccData(result);
      } catch {
        toast.error('Erro ao carregar dados do TCC.');
      } finally {
        setLoading(false);
      }
    };

    if (tccId) fetchTccDetails();
  }, [tccId]);

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <Suspense fallback={null}>
        <TccTabs />
      </Suspense>

      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 pb-10 truncate max-w-full">
        TCC -{' '}
        <span
          title={
            tccData?.infoStudent?.map((s) => s.name).join(', ') ||
            'Carregando...'
          }
        >
          {tccData?.infoStudent?.length
            ? tccData.infoStudent
                .map((s) => formatStudentName(s.name))
                .join(', ')
            : 'Aguardando cadastro do estudante'}
        </span>
      </h1>

      {!loading && tccData && (
        <div className="flex flex-col gap-5">
          <h2 className="text-lg font-extrabold uppercase">
            Informações do TCC
          </h2>
          <div className="grid md:grid-cols-2 gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Título da proposta</Label>
              <Input value={tccData.infoTcc.title} readOnly />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Resumo da proposta</Label>
              <Input value={tccData.infoTcc.summary} readOnly />
            </div>
            {tccData.infoTcc.presentationDate && (
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold">Data da apresentação</Label>
                <Input value={tccData.infoTcc.presentationDate} />
              </div>
            )}
            {tccData.infoTcc.presentationTime && (
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold">Hora da apresentação</Label>
                <Input value={tccData.infoTcc.presentationTime} />
              </div>
            )}
            {tccData.infoTcc.presentationLocation && (
              <div className="grid items-center gap-1.5 md:col-span-2">
                <Label className="font-semibold">Local da apresentação</Label>
                <Input value={tccData.infoTcc.presentationLocation} />
              </div>
            )}
          </div>

          <h2 className="text-lg font-extrabold uppercase mt-6">
            Informações do(s) estudante(s)
          </h2>
          {tccData.infoStudent.length > 0 ? (
            <div className="grid md:grid-cols-2 gap-4">
              {tccData.infoStudent.map((student, i) => (
                <React.Fragment key={i}>
                  <div className="grid items-center gap-1.5">
                    <Label className="font-semibold">Nome do estudante</Label>
                    <Input value={student.name} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label className="font-semibold">Matrícula</Label>
                    <Input value={student.registration} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label className="font-semibold">CPF</Label>
                    <Input value={student.cpf} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label className="font-semibold">Curso</Label>
                    <Input value={student.course} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label className="font-semibold">Email</Label>
                    <Input value={student.email} readOnly />
                  </div>
                </React.Fragment>
              ))}
            </div>
          ) : (
            <p className="text-gray-600 italic">
              Estudante ainda não se cadastrou na plataforma.
            </p>
          )}

          {tccData.infoAdvisor.name && tccData.infoAdvisor.email && (
            <>
              <h2 className="text-lg font-extrabold uppercase mt-6">
                Informações do orientador
              </h2>
              <div className="grid md:grid-cols-2 gap-4">
                <div className="grid items-center gap-1.5">
                  <Label className="font-semibold">Nome do orientador</Label>
                  <Input value={tccData.infoAdvisor.name} readOnly />
                </div>
                <div className="grid items-center gap-1.5">
                  <Label className="font-semibold">Email do orientador</Label>
                  <Input value={tccData.infoAdvisor.email} readOnly />
                </div>
              </div>
            </>
          )}

          {tccData.infoBanking.nameInternal && (
            <>
              <h2 className="text-lg font-extrabold uppercase mt-6">
                Informações da Banca
              </h2>
              <div className="grid md:grid-cols-2 gap-4">
                <div className="grid items-center gap-1.5">
                  <Label className="font-semibold">Membro Interno</Label>
                  <Input value={tccData.infoBanking.nameInternal} readOnly />
                </div>
                <div className="grid items-center gap-1.5">
                  <Label className="font-semibold">Email Interno</Label>
                  <Input value={tccData.infoBanking.emailInternal} readOnly />
                </div>
                <div className="grid items-center gap-1.5">
                  <Label className="font-semibold">Membro Externo</Label>
                  <Input value={tccData.infoBanking.nameExternal} readOnly />
                </div>
                <div className="grid items-center gap-1.5">
                  <Label className="font-semibold">Email Externo</Label>
                  <Input value={tccData.infoBanking.emailExternal} readOnly />
                </div>
              </div>
            </>
          )}

          <Button
            className="md:w-fit w-full mt-6 md:self-end"
            variant="destructive"
          >
            Solicitar cancelamento de proposta
          </Button>
        </div>
      )}
    </div>
  );
}
