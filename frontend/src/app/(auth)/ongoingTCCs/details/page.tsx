'use client';
import TccTabs from '@/components/TccTabs';
import { Suspense } from 'react';

export default function TCCDetails() {
  return (
    <div>
      <Suspense fallback={null}>
        <TccTabs />
      </Suspense>{' '}
      Detalhes do TCC
    </div>
  );
}
