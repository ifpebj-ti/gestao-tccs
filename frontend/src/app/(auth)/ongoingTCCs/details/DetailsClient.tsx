'use client';

import { Suspense } from 'react';
import { useTccCancellation } from '@/app/hooks/useTccCancellation';
import { FormProvider } from 'react-hook-form';
import React from 'react';
import TccTabs from '@/components/TccTabs';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';

export default function DetailsClient() {
  const {
    tccData,
    cancellationDetails,
    loading,
    profile,
    isModalOpen,
    setIsModalOpen,
    cancellationForm,
    handleRequestCancellation,
    handleApproveCancellation
  } = useTccCancellation();

  const formatStudentName = (fullName: string) => {
    const [first, second] = fullName.trim().split(' ');
    return `${first ?? ''} ${second ?? ''}`;
  };

  if (loading) {
    return <div className="p-4">Carregando...</div>;
  }

  if (!tccData) {
    return <div className="p-4">TCC não encontrado.</div>;
  }

  return (
    <FormProvider {...cancellationForm}>
      <div className="flex flex-col">
        <BreadcrumbAuto />
        <Suspense fallback={null}>
          <TccTabs />
        </Suspense>

        <div className="flex flex-col md:flex-row md:items-center md:gap-4">
          <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 pb-2 truncate max-w-full">
            TCC -{' '}
            <span
              title={
                tccData.infoStudent?.map((s) => s.name).join(', ') ||
                'Carregando...'
              }
            >
              {tccData.infoStudent?.length
                ? tccData.infoStudent
                    .map((s) => formatStudentName(s.name))
                    .join(', ')
                : 'Aguardando cadastro do estudante'}
            </span>
          </h1>
          {tccData.cancellationRequest && (
            <Badge variant="destructive" className="text-sm w-fit mt-2 md:mt-0">
              Cancelamento solicitado
            </Badge>
          )}
        </div>

        <div className="flex flex-col gap-5 mt-10">
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
                <Input
                  value={tccData.infoTcc.presentationDate}
                  readOnly={profile === 'STUDENT'}
                />
              </div>
            )}
            {tccData.infoTcc.presentationTime && (
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold">Hora da apresentação</Label>
                <Input
                  value={tccData.infoTcc.presentationTime}
                  readOnly={profile === 'STUDENT'}
                />
              </div>
            )}
            {tccData.infoTcc.presentationLocation && (
              <div className="grid items-center gap-1.5 md:col-span-2">
                <Label className="font-semibold">Local da apresentação</Label>
                <Input
                  value={tccData.infoTcc.presentationLocation}
                  readOnly={profile === 'STUDENT'}
                />
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
                  <div className="grid items-center gap-1.5 md:col-span-2">
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
        </div>

        <div className="flex flex-col md:flex-row justify-end gap-4 mt-6">
          {tccData.cancellationRequest &&
            ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR'].includes(
              profile ?? ''
            ) && (
              <div className="flex-1 p-4 bg-yellow-100 border-l-4 border-yellow-500 text-yellow-700 rounded-md">
                <p className="font-bold text-lg md:text-regular">
                  Solicitação de Cancelamento Pendente
                </p>

                {cancellationDetails ? (
                  <div className="mt-2 text-sm">
                    <p>
                      <strong>Motivo apresentado:</strong>
                    </p>
                    <blockquote className="mt-1 border-l-4 border-yellow-600 pl-4 italic">
                      {cancellationDetails.reasonCancellation}
                    </blockquote>
                  </div>
                ) : (
                  <p className="text-sm italic mt-2">Carregando motivo...</p>
                )}

                <div className="flex gap-2 mt-4">
                  <Button
                    size="default"
                    onClick={handleApproveCancellation}
                    className="w-full md:w-fit"
                  >
                    Aprovar Cancelamento
                  </Button>
                </div>
              </div>
            )}

          {profile === 'STUDENT' && !tccData.cancellationRequest && (
            <Button
              className="md:w-fit w-full"
              variant="destructive"
              onClick={() => setIsModalOpen(true)}
            >
              Solicitar cancelamento de proposta
            </Button>
          )}

          {profile === 'STUDENT' && tccData.cancellationRequest && (
            <p className="text-right text-yellow-600 font-semibold italic">
              Sua solicitação de cancelamento está em análise.
            </p>
          )}
        </div>
      </div>

      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent>
          <form
            onSubmit={cancellationForm.handleSubmit(handleRequestCancellation)}
          >
            <DialogHeader>
              <DialogTitle>Solicitar Cancelamento do TCC</DialogTitle>
              <DialogDescription>
                Descreva o motivo pelo qual você está solicitando o
                cancelamento.
              </DialogDescription>
            </DialogHeader>
            <div className="grid gap-4 py-4">
              <Label htmlFor="reason">Motivo</Label>
              <Textarea
                id="reason"
                {...cancellationForm.register('reason')}
                placeholder="Explique detalhadamente o motivo aqui..."
                className="min-h-[100px]"
              />
              {cancellationForm.formState.errors.reason && (
                <p className="text-sm text-red-600">
                  {cancellationForm.formState.errors.reason.message}
                </p>
              )}
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="ghost"
                onClick={() => setIsModalOpen(false)}
              >
                Fechar
              </Button>
              <Button
                type="submit"
                variant="destructive"
                disabled={cancellationForm.formState.isSubmitting}
              >
                {cancellationForm.formState.isSubmitting
                  ? 'Enviando...'
                  : 'Enviar Solicitação'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </FormProvider>
  );
}
