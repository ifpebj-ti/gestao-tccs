import { Suspense } from 'react';
import { PageState } from '@/components/layout/PageState';
import SignaturesClient from './SignaturesClient';

export default function SignaturesPage() {
  return (
    <Suspense
      fallback={<PageState loading>Carregando assinaturas do TCC...</PageState>}
    >
      <SignaturesClient />
    </Suspense>
  );
}
