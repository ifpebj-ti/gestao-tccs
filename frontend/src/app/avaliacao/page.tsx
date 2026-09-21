'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { AvaliacaoForm } from '@/components/AvaliacaoForm';

function AvaliacaoWrapper() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  
  return <AvaliacaoForm token={token} />;
}

export default function AvaliacaoPage() {
  return (
    <Suspense fallback={<div className="min-h-screen flex items-center justify-center">Carregando...</div>}>
      <AvaliacaoWrapper />
    </Suspense>
  );
}
