'use client';

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
import { useNewUserForm } from '@/app/hooks/useNewUser';
import { useRouter } from 'next/navigation';

export default function AutoRegister() {
  const { push } = useRouter();
  const { errors, handleSubmit, register, submitForm, isSubmitting } =
    useNewUserForm();

  const handleRedirectToLogin = () => {
    push('/');
  };

  const formatCPF = (value: string) => {
    const numericValue = value.replace(/\D/g, '');
    const limitedValue = numericValue.slice(0, 11);
    let formattedValue = limitedValue;
    if (limitedValue.length > 3) {
      formattedValue = formattedValue.replace(/(\d{3})(\d)/, '$1.$2');
    }
    if (limitedValue.length > 6) {
      formattedValue = formattedValue.replace(
        /(\d{3})\.(\d{3})(\d)/,
        '$1.$2.$3'
      );
    }
    if (limitedValue.length > 9) {
      formattedValue = formattedValue.replace(
        /(\d{3})\.(\d{3})\.(\d{3})(\d{1,2})/,
        '$1.$2.$3-$4'
      );
    }
    return formattedValue;
  };

  const { onChange: onCpfChange, ...cpfRest } = register('cpf');

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
          <form
            className="flex flex-col gap-4"
            onSubmit={handleSubmit((data) =>
              submitForm({ ...data, profile: 'STUDENT' })
            )}
          >
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="name">
                Nome Completo
              </Label>
              <Input
                placeholder="Digite seu nome completo"
                icon={faUser}
                errorText={errors.name?.message?.toString()}
                {...register('name')}
              />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="email">
                Email discente
              </Label>
              <Input
                placeholder="Digite seu email"
                icon={faEnvelope}
                errorText={errors.email?.message?.toString()}
                {...register('email')}
              />
            </div>
            {/* campo matrícula */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="registration">
                Matrícula
              </Label>
              <Input
                placeholder="Digite sua matrícula"
                icon={faGraduationCap}
                errorText={errors.registration?.message?.toString()}
                {...register('registration')}
              />
            </div>
            {/* campo CPF */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="cpf">
                CPF
              </Label>
              <Input
                placeholder="Digite seu CPF"
                icon={faAddressCard}
                errorText={errors.cpf?.message?.toString()}
                maxLength={14}
                {...cpfRest}
                onChange={(e) => {
                  e.target.value = formatCPF(e.target.value);
                  onCpfChange(e);
                }}
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
