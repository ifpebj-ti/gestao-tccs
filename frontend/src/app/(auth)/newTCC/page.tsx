'use client';

import { FormSelect } from '@/components/ui/form-select';

import { PageIntro } from '@/components/layout/PageIntro';
import { ProposalGuide } from '@/components/ProposalGuide';
import { SectionTitle } from '@/components/layout/SectionTitle';

import { Button } from '@/components/ui/button';
import { Mail, UserPlus, Trash2, Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useNewTccForm } from '@/app/hooks/useNewTcc';
import { Suspense } from 'react';

function NewTccContent() {
  const { push } = useRouter();
  const {
    register,
    errors,
    onSubmit,
    advisors,
    courses,
    fields,
    participantFields,
    addParticipant,
    removeParticipant,
    append,
    remove,
    isStudent,
    isReformulating,
    isSubmitting,
    form
  } = useNewTccForm();

  const watchedTitle = form.watch('title');
  const watchedSummary = form.watch('summary');

  const pageTitle = isStudent
    ? isReformulating
      ? 'Reformular Proposta de TCC'
      : 'Cadastrar Nova Proposta de TCC'
    : 'Nova Proposta de TCC';

  const {
    ref: summaryRegisterRef,
    onChange: summaryRegisterOnChange,
    ...summaryRegisterRest
  } = register('summary');

  return (
    <div className="flex flex-col w-full">
      {/* CABEÇALHO DIRETO E LIMPO */}

      {/* FORMULÁRIO VERTICAL COM DESTAQUE NOS TÍTULOS E LINHAS MINIMALISTAS */}
      <div className="grid items-start gap-12 lg:grid-cols-[minmax(0,1fr)_260px]">
        <form
          className="flex min-w-0 flex-col gap-7 w-full"
          onSubmit={onSubmit}
          noValidate
        >
          <PageIntro
            className="mb-0"
            title={pageTitle}
            description={
              isStudent
                ? 'Defina o docente orientador, título e resumo da sua proposta para envio à avaliação.'
                : 'Preencha os dados dos estudantes, orientador e informações gerais do projeto.'
            }
          />
          {/* Seção de Estudantes (Apenas para Coordenadores/Supervisores) */}
          {isStudent && !isReformulating && (
            <section
              className="space-y-4"
              aria-label="Outros integrantes da proposta"
            >
              <SectionTitle>Outros integrantes (opcional)</SectionTitle>
              <p className="text-sm text-gray-600">
                Adicione pelo e-mail. Todos devem ser do mesmo curso e aceitar o
                convite antes do envio ao professor.
              </p>
              {participantFields.map((field, index) => (
                <div key={field.id} className="flex items-start gap-3">
                  <div className="min-w-0 flex-1">
                    <label
                      htmlFor={`participant-${index}`}
                      className="mb-2 block text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700"
                    >
                      E-mail do integrante {index + 2} *
                    </label>
                    <div className="relative">
                      <input
                        id={`participant-${index}`}
                        type="email"
                        {...register(`participants.${index}.email`)}
                        aria-invalid={!!errors.participants?.[index]?.email}
                        aria-describedby={`participant-error-${index}`}
                        placeholder="estudante@discente.ifpe.edu.br"
                        className="w-full border-0 border-b-2 border-[#183E2D] bg-transparent py-2 pr-8 outline-none focus-visible:ring-2 focus-visible:ring-[#183E2D]/30"
                      />
                      <Mail
                        aria-hidden="true"
                        className="absolute right-1 top-1/2 size-4 -translate-y-1/2 text-gray-400"
                      />
                    </div>
                    {errors.participants?.[index]?.email && (
                      <p
                        id={`participant-error-${index}`}
                        role="alert"
                        className="mt-1 text-xs text-red-600"
                      >
                        {errors.participants[index]?.email?.message}
                      </p>
                    )}
                  </div>
                  <Button
                    type="button"
                    variant="destructive"
                    size="sm"
                    aria-label={`Remover integrante ${index + 2}`}
                    onClick={() => removeParticipant(index)}
                    disabled={isSubmitting}
                    className="mt-6 h-10 w-10 p-0"
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              ))}
              {participantFields.length > 0 && (
                <p className="text-xs text-gray-500">
                  Quem ainda não tem conta poderá se cadastrar com este e-mail
                  para responder ao convite.
                </p>
              )}
              {participantFields.length < 3 && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={addParticipant}
                  disabled={isSubmitting}
                  className="rounded-full border-[#183E2D]/20 text-[#183E2D]"
                >
                  <UserPlus className="mr-1.5 size-4" />
                  Adicionar outro estudante
                </Button>
              )}
            </section>
          )}
          {!isStudent && (
            <div className="space-y-4 pb-6 border-b border-[#002B18]/15">
              <SectionTitle>Informações do(s) Estudante(s)</SectionTitle>

              {fields.map((field, index) => (
                <div
                  key={field.id}
                  className="grid grid-cols-1 sm:grid-cols-[1fr_1fr_auto] gap-4 items-end pb-3"
                >
                  <div>
                    <label
                      htmlFor={`students.${index}.studentEmail`}
                      className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700"
                    >
                      E-mail do Estudante {index + 1} *
                    </label>
                    <div className="relative">
                      <input
                        id={`students.${index}.studentEmail`}
                        type="email"
                        placeholder="estudante@discente.ifpe.edu.br"
                        {...register(`students.${index}.studentEmail`)}
                        className={`block w-full bg-transparent border-0 border-b-2 ${
                          errors.students?.[index]?.studentEmail
                            ? 'border-red-500'
                            : 'border-[#002B18]'
                        } py-2 pr-7 text-sm sm:text-base text-gray-900 placeholder:text-gray-400 outline-none transition-colors`}
                      />
                      <Mail className="w-4 h-4 text-gray-400 absolute right-1 top-1/2 -translate-y-1/2 pointer-events-none" />
                    </div>
                    {errors.students?.[index]?.studentEmail && (
                      <p className="mt-1 text-xs text-red-500 font-medium">
                        {errors.students?.[index]?.studentEmail?.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <label
                      htmlFor={`students.${index}.courseId`}
                      className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700"
                    >
                      Curso do Estudante {index + 1} *
                    </label>
                    <div className="relative">
                      <FormSelect
                        id={`students.${index}.courseId`}
                        control={form.control}
                        name={`students.${index}.courseId`}
                        className={`block w-full appearance-none bg-transparent border-0 border-b-2 ${
                          errors.students?.[index]?.courseId
                            ? 'border-red-500'
                            : 'border-[#002B18]'
                        } py-2 pr-8 text-sm sm:text-base text-gray-900 outline-none transition-colors cursor-pointer`}
                      >
                        <option value={0} className="text-gray-500 bg-white">
                          Selecione um curso...
                        </option>
                        {courses.map((course) => (
                          <option
                            key={course.id}
                            value={course.id}
                            className="text-gray-900 bg-white"
                          >
                            {course.name}
                          </option>
                        ))}
                      </FormSelect>
                    </div>
                    {errors.students?.[index]?.courseId && (
                      <p className="mt-1 text-xs text-red-500 font-medium">
                        {errors.students?.[index]?.courseId?.message}
                      </p>
                    )}
                  </div>

                  {fields.length > 1 && (
                    <Button
                      type="button"
                      variant="destructive"
                      size="sm"
                      className="h-10 w-10 p-0 self-end"
                      aria-label="Remover estudante"
                      onClick={() => remove(index)}
                      title="Remover estudante"
                    >
                      <Trash2 className="w-4 h-4" strokeWidth={1.75} />
                    </Button>
                  )}
                </div>
              ))}

              {fields.length < 4 && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="font-medium text-xs text-gray-700 border-slate-300 rounded-full mt-2"
                  onClick={() => append({ studentEmail: '', courseId: 0 })}
                >
                  <UserPlus className="w-3.5 h-3.5 mr-1.5" strokeWidth={1.75} />
                  Adicionar outro estudante
                </Button>
              )}
            </div>
          )}

          {/* 1. CAMPO ORIENTADOR: TÍTULO COM MAIS DESTAQUE + CAIXA DE OPÇÕES CUSTOMIZADA (SEM AZUL NATIVO) */}
          <div>
            <label
              htmlFor="orientador-trigger"
              className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700"
            >
              Professor Orientador *
            </label>
            <FormSelect
              id="orientador-trigger"
              control={form.control}
              name="advisorId"
              disabled={isSubmitting}
              className="text-sm sm:text-base"
            >
              <option value={0}>Selecione o professor orientador...</option>
              {advisors.map((advisor) => (
                <option key={advisor.id} value={advisor.id}>
                  {advisor.name}
                </option>
              ))}
            </FormSelect>

            {errors.advisorId && (
              <p className="mt-1.5 text-xs text-red-500 font-medium">
                {errors.advisorId.message}
              </p>
            )}
            <p className="text-[11px] text-gray-400 mt-1.5">
              Docente responsável pela orientação do seu projeto no seu campus.
            </p>
          </div>

          {/* 2. CAMPO TÍTULO: TÍTULO COM MAIS DESTAQUE + LINHA VERTICAL */}
          <div>
            <div className="flex items-center justify-between mb-1.5">
              <label
                htmlFor="titulo"
                className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700"
              >
                Título do TCC *
              </label>
              <span className="text-[11px] text-gray-400 font-mono">
                {(watchedTitle || '').length}/255
              </span>
            </div>
            <input
              id="titulo"
              type="text"
              maxLength={255}
              placeholder="Ex.: Desenvolvimento de Sistema de Gestão Acadêmica Integrada"
              {...register('title')}
              className={`block w-full bg-transparent border-0 border-b-2 ${
                errors.title ? 'border-red-500' : 'border-[#002B18]'
              } py-2.5 text-sm sm:text-base text-gray-900 placeholder:text-gray-400 outline-none transition-colors`}
            />
            {errors.title && (
              <p className="mt-1.5 text-xs text-red-500 font-medium">
                {errors.title.message}
              </p>
            )}
            <p className="text-[11px] text-gray-400 mt-1.5">
              Tema principal da proposta de trabalho de conclusão de curso
              (máximo 255 caracteres).
            </p>
          </div>

          {/* 3. CAMPO RESUMO: TÍTULO COM MAIS DESTAQUE + PLACEHOLDER EXATAMENTE NA LINHA + AUTO-EXPANSÃO + CONTADOR */}
          <div>
            <div className="flex items-center justify-between mb-1.5">
              <label
                htmlFor="summary"
                className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700"
              >
                Resumo da Proposta *
              </label>
              <span
                className={`text-[11px] font-mono ${
                  (watchedSummary || '').length > 255
                    ? 'text-red-500 font-bold'
                    : 'text-gray-400'
                }`}
              >
                {(watchedSummary || '').length}/255
              </span>
            </div>
            <textarea
              id="summary"
              placeholder="Apresente brevemente o problema, objetivos gerais e metodologia pretendida do projeto..."
              rows={1}
              maxLength={255}
              {...summaryRegisterRest}
              ref={(element) => {
                summaryRegisterRef(element);
              }}
              onChange={(e) => {
                summaryRegisterOnChange(e);
                // Auto-expand dinâmico: a linha desce conforme o texto precisa de quebra
                e.target.style.height = 'auto';
                e.target.style.height = `${e.target.scrollHeight}px`;
              }}
              className={`block w-full resize-none overflow-hidden bg-transparent border-0 border-b-2 ${
                errors.summary ? 'border-red-500' : 'border-[#002B18]'
              } pt-1 pb-2 text-sm sm:text-base text-gray-900 placeholder:text-gray-400 outline-none transition-[border-color] leading-normal`}
            />
            {errors.summary && (
              <p className="mt-1.5 text-xs text-red-500 font-medium">
                {errors.summary.message}
              </p>
            )}
            <p className="text-[11px] text-gray-400 mt-1.5">
              Recomendado sintetizar os objetivos para facilitar o aceite do
              orientador (máximo 255 caracteres).
            </p>
          </div>

          {/* BARRA DE AÇÕES: COESA, RESPONSIVA */}
          <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-4">
            <Button
              type="button"
              onClick={() => push('/homePage')}
              variant="destructive"
              className="font-medium rounded-full px-6 py-2.5"
              disabled={isSubmitting}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="default"
              size="lg"
              className="font-bold rounded-full px-8 py-3 shadow-sm inline-flex items-center justify-center gap-2"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Submetendo...</span>
                </>
              ) : (
                <span>
                  {isStudent
                    ? isReformulating
                      ? 'Reenviar Proposta'
                      : participantFields.length > 0
                        ? 'Enviar convites aos integrantes'
                        : 'Submeter Proposta ao Orientador'
                    : 'Submeter Proposta'}
                </span>
              )}
            </Button>
          </div>
        </form>
        <ProposalGuide isStudent={isStudent} />
      </div>
    </div>
  );
}

export default function NewTcc() {
  return (
    <Suspense fallback={null}>
      <NewTccContent />
    </Suspense>
  );
}
