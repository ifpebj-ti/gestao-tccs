'use client';

import React, { useEffect, useState } from 'react';
import { Eye, EyeOff, ArrowRight } from 'lucide-react';
import { AuthLayout } from '@/components/auth/AuthLayout';
import { AuthEditorialPanel } from '@/components/auth/AuthEditorialPanel';
import { useAutoRegister } from '../hooks/useAutoRegister';

export default function AutoRegister() {
  const { form, submitForm, isSubmitting, campus, courses } = useAutoRegister();
  const [isEmailPreFilled, setIsEmailPreFilled] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors }
  } = form;

  useEffect(() => {
    if (typeof window !== 'undefined') {
      const storedEmail = sessionStorage.getItem('first_access_email');
      if (storedEmail) {
        setValue('email', storedEmail, { shouldValidate: true });
        setIsEmailPreFilled(true);
      }
    }
  }, [setValue]);

  // --- Máscara de CPF ---
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

  // --- Máscara de Telefone ---
  const formatPhone = (value: string) => {
    if (!value) return '';
    const numericValue = value.replace(/\D/g, '');
    const limitedValue = numericValue.slice(0, 11);

    let formattedValue = limitedValue;

    if (limitedValue.length > 0) {
      formattedValue = formattedValue.replace(/^(\d{2})/, '($1');
    }
    if (limitedValue.length > 2) {
      formattedValue = formattedValue.replace(/^(\(\d{2})(\d)/, '$1) $2');
    }
    if (limitedValue.length > 7) {
      if (limitedValue.length === 11) {
        formattedValue = formattedValue.replace(
          /^(\(\d{2}\) \d{5})(\d{4})/,
          '$1-$2'
        );
      } else {
        formattedValue = formattedValue.replace(
          /^(\(\d{2}\) \d{4})(\d{0,4})/,
          '$1-$2'
        );
      }
    }
    formattedValue = formattedValue.replace(/-$/, '');
    return formattedValue;
  };

  const { onChange: onCpfChange, ...cpfRest } = register('cpf');
  const { onChange: onPhoneChange, ...phoneRest } = register('phone');

  return (
    <AuthLayout showBackToLogin={true} gridCols="1fr 1.15fr">
      {/* ── LEFT: Editorial Panel ── */}
      <AuthEditorialPanel
        title="Conclua seu cadastro institucional."
        description="Preencha seus dados de estudante para ativar seu perfil no Gradus Flow e submeter propostas aos orientadores."
        stat1Value="100%"
        stat1Label="Digital & Sem papel"
        stat2Value="Discente"
        stat2Label="Acesso ao Sistema"
      />

      {/* ── RIGHT: Seamless Flat Form ── */}
      <div style={{ paddingLeft: '1rem', paddingRight: '0.5rem' }}>
        <h2
          style={{
            fontSize: '1.85rem',
            fontWeight: 800,
            letterSpacing: '-0.03em',
            color: '#002B18',
            margin: 0,
            marginBottom: '0.5rem'
          }}
        >
          Cadastro de Estudante
        </h2>
        <p style={{ fontSize: '0.875rem', color: '#6B7280', marginBottom: '1.75rem' }}>
          Preencha seus dados cadastrais para acessar o sistema.
        </p>

        <form onSubmit={handleSubmit(submitForm)} noValidate className="space-y-4">
          {/* Nome Completo */}
          <div>
            <label
              htmlFor="auto-name"
              style={{
                display: 'block',
                fontSize: '0.72rem',
                fontWeight: 700,
                color: '#374151',
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                marginBottom: '0.35rem'
              }}
            >
              Nome Completo *
            </label>
            <input
              id="auto-name"
              type="text"
              placeholder="Digite seu nome completo"
              {...register('name')}
              style={{
                display: 'block',
                width: '100%',
                background: 'transparent',
                border: 'none',
                borderBottom: `2px solid ${errors.name ? '#EF4444' : '#002B18'}`,
                padding: '0.5rem 0',
                fontSize: '0.9rem',
                color: '#111827',
                outline: 'none',
                transition: 'border-color 0.2s ease',
                boxSizing: 'border-box'
              }}
            />
            {errors.name && (
              <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                {errors.name.message?.toString()}
              </p>
            )}
          </div>

          {/* Email e Matrícula (Lado a Lado) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="auto-email"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                E-mail Discente *
              </label>
              <input
                id="auto-email"
                type="email"
                placeholder="seu.email@discente.ifpe.edu.br"
                readOnly={isEmailPreFilled}
                disabled={isEmailPreFilled}
                {...register('email')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.email ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
              {errors.email && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.email.message?.toString()}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="auto-reg"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Matrícula *
              </label>
              <input
                id="auto-reg"
                type="text"
                placeholder="Digite sua matrícula"
                {...register('registration')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.registration ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
              {errors.registration && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.registration.message?.toString()}
                </p>
              )}
            </div>
          </div>

          {/* CPF e Telefone (Lado a Lado) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="auto-cpf"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                CPF *
              </label>
              <input
                id="auto-cpf"
                type="text"
                placeholder="000.000.000-00"
                maxLength={14}
                {...cpfRest}
                onChange={(e) => {
                  e.target.value = formatCPF(e.target.value);
                  onCpfChange(e);
                }}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.cpf ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
              {errors.cpf && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.cpf.message?.toString()}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="auto-phone"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Telefone *
              </label>
              <input
                id="auto-phone"
                type="text"
                placeholder="(81) 90000-0000"
                maxLength={15}
                {...phoneRest}
                onChange={(e) => {
                  e.target.value = formatPhone(e.target.value);
                  onPhoneChange(e);
                }}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.phone ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
              {errors.phone && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.phone.message?.toString()}
                </p>
              )}
            </div>
          </div>

          {/* Turma e Turno (Lado a Lado) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="auto-class"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Turma *
              </label>
              <input
                id="auto-class"
                type="text"
                placeholder="Ex: 2024.1"
                {...register('userClass')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.userClass ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
              {errors.userClass && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.userClass.message?.toString()}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="auto-shift"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Turno *
              </label>
              <select
                id="auto-shift"
                {...register('shift')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.shift ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box',
                  cursor: 'pointer'
                }}
              >
                <option value="">Selecione o turno</option>
                <option value="1">Matutino (Manhã)</option>
                <option value="2">Vespertino (Tarde)</option>
                <option value="3">Noturno (Noite)</option>
                <option value="4">Diurno (Integral)</option>
              </select>
              {errors.shift && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.shift.message?.toString()}
                </p>
              )}
            </div>
          </div>

          {/* Campus e Curso (Lado a Lado) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="auto-campus"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Campus *
              </label>
              <select
                id="auto-campus"
                {...register('campusId')}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.campusId ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box',
                  cursor: 'pointer'
                }}
              >
                <option value="">Selecione um campus</option>
                {campus?.map((camp: { id: number; name: string }) => (
                  <option key={camp.id} value={camp.id}>
                    {camp.name}
                  </option>
                ))}
              </select>
              {errors.campusId && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.campusId.message?.toString()}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="auto-course"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Curso *
              </label>
              <select
                id="auto-course"
                {...register('courseId')}
                disabled={!courses || courses.length === 0}
                style={{
                  display: 'block',
                  width: '100%',
                  background: 'transparent',
                  border: 'none',
                  borderBottom: `2px solid ${errors.courseId ? '#EF4444' : '#002B18'}`,
                  padding: '0.5rem 0',
                  fontSize: '0.9rem',
                  color: '#111827',
                  outline: 'none',
                  boxSizing: 'border-box',
                  cursor: 'pointer'
                }}
              >
                <option value="">Selecione um curso</option>
                {courses?.map((course: { id: number; name: string }) => (
                  <option key={course.id} value={course.id}>
                    {course.name}
                  </option>
                ))}
              </select>
              {errors.courseId && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.courseId.message?.toString()}
                </p>
              )}
            </div>
          </div>

          {/* Senha e Confirmar Senha (Lado a Lado) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="auto-password"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Senha de acesso *
              </label>
              <div style={{ position: 'relative' }}>
                <input
                  id="auto-password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Mínimo 8 caracteres"
                  {...register('password')}
                  style={{
                    display: 'block',
                    width: '100%',
                    background: 'transparent',
                    border: 'none',
                    borderBottom: `2px solid ${errors.password ? '#EF4444' : '#002B18'}`,
                    padding: '0.5rem 2rem 0.5rem 0',
                    fontSize: '0.9rem',
                    color: '#111827',
                    outline: 'none',
                    boxSizing: 'border-box'
                  }}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((p) => !p)}
                  style={{
                    position: 'absolute',
                    right: 0,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    background: 'none',
                    border: 'none',
                    cursor: 'pointer',
                    color: '#6B7280'
                  }}
                  aria-label={showPassword ? 'Ocultar senha' : 'Exibir senha'}
                >
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
              {errors.password && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.password.message?.toString()}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="auto-confirm-password"
                style={{
                  display: 'block',
                  fontSize: '0.72rem',
                  fontWeight: 700,
                  color: '#374151',
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: '0.35rem'
                }}
              >
                Confirmar senha *
              </label>
              <div style={{ position: 'relative' }}>
                <input
                  id="auto-confirm-password"
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="Repita sua senha"
                  {...register('confirmPassword')}
                  style={{
                    display: 'block',
                    width: '100%',
                    background: 'transparent',
                    border: 'none',
                    borderBottom: `2px solid ${errors.confirmPassword ? '#EF4444' : '#002B18'}`,
                    padding: '0.5rem 2rem 0.5rem 0',
                    fontSize: '0.9rem',
                    color: '#111827',
                    outline: 'none',
                    boxSizing: 'border-box'
                  }}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword((p) => !p)}
                  style={{
                    position: 'absolute',
                    right: 0,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    background: 'none',
                    border: 'none',
                    cursor: 'pointer',
                    color: '#6B7280'
                  }}
                  aria-label={showConfirmPassword ? 'Ocultar senha' : 'Exibir senha'}
                >
                  {showConfirmPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
              {errors.confirmPassword && (
                <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#EF4444' }}>
                  {errors.confirmPassword.message?.toString()}
                </p>
              )}
            </div>
          </div>

          {/* Botão de Finalização */}
          <div className="pt-4">
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
                transition: 'all 0.15s ease'
              }}
            >
              <span>{isSubmitting ? 'Concluindo cadastro...' : 'Concluir cadastro e acessar'}</span>
              <ArrowRight size={16} />
            </button>
          </div>
        </form>
      </div>
    </AuthLayout>
  );
}
