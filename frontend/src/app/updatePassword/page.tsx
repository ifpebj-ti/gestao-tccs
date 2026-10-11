'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { ArrowRight, Eye, EyeOff } from 'lucide-react';
import { useUpdatePassword } from '../hooks/useUpdatePassword';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';

// O cookie é gravado pelo useVerifyAccessCode no formato `verified_<email codificado>`
function getVerifiedEmail(): string | null {
  const value = Cookies.get('access_token_temp');
  if (!value?.startsWith('verified_')) return null;

  try {
    const email = decodeURIComponent(value.slice('verified_'.length)).trim();
    return email || null;
  } catch {
    return null;
  }
}

export default function UpdatePassword() {
  const router = useRouter();
  const { form, submitForm, isSubmitting } = useUpdatePassword();
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors }
  } = form;

  const [isReady, setIsReady] = useState(false);

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // Só chega aqui quem validou o código na tela de recuperação.
  // Sem o cookie, volta pro início do fluxo; com ele, já preenche o e-mail.
  useEffect(() => {
    const email = getVerifiedEmail();

    if (!email) {
      router.replace('/forgotPassword');
      return;
    }

    setValue('userEmail', email);
    setIsReady(true);
  }, [router, setValue]);

  // Evita piscar o formulário antes do redirecionamento
  if (!isReady) return null;

  return (
    <AuthLayout showBackToLogin={true}>
      {/* ── LEFT: Editorial Reutilizável ── */}
      <AuthEditorialPanel
        title="Proteja seu acesso. Defina sua nova senha."
        description="Falta pouco para recuperar seu acesso. Escolha uma nova senha para sua conta no Gradus Flow e continue acompanhando suas propostas e atividades acadêmicas."
        stat1Value="Seguro"
        stat1Label="Proteção da sua conta"
        stat2Value="Simples"
        stat2Label="Acesso em poucos passos"
      />

      {/* ── RIGHT: Flat Form ── */}
      <div style={{ paddingLeft: '1rem' }}>
        <h2
          style={{
            fontSize: '1.9rem',
            fontWeight: 800,
            letterSpacing: '-0.03em',
            color: '#002B18',
            margin: 0,
            marginBottom: '0.5rem'
          }}
        >
          Nova senha
        </h2>

        <p
          style={{
            fontSize: '0.88rem',
            color: '#6B7280',
            margin: 0,
            marginBottom: '2rem'
          }}
        >
          Informe seu e-mail e defina uma nova senha para continuar.
        </p>

        <form onSubmit={handleSubmit(submitForm)} noValidate style={{ width: '100%' }}>
          {/* E-mail para Docente e Discente */}
          <div style={{ marginBottom: '1.5rem' }}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '0.5rem'
              }}
            >
              <label
                htmlFor="userEmail"
                style={{
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase'
                }}
              >
                E-mail institucional (docente ou discente)
              </label>
              <Link
                href="/forgotPassword"
                style={{
                  fontSize: '0.75rem',
                  fontWeight: 600,
                  color: '#002B18',
                  textDecoration: 'underline'
                }}
              >
                Alterar e-mail
              </Link>
            </div>
            <input
              id="userEmail"
              type="email"
              placeholder="seu.email@ifpe.edu.br ou @discente.ifpe.edu.br"
              autoComplete="username"
              {...register('userEmail')}
              readOnly
              style={{
                display: 'block',
                width: '100%',
                background: 'transparent',
                border: 'none',
                borderBottom: `2px solid ${errors.userEmail ? '#EF4444' : '#002B18'}`,
                padding: '0.65rem 0',
                fontSize: '0.95rem',
                color: '#6B7280',
                outline: 'none',
                transition: 'border-color 0.2s ease',
                boxSizing: 'border-box',
                fontFamily: 'inherit'
              }}
            />
            {errors.userEmail && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.userEmail.message?.toString()}
              </p>
            )}
          </div>

          {/* Nova Senha */}
          <div style={{ marginBottom: '1.5rem' }}>
            <label
              htmlFor="userPassword"
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
              Nova senha
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="userPassword"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••••••"
                autoComplete="new-password"
                {...register('userPassword')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.userPassword ? '#EF4444' : '#002B18'}`,
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
            {errors.userPassword && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.userPassword.message?.toString()}
              </p>
            )}
          </div>

          {/* Confirmar Nova Senha */}
          <div style={{ marginBottom: '2.5rem' }}>
            <label
              htmlFor="confirmPassword"
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
              Confirmar nova senha
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="confirmPassword"
                type={showConfirmPassword ? 'text' : 'password'}
                placeholder="••••••••••••"
                autoComplete="new-password"
                {...register('confirmPassword')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.confirmPassword ? '#EF4444' : '#002B18'}`,
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
                aria-label={showConfirmPassword ? 'Ocultar senha' : 'Mostrar senha'}
                onClick={() => setShowConfirmPassword(p => !p)}
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
                {showConfirmPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
            {errors.confirmPassword && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.confirmPassword.message?.toString()}
              </p>
            )}
          </div>

          {/* Botão de Envio */}
          <button
            type="submit"
            disabled={isSubmitting}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '0.75rem',
              width: '100%',
              padding: '0.95rem 1.5rem',
              background: isSubmitting ? '#A3E635' : '#C8FF47',
              color: '#002B18',
              fontSize: '0.95rem',
              fontWeight: 700,
              border: 'none',
              borderRadius: '9999px',
              cursor: isSubmitting ? 'not-allowed' : 'pointer',
              letterSpacing: '0.02em',
              boxShadow: '0 4px 14px rgba(200, 255, 71, 0.35)',
              transition: 'all 0.15s ease',
              fontFamily: 'inherit'
            }}
            onMouseEnter={e => {
              if (!isSubmitting) {
                e.currentTarget.style.background = '#002B18';
                e.currentTarget.style.color = '#C8FF47';
                e.currentTarget.style.transform = 'translateY(-2px)';
                const arrow = e.currentTarget.querySelector('.btn-arrow') as HTMLElement;
                if (arrow) {
                  arrow.style.background = '#C8FF47';
                  arrow.style.color = '#002B18';
                }
              }
            }}
            onMouseLeave={e => {
              if (!isSubmitting) {
                e.currentTarget.style.background = '#C8FF47';
                e.currentTarget.style.color = '#002B18';
                e.currentTarget.style.transform = 'translateY(0)';
                const arrow = e.currentTarget.querySelector('.btn-arrow') as HTMLElement;
                if (arrow) {
                  arrow.style.background = '#002B18';
                  arrow.style.color = '#C8FF47';
                }
              }
            }}
          >
            <span>{isSubmitting ? 'Atualizando senha...' : 'Salvar nova senha'}</span>
            <span
              className="btn-arrow"
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                width: '26px',
                height: '26px',
                borderRadius: '50%',
                background: '#002B18',
                color: '#C8FF47',
                fontSize: '0.85rem',
                fontWeight: 900,
                transition: 'all 0.15s ease'
              }}
            >
              <ArrowRight size={14} />
            </span>
          </button>
        </form>

        {/* Link voltar para login */}
        <div style={{ marginTop: '2.25rem', textAlign: 'center' }}>
          <p style={{ fontSize: '0.85rem', color: '#6B7280', margin: 0 }}>
            Lembrou sua senha?{' '}
            <Link
              href="/"
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
              Faça login aqui
            </Link>
          </p>
        </div>
      </div>
    </AuthLayout>
  );
}