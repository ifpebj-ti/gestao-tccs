'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  faArrowLeft,
  faEnvelope,
  faTag,
  faPaperPlane,
  faCheckCircle,
  faRotateRight,
  faPen
} from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import Image from 'next/image';
import LoginImage from '../../../public/login image.svg';
import IFPELogo from '../../../public/IFPE Logo.png';
import { useEffect, useState, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { useVerifyTccCode } from '@/app/hooks/useVerifyTccCode';

function FirstAccessContent() {
  const searchParams = useSearchParams();
  const { form, submitForm, sendCode, isSendingCode } = useVerifyTccCode();
  const [step, setStep] = useState<'send' | 'verify'>('send');

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting }
  } = form;

  const userEmail = watch('userEmail');

  useEffect(() => {
    const emailParam = searchParams.get('email');
    const codeParam = searchParams.get('code');

    if (emailParam) {
      setValue('userEmail', emailParam, { shouldValidate: true });
    }
    if (codeParam) {
      setValue('code', codeParam, { shouldValidate: true });
      setStep('verify');
    }
  }, [searchParams, setValue]);

  const handleSendCode = async () => {
    const success = await sendCode(userEmail);
    if (success) {
      setStep('verify');
    }
  };

  const handleRedirectToLogin = () => {
    window.location.href = '/';
  };

  return (
    <div className="flex flex-col lg:flex-row items-center justify-between min-h-screen lg:max-h-screen py-10 lg:py-40 px-6 lg:px-10">
      {/* image */}
      <Image src={LoginImage} alt="Login Image" className="w-full md:w-2/3" priority />

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

          <div>
            <h1 className="text-2xl lg:text-4xl font-medium mt-2 mb-1">
              Primeiro acesso
            </h1>
            <p className="text-sm text-gray-500">
              {step === 'send'
                ? 'Informe seu e-mail institucional para receber um código de acesso.'
                : 'Insira o código de 6 caracteres enviado para o seu e-mail.'}
            </p>
          </div>

          {step === 'send' ? (
            <div className="flex flex-col gap-4">
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold" htmlFor="userEmail">
                  E-mail institucional
                </Label>
                <Input
                  placeholder="exemplo@discente.ifpe.edu.br"
                  icon={faEnvelope}
                  errorText={errors.userEmail?.message?.toString()}
                  {...register('userEmail')}
                />
                <span className="text-xs text-gray-500">
                  O e-mail deve pertencer ao domínio @discente.ifpe.edu.br
                </span>
              </div>

              <Button
                type="button"
                icon={faPaperPlane}
                disabled={isSendingCode}
                onClick={handleSendCode}
                className="mt-2"
              >
                {isSendingCode ? 'Enviando código...' : 'Receber código por e-mail'}
              </Button>

              <div className="text-center mt-2">
                <button
                  type="button"
                  onClick={() => setStep('verify')}
                  className="text-sm text-blue-600 font-semibold underline hover:text-blue-800"
                >
                  Já possuo um código de verificação
                </button>
              </div>
            </div>
          ) : (
            <form
              className="flex flex-col gap-4"
              onSubmit={handleSubmit(submitForm)}
            >
              {/* Badge com o e-mail em validação */}
              <div className="flex items-center justify-between p-3 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-900">
                <div className="flex items-center gap-2 truncate">
                  <FontAwesomeIcon icon={faEnvelope} className="text-blue-600" />
                  <span className="font-medium truncate">{userEmail}</span>
                </div>
                <button
                  type="button"
                  onClick={() => setStep('send')}
                  className="text-xs text-blue-700 hover:text-blue-900 font-semibold flex items-center gap-1 shrink-0 ml-2"
                >
                  <FontAwesomeIcon icon={faPen} className="text-[10px]" />
                  Alterar
                </button>
              </div>

              <div className="grid items-center gap-1.5">
                <Label className="font-semibold" htmlFor="accessCode">
                  Código de verificação
                </Label>
                <Input
                  placeholder="Ex: ABC123"
                  icon={faTag}
                  maxLength={6}
                  className="uppercase tracking-widest font-mono text-center text-lg"
                  helperText="Verifique também sua caixa de spam ou lixo eletrônico."
                  errorText={errors.code?.message?.toString()}
                  {...register('code')}
                />
              </div>

              <Button
                type="submit"
                icon={faCheckCircle}
                disabled={isSubmitting}
                className="mt-2"
              >
                {isSubmitting ? 'Confirmando...' : 'Confirmar código e continuar'}
              </Button>

              <div className="flex items-center justify-between text-xs text-gray-500 mt-2">
                <span>Não recebeu o e-mail?</span>
                <button
                  type="button"
                  disabled={isSendingCode}
                  onClick={handleSendCode}
                  className="text-blue-600 font-semibold hover:text-blue-800 underline flex items-center gap-1"
                >
                  <FontAwesomeIcon icon={faRotateRight} className={isSendingCode ? 'animate-spin' : ''} />
                  {isSendingCode ? 'Reenviando...' : 'Reenviar código'}
                </button>
              </div>
            </form>
          )}

          <Button
            icon={faArrowLeft}
            className="lg:hidden block mt-4"
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

export default function FirstAccess() {
  return (
    <Suspense fallback={null}>
      <FirstAccessContent />
    </Suspense>
  );
}
