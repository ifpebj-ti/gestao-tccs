'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';

interface DecodedToken {
  userId: string;
}

export function useSignaturePage() {
  const { push } = useRouter();
  const [documentUrl, setDocumentUrl] = useState<string | null>(null);
  const [documentHtml, setDocumentHtml] = useState<string | null>(null);
  const [documentName, setDocumentName] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  const params = useParams();
  const searchParams = useSearchParams();

  const documentId = params.documentId as string;
  const tccId = searchParams.get('tccId');
  const docNameFromParams = searchParams.get('docName');
  const studentId = searchParams.get('studentId');

  const API_URL = env('NEXT_PUBLIC_API_URL');

  const fetchDocument = useCallback(async () => {
    if (!documentId || !tccId) return;

    setIsLoading(true);
    const token = Cookies.get('token');
    try {
      const res = await fetch(
        `${API_URL}/Signature/document?tccId=${tccId}&documentId=${documentId}&studentId=${studentId == "null" ? 0 : studentId}`,
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );
      if (!res.ok)
        throw new Error('Documento não encontrado ou link expirado.');

      const data = await res.json();
      if (data.isHtml) {
        setDocumentHtml(data.url);
        setDocumentUrl(null);
      } else {
        setDocumentUrl('data:application/pdf;base64,' + data.url);
        setDocumentHtml(null);
      }

      if (data.url) {
        const path = data.url.split('?')[0];
        const filenameEncoded = path.substring(path.lastIndexOf('/') + 1);
        const filenameDecoded = decodeURIComponent(filenameEncoded);
        setDocumentName(filenameDecoded);
      }
    } catch {
      toast.error('Erro ao carregar o documento. O link pode ter expirado.');
    } finally {
      setIsLoading(false);
    }
  }, [documentId, tccId, studentId, API_URL]);

  useEffect(() => {
    fetchDocument();
  }, [fetchDocument]);

  const handleDownloadDocument = async () => {
    const token = Cookies.get('token');
    if (!tccId || !documentId || !token) {
      toast.error('Informações insuficientes para realizar o download.');
      return;
    }

    try {
      let res: Response;
      
      if (documentHtml) {
        // Capturar HTML preenchido no iframe
        let currentHtml = documentHtml;
        if (iframeRef.current && iframeRef.current.contentDocument) {
          const doc = iframeRef.current.contentDocument;
          
          // Propagar valores dos inputs para os atributos HTML para captura
          const inputs = doc.querySelectorAll('input');
          inputs.forEach(input => {
             if (input.type === 'checkbox' || input.type === 'radio') {
               if (input.checked) input.setAttribute('checked', 'checked');
               else input.removeAttribute('checked');
             } else {
               input.setAttribute('value', input.value);
             }
          });
          const textareas = doc.querySelectorAll('textarea');
          textareas.forEach(textarea => {
             textarea.innerHTML = textarea.value;
          });
          
          currentHtml = doc.documentElement.outerHTML;
        }

        res = await fetch(`${API_URL}/Signature/document/download/html`, {
          method: 'POST',
          headers: { 
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json' 
          },
          body: JSON.stringify({
             TccId: Number(tccId),
             DocumentId: Number(documentId),
             StudentId: studentId,
             HtmlContent: currentHtml
          })
        });
      } else {
        res = await fetch(
          `${API_URL}/Signature/document/download?tccId=${tccId}&documentId=${documentId}&studentId=${studentId}`,
          {
            headers: { Authorization: `Bearer ${token}` }
          }
        );
      }

      if (!res.ok) throw new Error('Erro ao baixar o documento.');

      let filename = docNameFromParams || documentName;

      if (!filename) {
        const contentDisposition = res.headers.get('Content-Disposition');
        if (contentDisposition) {
          const filenameMatch =
            contentDisposition.match(/filename="?([^"]+)"?/);
          if (filenameMatch && filenameMatch[1]) {
            filename = decodeURIComponent(filenameMatch[1]);
          }
        }
      }

      if (!filename) {
        filename = 'documento.pdf';
      }

      if (!filename.toLowerCase().endsWith('.pdf')) {
        filename += '.pdf';
      }

      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;

      link.setAttribute('download', filename);

      document.body.appendChild(link);
      link.click();
      link.parentNode?.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch {
      toast.error('Não foi possível baixar o documento.');
    }
  };

  const handleSignDocument = async () => {
    if (!selectedFile) {
      toast.warn('Por favor, selecione um arquivo para assinar.');
      return;
    }
    setIsSubmitting(true);
    const token = Cookies.get('token');

    let userId = '';
    try {
      userId = jwtDecode<DecodedToken>(token!).userId;
    } catch {
      toast.error('Erro de autenticação.');
      setIsSubmitting(false);
      return;
    }

    const formData = new FormData();
    formData.append('File', selectedFile);
    formData.append('TccId', tccId!);
    formData.append('DocumentId', documentId);
    formData.append('UserId', userId);

    try {
      const res = await fetch(`${API_URL}/Signature`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
        body: formData
      });
      if (!res.ok) throw new Error('Erro ao submeter a assinatura.');
      push('/pendingSignatures');
      toast.success('Documento assinado e enviado com sucesso!');
    } catch {
      toast.error('Ocorreu um erro ao submeter sua assinatura.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    documentId,
    tccId,
    documentUrl,
    documentHtml,
    documentName,
    isLoading,
    isSubmitting,
    selectedFile,
    setSelectedFile,
    handleSignDocument,
    handleDownloadDocument,
    API_URL,
    iframeRef
  };
}
