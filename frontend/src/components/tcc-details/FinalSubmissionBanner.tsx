'use client';

import React, { useState } from 'react';
import {
  Clock,
  AlertTriangle,
  CheckCircle2,
  BookOpen,
  Download,
  Upload,
  LoaderCircle,
  Library,
  Send
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from '@/components/ui/alert-dialog';

interface FinalSubmissionBannerProps {
  step?: string | null;
  finalSubmissionDeadline?: string | null;
  finalTccFile?: string | null;
  hardCoverReceivedAt?: string | null;
  forwardedToLibraryAt?: string | null;
  libraryReceivedAt?: string | null;
  libraryCatalogCode?: string | null;
  isCompleted?: boolean;
  // Perfil e Ações
  profile?: string | string[] | null;
  onUploadFinalFile?: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onConfirmHardCover?: () => void;
  onDownloadFinalFile?: () => void;
  onForwardToLibrary?: () => void;
  onConfirmLibrary?: (catalogCode?: string) => void;
  isUploadingFinalFile?: boolean;
  isConfirmingHardCover?: boolean;
  isDownloadingFinalFile?: boolean;
  isForwardingToLibrary?: boolean;
  isConfirmingLibrary?: boolean;
}

export function FinalSubmissionBanner({
  step,
  finalSubmissionDeadline,
  finalTccFile,
  hardCoverReceivedAt,
  forwardedToLibraryAt,
  libraryReceivedAt,
  libraryCatalogCode,
  isCompleted,
  profile,
  onUploadFinalFile,
  onConfirmHardCover,
  onDownloadFinalFile,
  onForwardToLibrary,
  onConfirmLibrary,
  isUploadingFinalFile = false,
  isConfirmingHardCover = false,
  isDownloadingFinalFile = false,
  isForwardingToLibrary = false,
  isConfirmingLibrary = false
}: FinalSubmissionBannerProps) {
  const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);
  const [isForwardModalOpen, setIsForwardModalOpen] = useState(false);
  const [isLibraryModalOpen, setIsLibraryModalOpen] = useState(false);
  const [catalogCodeInput, setCatalogCodeInput] = useState('');

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

  const userRoles = Array.isArray(profile)
    ? profile
    : profile
      ? [profile]
      : [];

  const canUpload = userRoles.some((r) =>
    ['STUDENT', 'ADVISOR', 'COORDINATOR', 'SUPERVISOR', 'ADMIN'].includes(r)
  );
  const canConfirmHardCover = userRoles.some((r) =>
    ['COORDINATOR', 'SUPERVISOR', 'ADMIN'].includes(r)
  );
  const canForwardToLibrary = userRoles.some((r) =>
    ['COORDINATOR', 'SUPERVISOR', 'ADMIN'].includes(r)
  );
  const canConfirmLibrary = userRoles.some((r) =>
    ['LIBRARY', 'ADMIN'].includes(r)
  );

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
                {isCompleted ? 'TCC Concluído com Sucesso' : 'TCC Aprovado na Defesa'}
              </span>
            </div>
            <h3 className="mt-1 text-lg font-bold text-[#002B18]">
              {isCompleted
                ? 'Versão Final Homologada no Acervo da Biblioteca'
                : 'Entrega da Versão Final e Envio à Biblioteca'}
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

        {isHardCoverDelivered && !isCompleted && (
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

        {isCompleted && (
          <div className="flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-semibold text-emerald-800 shadow-sm">
            <CheckCircle2 className="size-4 shrink-0 text-emerald-600" />
            <div>
              <div className="text-xs font-normal text-emerald-600">
                TCC no Acervo:
              </div>
              <div>
                {libraryCatalogCode ? `Código: ${libraryCatalogCode}` : 'Homologado'}
              </div>
            </div>
          </div>
        )}
      </div>

      <p className="mt-3 text-sm leading-relaxed text-[#4A5A4A]">
        O estudante tem o prazo máximo de <strong>30 dias</strong> após a
        aprovação para incorporar as correções da banca e entregar a versão
        final em capa dura à <strong>Coordenação do Curso</strong>. A coordenação
        encaminha a cópia aprovada para a <strong>Biblioteca do Campus</strong> para
        concluir o fluxo e arquivar no acervo.
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
            <p className="font-semibold text-[#002B18]">
              1. Capa Dura (Coordenação)
            </p>
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
            <p className="font-semibold text-[#002B18]">
              2. Envio à Biblioteca
            </p>
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
            <p className="font-semibold text-[#002B18]">
              3. Homologação no Acervo
            </p>
            <p className="text-gray-500">
              {isLibraryConfirmed
                ? `Homologado em ${formatDate(libraryReceivedAt)}${libraryCatalogCode ? ` (${libraryCatalogCode})` : ''}`
                : 'Aguardando recebimento na biblioteca'}
            </p>
          </div>
        </div>
      </div>

      {/* Barra de Ações Operacionais */}
      <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-[#006633]/15 pt-4">
        <div className="flex flex-wrap items-center gap-2">
          {/* Download da versão final se já foi enviada */}
          {finalTccFile && onDownloadFinalFile && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isDownloadingFinalFile}
              onClick={onDownloadFinalFile}
              className="border-[#006633] text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47] font-semibold cursor-pointer"
            >
              {isDownloadingFinalFile ? (
                <LoaderCircle className="size-4 animate-spin motion-reduce:animate-none" />
              ) : (
                <Download className="size-4" />
              )}
              Baixar Versão Final (PDF)
            </Button>
          )}

          {/* Upload da versão final pelo estudante ou gestão */}
          {canUpload && onUploadFinalFile && !isCompleted && (
            <>
              <input
                id="finalSubmissionFileInput"
                type="file"
                accept=".pdf"
                className="hidden"
                onChange={onUploadFinalFile}
                disabled={isUploadingFinalFile}
              />
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={isUploadingFinalFile}
                onClick={() =>
                  document.getElementById('finalSubmissionFileInput')?.click()
                }
                className="border-[#006633] text-[#002B18] hover:bg-[#006633] hover:text-white font-semibold cursor-pointer"
              >
                {isUploadingFinalFile ? (
                  <LoaderCircle className="size-4 animate-spin motion-reduce:animate-none" />
                ) : (
                  <Upload className="size-4" />
                )}
                {finalTccFile
                  ? 'Substituir Versão Final (PDF)'
                  : 'Submeter Versão Final (PDF Corrigido)'}
              </Button>
            </>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Confirmação do recebimento da capa dura pela Coordenação */}
          {canConfirmHardCover &&
            onConfirmHardCover &&
            !isHardCoverDelivered &&
            !isCompleted && (
              <Button
                type="button"
                size="sm"
                disabled={isConfirmingHardCover}
                onClick={() => setIsConfirmModalOpen(true)}
                className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold shadow-xs cursor-pointer"
              >
                {isConfirmingHardCover ? (
                  <LoaderCircle className="size-4 animate-spin motion-reduce:animate-none" />
                ) : (
                  <CheckCircle2 className="size-4" />
                )}
                Confirmar Recebimento da Capa Dura
              </Button>
            )}

          {/* Encaminhamento à Biblioteca pela Coordenação */}
          {canForwardToLibrary &&
            onForwardToLibrary &&
            isHardCoverDelivered &&
            !isForwardedToLibrary &&
            !isCompleted && (
              <Button
                type="button"
                size="sm"
                disabled={isForwardingToLibrary}
                onClick={() => setIsForwardModalOpen(true)}
                className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold shadow-xs cursor-pointer"
              >
                {isForwardingToLibrary ? (
                  <LoaderCircle className="size-4 animate-spin motion-reduce:animate-none" />
                ) : (
                  <Send className="size-4" />
                )}
                Encaminhar à Biblioteca
              </Button>
            )}

          {/* Homologação no Acervo pelo Bibliotecário */}
          {canConfirmLibrary &&
            onConfirmLibrary &&
            isForwardedToLibrary &&
            !isLibraryConfirmed &&
            !isCompleted && (
              <Button
                type="button"
                size="sm"
                disabled={isConfirmingLibrary}
                onClick={() => {
                  setCatalogCodeInput('');
                  setIsLibraryModalOpen(true);
                }}
                className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold shadow-xs cursor-pointer"
              >
                {isConfirmingLibrary ? (
                  <LoaderCircle className="size-4 animate-spin motion-reduce:animate-none" />
                ) : (
                  <Library className="size-4" />
                )}
                Homologar no Acervo e Concluir TCC
              </Button>
            )}
        </div>
      </div>

      {/* Modal de Confirmação da Capa Dura (Coordenação) */}
      <AlertDialog
        open={isConfirmModalOpen}
        onOpenChange={setIsConfirmModalOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirmar Recebimento da Capa Dura</AlertDialogTitle>
            <AlertDialogDescription>
              Você confirma que o estudante entregou a versão física encadernada
              em capa dura incorporando as correções e sugestões da banca examinadora
              à Coordenação do Curso?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold cursor-pointer"
              onClick={() => {
                setIsConfirmModalOpen(false);
                onConfirmHardCover?.();
              }}
            >
              Confirmar Recebimento
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Modal de Encaminhamento à Biblioteca (Coordenação) */}
      <AlertDialog
        open={isForwardModalOpen}
        onOpenChange={setIsForwardModalOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Encaminhar TCC à Biblioteca</AlertDialogTitle>
            <AlertDialogDescription>
              Deseja encaminhar a versão final em capa dura deste TCC para a Biblioteca do Campus?
              A equipe da Biblioteca será notificada automaticamente para incluir a obra no acervo.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold cursor-pointer"
              onClick={() => {
                setIsForwardModalOpen(false);
                onForwardToLibrary?.();
              }}
            >
              Sim, Encaminhar à Biblioteca
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Modal de Homologação no Acervo (Biblioteca) */}
      <AlertDialog
        open={isLibraryModalOpen}
        onOpenChange={setIsLibraryModalOpen}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Homologação no Acervo da Biblioteca</AlertDialogTitle>
            <AlertDialogDescription>
              Confirma o recebimento e homologação deste TCC no acervo da Biblioteca?
              Esta ação marcará oficialmente o TCC como <strong>CONCLUÍDO (COMPLETED)</strong> e notificará
              o estudante, orientador e coordenação.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <div className="py-2">
            <label className="block text-xs font-semibold text-[#002B18] mb-1">
              Código de Catalogação / Tombo (Opcional):
            </label>
            <input
              type="text"
              value={catalogCodeInput}
              onChange={(e) => setCatalogCodeInput(e.target.value)}
              placeholder="Ex: TCC-2026-042 ou 004.42"
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-[#002B18] focus:outline-none"
            />
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-[#002B18] text-[#C8FF47] hover:bg-[#003820] font-semibold cursor-pointer"
              onClick={() => {
                setIsLibraryModalOpen(false);
                onConfirmLibrary?.(catalogCodeInput);
              }}
            >
              Homologar e Concluir
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
