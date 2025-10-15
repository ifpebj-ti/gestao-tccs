'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
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
import { useResendAccessCode } from '@/app/hooks/useResendAccessCode';
import { useVerifyAccessCode } from '@/app/hooks/useVerifyAccessCode';
import { toast } from 'react-toastify';

export default function ForgotPassword() {
  const [isCodeSent, setIsCodeSent] = useState(false);

  const { register, handleSubmit, reset } = useForm<{
    userEmail: string;
    accessCode?: string;
  }>();

  const { submitForm: resendSubmit, isSubmitting: isResending } =
    useResendAccessCode();
  const { submitForm: verifySubmit, isSubmitting: isVerifying } =
    useVerifyAccessCode();

  const onSubmit = async (data: { userEmail: string; accessCode?: string }) => {
    if (!isCodeSent) {
      // Resend code
      try {
        const success = await resendSubmit({ userEmail: data.userEmail });
        if (success) {
          setIsCodeSent(true);
        } else {
          setIsCodeSent(false);
        }
      } catch {
        toast.error('Erro ao enviar o código');
      }
    } else {
      // Verify code
      try {
        await verifySubmit({
          userEmail: data.userEmail,
          accessCode: data.accessCode!
        });
      } catch {
        toast.error('Erro ao verificar o código');
      }
    }
  };

  const handleChangeEmail = () => {
    reset();
    setIsCodeSent(false);
  };

  const handleRedirectToLogin = () => {
    window.location.href = '/';
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
          <form
            onSubmit={handleSubmit(onSubmit)}
            className="grid items-center gap-4"
          >
            <Label htmlFor="email" className="font-semibold">
              Email
            </Label>
            <Input
              placeholder="Digite seu email"
              icon={faEnvelope}
              {...register('userEmail', { required: true })}
              readOnly={isCodeSent}
              className={isCodeSent ? 'cursor-not-allowed select-none' : ''}
            />
            {isCodeSent && (
              <>
                <Label htmlFor="accessCode" className="font-semibold">
                  Código
                </Label>
                <Input
                  placeholder="Digite o código"
                  icon={faTag}
                  {...register('accessCode', { required: true })}
                />
              </>
            )}
            <div className="flex items-center gap-2">
              {isCodeSent && (
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleChangeEmail}
                  className="w-full"
                >
                  Modificar email
                </Button>
              )}
              <Button type="submit" className="w-full">
                {isCodeSent
                  ? isVerifying
                    ? 'Verificando...'
                    : 'Verificar Código'
                  : isResending
                    ? 'Enviando...'
                    : 'Enviar Código'}
              </Button>
            </div>
          </form>
          <Button
            icon={faArrowLeft}
            className="lg:hidden block"
            onClick={handleRedirectToLogin}
            variant={'ghost'}
          >
            Voltar para Login
          </Button>
        </div>
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
