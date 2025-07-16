'use client';

import { useState, useEffect } from 'react';
import { useParams, useSearchParams } from 'next/navigation';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { env } from 'next-runtime-env';

interface DecodedToken {
  userId: string;
}

export function useSignaturePage() {
  const [documentUrl, setDocumentUrl] = useState<string | null>(null);
  const [documentName, setDocumentName] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const params = useParams();
  const searchParams = useSearchParams();

  const documentId = params.documentId as string;
  const tccId = searchParams.get('tccId');
  const API_URL = env('NEXT_PUBLIC_API_URL');

  useEffect(() => {
    if (!documentId || !tccId) return;

    const fetchDocument = async () => {
      setIsLoading(true);
      const token = Cookies.get('token');
      try {
        const res = await fetch(`${API_URL}/Signature/document?tccId=${tccId}&documentId=${documentId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (!res.ok) throw new Error('Documento não encontrado ou link expirado.');
        const data = await res.json();
        setDocumentUrl(data.url);
        setDocumentName(data.documentName);
      } catch {
        toast.error('Erro ao carregar o documento. O link pode ter expirado.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchDocument();
  }, [documentId, tccId, API_URL]);

  const handleSignDocument = async () => {
    if (!selectedFile) {
      toast.warn('Por favor, selecione um arquivo para assinar.');
      return;
    }
    setIsSubmitting(true);
    const token = Cookies.get('token');
    const userId = jwtDecode<DecodedToken>(token!).userId;

    const formData = new FormData();
    formData.append('File', selectedFile);
    formData.append('TccId', tccId!);
    formData.append('DocumentId', documentId);
    formData.append('UserId', userId);

    try {
        const res = await fetch(`${API_URL}/Signature`, {
            method: 'POST',
            headers: { Authorization: `Bearer ${token}` },
            body: formData,
        });
        if(!res.ok) throw new Error('Erro ao submeter a assinatura.');
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
    documentName,
    isLoading,
    isSubmitting,
    selectedFile,
    setSelectedFile,
    handleSignDocument,
    API_URL,
  };
}