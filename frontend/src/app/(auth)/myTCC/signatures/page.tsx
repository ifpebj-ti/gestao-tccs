'use client';

import ReusableSignaturesPage from '@/app/(auth)/ongoingTCCs/signatures/page';
import { Suspense } from 'react';

export default function MeuTccSignaturesPage() {
  return (
    <Suspense>
      <ReusableSignaturesPage />
    </Suspense>
  );
}
