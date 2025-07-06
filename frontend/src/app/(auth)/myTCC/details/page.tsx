'use client';

import ReusableTccDetailsPage from '@/app/(auth)/ongoingTCCs/details/page';
import { Suspense } from 'react';

export default function MeuTccDetailsPage() {
  return (
    <Suspense>
      <ReusableTccDetailsPage />
    </Suspense>
  );
}
