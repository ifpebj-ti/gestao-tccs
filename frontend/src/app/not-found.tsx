// app/not-found.tsx
'use client';

import { Button } from '@/components/ui/button';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { AccessPage } from '@/components/layout/AccessPage';

export default function NotFound() {
  const { push } = useRouter();

  const handleRedirect = () => {
    if (Cookies.get('token')) {
      push('/homePage');
    } else {
      push('/');
    }
  };
  return (
    <AccessPage code="404" title="Página não encontrada" description="Não encontramos a página que você procura. Volte ao início para continuar sua jornada.">
      <Button onClick={handleRedirect}>Voltar</Button>
    </AccessPage>
  );
}
