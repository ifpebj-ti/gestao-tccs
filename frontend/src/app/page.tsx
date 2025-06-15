'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { faEnvelope, faLock } from '@fortawesome/free-solid-svg-icons';
import Image from 'next/image';
import LoginImage from '../../public/login image.svg';
import IFPELogo from '../../public/IFPE Logo.png';
import { Checkbox } from '@/components/ui/checkbox';
import { useLogin } from '@/app/hooks/useLogin';

export default function Login() {
  const { form, submitForm } = useLogin();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = form;

  const handleRedirectToFirstAccess = () => {
    window.location.href = '/firstAccess';
  };

  return (
    <div className="flex flex-col lg:flex-row items-center justify-between min-h-screen lg:max-h-screen py-10 lg:py-40 px-6 lg:px-10">
      {/* image */}
      <Image src={LoginImage} alt="Login Image" className="w-full md:w-2/3" />

      {/* content */}
      <div className="w-full lg:w-1/3 flex flex-col justify-between gap-10">
        <div className="flex flex-col w-full gap-6">
          <h1 className="text-2xl lg:text-4xl font-medium my-6">
            Acesso à Gestão de TCCs
          </h1>
          {/* form */}
          <form
            className="flex flex-col gap-4"
            onSubmit={handleSubmit(submitForm)}
          >
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="email">
                Email
              </Label>
              <Input
                placeholder="Digite seu email"
                icon={faEnvelope}
                errorText={errors.email?.message?.toString()}
                {...register('email')}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="password">
                Senha
              </Label>
              <Input
                placeholder="Digite sua senha"
                icon={faLock}
                isPassword={true}
                errorText={errors.password?.message?.toString()}
                {...register('password')}
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
                href="/forgotPassword"
                className="text-[#1351B4] text-xs font-semibold underline"
              >
                Esqueceu a senha?
              </Link>
            </div>
            <Button type="submit">
              {isSubmitting ? 'Carregando...' : 'Entrar'}
            </Button>
          </form>
          <Button onClick={handleRedirectToFirstAccess} variant={'ghost'}>
            Primeiro acesso?
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
