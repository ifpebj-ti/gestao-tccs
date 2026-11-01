'use client';

import { SectionTitle } from '@/components/layout/SectionTitle';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UserMinus, AlertCircle } from 'lucide-react';
import { PendingAdvisorChange } from '@/app/hooks/useTccDetails';

interface Advisor {
  name: string;
  email: string;
}

interface AdvisorInfoSectionProps {
  advisor: Advisor;
  profile?: string | string[] | null;
  pendingAdvisorChange?: PendingAdvisorChange | null;
  onRequestAdvisorChange?: (type: 'STUDENT_REQUEST' | 'ADVISOR_DEPARTURE') => void;
  isTccActive?: boolean;
}

export function AdvisorInfoSection({
  advisor,
  profile,
  pendingAdvisorChange,
  onRequestAdvisorChange,
  isTccActive = true
}: AdvisorInfoSectionProps) {
  const isStudent =
    profile === 'STUDENT' ||
    (Array.isArray(profile) && profile.includes('STUDENT'));

  const isAdvisor =
    profile === 'ADVISOR' ||
    (Array.isArray(profile) && profile.includes('ADVISOR'));

  return (
    <section className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <SectionTitle>Informações do orientador</SectionTitle>

        {!pendingAdvisorChange && isTccActive && (
          <div className="flex gap-2">
            {isStudent && onRequestAdvisorChange && (
              <Button
                variant="outline"
                size="sm"
                className="text-amber-700 border-amber-300 hover:bg-amber-50"
                onClick={() => onRequestAdvisorChange('STUDENT_REQUEST')}
              >
                <UserMinus className="w-4 h-4 mr-2" />
                Solicitar Substituição
              </Button>
            )}

            {isAdvisor && onRequestAdvisorChange && (
              <Button
                variant="outline"
                size="sm"
                className="text-amber-700 border-amber-300 hover:bg-amber-50"
                onClick={() => onRequestAdvisorChange('ADVISOR_DEPARTURE')}
              >
                <UserMinus className="w-4 h-4 mr-2" />
                Solicitar Afastamento
              </Button>
            )}
          </div>
        )}
      </div>

      <div className="grid md:grid-cols-2 gap-4">
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Nome do orientador</Label>
          <Input value={advisor.name} readOnly />
        </div>
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Email do orientador</Label>
          <Input value={advisor.email} readOnly />
        </div>
      </div>

      {pendingAdvisorChange && (
        <div className="p-4 bg-amber-50 border-l-4 border-amber-500 rounded-md text-amber-900 space-y-2 mt-4">
          <div className="flex items-center gap-2">
            <AlertCircle className="w-5 h-5 text-amber-600 shrink-0" />
            <h4 className="font-bold text-sm sm:text-base">
              {pendingAdvisorChange.requesterRole === 'STUDENT'
                ? 'Solicitação de Substituição de Orientador em Andamento (Anexo IX)'
                : 'Solicitação de Afastamento de Orientador em Andamento (Anexo V)'}
            </h4>
          </div>

          <div className="text-xs sm:text-sm space-y-1 pl-7">
            <p>
              <strong>Status:</strong>{' '}
              <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-amber-200 text-amber-800">
                {pendingAdvisorChange.status === 'PENDING_SIGNATURES'
                  ? 'Aguardando Assinaturas das Partes'
                  : pendingAdvisorChange.status === 'PENDING_COORDINATION'
                  ? 'Aguardando Análise da Coordenação'
                  : pendingAdvisorChange.status}
              </span>
            </p>
            <p>
              <strong>Solicitante:</strong> {pendingAdvisorChange.requesterName} (
              {pendingAdvisorChange.requesterRole === 'STUDENT'
                ? 'Estudante'
                : 'Orientador'}
              )
            </p>
            {pendingAdvisorChange.suggestedAdvisorName && (
              <p>
                <strong>Orientador sugerido/substituto:</strong>{' '}
                {pendingAdvisorChange.suggestedAdvisorName}
              </p>
            )}
            <p>
              <strong>Justificativa apresentada:</strong>
            </p>
            <blockquote className="border-l-2 border-amber-400 pl-3 italic text-xs text-amber-800">
              {pendingAdvisorChange.reason}
            </blockquote>
          </div>
        </div>
      )}
    </section>
  );
}
