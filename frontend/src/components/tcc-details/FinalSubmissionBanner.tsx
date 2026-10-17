import React from 'react';
import { Clock, AlertTriangle, CheckCircle2, BookOpen } from 'lucide-react';

interface FinalSubmissionBannerProps {
  step?: string | null;
  finalSubmissionDeadline?: string | null;
  hardCoverReceivedAt?: string | null;
  forwardedToLibraryAt?: string | null;
  libraryReceivedAt?: string | null;
  isCompleted?: boolean;
}

export function FinalSubmissionBanner({
  step,
  finalSubmissionDeadline,
  hardCoverReceivedAt,
  forwardedToLibraryAt,
  libraryReceivedAt,
  isCompleted
}: FinalSubmissionBannerProps) {
  // Exibe se estiver na etapa 6 ou se houver prazo/dados de finalização
  const isFinalStage =
    step === 'FINALIZATION_AND_PUBLICATION' ||
    step === '6' ||
    Boolean(finalSubmissionDeadline);

  if (!isFinalStage && !isCompleted) {
    return null;
  }

  const formatDate = (dateStr?: string | null) => {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return null;
    return date.toLocaleDateString('pt-BR');
  };

  const deadlineFormatted = formatDate(finalSubmissionDeadline);

  // Cálculo de dias restantes
  let remainingDays: number | null = null;
  let isExpired = false;

  if (finalSubmissionDeadline) {
    const deadline = new Date(finalSubmissionDeadline);
    const now = new Date();
    // Zerar horas para comparar apenas os dias
    deadline.setHours(23, 59, 59, 999);
    const diffTime = deadline.getTime() - now.getTime();
    remainingDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    if (remainingDays < 0) {
      isExpired = true;
    }
  }

  const isHardCoverDelivered = Boolean(hardCoverReceivedAt);
  const isForwardedToLibrary = Boolean(forwardedToLibraryAt);
  const isLibraryConfirmed = Boolean(libraryReceivedAt) || isCompleted;

  return (
    <div className="w-full rounded-xl border border-[#006633]/30 bg-gradient-to-br from-[#F4FAF4] to-[#E8F5E9] p-6 shadow-sm">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-[#002B18] text-[#C8FF47]">
            <BookOpen size={20} />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="rounded-full bg-[#002B18] px-2.5 py-0.5 text-xs font-semibold uppercase tracking-wider text-[#C8FF47]">
                Etapa Final
              </span>
              <span className="text-xs font-bold text-[#006633]">
                TCC Aprovado na Defesa
              </span>
            </div>
            <h3 className="mt-1 text-lg font-bold text-[#002B18]">
              Entrega da Versão Final e Envio à Biblioteca
            </h3>
          </div>
        </div>

        {/* Badge do Prazo de 30 dias */}
        {deadlineFormatted && !isHardCoverDelivered && (
          <div
            className={`flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold shadow-sm ${
              isExpired
                ? 'bg-red-50 border border-red-200 text-red-700'
                : remainingDays !== null && remainingDays <= 5
                ? 'bg-amber-50 border border-amber-200 text-amber-800'
                : 'bg-white border border-[#006633]/30 text-[#002B18]'
            }`}
          >
            {isExpired ? (
              <AlertTriangle className="size-4 shrink-0 text-red-600" />
            ) : (
              <Clock className="size-4 shrink-0 text-[#006633]" />
            )}
            <div>
              <div className="text-xs font-normal text-gray-500">
                Prazo máx. de 30 dias:
              </div>
              <div>
                {isExpired
                  ? `Expirou em ${deadlineFormatted}`
                  : remainingDays === 0
                  ? `Hoje é o prazo final (${deadlineFormatted})`
                  : `Faltam ${remainingDays} dias (${deadlineFormatted})`}
              </div>
            </div>
          </div>
        )}

        {isHardCoverDelivered && (
          <div className="flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-semibold text-emerald-800 shadow-sm">
            <CheckCircle2 className="size-4 shrink-0 text-emerald-600" />
            <div>
              <div className="text-xs font-normal text-emerald-600">
                Capa dura entregue:
              </div>
              <div>{formatDate(hardCoverReceivedAt)}</div>
            </div>
          </div>
        )}
      </div>

      <p className="mt-3 text-sm leading-relaxed text-[#4A5A4A]">
        O estudante tem o prazo máximo de <strong>30 dias</strong> após a aprovação para incorporar as correções da banca e entregar a versão final em capa dura à <strong>Coordenação do Curso</strong>. A coordenação encaminha a cópia aprovada para a <strong>Biblioteca do Campus</strong> para concluir o fluxo.
      </p>

      {/* Checklist das 3 fases finais */}
      <div className="mt-5 grid grid-cols-1 gap-3 border-t border-[#006633]/15 pt-4 sm:grid-cols-3">
        {/* Passo 1: Capa Dura */}
        <div className="flex items-start gap-2.5 rounded-lg bg-white/80 p-3 shadow-xs">
          {isHardCoverDelivered ? (
            <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-emerald-600" />
          ) : (
            <Clock className="mt-0.5 size-5 shrink-0 text-amber-600" />
          )}
          <div className="text-xs">
            <p className="font-semibold text-[#002B18]">1. Capa Dura (Coordenação)</p>
            <p className="text-gray-500">
              {isHardCoverDelivered
                ? `Recebida em ${formatDate(hardCoverReceivedAt)}`
                : 'Pendente de entrega pelo discente'}
            </p>
          </div>
        </div>

        {/* Passo 2: Encaminhamento */}
        <div className="flex items-start gap-2.5 rounded-lg bg-white/80 p-3 shadow-xs">
          {isForwardedToLibrary ? (
            <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-emerald-600" />
          ) : (
            <Clock className="mt-0.5 size-5 shrink-0 text-gray-400" />
          )}
          <div className="text-xs">
            <p className="font-semibold text-[#002B18]">2. Envio à Biblioteca</p>
            <p className="text-gray-500">
              {isForwardedToLibrary
                ? `Encaminhado em ${formatDate(forwardedToLibraryAt)}`
                : 'Aguardando entrega da capa dura'}
            </p>
          </div>
        </div>

        {/* Passo 3: Acervo da Biblioteca */}
        <div className="flex items-start gap-2.5 rounded-lg bg-white/80 p-3 shadow-xs">
          {isLibraryConfirmed ? (
            <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-emerald-600" />
          ) : (
            <Clock className="mt-0.5 size-5 shrink-0 text-gray-400" />
          )}
          <div className="text-xs">
            <p className="font-semibold text-[#002B18]">3. Homologação no Acervo</p>
            <p className="text-gray-500">
              {isLibraryConfirmed
                ? `Homologado em ${formatDate(libraryReceivedAt)}`
                : 'Aguardando recebimento na biblioteca'}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
