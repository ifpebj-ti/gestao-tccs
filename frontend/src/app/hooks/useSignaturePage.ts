'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';

interface DecodedToken {
  userId: string;
  role: string | string[];
}

export function useSignaturePage() {
  const { push } = useRouter();
  const [documentUrl, setDocumentUrl] = useState<string | null>(null);
  const [documentHtml, setDocumentHtml] = useState<string | null>(null);
  const [documentName, setDocumentName] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [tccFile, setTccFile] = useState<File | null>(null);
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

  const handleDownloadDocument = async (scheduleData?: { date: string, time: string, location: string }) => {
    const token = Cookies.get('token');
    if (!tccId || !documentId || !token) {
      toast.error('Informações insuficientes para realizar o download.');
      return;
    }

    try {

      let res: Response;
      
      // Sempre processar documentHtml se existir, para injetar valores e converter para PDF no backend
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
          
          // Se temos os dados do formulário externo, usamos eles diretamente!
          if (scheduleData && scheduleData.date && scheduleData.time && scheduleData.location) {
             const [year, month, day] = scheduleData.date.split('-');
             const dataFormatada = `${day}/${month}/${year}`;
             const horaFormatada = scheduleData.time.replace(':', 'h');
             
             currentHtml = currentHtml.replace(/<input[^>]*name="data_defesa"[^>]*>/i, dataFormatada);
             currentHtml = currentHtml.replace(/<input[^>]*name="hora_defesa"[^>]*>/i, horaFormatada);
             currentHtml = currentHtml.replace(/<input[^>]*name="local_defesa"[^>]*>/i, scheduleData.location);
          } else {
            // Fallback original: tenta ler as informações (Anexo VIII) diretamente do iframe
            const dataDefesaInput = doc.querySelector('input[name="data_defesa"]') as HTMLInputElement;
            const horaDefesaInput = doc.querySelector('input[name="hora_defesa"]') as HTMLInputElement;
            const localDefesaInput = doc.querySelector('input[name="local_defesa"]') as HTMLInputElement;

            if (dataDefesaInput && horaDefesaInput && localDefesaInput) {
              const dataDefesa = dataDefesaInput.value;
              const horaDefesa = horaDefesaInput.value;
              const localDefesa = localDefesaInput.value;
              
              if (dataDefesa && horaDefesa && localDefesa) {
                 
                 const [year, month, day] = dataDefesa.split('-');
                 const dataFormatada = `${day}/${month}/${year}`;
                 const horaFormatada = horaDefesa.replace(':', 'h');
                 
                 currentHtml = currentHtml.replace(/<input[^>]*name="data_defesa"[^>]*>/i, dataFormatada);
                 currentHtml = currentHtml.replace(/<input[^>]*name="hora_defesa"[^>]*>/i, horaFormatada);
                 currentHtml = currentHtml.replace(/<input[^>]*name="local_defesa"[^>]*>/i, localDefesa);

                 try {
                     await fetch(`${API_URL}/Tcc/${tccId}/schedule-info`, {
                        method: 'POST',
                        headers: { 
                          Authorization: `Bearer ${token}`,
                          'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                          scheduleDate: dataDefesa,
                          scheduleTime: horaDefesa,
                          scheduleLocation: localDefesa,
                          idTcc: Number(tccId)
                        })
                     });
                 } catch (e) {
                     console.error("Erro ao salvar infos de agendamento", e);
                 }
              }
            }
          }
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

      if (filename.toLowerCase().endsWith('.html')) {
        filename = filename.substring(0, filename.length - 5) + '.pdf';
      } else if (!filename.toLowerCase().endsWith('.pdf')) {
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

  const handleSignDocument = async (scheduleData?: { date: string, time: string, location: string }) => {
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

    if (scheduleData) {
      if (scheduleData.date) formData.append('ScheduleDate', scheduleData.date);
      if (scheduleData.time) formData.append('ScheduleTime', scheduleData.time);
      if (scheduleData.location) formData.append('ScheduleLocation', scheduleData.location);
    }

    try {
      if (tccFile) {
        const formDataTcc = new FormData();
        formDataTcc.append('file', tccFile);
        
        await fetch(`${API_URL}/Tcc/${tccId}/upload-file`, {
          method: 'POST',
          headers: { Authorization: `Bearer ${token}` },
          body: formDataTcc
        });
      }

      const res = await fetch(`${API_URL}/Signature`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
        body: formData
      });
      if (!res.ok) throw new Error('Erro ao submeter a assinatura.');
      
      if (documentName.includes("ANEXO VIII") || docNameFromParams?.includes("ANEXO VIII")) {
        push(`/ongoingTCCs/details?id=${tccId}`);
      } else {
        push('/pendingSignatures');
      }
      toast.success('Documento assinado e enviado com sucesso!');
    } catch {
      toast.error('Ocorreu um erro ao submeter sua assinatura.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const isAnexoVIII = documentName.includes("ANEXO VIII") || docNameFromParams?.includes("ANEXO VIII");
  let isAdvisor = false;
  
  if (typeof window !== 'undefined') {
    const token = Cookies.get('token');
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        const roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
        isAdvisor = roles.includes('ADVISOR');
      } catch {}
    }
  }

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
    tccFile,
    setTccFile,
    handleSignDocument,
    handleDownloadDocument,
    API_URL,
    iframeRef,
    isAnexoVIII,
    isAdvisor
  };
}
