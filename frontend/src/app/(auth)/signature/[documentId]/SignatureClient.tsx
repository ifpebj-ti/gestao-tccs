'use client';

import { useState } from 'react';
import { useSignaturePage } from '@/app/hooks/useSignaturePage';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import {
  faDownload,
  faSignature,
  faSpinner,
  faInfoCircle
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export default function SignatureClient() {
  const {
    documentId,
    tccId,
    documentUrl,
    documentName,
    isLoading,
    isSubmitting,
    selectedFile,
    setSelectedFile,
    handleSignDocument,
    API_URL
  } = useSignaturePage();

  const [downloadClicked, setDownloadClicked] = useState(false);

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10 truncate">
        {isLoading ? 'Carregando documento...' : documentName}
      </h1>

      <div className="grid md:grid-cols-3 gap-8">
        {/* Coluna do Visualizador de Documento */}
        <div className="md:col-span-2">
          {isLoading ? (
            <div className="w-full h-[80vh] bg-gray-100 rounded-md flex items-center justify-center">
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
              className="w-full h-[80vh] border rounded-md"
              title="Visualizador de Documento"
            />
          ) : (
            <div className="w-full h-[80vh] bg-gray-100 rounded-md flex items-center justify-center text-center p-4">
              <p className="text-red-500">
                Não foi possível carregar o documento. O link pode ter expirado.
                Por favor, volte e tente novamente.
              </p>
            </div>
          )}
        </div>

        {/* Coluna de Ações */}
        <div className="md:col-span-1">
          <div className="p-6 border rounded-lg shadow-sm bg-white space-y-6">
            <div>
              <h2 className="font-bold text-lg">Ações para Assinatura</h2>
              <p className="text-sm text-gray-500">
                Siga os passos abaixo para concluir sua assinatura.
              </p>
            </div>

            {/* Passo 1: Baixar */}
            <div className="space-y-2">
              <h2 className="font-bold text-lg">Passo 1: Baixar Documento</h2>
              <a
                href={`${API_URL}/Signature/document/download?tccId=${tccId}&documentId=${documentId}`}
                target="_blank"
                rel="noopener noreferrer"
                className="w-full"
                onClick={() => setDownloadClicked(true)}
              >
                <Button variant="outline" className="w-full">
                  <FontAwesomeIcon icon={faDownload} className="mr-2" />
                  Baixar Documento para Assinar
                </Button>
              </a>
            </div>

            {/* Passo 2: Upload */}
            <div
              className={`border-t pt-6 space-y-4 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
            >
              <h2 className="font-bold text-lg">
                Passo 2: Enviar Documento Assinado
              </h2>
              <div>
                <Label htmlFor="signature-file" className="text-sm font-medium">
                  Arquivo PDF
                </Label>
                <Input
                  id="signature-file"
                  type="file"
                  accept=".pdf"
                  className="mt-2"
                  disabled={!downloadClicked}
                  onChange={(e) =>
                    setSelectedFile(e.target.files ? e.target.files[0] : null)
                  }
                />
                {!downloadClicked && (
                  <p className="text-xs text-gray-500 italic mt-1 flex items-center gap-1">
                    <FontAwesomeIcon icon={faInfoCircle} />
                    Clique em &apos;Baixar Documento&apos; para habilitar o
                    envio.
                  </p>
                )}
              </div>
            </div>

            {/* Passo 3: Confirmar */}
            <div
              className={`border-t pt-6 space-y-4 transition-opacity ${!downloadClicked ? 'opacity-50' : ''}`}
            >
              <h2 className="font-bold text-lg">3. Confirmar Assinatura</h2>
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
        </div>
      </div>
    </div>
  );
}
