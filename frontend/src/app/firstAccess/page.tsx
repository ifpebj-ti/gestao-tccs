'use client';

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
import { useVerifyTccCode } from '@/app/hooks/useVerifyTccCode';

export default function FirstAccess() {
  const { form, submitForm } = useVerifyTccCode();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = form;

  const handleRedirectToLogin = () => {
    window.location.href = '/';
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
          <form
            className="flex flex-col gap-4"
            onSubmit={handleSubmit(submitForm)}
          >
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="userEmail">
                Email
              </Label>
              <Input
                placeholder="Digite seu email institucional"
                icon={faEnvelope}
                errorText={errors.userEmail?.message?.toString()}
                {...register('userEmail')}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="accessCode">
                Código
              </Label>
              <Input
                placeholder="Digite o código"
                icon={faTag}
                helperText="Solicite o código ao coordenador ou orientador"
                errorText={errors.code?.message?.toString()}
                {...register('code')}
              />
            </div>
            <Button type="submit">
              {isSubmitting ? 'Carregando...' : 'Continuar'}
            </Button>
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
