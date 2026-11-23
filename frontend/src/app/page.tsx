'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Eye, EyeOff } from 'lucide-react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faGoogle } from '@fortawesome/free-brands-svg-icons';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import Cookies from 'js-cookie';
import { useLogin } from '@/app/hooks/useLogin';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';
import { persistGoogleLoginSession } from '@/lib/googleLoginSession';

export default function LoginPage() {
  const { form, submitForm, isSubmitting } = useLogin();
  const router = useRouter();
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = form;
  const [showPassword, setShowPassword] = useState(false);
  const [isGoogleSubmitting, setIsGoogleSubmitting] = useState(false);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const googleLogin = params.get('googleLogin');
    const googleError = params.get('googleError');
    if (!googleLogin && !googleError) return;

    window.history.replaceState(null, '', window.location.pathname);
    if (googleLogin === 'success') {
      const token = Cookies.get('token');
      if (!persistGoogleLoginSession(token)) {
        toast.error(
          'O Google confirmou a autenticação, mas não foi possível iniciar sua sessão no sistema. Tente novamente.'
        );
        return;
      }

      toast.success('Login com Google realizado com sucesso!');
      router.replace('/homePage');
    } else if (googleError === 'unavailable') {
      toast.error('Login com Google indisponível. Tente novamente mais tarde.');
    } else {
      toast.error('Não foi possível entrar com Google. Verifique sua conta e tente novamente.');
    }
  }, [router]);

  const startGoogleLogin = () => {
    const apiUrl = env('NEXT_PUBLIC_API_URL');
    if (!apiUrl) {
      toast.error('Login com Google indisponível. Tente novamente mais tarde.');
      return;
    }

    setIsGoogleSubmitting(true);
    window.location.assign(`${apiUrl.replace(/\/$/, '')}/Auth/google`);
  };

  return (
    <AuthLayout>
      {/* ── LEFT: Editorial Panel Reutilizável ── */}
      <AuthEditorialPanel
        title="Potencialize sua jornada acadêmica com clareza."
        description="Acompanhe cada etapa do seu trabalho, desde a proposta inicial até a defesa final em um fluxo contínuo e transparente."
        stat1Value="100%"
        stat1Label="Digital & Sem papel"
        stat2Value="Fluxo"
        stat2Label="Integrado ao Campus"
      />

      {/* ── RIGHT: Seamless Flat Form (NO CARD) ── */}
      <div className="w-full max-w-md mx-auto lg:max-w-none lg:pl-4">
        <h2
          className="text-2xl sm:text-3xl font-extrabold tracking-tight text-[#002B18] mb-6 sm:mb-8"
        >
          Faça login para continuar
        </h2>

        <form onSubmit={handleSubmit(submitForm)} noValidate style={{ width: '100%' }}>
          {/* E-mail para Docente e Discente */}
          <div style={{ marginBottom: '1.75rem' }}>
            <label
              htmlFor="login-email"
              style={{
                display: 'block',
                fontSize: '0.72rem',
                fontWeight: 700,
                color: '#374151',
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                marginBottom: '0.5rem'
              }}
            >
              E-mail institucional (docente ou discente)
            </label>
            <input
              id="login-email"
              type="email"
              placeholder="seu.email@ifpe.edu.br ou @discente.ifpe.edu.br"
              {...register('email')}
              autoComplete="email"
              style={{
                display: 'block',
                width: '100%',
                background: 'transparent',
                border: 'none',
                borderBottom: `2px solid ${errors.email ? '#EF4444' : '#002B18'}`,
                padding: '0.65rem 0',
                fontSize: '0.95rem',
                color: '#111827',
                outline: 'none',
                transition: 'border-color 0.2s ease',
                boxSizing: 'border-box',
                fontFamily: 'inherit'
              }}
            />
            {errors.email && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.email.message}
              </p>
            )}
          </div>

          {/* Senha */}
          <div style={{ marginBottom: '2.5rem' }}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '0.5rem'
              }}
            >
              <label
                htmlFor="login-password"
                style={{
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase'
                }}
              >
                Senha
              </label>
              <Link
                href="/forgotPassword"
                style={{
                  fontSize: '0.75rem',
                  color: '#6B7280',
                  textDecoration: 'none',
                  fontWeight: 500,
                  transition: 'color 0.15s'
                }}
                onMouseEnter={e => (e.currentTarget.style.color = '#002B18')}
                onMouseLeave={e => (e.currentTarget.style.color = '#6B7280')}
              >
                Esqueceu a senha?
              </Link>
            </div>
            <div style={{ position: 'relative' }}>
              <input
                id="login-password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••••••"
                {...register('password')}
                autoComplete="current-password"
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.password ? '#EF4444' : '#002B18'}`,
                  padding: '0.65rem 2.25rem 0.65rem 0',
                  fontSize: '1rem',
                  color: '#111827',
                  outline: 'none',
                  transition: 'border-color 0.2s ease',
                  boxSizing: 'border-box',
                  fontFamily: 'inherit'
                }}
              />
              <button
                type="button"
                id="toggle-password-visibility"
                aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                onClick={() => setShowPassword(p => !p)}
                style={{
                  position: 'absolute',
                  right: 0,
                  top: '50%',
                  transform: 'translateY(-50%)',
                  background: 'none',
                  border: 'none',
                  cursor: 'pointer',
                  color: '#6B7280',
                  padding: '0.25rem',
                  display: 'flex',
                  alignItems: 'center'
                }}
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            {errors.password && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.password.message}
              </p>
            )}
          </div>

          <button
            id="login-google"
            type="button"
            onClick={startGoogleLogin}
            disabled={isGoogleSubmitting || isSubmitting}
            aria-busy={isGoogleSubmitting}
            className="flex w-full items-center justify-center gap-3 rounded-full bg-[#C8FF47] px-6 py-4 text-[0.95rem] font-bold text-[#002B18] shadow-[0_4px_14px_rgba(200,255,71,0.35)] enabled:hover:bg-[#002B18] enabled:hover:text-[#C8FF47] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#002B18] disabled:cursor-not-allowed disabled:opacity-70 motion-safe:transition motion-safe:duration-200 motion-safe:enabled:hover:-translate-y-0.5"
          >
            <FontAwesomeIcon icon={faGoogle} aria-hidden="true" />
            {isGoogleSubmitting ? 'Redirecionando para o Google...' : 'Entrar com Google'}
          </button>
          <div aria-hidden="true" className="my-6 flex items-center gap-4 text-[0.8rem] text-gray-500">
            <span className="h-px flex-1 bg-gray-200" />
            ou
            <span className="h-px flex-1 bg-gray-200" />
          </div>

          <button
            id="login-submit"
            type="submit"
            disabled={isSubmitting}
            className="group flex w-full items-center justify-center gap-3 rounded-full border border-[#CAD4CB] bg-white px-6 py-3.5 text-[0.95rem] font-bold text-[#002B18] enabled:hover:border-[#91AA95] enabled:hover:bg-[#DDEBDD] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#002B18] disabled:cursor-not-allowed disabled:opacity-70 motion-safe:transition-colors motion-safe:duration-200"
          >
            <span>{isSubmitting ? 'Autenticando...' : 'Acessar Plataforma'}</span>
            <span aria-hidden="true" className="inline-flex size-6 items-center justify-center rounded-full bg-[#E9F2E8] text-sm group-enabled:group-hover:bg-[#002B18] group-enabled:group-hover:text-white motion-safe:transition motion-safe:duration-200 motion-safe:group-enabled:group-hover:translate-x-1">
              →
            </span>
          </button>
        </form>
        <p role="status" aria-live="polite" style={{ position: 'absolute', width: 1, height: 1, padding: 0, margin: -1, overflow: 'hidden', clip: 'rect(0, 0, 0, 0)', whiteSpace: 'nowrap', border: 0 }}>
          {isGoogleSubmitting ? 'Redirecionando para o Google...' : ''}
        </p>

        {/* Primeiro Acesso com sublinhado limpo e profissional */}
        <div style={{ marginTop: '2.25rem', textAlign: 'center' }}>
          <p style={{ fontSize: '0.85rem', color: '#6B7280', margin: 0 }}>
            Primeiro acesso ao sistema?{' '}
            <Link
              href="/firstAccess"
              style={{
                color: '#002B18',
                fontWeight: 700,
                textDecoration: 'underline',
                textUnderlineOffset: '3px',
                transition: 'color 0.15s ease'
              }}
              onMouseEnter={e => (e.currentTarget.style.color = '#006633')}
              onMouseLeave={e => (e.currentTarget.style.color = '#002B18')}
            >
              Configure sua conta aqui
            </Link>
          </p>
        </div>
      </div>
    </AuthLayout>
  );
}
