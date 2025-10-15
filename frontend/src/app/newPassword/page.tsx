'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  faArrowLeft,
  faEnvelope,
  faLock,
  faTag
} from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../../public/login image.svg';
import IFPELogo from '../../../public/IFPE Logo.png';
import { useNewPassword } from '../hooks/useNewPassword';

export default function NewPassword() {
  const { form, submitForm, isSubmitting } = useNewPassword();
  const {
    register,
    handleSubmit,
    formState: { errors }
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
          <h1 className="text-2xl lg:text-4xl font-medium my-6">Nova senha</h1>
          <form
            className="flex flex-col gap-4"
            onSubmit={handleSubmit(submitForm)}
          >
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="email">
                Email
              </Label>
              <Input
                placeholder="Digite seu email institucional"
                icon={faEnvelope}
                errorText={errors.email?.message?.toString()}
                {...register('email')}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="inviteCode">
                Código de Convite
              </Label>
              <Input
                id="inviteCode"
                placeholder="Digite o código recebido"
                icon={faTag}
                errorText={errors.inviteCode?.message}
                {...register('inviteCode')}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="password">
                Senha
              </Label>
              <Input
                type="password"
                placeholder="Digite sua senha"
                icon={faLock}
                isPassword={true}
                errorText={errors.password?.message?.toString()}
                {...register('password')}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="confirmPassword">
                Confirmar Senha
              </Label>
              <Input
                type="password"
                placeholder="Confirme sua senha"
                icon={faLock}
                isPassword={true}
                errorText={errors.confirmPassword?.message?.toString()}
                {...register('confirmPassword')}
              />
            </div>
            <Button type="submit">
              {isSubmitting ? 'Carregando...' : 'Finalizar'}
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
        {/* footer mobile */}
      </div>
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
