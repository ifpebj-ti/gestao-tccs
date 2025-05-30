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
  faGraduationCap,
  faAddressCard
} from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../../public/login image.svg';
import IFPELogo from '../../../public/IFPE Logo.png';

export default function AutoRegister() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [registration, setRegistration] = useState('');
  const [cpf, setCpf] = useState('');

  const handleRedirectToLogin = () => {
    window.location.href = '/';
  };

  const handleRedirectToNewPassword = () => {
    window.location.href = '/newPassword';
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
          <h1 className="text-2xl lg:text-4xl font-medium my-6">Cadastro</h1>
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
                Email discente
              </Label>
              <Input
                placeholder="Digite seu email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                icon={faEnvelope}
              />
            </div>
            {/* campo matrícula */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="registration">
                Matrícula
              </Label>
              <Input
                placeholder="Digite sua matrícula"
                value={registration}
                onChange={(e) => setRegistration(e.target.value)}
                icon={faGraduationCap}
              />
            </div>
            {/* campo CPF */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="cpf">
                CPF
              </Label>
              <Input
                placeholder="Digite seu CPF"
                value={cpf}
                onChange={(e) => setCpf(e.target.value)}
                icon={faAddressCard}
              />
            </div>
            <Button onClick={handleRedirectToNewPassword}>Continuar</Button>
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
