'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  faArrowLeft,
  faEnvelope,
  faTag
} from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../../public/login image.svg';
import IFPELogo from '../../../public/IFPE Logo.png';
import { Alert } from '@/components/AlertModal';

export default function ForgotPassword() {
  const [isCodeSent, setIsCodeSent] = useState(false);
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
  const [showAlert, setShowAlert] = useState(false);

  const handleRedirectToLogin = () => {
    window.location.href = '/';
  };

  const handleRedirectToNewPassword = () => {
    window.location.href = '/newPassword';
  };

  const handleSendCode = () => {
    setIsCodeSent(true);
  };

  const handleChangeEmail = () => {
    setIsCodeSent(false);
    setEmail('');
  };

  const handleAlert = () => {
    setShowAlert(true);
  };

  const handleCloseAlert = () => {
    setShowAlert(false);
    handleRedirectToNewPassword();
  };

  return (
    <div className="flex flex-col lg:flex-row items-center justify-center lg:justify-between max-h-screen py-40 px-5 lg:px-10">
      {/* image */}
      <Image src={LoginImage} alt="Login Image" className="w-full sm:w-2/3" />

      {/* content */}
      <div className="w-full lg:w-1/3 flex flex-col items-center justify-center gap-10">
        <div className="flex flex-col w-full gap-5">
          <Button
            icon={faArrowLeft}
            className="w-min lg:block hidden"
            onClick={handleRedirectToLogin}
            variant={'ghost'}
          >
            Voltar para Login
          </Button>
          <h1 className="text-2xl lg:text-4xl font-medium my-6">
            Recuperar Senha
          </h1>
          <div className="flex flex-col">
            {!isCodeSent ? (
              <div className="grid items-center gap-4">
                <Label className="font-semibold" htmlFor="email">
                  Email
                </Label>
                <Input
                  placeholder="Digite seu email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  icon={faEnvelope}
                  helperText="Um código de verificação será enviado para o seu email"
                />
                <Button onClick={handleSendCode}>Enviar código</Button>
              </div>
            ) : (
              <div className="grid items-center gap-4">
                <Label className="font-semibold" htmlFor="code">
                  Código
                </Label>
                <Input
                  placeholder="Digite o código"
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  icon={faTag}
                />
                <div className="flex items-center gap-2">
                  <Button
                    onClick={handleChangeEmail}
                    variant={'outline'}
                    className="w-full"
                  >
                    Modificar email
                  </Button>
                  <Button onClick={handleAlert} className="w-full">
                    Verificar
                  </Button>
                </div>
              </div>
            )}
          </div>
          <Button
            icon={faArrowLeft}
            className="lg:hidden block"
            onClick={handleRedirectToLogin}
            variant={'ghost'}
          >
            Voltar para Login
          </Button>
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
          title="Sucesso!"
          description="Agora você pode cadastrar sua nova senha."
          closeButtonText="Cadastrar nova senha"
          onClose={handleCloseAlert}
        />
      )}
    </div>
  );
}
