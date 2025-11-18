'use client';

import { useState } from 'react';
import { useSignaturePage } from '@/app/hooks/useSignaturePage';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import {
  faDownload,
  faSignature,
  faSpinner,
  faInfoCircle,
  faFilePdf,
  faExternalLinkAlt,
  faExclamationTriangle
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { CollapseCard } from '@/components/CollapseCard';
import { CustomFileInput } from '@/components/CustomFileInput/page';

export default function SignatureClient() {
  const {
    documentUrl,
    documentName,
    isLoading,
    isSubmitting,
    selectedFile,
    setSelectedFile,
    handleSignDocument,
    handleDownloadDocument
  } = useSignaturePage();

  const [downloadClicked, setDownloadClicked] = useState(false);

  const ActionPanel = () => (
    <div className="p-6 border rounded-lg shadow-sm bg-white space-y-6">
      <div>
        <h2 className="font-bold text-lg">Ações para Assinatura</h2>
        <p className="text-sm text-gray-500">
          Siga os passos abaixo para concluir sua assinatura.
        </p>
      </div>

      {/* Passo 1: Baixar Documento */}
      <div className="space-y-2">
        <h3 className="font-bold text-base">Passo 1: Baixar Documento</h3>
        <Button
          variant="outline"
          className="w-full"
          onClick={() => {
            handleDownloadDocument();
            setDownloadClicked(true);
          }}
        >
          <FontAwesomeIcon icon={faDownload} className="mr-2" />
          Baixar Documento
        </Button>
      </div>

      {/* Passo 2: Assinar (Link Externo) */}
      <div
        className={`border-t pt-4 space-y-2 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
      >
        <h3 className="font-bold text-base">Passo 2: Assinar o Documento</h3>
        <p className="text-sm text-gray-600">
          Não tem um assinador? Utilize o serviço gratuito do Governo Federal.
        </p>
        <a
          href="https://www.gov.br/pt-br/servicos/assinatura-eletronica"
          target="_blank"
          rel="noopener noreferrer"
          className={!downloadClicked ? 'pointer-events-none' : ''}
        >
          <Button variant="link" className="p-0 h-auto text-blue-600">
            Assinar com gov.br
            <FontAwesomeIcon
              icon={faExternalLinkAlt}
              className="ml-2 h-3 w-3"
            />
          </Button>
        </a>
      </div>

      {/* Passo 3: Enviar Documento */}
      <div
        className={`border-t pt-4 space-y-2 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
      >
        <h3 className="font-bold text-base">
          Passo 3: Enviar Documento Assinado
        </h3>
        <div>
          <CustomFileInput
            selectedFile={selectedFile}
            onFileSelect={setSelectedFile}
            disabled={!downloadClicked}
            accept=".pdf"
          />
          {!downloadClicked && (
            <p className="text-xs text-gray-500 italic mt-1 flex items-center gap-1">
              <FontAwesomeIcon icon={faInfoCircle} />
              Clique em &apos;Baixar Documento&apos; para habilitar o envio.
            </p>
          )}
        </div>
      </div>

      {/* Passo 4: Confirmar Assinatura */}
      <div
        className={`border-t pt-4 space-y-4 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
      >
        <h3 className="font-bold text-base">Passo 4: Confirmar Assinatura</h3>

        {/* 2. Bloco de aviso adicionado aqui */}
        <div className="p-3 text-sm text-amber-700 rounded-lg bg-amber-50 border border-amber-200">
          <div className="flex items-start gap-2">
            <FontAwesomeIcon
              icon={faExclamationTriangle}
              className="mt-0.5 h-4 w-4"
            />
            <div>
              <span className="font-bold">Atenção:</span> Verifique se o arquivo
              anexado é o documento correto e devidamente assinado. Esta ação
              não poderá ser desfeita.
            </div>
          </div>
        </div>

        <Button
          onClick={handleSignDocument}
          disabled={!downloadClicked || !selectedFile || isSubmitting}
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
    </div>
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

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10 truncate">
        {isLoading
          ? 'Carregando documento...'
          : documentName || 'Erro ao carregar documento'}
      </h1>

      {/* Layout para Telas Grandes (lg em diante) */}
      <div className="hidden lg:grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 h-[80vh]">
          <DocumentViewer />
        </div>
        <div className="lg:col-span-1">
          <ActionPanel />
        </div>
      </div>

      {/* Layout para Mobile e Tablets (até lg) */}
      <div className="lg:hidden flex flex-col gap-4 ">
        <div>
          <CollapseCard title="Visualizar Documento" icon={faFilePdf}>
            <div className="w-full h-[70vh]">
              <DocumentViewer />
            </div>
          </CollapseCard>
        </div>
        <div>
          <ActionPanel />
        </div>
      </div>
    </div>
  );
}
