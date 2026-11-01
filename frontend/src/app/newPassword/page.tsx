'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { ArrowRight, Eye, EyeOff } from 'lucide-react';
import { useNewPassword } from '../hooks/useNewPassword';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';

export default function NewPassword() {
  const { form, submitForm, isSubmitting } = useNewPassword();
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = form;

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  return (
    <AuthLayout showBackToLogin={true}>
      {/* ── LEFT: Editorial Reutilizável ── */}
      <AuthEditorialPanel
        title="Defina sua nova credencial de acesso."
        description="Crie uma senha forte e segura para garantir a integridade dos seus dados e trabalhos acadêmicos no IFPE."
        stat1Value="100%"
        stat1Label="Criptografia Segura"
        stat2Value="Acesso"
        stat2Label="Imediato após Redefinição"
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
          Criar Nova Senha
        </h2>

        <p
          style={{
            fontSize: '0.88rem',
            color: '#6B7280',
            margin: 0,
            marginBottom: '2rem'
          }}
        >
          Preencha os campos abaixo para concluir a atualização.
        </p>

        <form onSubmit={handleSubmit(submitForm)} noValidate style={{ width: '100%' }}>
          {/* E-mail para Docente e Discente */}
          <div style={{ marginBottom: '1.5rem' }}>
            <label
              htmlFor="email"
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
              id="email"
              type="email"
              placeholder="seu.email@ifpe.edu.br ou @discente.ifpe.edu.br"
              {...register('email')}
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
                boxSizing: 'border-box',
                fontFamily: 'inherit'
              }}
            />
            {errors.email && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.email.message?.toString()}
              </p>
            )}
          </div>

          {/* Código de Convite */}
          <div style={{ marginBottom: '1.5rem' }}>
            <label
              htmlFor="inviteCode"
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
              Código de Convite
            </label>
            <input
              id="inviteCode"
              type="text"
              placeholder="Digite o código recebido"
              {...register('inviteCode')}
              style={{
                display: 'block',
                width: '100%',
                background: 'transparent',
                border: 'none',
                borderBottom: `2px solid ${errors.inviteCode ? '#EF4444' : '#002B18'}`,
                padding: '0.65rem 0',
                fontSize: '1rem',
                color: '#111827',
                outline: 'none',
                boxSizing: 'border-box',
                fontFamily: 'inherit'
              }}
            />
            {errors.inviteCode && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.inviteCode.message?.toString()}
              </p>
            )}
          </div>

          {/* Nova Senha */}
          <div style={{ marginBottom: '1.5rem' }}>
            <label
              htmlFor="password"
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
              Nova Senha
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••••••"
                {...register('password')}
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
            {errors.password && (
              <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.password.message?.toString()}
              </p>
            )}
          </div>

          {/* Confirmar Senha */}
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
              Confirmar Senha
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="confirmPassword"
                type={showConfirmPassword ? 'text' : 'password'}
                placeholder="••••••••••••"
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

          {/* Botão Finalizar */}
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
              }
            }}
            onMouseLeave={e => {
              if (!isSubmitting) {
                e.currentTarget.style.background = '#C8FF47';
                e.currentTarget.style.color = '#002B18';
                e.currentTarget.style.transform = 'translateY(0)';
              }
            }}
          >
            <span>{isSubmitting ? 'Salvando...' : 'Finalizar e Acessar'}</span>
            <span
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
                fontWeight: 900
              }}
            >
              <ArrowRight size={14} />
            </span>
          </button>
        </form>

        {/* Link voltar */}
        <div style={{ marginTop: '2.25rem', textAlign: 'center' }}>
          <p style={{ fontSize: '0.85rem', color: '#6B7280', margin: 0 }}>
            Já sabe sua senha?{' '}
            <Link
              href="/"
              style={{
                color: '#002B18',
                fontWeight: 700,
                textDecoration: 'underline',
                textUnderlineOffset: '3px'
              }}
            >
              Faça login aqui
            </Link>
          </p>
        </div>
      </div>
    </AuthLayout>
  );
}