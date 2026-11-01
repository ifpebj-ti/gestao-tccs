'use client';

import { PageTitle } from '@/components/layout/PageTitle';
import { NoticeCard } from '@/components/layout/NoticeCard';

import { useState } from 'react';
import { useSignaturePage } from '@/app/hooks/useSignaturePage';
import { Button, buttonVariants } from '@/components/ui/button';
import {
  faDownload,
  faSignature,
  faSpinner,
  faInfoCircle,
  faExternalLinkAlt
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { CustomFileInput } from '@/components/CustomFileInput/page';
import { useSearchParams, useRouter } from 'next/navigation';
import { Award, ArrowRight, ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription
} from '@/components/ui/dialog';

export default function SignatureClient() {
  const router = useRouter();
  const {
    documentUrl,
    documentHtml,
    documentName,
    isLoading,
    isSubmitting,
    selectedFile,
    setSelectedFile,
    tccFile,
    setTccFile,
    handleSignDocument,
    handleDownloadDocument,
    iframeRef,
    isAnexoVIII,
    isAdvisor,
    signatureSuccess,
    tccId
  } = useSignaturePage();

  const searchParams = useSearchParams();
  const docNameFromParams = searchParams.get('docName');
  const [downloadClicked, setDownloadClicked] = useState(false);
  const [downloading, setDownloading] = useState(false);

  const [scheduleDate, setScheduleDate] = useState('');
  const [scheduleTime, setScheduleTime] = useState('');
  const [scheduleLocation, setScheduleLocation] = useState('');

  const renderActionPanel = () => (
    <section
      aria-label="Etapas de assinatura"
      className="space-y-5 rounded-2xl bg-[#183E2D] p-5 text-[#FAF9F5] md:p-6"
    >
      <div>
        <h2 className="mb-2 text-lg font-bold text-[#C8FF47]">
          Concluir assinatura
        </h2>
        <p className="text-sm text-[#FAF9F5]/85">
          Siga os passos abaixo para concluir sua assinatura.
        </p>
      </div>

      {/* Passo 1: Agendamento (Obrigatório para Orientador no Anexo VIII) */}
      {isAnexoVIII && isAdvisor && (
        <div className="space-y-2">
          <h3 className="font-bold text-sm">
            Passo 1: Agendamento da Apresentação
          </h3>
          <p className="text-sm text-[#FAF9F5]/85 mb-2">
            Preencha a data, horário e local antes de baixar o documento. Ele
            será gerado com esses dados.
          </p>
          <div className="flex flex-col gap-3 mt-2">
            <div>
              <label
                htmlFor="signature-date"
                className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-[#FAF9F5]/85"
              >
                Data da apresentação
              </label>
              <Input
                id="signature-date"
                className="border-[#C8FF47]/60 text-[#FAF9F5] placeholder:text-[#FAF9F5]/50 focus-visible:border-[#C8FF47] [color-scheme:dark]"
                type="date"
                value={scheduleDate}
                onChange={(e) => setScheduleDate(e.target.value)}
              />
            </div>
            <div>
              <label
                htmlFor="signature-time"
                className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-[#FAF9F5]/85"
              >
                Hora da apresentação
              </label>
              <Input
                id="signature-time"
                className="border-[#C8FF47]/60 text-[#FAF9F5] placeholder:text-[#FAF9F5]/50 focus-visible:border-[#C8FF47] [color-scheme:dark]"
                type="time"
                value={scheduleTime}
                onChange={(e) => setScheduleTime(e.target.value)}
              />
            </div>
            <div>
              <label
                htmlFor="signature-location"
                className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-[#FAF9F5]/85"
              >
                Local/Link
              </label>
              <Input
                id="signature-location"
                className="border-[#C8FF47]/60 text-[#FAF9F5] placeholder:text-[#FAF9F5]/50 focus-visible:border-[#C8FF47] [color-scheme:dark]"
                type="text"
                value={scheduleLocation}
                onChange={(e) => setScheduleLocation(e.target.value)}
                placeholder="Ex: Sala 20 ou Link do Meet"
              />
            </div>
          </div>
        </div>
      )}

      {/* Passo 2: Baixar Documento */}
      <div
        className={`${isAnexoVIII && isAdvisor ? 'border-t border-[#FAF9F5]/20 pt-4 ' : ''}space-y-2`}
      >
        <h3 className="font-bold text-base">
          {isAnexoVIII && isAdvisor ? 'Passo 2' : 'Passo 1'}: Baixar Documento
        </h3>
        <Button
          variant="accent"
          className="w-full"
          disabled={
            isLoading ||
            downloading ||
            (isAnexoVIII &&
              isAdvisor &&
              (!scheduleDate || !scheduleTime || !scheduleLocation))
          }
          onClick={async () => {
            setDownloading(true);
            try {
              const succeeded = await handleDownloadDocument({
                date: scheduleDate,
                time: scheduleTime,
                location: scheduleLocation
              });
              if (succeeded) setDownloadClicked(true);
            } finally {
              setDownloading(false);
            }
          }}
        >
          <FontAwesomeIcon icon={faDownload} className="mr-2" />
          {downloading ? 'Baixando…' : 'Baixar documento'}
        </Button>
      </div>

      {/* Passo 3: Assinar (Link Externo) */}
      <div className="border-t border-[#FAF9F5]/20 pt-4 space-y-3">
        <h3 className="font-bold text-base">
          {isAnexoVIII && isAdvisor ? 'Passo 3' : 'Passo 2'}: Assinar o
          Documento
        </h3>
        <p className="text-sm text-[#FAF9F5]/85">
          Não tem um assinador? Utilize o serviço gratuito do Governo Federal.
        </p>
        <a
          href="https://www.gov.br/pt-br/servicos/assinatura-eletronica"
          target="_blank"
          rel="noopener noreferrer"
          className={buttonVariants({
            variant: 'outline',
            className:
              'w-full border-[#C8FF47]/60 text-[#C8FF47] hover:bg-[#C8FF47] hover:text-[#002B18]'
          })}
        >
          Assinar com gov.br
          <FontAwesomeIcon icon={faExternalLinkAlt} className="ml-2 h-3 w-3" />
        </a>
      </div>

      {/* Passo 4: Enviar Documento */}
      <div className="border-t border-[#FAF9F5]/20 pt-4 space-y-3">
        <h3 className="font-bold text-base">
          {isAnexoVIII && isAdvisor ? 'Passo 4' : 'Passo 3'}: Enviar Documento
          Assinado
        </h3>
        <div>
          <CustomFileInput
            selectedFile={selectedFile}
            onFileSelect={setSelectedFile}
            disabled={!downloadClicked}
            accept=".pdf,.html"
            buttonVariant="accent"
          />
          {!downloadClicked && (
            <p className="text-xs text-[#FAF9F5]/75 italic mt-1 flex items-center gap-1">
              <FontAwesomeIcon icon={faInfoCircle} />
              Clique em &apos;Baixar Documento&apos; para habilitar o envio.
            </p>
          )}
        </div>
      </div>

      {/* Passo Especial: Enviar TCC (Obrigatório para Orientador no Anexo VIII) */}
      {isAnexoVIII && isAdvisor && (
        <div className="border-t border-[#FAF9F5]/20 pt-4 space-y-3">
          <h3 className="font-bold text-base">
            Passo 5: Enviar Arquivo do TCC
          </h3>
          <p className="text-sm text-[#FAF9F5]/85 mb-2">
            Como orientador(a), você deve enviar o arquivo final do TCC
            (Monografia) junto com este termo.
          </p>
          <div>
            <CustomFileInput
              selectedFile={tccFile}
              onFileSelect={setTccFile}
              disabled={!downloadClicked}
              accept=".pdf"
              buttonVariant="accent"
            />
          </div>
        </div>
      )}

      {/* Passo Final: Confirmar Assinatura */}
      <div className="border-t border-[#FAF9F5]/20 pt-4 space-y-4">
        <h3 className="font-bold text-base">
          {isAnexoVIII && isAdvisor ? 'Passo 6' : 'Passo 4'}: Confirmar
          Assinatura
        </h3>

        <NoticeCard
          title="Confira antes de confirmar"
          className="rounded-xl border border-[#C8FF47]/25 bg-[#002B18] p-4"
        >
          Verifique se o arquivo anexado é o documento correto e devidamente
          assinado. Esta ação não poderá ser desfeita.
        </NoticeCard>

        <Button
          variant="accent"
          onClick={() =>
            handleSignDocument({
              date: scheduleDate,
              time: scheduleTime,
              location: scheduleLocation
            })
          }
          disabled={
            !downloadClicked ||
            !selectedFile ||
            (isAnexoVIII && isAdvisor && !tccFile) ||
            (isAnexoVIII &&
              isAdvisor &&
              (!scheduleDate || !scheduleTime || !scheduleLocation)) ||
            isSubmitting
          }
          className="w-full"
        >
          {isSubmitting ? (
            <FontAwesomeIcon icon={faSpinner} spin className="mr-2" />
          ) : (
            <FontAwesomeIcon icon={faSignature} className="mr-2" />
          )}
          {isSubmitting ? 'Enviando...' : 'Confirmar Assinatura'}
        </Button>
      </div>
    </section>
  );

  const DocumentViewer = ({ className = '' }: { className?: string }) => (
    <div className={`w-full h-full ${className}`}>
      {isLoading ? (
        <div className="w-full h-full bg-gray-100 rounded-md flex items-center justify-center">
          <FontAwesomeIcon
            icon={faSpinner}
            spin
            size="2x"
            className="text-gray-400"
          />
        </div>
      ) : documentUrl ? (
        <iframe
          src={documentUrl}
          className="w-full h-full border rounded-md"
          title="Visualizador de Documento"
        />
      ) : documentHtml ? (
        <iframe
          ref={iframeRef}
          srcDoc={documentHtml}
          sandbox="allow-same-origin allow-forms allow-scripts"
          className="w-full h-full border rounded-md"
          title="Visualizador HTML Seguro"
        />
      ) : (
        <div className="w-full h-full bg-gray-100 rounded-md flex items-center justify-center text-center p-4">
          <p className="text-red-500">
            Não foi possível carregar o documento. O link pode ter expirado. Por
            favor, volte e tente novamente.
          </p>
        </div>
      )}
    </div>
  );

  const pageTitle =
    docNameFromParams ||
    documentName ||
    (isLoading ? 'Carregando documento...' : 'Erro ao carregar documento');

  return (
    <div className="flex flex-col">
      <header className="mb-4 space-y-2">
        <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
          <PageTitle className="text-2xl md:text-3xl">
            Assinar documento
          </PageTitle>
          <Link
            href="/pendingSignatures"
            className={buttonVariants({
              variant: 'ghost',
              size: 'sm',
              className: '-mr-3.5'
            })}
          >
            <ArrowLeft className="size-4" />
            Voltar às assinaturas
          </Link>
        </div>
        <p className="max-w-3xl break-words text-sm leading-relaxed text-slate-600">
          {pageTitle}
        </p>
      </header>
      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1.6fr)_minmax(320px,1fr)]">
        <div className="h-[60vh] min-w-0 overflow-hidden rounded-xl border border-[#E2E5DC] bg-white xl:sticky xl:top-6 xl:h-[78vh]">
          {DocumentViewer({})}
        </div>
        {renderActionPanel()}
      </div>

      <Dialog open={!!signatureSuccess?.isOpen} onOpenChange={() => {}}>
        <DialogContent className="max-w-md p-6 text-center sm:text-left">
          <div className="flex flex-col items-center text-center p-2">
            <div className="w-16 h-16 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center mb-4 shadow-inner">
              <Award className="w-8 h-8" />
            </div>

            <DialogHeader className="items-center text-center">
              <DialogTitle className="text-xl font-black text-gray-900">
                Assinatura Registrada com Sucesso!
              </DialogTitle>
              <DialogDescription className="text-sm text-gray-600 mt-2">
                Parabéns! Sua assinatura no documento da banca avaliadora foi
                homologada no sistema.
              </DialogDescription>
            </DialogHeader>

            <div className="w-full mt-4 p-4 rounded-lg bg-emerald-50 border border-emerald-100 text-xs text-emerald-800">
              O documento assinado agora compõe o histórico oficial do TCC e as
              avaliações da comissão examinadora estão disponíveis para
              consulta.
            </div>

            <div className="flex flex-col w-full gap-2 mt-6">
              <Button
                variant="brand"
                className="w-full"
                onClick={() => {
                  router.push(
                    `/myTCC/details?id=${signatureSuccess?.tccId || tccId || ''}`
                  );
                }}
              >
                Acessar Informações do Meu TCC
                <ArrowRight className="w-4 h-4 ml-2" />
              </Button>

              <Button
                variant="outline"
                className="w-full"
                onClick={() => {
                  router.push('/pendingSignatures');
                }}
              >
                Voltar às Assinaturas Pendentes
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
