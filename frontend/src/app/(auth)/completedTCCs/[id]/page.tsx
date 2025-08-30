import { Suspense } from 'react';
import CompletedTccDetailsClient from './DetailsClient';

export default function CompletedTccDetailsPage() {
  return (
    <Suspense fallback={<div className="p-4 text-center">Carregando...</div>}>
      <CompletedTccDetailsClient />
    </Suspense>
  );
}
