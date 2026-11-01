'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { Download, FileCheck2, FolderCheck, LoaderCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { PageState } from '@/components/layout/PageState';
import { TccOverview } from './TccOverview';

type SignedFile = {
  id: number;
  name: string;
  student: string | null;
  signatureCount: number;
  lastSignature: string;
};
type Details = {
  infoTcc: { title: string; status?: string };
  infoStudent: { name: string }[];
  cancellationRequest: boolean;
};

export function TccFiles() {
  const id = useSearchParams().get('id');
  const [files, setFiles] = useState<SignedFile[] | null>(null);
  const [details, setDetails] = useState<Details | null>(null);
  const [error, setError] = useState('');
  const [retry, setRetry] = useState(0);
  const [downloading, setDownloading] = useState<number | 'all' | null>(null);
  const [downloadError, setDownloadError] = useState('');
  const endpoint = `${env('NEXT_PUBLIC_API_URL')}/Tcc/${id}/files`;

  useEffect(() => {
    const controller = new AbortController();
    setFiles(null);
    setDetails(null);
    setError('');
    const headers = { Authorization: `Bearer ${Cookies.get('token')}` };
    (async () => {
      try {
        if (!id || !/^[1-9]\d*$/.test(id)) throw new Error('TCC inválido.');
        const response = await fetch(endpoint, {
          headers,
          signal: controller.signal
        });
        if (!response.ok)
          throw new Error(
            response.status === 404 || response.status === 403
              ? 'TCC não encontrado ou acesso indisponível.'
              : 'Não foi possível carregar os arquivos.'
          );
        const result: SignedFile[] = await response.json();
        const tcc = await fetch(
          `${env('NEXT_PUBLIC_API_URL')}/Tcc?tccId=${id}`,
          { headers, signal: controller.signal }
        );
        if (!tcc.ok)
          throw new Error('Não foi possível carregar as informações do TCC.');
        setDetails(await tcc.json());
        setFiles(result);
      } catch (failure) {
        if (!controller.signal.aborted)
          setError(
            failure instanceof Error
              ? failure.message
              : 'Não foi possível carregar os arquivos.'
          );
      }
    })();
    return () => controller.abort();
  }, [id, endpoint, retry]);

  const download = async (file: SignedFile | null) => {
    setDownloading(file?.id ?? 'all');
    setDownloadError('');
    try {
      const response = await fetch(
        `${endpoint}/${file ? `${file.id}/` : ''}download`,
        { headers: { Authorization: `Bearer ${Cookies.get('token')}` } }
      );
      if (!response.ok)
        throw new Error(
          response.status === 413
            ? 'O conjunto excede 50 MB. Baixe os documentos individualmente.'
            : 'Não foi possível baixar o arquivo. Tente novamente.'
        );
      const url = URL.createObjectURL(await response.blob());
      const link = document.createElement('a');
      const extension = response.headers
        .get('Content-Disposition')
        ?.includes('.html')
        ? 'html'
        : 'pdf';
      link.href = url;
      link.download = file
        ? `Documento_${file.id}.${extension}`
        : `TCC_${id}_assinados.zip`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch (failure) {
      setDownloadError(
        failure instanceof Error ? failure.message : 'Erro ao baixar arquivo.'
      );
    } finally {
      setDownloading(null);
    }
  };

  if (error)
    return (
      <div className="space-y-4">
        <PageState>{error}</PageState>
        <Button variant="brand" onClick={() => setRetry(retry + 1)}>
          Tentar novamente
        </Button>
      </div>
    );
  if (!files || !details)
    return <PageState loading>Carregando documentos assinados...</PageState>;
  return (
    <>
      <TccOverview
        title={details.infoTcc.title}
        cancellationRequested={details.cancellationRequest}
        step={details.infoTcc.status === 'COMPLETED' ? 6 : undefined}
      />
      <section aria-label="Documentos assinados">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <FolderCheck className="text-[#006633]" />
            <div>
              <h2 className="font-semibold text-[#002B18]">
                Documentos assinados
              </h2>
              <p className="mt-1 text-sm text-gray-500">
                {files.length} documento(s) com assinaturas registradas
              </p>
            </div>
          </div>
          {!!files.length && (
            <Button
              variant="brand"
              disabled={downloading !== null}
              onClick={() => download(null)}
            >
              <Download size={16} />
              {downloading === 'all' ? 'Baixando...' : 'Baixar pasta'}
            </Button>
          )}
        </div>
        {downloadError && (
          <p role="alert" className="mb-4 text-sm text-red-700">
            {downloadError}
          </p>
        )}
        {!files.length ? (
          <PageState>Nenhum documento assinado disponível.</PageState>
        ) : (
          <ul>
            {files.map((file) => (
              <li
                key={file.id}
                className="flex flex-wrap items-center gap-4 border-b border-[#ECEAE2] py-5"
              >
                <FileCheck2
                  aria-hidden="true"
                  className="shrink-0 text-[#006633]"
                />
                <div className="min-w-0 flex-1">
                  <h3 className="break-words text-sm font-semibold text-[#002B18]">
                    {file.name}
                  </h3>
                  {file.student && (
                    <p className="mt-1 text-sm text-gray-500">{file.student}</p>
                  )}
                  <p className="mt-1 text-xs text-gray-500">
                    {file.signatureCount} assinatura(s) registrada(s)
                  </p>
                </div>
                <Button
                  variant="brand"
                  size="sm"
                  disabled={downloading !== null}
                  aria-label={`Baixar ${file.name}`}
                  title={`Baixar ${file.name}`}
                  className="size-9 shrink-0 px-0"
                  aria-busy={downloading === file.id}
                  onClick={() => download(file)}
                >
                  {downloading === file.id ? (
                    <LoaderCircle
                      size={16}
                      className="animate-spin motion-reduce:animate-none"
                    />
                  ) : (
                    <Download size={16} />
                  )}
                </Button>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}
