// app/not-found.tsx
'use client';

import { Button } from '@/components/ui/button';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import notFoundImage from '../../public/404-art.svg';
import Image from 'next/image';

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
    <div className="flex flex-col items-center justify-center h-screen text-center p-4 gap-4">
      <Image alt="Not Found" src={notFoundImage} className="h-1/2 mb-6" />
      <p className="text-xl mt-4 text-gray-700">Página não encontrada</p>
      <Button onClick={handleRedirect}>Voltar</Button>
    </div>
  );
}
