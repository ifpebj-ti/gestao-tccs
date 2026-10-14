import { Suspense } from 'react';
import { PageState } from '@/components/layout/PageState';
import DetailsClient from './DetailsClient';

export default function DetailsPage() {
  return (
    <Suspense fallback={<PageState loading>Carregando detalhes do TCC...</PageState>}>
      <DetailsClient />
    </Suspense>
  );
}
