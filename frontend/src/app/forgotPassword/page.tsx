'use client';

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import Link from 'next/link';
import { ArrowRight } from 'lucide-react';
import { useResendAccessCode } from '@/app/hooks/useResendAccessCode';
import { useVerifyAccessCode } from '@/app/hooks/useVerifyAccessCode';
import { toast } from 'react-toastify';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';

export default function ForgotPassword() {
  const [isCodeSent, setIsCodeSent] = useState(false);

  const { register, handleSubmit, reset } = useForm<{
    userEmail: string;
    accessCode?: string;
  }>();

  const { submitForm: resendSubmit, isSubmitting: isResending } =
    useResendAccessCode();
  const { submitForm: verifySubmit, isSubmitting: isVerifying } =
    useVerifyAccessCode();

  const onSubmit = async (data: { userEmail: string; accessCode?: string }) => {
    if (!isCodeSent) {
      try {
        const success = await resendSubmit({ userEmail: data.userEmail });
        if (success) {
          setIsCodeSent(true);
        } else {
          setIsCodeSent(false);
        }
      } catch {
        toast.error('Erro ao enviar o código');
      }
    } else {
      try {
        await verifySubmit({
          userEmail: data.userEmail,
          accessCode: data.accessCode!
        });
      } catch {
        toast.error('Erro ao verificar o código');
      }
    }
  };

  const handleChangeEmail = () => {
    reset();
    setIsCodeSent(false);
  };

  return (
    <AuthLayout showBackToLogin={true}>
      {/* ── LEFT: Editorial Reutilizável ── */}
      <AuthEditorialPanel
        title="Recuperação segura do seu acesso."
        description="Informe seu e-mail institucional para receber um código de validação e redefinir sua credencial sem burocracia."
        stat1Value="100%"
        stat1Label="Digital & Seguro"
        stat2Value="Validação"
        stat2Label="Instantânea por E-mail"
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
          Recuperar Senha
        </h2>

        <p
          style={{
            fontSize: '0.88rem',
            color: '#6B7280',
            margin: 0,
            marginBottom: '2rem'
          }}
        >
          {isCodeSent
            ? 'Insira o código de validação que você recebeu no e-mail.'
            : 'Digite o e-mail cadastrado na plataforma.'}
        </p>

        <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ width: '100%' }}>
          {/* E-mail para Docente e Discente */}
          <div style={{ marginBottom: isCodeSent ? '1.5rem' : '2.5rem' }}>
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
              {isCodeSent && (
                <button
                  type="button"
                  onClick={handleChangeEmail}
                  style={{
                    background: 'none',
                    border: 'none',
                    padding: 0,
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    color: '#002B18',
                    textDecoration: 'underline',
                    cursor: 'pointer'
                  }}
                >
                  Alterar e-mail
                </button>
              )}
            </div>
            <input
              id="userEmail"
              type="email"
              placeholder="seu.email@ifpe.edu.br ou @discente.ifpe.edu.br"
              {...register('userEmail', { required: true })}
              readOnly={isCodeSent}
              style={{
                display: 'block',
                width: '100%',
                background: 'transparent',
                border: 'none',
                borderBottom: '2px solid #002B18',
                padding: '0.65rem 0',
                fontSize: '0.95rem',
                color: isCodeSent ? '#6B7280' : '#111827',
                outline: 'none',
                boxSizing: 'border-box',
                fontFamily: 'inherit'
              }}
            />
          </div>

          {/* Código de Verificação */}
          {isCodeSent && (
            <div style={{ marginBottom: '2.5rem' }}>
              <label
                htmlFor="accessCode"
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
                id="accessCode"
                type="text"
                placeholder="Digite o código recebido"
                {...register('accessCode', { required: true })}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: '2px solid #002B18',
                  padding: '0.65rem 0',
                  fontSize: '1rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box',
                  fontFamily: 'inherit',
                  letterSpacing: '0.1em'
                }}
              />
            </div>
          )}

          {/* Botão de Envio */}
          <button
            type="submit"
            disabled={isResending || isVerifying}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '0.75rem',
              width: '100%',
              padding: '0.95rem 1.5rem',
              background: isResending || isVerifying ? '#A3E635' : '#C8FF47',
              color: '#002B18',
              fontSize: '0.95rem',
              fontWeight: 700,
              border: 'none',
              borderRadius: '9999px',
              cursor: isResending || isVerifying ? 'not-allowed' : 'pointer',
              letterSpacing: '0.02em',
              boxShadow: '0 4px 14px rgba(200, 255, 71, 0.35)',
              transition: 'all 0.15s ease',
              fontFamily: 'inherit'
            }}
            onMouseEnter={e => {
              if (!isResending && !isVerifying) {
                e.currentTarget.style.background = '#002B18';
                e.currentTarget.style.color = '#C8FF47';
                e.currentTarget.style.transform = 'translateY(-2px)';
              }
            }}
            onMouseLeave={e => {
              if (!isResending && !isVerifying) {
                e.currentTarget.style.background = '#C8FF47';
                e.currentTarget.style.color = '#002B18';
                e.currentTarget.style.transform = 'translateY(0)';
              }
            }}
          >
            <span>
              {isCodeSent
                ? isVerifying
                  ? 'Verificando...'
                  : 'Verificar Código'
                : isResending
                  ? 'Enviando...'
                  : 'Enviar Código'}
            </span>
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