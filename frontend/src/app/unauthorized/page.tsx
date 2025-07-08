// app/unauthorized/page.tsx
'use client';

import { Button } from '@/components/ui/button';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import notAllowedImage from '../../../public/403-art.svg';
import Image from 'next/image';

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
    <div className="flex flex-col items-center justify-center h-screen text-center p-4 gap-4">
      <Image alt="Not Found" src={notAllowedImage} className="h-1/2 mb-6" />
      <p className="text-xl mt-4 text-gray-700">
        Você não tem permissão para acessar essa página.
      </p>
      <Button onClick={handleRedirect}>Voltar</Button>
    </div>
  );
}
