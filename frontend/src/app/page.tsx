'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { Eye, EyeOff } from 'lucide-react';
import { useLogin } from '@/app/hooks/useLogin';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';

export default function LoginPage() {
  const { form, submitForm, isSubmitting } = useLogin();
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = form;
  const [showPassword, setShowPassword] = useState(false);

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
      <div style={{ paddingLeft: '1rem' }}>
        <h2
          style={{
            fontSize: '1.9rem',
            fontWeight: 800,
            letterSpacing: '-0.03em',
            color: '#002B18',
            margin: 0,
            marginBottom: '2rem'
          }}
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

          {/* Botão de Entrar */}
          <button
            id="login-submit"
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
            <span>{isSubmitting ? 'Autenticando...' : 'Acessar Plataforma'}</span>
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
              →
            </span>
          </button>
        </form>

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