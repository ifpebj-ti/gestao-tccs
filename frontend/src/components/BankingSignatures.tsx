'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { faDownload, faUpload, faExternalLinkAlt, faFileSignature } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { CustomFileInput } from '@/components/CustomFileInput/page';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

interface DocumentSignatureDTO {
  documentId: number;
  documentName: string;
  studentId: number | null;
}

export interface FindAllPendingSignatureDTO {
  tccId: number;
  studentNames: string[];
  documents: DocumentSignatureDTO[];
}

interface BankingSignaturesProps {
  signatures: FindAllPendingSignatureDTO[];
  jwt: string;
  onSuccess: () => void;
}

export function BankingSignatures({ signatures, jwt, onSuccess }: BankingSignaturesProps) {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [downloadedDocs, setDownloadedDocs] = useState<Set<number>>(new Set());
  const [selectedFiles, setSelectedFiles] = useState<{ [key: number]: File | null }>({});
  const [isSubmitting, setIsSubmitting] = useState<{ [key: number]: boolean }>({});

  const handleDownload = async (docId: number, tccId: number, studentId: number | null, docName: string) => {
    try {
      const url = `${API_URL}/Signature/document/download?tccId=${tccId}&documentId=${docId}${studentId ? `&studentId=${studentId}` : ''}`;
      const res = await fetch(url, {
        headers: { Authorization: `Bearer ${jwt}` }
      });
      
      if (!res.ok) throw new Error('Erro ao baixar documento');
      
      const blob = await res.blob();
      const downloadUrl = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = downloadUrl;
      a.download = `${docName}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      
      setDownloadedDocs(prev => new Set(prev).add(docId));
      toast.success('Documento baixado com sucesso!');
    } catch {
      toast.error('Erro ao baixar o documento.');
    }
  };

  const handleUpload = async (docId: number, tccId: number) => {
    const file = selectedFiles[docId];
    if (!file) {
      toast.error('Selecione um arquivo PDF.');
      return;
    }

    try {
      setIsSubmitting(prev => ({ ...prev, [docId]: true }));
      
      // Precisamos do UserId do JWT
      const tokenPayload = JSON.parse(atob(jwt.split('.')[1]));
      const userId = tokenPayload.userId || tokenPayload.id || tokenPayload.nameid || tokenPayload.sub;

      const formData = new FormData();
      formData.append('File', file);
      formData.append('TccId', tccId.toString());
      formData.append('DocumentId', docId.toString());
      formData.append('UserId', userId.toString());

      const res = await fetch(`${API_URL}/Signature`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${jwt}` },
        body: formData
      });

      if (res.ok) {
        toast.success('Documento assinado enviado com sucesso!');
        onSuccess(); // Re-fetch the pending signatures to hide this doc
      } else {
        toast.error('Erro ao enviar o documento assinado.');
      }
    } catch {
      toast.error('Erro inesperado ao enviar o documento.');
    } finally {
      setIsSubmitting(prev => ({ ...prev, [docId]: false }));
    }
  };

  return (
    <div className="mt-6 w-full space-y-6">
      <div className="bg-green-50 border border-green-200 rounded-lg p-4">
        <h3 className="text-green-800 font-bold mb-1">Documentos Prontos para Assinatura!</h3>
        <p className="text-sm text-green-700">Baixe o documento gerado, assine digitalmente via Gov.br e faça o envio do arquivo finalizado abaixo.</p>
      </div>

      {signatures.map((sigGroup) => (
        <div key={sigGroup.tccId} className="border border-gray-200 rounded-lg p-4 bg-white shadow-sm">
          <h3 className="font-bold text-gray-800 mb-3 border-b pb-2">
            TCC: {sigGroup.studentNames.join(', ')}
          </h3>
          
          <div className="space-y-4">
            {sigGroup.documents.map(doc => {
              const isDownloaded = downloadedDocs.has(doc.documentId);
              
              return (
                <div key={doc.documentId} className="p-4 bg-gray-50 rounded-md border border-gray-100 flex flex-col gap-4">
                  <div className="flex items-center gap-2 text-gray-700 font-semibold">
                    <FontAwesomeIcon icon={faFileSignature} className="text-blue-600" />
                    <span>{doc.documentName}</span>
                  </div>
                  
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Passo 1 */}
                    <div className="space-y-2">
                      <p className="text-sm font-medium text-gray-600">1. Baixar Documento</p>
                      <Button 
                        variant="outline" 
                        className="w-full justify-start"
                        onClick={() => handleDownload(doc.documentId, sigGroup.tccId, doc.studentId, doc.documentName)}
                      >
                        <FontAwesomeIcon icon={faDownload} className="mr-2" />
                        Baixar PDF
                      </Button>
                      
                      <div className={`pt-2 transition-opacity ${!isDownloaded ? 'opacity-50 pointer-events-none' : ''}`}>
                        <p className="text-sm font-medium text-gray-600 mb-1">2. Assinar (Gov.br)</p>
                        <a href="https://www.gov.br/pt-br/servicos/assinatura-eletronica" target="_blank" rel="noopener noreferrer">
                          <Button variant="link" className="p-0 h-auto text-blue-600 text-sm">
                            Assinar com Gov.br <FontAwesomeIcon icon={faExternalLinkAlt} className="ml-2 h-3 w-3" />
                          </Button>
                        </a>
                      </div>
                    </div>
                    
                    {/* Passo 3 */}
                    <div className={`space-y-2 md:border-l md:pl-4 border-gray-200 transition-opacity ${!isDownloaded ? 'opacity-50 pointer-events-none' : ''}`}>
                      <p className="text-sm font-medium text-gray-600">3. Enviar Assinado</p>
                      <CustomFileInput 
                        selectedFile={selectedFiles[doc.documentId] || null}
                        onFileSelect={(f) => setSelectedFiles(prev => ({ ...prev, [doc.documentId]: f }))}
                        accept="application/pdf"
                      />
                      <Button 
                        className="w-full mt-2" 
                        disabled={!selectedFiles[doc.documentId] || isSubmitting[doc.documentId]}
                        onClick={() => handleUpload(doc.documentId, sigGroup.tccId)}
                      >
                        {isSubmitting[doc.documentId] ? 'Enviando...' : (
                          <><FontAwesomeIcon icon={faUpload} className="mr-2" /> Enviar Arquivo</>
                        )}
                      </Button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
