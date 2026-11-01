'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import Step from '@/components/Steps';
import TccTabs from '@/components/TccTabs';
import { TccHeader } from './TccHeader';

type Props = {
  title?: string;
  tccId?: string;
  cancellationRequested?: boolean;
  step?: number | null;
};

export function TccOverview({
  title,
  tccId,
  cancellationRequested = false,
  step
}: Props) {
  const searchId = useSearchParams().get('id');
  const id = tccId ?? searchId;
  const [loadedStep, setLoadedStep] = useState<number | null>(null);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    if (step !== undefined || !id) return;
    const controller = new AbortController();
    setLoadedStep(null);
    setFailed(false);
    fetch(`${env('NEXT_PUBLIC_API_URL')}/Tcc/workflow?tccId=${id}`, {
      headers: { Authorization: `Bearer ${Cookies.get('token')}` },
      signal: controller.signal
    })
      .then(async (response) => {
        if (!response.ok) throw new Error();
        return response.json();
      })
      .then((data) => {
        if (!Number.isInteger(data.step) || data.step < 1 || data.step > 6)
          throw new Error();
        setLoadedStep(data.step);
      })
      .catch(() => {
        if (!controller.signal.aborted) setFailed(true);
      });
    return () => controller.abort();
  }, [id, step]);
  const current = step ?? loadedStep;
  return (
    <>
      <TccHeader title={title} cancellationRequested={cancellationRequested} />
      {current ? (
        <Step currentStep={current} />
      ) : (
        <p role="status" className="my-5 text-sm text-gray-500">
          {failed
            ? 'Etapa indisponível no momento.'
            : 'Consultando etapa do TCC...'}
        </p>
      )}
      <TccTabs tccId={id ?? undefined} />
    </>
  );
}
