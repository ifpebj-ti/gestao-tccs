// app/unauthorized/page.tsx
'use client';

import { Button } from '@/components/ui/button';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { AccessPage } from '@/components/layout/AccessPage';

export default function Unauthorized() {
  const { push } = useRouter();

  const handleRedirect = () => {
    if (Cookies.get('token')) {
      push('/homePage');
    } else {
      push('/');
    }
  };

  return (
    <AccessPage code="403" title="Acesso restrito" description="Você não tem permissão para acessar essa página. Volte ao início para acessar as ferramentas disponíveis para seu perfil.">
      <Button onClick={handleRedirect}>Voltar</Button>
    </AccessPage>
  );
}
