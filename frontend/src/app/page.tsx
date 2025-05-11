'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { faLock, faUser } from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../public/login image.svg';
import IFPELogo from '../../public/IFPE Logo.png';
import WellcomeImage from '../../public/wellcome.png';
import { Checkbox } from '@/components/ui/checkbox';
import { Alert } from '@/components/AlertModal';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showAlert, setShowAlert] = useState(false);

  const handleLogin = async () => {
    setShowAlert(true);
  };

  const handleCloseAlert = () => {
    setShowAlert(false);
  };

  return (
    <div className="flex flex-col lg:flex-row items-center justify-center lg:justify-between max-h-screen py-40 px-6 lg:px-10">
      {/* image */}
      <div className="sm:w-2/3">
        <Image src={LoginImage} alt="Login Image" className="w-full h-auto" />
      </div>

      {/* content */}
      <div className="w-full lg:w-1/3 flex flex-col items-center justify-center gap-10">
        <div className="flex flex-col w-full gap-6">
          <h1 className="text-2xl lg:text-4xl font-medium text-center mb-6">
            Acesso à Gestão de TCCs
          </h1>
          <div className="flex flex-col gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="email">
                Email
              </Label>
              <Input
                placeholder="Email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                icon={faUser}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="password">
                Senha
              </Label>
              <Input
                placeholder="Senha"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                icon={faLock}
                isPassword={true}
              />
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <Checkbox id="terms" />
                <label
                  htmlFor="terms"
                  className="text-sm font-regular leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                >
                  Manter-me conectado
                </label>
              </div>
              <Link
                href="/"
                className="text-[#1351B4] text-xs font-semibold underline"
              >
                Esqueceu a senha?
              </Link>
            </div>
            <Button onClick={handleLogin}>Entrar</Button>
            <Button variant={'ghost'}>Primeiro acesso?</Button>
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

      {/* Alert */}
      {showAlert && (
        <Alert
          imageUrl={WellcomeImage.src}
          title="Bem vindo ao sistema de TCCs"
          showCloseButton={true}
          closeButtonText="Entrar"
          onClose={handleCloseAlert}
        />
      )}
    </div>
  );
}
