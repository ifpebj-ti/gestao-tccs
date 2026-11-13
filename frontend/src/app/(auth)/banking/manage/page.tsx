import { Suspense } from 'react';
import { PageState } from '@/components/layout/PageState';
import BankingManagementClient from './BankingManagementClient';

export default function BankingManagementPage() {
  return (
    <Suspense fallback={<PageState loading>Carregando banca...</PageState>}>
      <BankingManagementClient />
    </Suspense>
  );
}
