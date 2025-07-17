'use client';

import { useState } from 'react';
import { useSignaturePage } from '@/app/hooks/useSignaturePage';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  faDownload,
  faSignature,
  faSpinner,
  faInfoCircle,
  faFilePdf
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

      <div
        className={`border-t pt-4 space-y-2 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
      >
        <h3 className="font-bold text-base">
          Passo 2: Enviar Documento Assinado
        </h3>
        <div>
          <Label htmlFor="signature-file" className="text-sm font-medium">
            Arquivo PDF
          </Label>
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

      <div
        className={`border-t pt-4 space-y-2 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
      >
        <h3 className="font-bold text-base">Passo 3: Confirmar Assinatura</h3>
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
        {isLoading ? 'Carregando documento...' : documentName}
      </h1>

      {/* Layout para Telas Grandes (lg em diante) -> Duas Colunas */}
      <div className="hidden lg:grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 h-[80vh]">
          <DocumentViewer />
        </div>
        <div className="lg:col-span-1">
          <ActionPanel />
        </div>
      </div>

      {/* Layout para Mobile e Tablets (até lg) -> Coluna Única */}
      <div className="lg:hidden flex flex-col gap-8">
        <div>
          <ActionPanel />
        </div>
        {/* O visualizador é um card expansível */}
        <div>
          <CollapseCard title="Visualizar Documento" icon={faFilePdf}>
            <div className="w-full h-[70vh]">
              <DocumentViewer />
            </div>
          </CollapseCard>
        </div>
      </div>
    </div>
  );
}
