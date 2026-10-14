import { Suspense } from 'react';
import { TccFiles } from '@/components/tcc-details/TccFiles';
import { PageState } from '@/components/layout/PageState';

export default function TccFilesPage() {
  return (
    <Suspense fallback={<PageState loading>Carregando arquivos...</PageState>}>
      <TccFiles />
    </Suspense>
  );
}
