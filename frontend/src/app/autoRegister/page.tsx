'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  faArrowLeft,
  faUser,
  faEnvelope,
  faPhone
} from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../../public/login image.svg';
import IFPELogo from '../../../public/IFPE Logo.png';

export default function AutoRegister() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');

  const handleRedirectToLogin = () => {
    window.location.href = '/';
  };

  return (
    <div className="flex flex-col lg:flex-row items-center justify-center lg:justify-between max-h-screen py-40 px-6 lg:px-10">
      {/* image */}
      <Image src={LoginImage} alt="Login Image" className="w-full sm:w-2/3" />

      {/* content */}
      <div className="w-full lg:w-1/3 flex flex-col items-center justify-center gap-10">
        <div className="flex flex-col w-full gap-6">
          <Button
            icon={faArrowLeft}
            className="w-min"
            onClick={handleRedirectToLogin}
            variant={'ghost'}
          >
            Voltar para Login
          </Button>
          <h1 className="text-2xl lg:text-4xl font-medium mb-6">Cadastro</h1>
          <div className="flex flex-col gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="fullName">
                Nome Completo
              </Label>
              <Input
                placeholder="Digite seu nome completo"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                icon={faUser}
              />
            </div>

            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="email">
                Email
              </Label>
              <Input
                placeholder="Digite seu email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                icon={faEnvelope}
              />
            </div>

            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="phone">
                Telefone
              </Label>
              <Input
                placeholder="Digite seu telefone"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                icon={faPhone}
              />
            </div>

            <Button>Continuar</Button>
          </div>
        </div>

        {/* footer */}
        <div className="flex items-center justify-between w-full">
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
    </div>
  );
}
