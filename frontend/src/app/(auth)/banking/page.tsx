'use client';

import { useEffect, useState } from 'react';
import { CalendarDays, ClipboardCheck, ClipboardList, MapPin, Users } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';
import { PageState } from '@/components/layout/PageState';
import { PageTitle } from '@/components/layout/PageTitle';
import { Button } from '@/components/ui/button';
import { CollapseCard } from '@/components/CollapseCard';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import {
  notifyBankingEvaluationsUpdated,
  usePendingInternalBankingEvaluations
} from '@/app/hooks/usePendingInternalBankingEvaluations';

interface DecodedToken {
  role: string | string[];
  userId: string;
}

interface OngoingTcc {
  tccId: number;
  studanteNames: string[];
  title?: string | null;
  advisorUserId?: number | null;
  step?: string | null;
  hasSchedule?: boolean;
}

export default function BankingPage() {
  const apiUrl = env('NEXT_PUBLIC_API_URL');
  const router = useRouter();
  const { evaluations, isLoading, refresh } = usePendingInternalBankingEvaluations();
  const [tccs, setTccs] = useState<OngoingTcc[]>([]);
  const [isLoadingTccs, setIsLoadingTccs] = useState(true);
  const [selectedTccId, setSelectedTccId] = useState<number | null>(null);
  const [areManagedBanksOpen, setAreManagedBanksOpen] = useState(false);
  const [arePendingEvaluationsOpen, setArePendingEvaluationsOpen] = useState(false);

  useEffect(() => {
    const loadTccs = async () => {
      const token = Cookies.get('token');
      if (!token) {
        setIsLoadingTccs(false);
        return;
      }

      try {
        const { role, userId } = jwtDecode<DecodedToken>(token);
        const roles = Array.isArray(role) ? role : [role];
        if (!roles.includes('ADVISOR')) {
          setTccs([]);
          return;
        }
        const query = `?UserId=${encodeURIComponent(userId)}&StatusTcc=IN_PROGRESS&StepTcc=PRESENTATION_AND_EVALUATION`;
        const response = await fetch(`${apiUrl}/Tcc/filter${query}`, {
          headers: { Authorization: `Bearer ${token}` },
          cache: 'no-store'
        });
        if (!response.ok) throw new Error('Falha ao carregar TCCs em andamento.');
        const listedTccs: OngoingTcc[] = await response.json();
        setTccs(listedTccs.filter((tcc) => Number(tcc.advisorUserId) === Number(userId)));
      } catch {
        toast.error('Erro ao carregar as bancas que você pode gerenciar.');
      } finally {
        setIsLoadingTccs(false);
      }
    };

    void loadTccs();
  }, [apiUrl]);

  return (
    <div className="space-y-6">
      <div>
        <PageTitle>Banca</PageTitle>
        <p className="mt-2 text-sm text-slate-600">
          Gerencie as bancas dos TCCs na etapa de apresentação e acompanhe suas avaliações pendentes. As assinaturas ficam na área Assinaturas.
        </p>
      </div>

      <CollapseCard
        title="Bancas dos meus TCCs"
        icon={<ClipboardCheck className="size-5" aria-hidden="true" />}
        indicatorNumber={tccs.length}
        isOpen={areManagedBanksOpen}
        onToggle={() => setAreManagedBanksOpen((open) => !open)}
      >
        <p className="mb-4 text-sm text-slate-600">Aqui aparecem somente os TCCs do orientador que chegaram à etapa de apresentação.</p>
        {isLoadingTccs ? (
          <PageState loading>Carregando TCCs em andamento...</PageState>
        ) : tccs.length === 0 ? (
          <PageState>Nenhum TCC está na etapa de cadastro da banca. Quando um trabalho chegar à etapa de apresentação, ele aparecerá aqui.</PageState>
        ) : (
          <div className="grid gap-4 md:grid-cols-2">
            {tccs.map((tcc) => (
              <article key={tcc.tccId} className="flex items-center justify-between gap-4 rounded-xl border border-[#DDD9CD] bg-white p-5 shadow-sm">
                <div>
                  <h3 className="font-semibold text-[#002B18]">{tcc.title || tcc.studanteNames?.join(', ') || `TCC ${tcc.tccId}`}</h3>
                  <p className="mt-1 text-sm text-slate-600">{tcc.studanteNames?.join(', ') || 'Estudante não informado'}</p>
                  <p className="mt-2 text-xs font-semibold uppercase tracking-wide text-[#006633]">
                    {tcc.hasSchedule ? 'Apresentação agendada' : 'Cadastro da banca pendente'}
                  </p>
                </div>
                <Button onClick={() => router.push(`/banking/manage?id=${tcc.tccId}`)}>
                  {tcc.hasSchedule ? 'Gerenciar banca' : 'Cadastrar banca'}
                </Button>
              </article>
            ))}
          </div>
        )}
      </CollapseCard>

      <CollapseCard
        title="Avaliações pendentes"
        icon={<ClipboardList className="size-5" aria-hidden="true" />}
        indicatorNumber={evaluations.length}
        indicatorColor="bg-red-600"
        isOpen={arePendingEvaluationsOpen}
        onToggle={() => setArePendingEvaluationsOpen((open) => !open)}
      >
        <p className="mb-4 text-sm text-slate-600">Avalie aqui as apresentações já concluídas.</p>
      {isLoading ? (
        <PageState loading>Carregando avaliações pendentes...</PageState>
      ) : evaluations.length === 0 ? (
        <PageState>Nenhuma avaliação de banca pendente.</PageState>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {evaluations.map((evaluation) => (
            <article key={evaluation.tccId} className="rounded-xl border border-[#DDD9CD] bg-white p-5 shadow-sm">
              <h2 className="text-lg font-semibold text-[#002B18]">
                {evaluation.tccTitle || `TCC ${evaluation.tccId}`}
              </h2>
              <p className="mt-2 flex items-start gap-2 text-sm text-slate-600">
                <Users className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
                {evaluation.studentNames.join(', ') || 'TCC sem aluno definido'}
              </p>
              <p className="mt-2 flex items-center gap-2 text-sm text-slate-600">
                <CalendarDays className="size-4 shrink-0" aria-hidden="true" />
                {new Date(evaluation.scheduledDate).toLocaleString('pt-BR')}
              </p>
              <p className="mt-2 flex items-center gap-2 text-sm text-slate-600">
                <MapPin className="size-4 shrink-0" aria-hidden="true" />
                {evaluation.location}
              </p>
              <Button className="mt-5" onClick={() => setSelectedTccId(evaluation.tccId)}>
                Avaliar TCC
              </Button>
            </article>
          ))}
        </div>
      )}
      </CollapseCard>

      <Dialog open={selectedTccId !== null} onOpenChange={(open) => !open && setSelectedTccId(null)}>
        <DialogContent className="max-h-[90vh] max-w-[95vw] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Avaliar apresentação</DialogTitle>
          </DialogHeader>
          {selectedTccId !== null && (
            <AvaliacaoForm
              internalTccId={selectedTccId}
              hideLogoAndMinHeight
              onSuccessCallback={() => {
                setSelectedTccId(null);
                notifyBankingEvaluationsUpdated();
                void refresh();
                toast.success('Avaliação registrada.');
              }}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
