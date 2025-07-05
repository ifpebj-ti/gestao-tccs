import { Suspense } from 'react';
import SignaturesClient from './SignaturesClient';

export default function SignaturesPage() {
  return (
    <Suspense
      fallback={<p className="p-4">Carregando assinaturas do TCC...</p>}
    >
      <SignaturesClient />
    </Suspense>
  );
}
