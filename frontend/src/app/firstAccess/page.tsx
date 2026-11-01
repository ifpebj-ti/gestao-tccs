'use client';

import React, { useEffect, useState, Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { ArrowRight, RotateCw } from 'lucide-react';
import { useVerifyTccCode } from '@/app/hooks/useVerifyTccCode';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';

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

  return (
    <AuthLayout showBackToLogin={true}>
      {/* ── LEFT: Editorial Reutilizável ── */}
      <AuthEditorialPanel
        title="Primeiro acesso à plataforma."
        description="Ative sua conta institucional no sistema de TCCs. Você receberá um código de confirmação no seu e-mail do IFPE."
        stat1Value="100%"
        stat1Label="Digital & Sem papel"
        stat2Value="Ativação"
        stat2Label="Rápida em 2 Etapas"
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
          {step === 'send' ? 'Configurar Conta' : 'Confirmar Código'}
        </h2>

        <p
          style={{
            fontSize: '0.88rem',
            color: '#6B7280',
            margin: 0,
            marginBottom: '2rem'
          }}
        >
          {step === 'send'
            ? 'Informe seu e-mail institucional para receber o código.'
            : 'Insira o código de 6 caracteres enviado para o seu e-mail.'}
        </p>

        {step === 'send' ? (
          <div style={{ width: '100%' }}>
            {/* E-mail para Docente e Discente */}
            <div style={{ marginBottom: '2.5rem' }}>
              <label
                htmlFor="userEmail"
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
                id="userEmail"
                type="email"
                placeholder="seu.email@ifpe.edu.br ou @discente.ifpe.edu.br"
                {...register('userEmail')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.userEmail ? '#EF4444' : '#002B18'}`,
                  padding: '0.65rem 0',
                  fontSize: '0.95rem',
                  color: '#111827',
                  outline: 'none',
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

            {/* Botão Enviar Código */}
            <button
              type="button"
              onClick={handleSendCode}
              disabled={isSendingCode}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '0.75rem',
                width: '100%',
                padding: '0.95rem 1.5rem',
                background: isSendingCode ? '#A3E635' : '#C8FF47',
                color: '#002B18',
                fontSize: '0.95rem',
                fontWeight: 700,
                border: 'none',
                borderRadius: '9999px',
                cursor: isSendingCode ? 'not-allowed' : 'pointer',
                letterSpacing: '0.02em',
                boxShadow: '0 4px 14px rgba(200, 255, 71, 0.35)',
                transition: 'all 0.15s ease',
                fontFamily: 'inherit'
              }}
              onMouseEnter={e => {
                if (!isSendingCode) {
                  e.currentTarget.style.background = '#002B18';
                  e.currentTarget.style.color = '#C8FF47';
                  e.currentTarget.style.transform = 'translateY(-2px)';
                }
              }}
              onMouseLeave={e => {
                if (!isSendingCode) {
                  e.currentTarget.style.background = '#C8FF47';
                  e.currentTarget.style.color = '#002B18';
                  e.currentTarget.style.transform = 'translateY(0)';
                }
              }}
            >
              <span>{isSendingCode ? 'Enviando...' : 'Enviar Código de Acesso'}</span>
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

            <div style={{ marginTop: '1.5rem', textAlign: 'center' }}>
              <button
                type="button"
                onClick={() => setStep('verify')}
                style={{
                  background: 'none',
                  border: 'none',
                  fontSize: '0.82rem',
                  color: '#002B18',
                  fontWeight: 600,
                  textDecoration: 'underline',
                  cursor: 'pointer'
                }}
              >
                Já possuo um código de verificação
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit(submitForm)} noValidate style={{ width: '100%' }}>
            {/* Badge com e-mail */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '0.65rem 0.95rem',
                background: 'rgba(0, 43, 24, 0.05)',
                borderRadius: '6px',
                marginBottom: '1.75rem',
                fontSize: '0.85rem'
              }}
            >
              <span style={{ color: '#002B18', fontWeight: 600 }}>{userEmail}</span>
              <button
                type="button"
                onClick={() => setStep('send')}
                style={{
                  background: 'none',
                  border: 'none',
                  fontSize: '0.78rem',
                  color: '#002B18',
                  fontWeight: 700,
                  textDecoration: 'underline',
                  cursor: 'pointer'
                }}
              >
                Alterar
              </button>
            </div>

            {/* Código */}
            <div style={{ marginBottom: '2.5rem' }}>
              <label
                htmlFor="code"
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
                Código de verificação
              </label>
              <input
                id="code"
                type="text"
                placeholder="Ex: ABC123"
                maxLength={6}
                {...register('code')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.code ? '#EF4444' : '#002B18'}`,
                  padding: '0.65rem 0',
                  fontSize: '1.25rem',
                  fontWeight: 700,
                  letterSpacing: '0.2em',
                  color: '#002B18',
                  outline: 'none',
                  boxSizing: 'border-box',
                  fontFamily: 'monospace'
                }}
              />
              {errors.code && (
                <p style={{ marginTop: '0.4rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.code.message?.toString()}
                </p>
              )}
            </div>

            {/* Botão Confirmar */}
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
              <span>{isSubmitting ? 'Confirmando...' : 'Confirmar e Continuar'}</span>
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

            {/* Reenviar */}
            <div style={{ marginTop: '1.5rem', textAlign: 'center' }}>
              <button
                type="button"
                disabled={isSendingCode}
                onClick={handleSendCode}
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '0.35rem',
                  background: 'none',
                  border: 'none',
                  fontSize: '0.82rem',
                  color: '#002B18',
                  fontWeight: 600,
                  textDecoration: 'underline',
                  cursor: isSendingCode ? 'not-allowed' : 'pointer'
                }}
              >
                <RotateCw size={13} className={isSendingCode ? 'animate-spin' : ''} />
                <span>{isSendingCode ? 'Reenviando...' : 'Não recebeu? Reenviar código'}</span>
              </button>
            </div>
          </form>
        )}

        {/* Link voltar para login */}
        <div style={{ marginTop: '2.25rem', textAlign: 'center' }}>
          <p style={{ fontSize: '0.85rem', color: '#6B7280', margin: 0 }}>
            Já possui acesso configurado?{' '}
            <Link
              href="/"
              style={{
                color: '#002B18',
                fontWeight: 700,
                textDecoration: 'underline',
                textUnderlineOffset: '3px'
              }}
            >
              Fazer login
            </Link>
          </p>
        </div>
      </div>
    </AuthLayout>
  );
}

export default function FirstAccess() {
  return (
    <Suspense fallback={null}>
      <FirstAccessContent />
    </Suspense>
  );
}