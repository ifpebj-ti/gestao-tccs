import { Suspense } from 'react';
import DetailsClient from './DetailsClient';

export default function DetailsPage() {
  return (
    <Suspense fallback={<p className="p-4">Carregando detalhes do TCC...</p>}>
      <DetailsClient />
    </Suspense>
  );
}
