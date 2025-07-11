'use client';

import { Button } from '@/components/ui/button';
import { faEnvelope, faPen } from '@fortawesome/free-solid-svg-icons';

interface CancellationDetails {
  reasonCancellation: string;
}

interface ActionPanelProps {
  profile: string | string[] | null;
  cancellationRequested: boolean;
  cancellationDetails: CancellationDetails | null;
  hasBanking: boolean;
  isBankingFormVisible: boolean;
  hasSchedule: boolean;
  isScheduleFormVisible: boolean;
  onApprove: () => void;
  onRequest: () => void;
  onRegisterBankingClick: () => void;
  onScheduleClick: () => void;
  onSendScheduleEmail: () => void;
}

export function ActionPanel({
  profile,
  cancellationRequested,
  cancellationDetails,
  hasBanking,
  isBankingFormVisible,
  hasSchedule,
  isScheduleFormVisible,
  onApprove,
  onRequest,
  onRegisterBankingClick,
  onScheduleClick,
  onSendScheduleEmail
}: ActionPanelProps) {
  const isApprover =
    profile &&
    ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR'].some((role) =>
      Array.isArray(profile) ? profile.includes(role) : profile === role
    );

  const canSchedule = isApprover && hasBanking && !cancellationRequested;

  return (
    <div className="flex flex-col md:flex-row justify-end gap-4 mt-6">
      {isApprover &&
        !hasBanking &&
        !cancellationRequested &&
        !isBankingFormVisible && (
          <Button onClick={onRegisterBankingClick} className="md:w-fit w-full">
            Cadastrar Banca
          </Button>
        )}

      {canSchedule && !isBankingFormVisible && (
        <div className="flex gap-2 justify-end">
          <Button
            onClick={onScheduleClick}
            variant="outline"
            className="md:w-fit w-full"
            disabled={isScheduleFormVisible}
            icon={hasSchedule ? faPen : undefined}
          >
            {hasSchedule ? 'Editar Apresentação' : 'Marcar Apresentação'}
          </Button>
          {hasSchedule && (
            <Button
              icon={faEnvelope}
              onClick={onSendScheduleEmail}
              className="md:w-fit w-full"
            >
              Enviar Agenda
            </Button>
          )}
        </div>
      )}

      {cancellationRequested && isApprover && (
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
              onClick={onApprove}
              className="w-full md:w-fit"
            >
              Aprovar Cancelamento
            </Button>
          </div>
        </div>
      )}

      {profile === 'STUDENT' && !cancellationRequested && (
        <Button
          className="md:w-fit w-full"
          variant="destructive"
          onClick={onRequest}
        >
          Solicitar cancelamento de proposta
        </Button>
      )}

      {profile === 'STUDENT' && cancellationRequested && (
        <p className="text-right text-yellow-600 font-semibold italic">
          Sua solicitação de cancelamento está em análise.
        </p>
      )}
    </div>
  );
}
