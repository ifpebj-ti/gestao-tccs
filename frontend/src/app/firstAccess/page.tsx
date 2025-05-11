'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { faArrowLeft, faTag } from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../../public/login image.svg';
import IFPELogo from '../../../public/IFPE Logo.png';

export default function FirstAccess() {
  const [code, setCode] = useState('');

  const handleRedirectToLogin = () => {
    window.location.href = '/';
  };

  const handleRedirectToAutoRegister = () => {
    window.location.href = '/autoRegister';
  };

  return (
    <div className="flex flex-col lg:flex-row items-center justify-between min-h-screen lg:max-h-screen py-10 lg:py-40 px-6 lg:px-10">
      {/* image */}
      <Image src={LoginImage} alt="Login Image" className="w-full md:w-2/3" />

      {/* content */}
      <div className="w-full lg:w-1/3 flex flex-col justify-between gap-10">
        <div className="flex flex-col w-full gap-6">
          <Button
            icon={faArrowLeft}
            className="w-min lg:block hidden"
            onClick={handleRedirectToLogin}
            variant={'ghost'}
          >
            Voltar para Login
          </Button>
          <h1 className="text-2xl lg:text-4xl font-medium my-6">
            Primeiro acesso
          </h1>
          <div className="flex flex-col gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="code">
                Código
              </Label>
              <Input
                placeholder="Digite o código"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                icon={faTag}
                helperText="Solicite o código ao coordenador ou orientador"
              />
            </div>
            <Button onClick={handleRedirectToAutoRegister}>Continuar</Button>
            <Button
              icon={faArrowLeft}
              className="lg:hidden block"
              onClick={handleRedirectToLogin}
              variant={'ghost'}
            >
              Voltar para Login
            </Button>
          </div>
        </div>
        {/* footer desktop */}
        <div className="lg:flex hidden items-center justify-between w-full">
          <Image
            src={IFPELogo}
            alt="Logo IFPE"
            className="w-32 lg:w-40 h-auto"
          />
          <Link
            href="/"
            className="text-[#1351B4] text-xs font-semibold underline"
          >
            Precisa de ajuda?
          </Link>
        </div>
      </div>
      {/* footer mobile */}
      <div className="flex lg:hidden items-center justify-between w-full">
        <Image src={IFPELogo} alt="Logo IFPE" className="w-32 lg:w-40 h-auto" />
        <Link
          href="/"
          className="text-[#1351B4] text-xs font-semibold underline"
        >
          Precisa de ajuda?
        </Link>
      </div>
    </div>
  );
}
