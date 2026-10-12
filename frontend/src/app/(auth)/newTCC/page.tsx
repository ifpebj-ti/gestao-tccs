'use client';

import { Button } from '@/components/ui/button';
import {
  Mail,
  UserPlus,
  Trash2,
  Loader2,
  ChevronDown,
  Check
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useNewTccForm } from '@/app/hooks/useNewTcc';
import { Suspense, useRef, useState, useEffect } from 'react';

function NewTccContent() {
  const { push } = useRouter();
  const {
    register,
    errors,
    onSubmit,
    advisors,
    courses,
    fields,
    append,
    remove,
    isStudent,
    isReformulating,
    isSubmitting,
    form
  } = useNewTccForm();

  const [isSelectOpen, setIsSelectOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement | null>(null);

  const selectedAdvisorId = form.watch('advisorId');
  const selectedAdvisor = advisors.find((adv) => adv.id === Number(selectedAdvisorId));
  const watchedTitle = form.watch('title');
  const watchedSummary = form.watch('summary');

  const pageTitle = isStudent
    ? isReformulating
      ? 'Reformular Proposta de TCC'
      : 'Cadastrar Nova Proposta de TCC'
    : 'Nova Proposta de TCC';

  const { ref: summaryRegisterRef, onChange: summaryRegisterOnChange, ...summaryRegisterRest } = register('summary');

  // Fechar dropdown ao clicar fora
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsSelectOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className="flex flex-col w-full max-w-4xl">
      {/* CABEÇALHO DIRETO E LIMPO */}
      <div className="mb-10">
        <h1 className="text-2xl sm:text-3xl md:text-4xl font-extrabold tracking-tight text-[#002B18]">
          {pageTitle}
        </h1>
        <p className="mt-2 text-sm md:text-base text-gray-600 leading-relaxed">
          {isStudent
            ? 'Defina o docente orientador, título e resumo da sua proposta para envio à avaliação.'
            : 'Preencha os dados dos estudantes, orientador e informações gerais do projeto.'}
        </p>
      </div>

      {/* FORMULÁRIO VERTICAL COM DESTAQUE NOS TÍTULOS E LINHAS MINIMALISTAS */}
      <form className="flex flex-col gap-9 w-full" onSubmit={onSubmit} noValidate>
        {/* Seção de Estudantes (Apenas para Coordenadores/Supervisores) */}
        {!isStudent && (
          <div className="space-y-4 pb-6 border-b border-[#002B18]/15">
            <h2 className="text-sm font-bold uppercase tracking-wider text-[#002B18]">
              Informações do(s) Estudante(s)
            </h2>

            {fields.map((field, index) => (
              <div
                key={field.id}
                className="grid grid-cols-1 sm:grid-cols-[1fr_1fr_auto] gap-4 items-end pb-3"
              >
                <div>
                  <label
                    htmlFor={`students.${index}.studentEmail`}
                    className="block text-xs sm:text-sm font-bold text-[#002B18] tracking-wider uppercase mb-1.5"
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
                        errors.students?.[index]?.studentEmail ? 'border-red-500' : 'border-[#002B18]'
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
                    className="block text-xs sm:text-sm font-bold text-[#002B18] tracking-wider uppercase mb-1.5"
                  >
                    Curso do Estudante {index + 1} *
                  </label>
                  <div className="relative">
                    <select
                      id={`students.${index}.courseId`}
                      {...register(`students.${index}.courseId`)}
                      className={`block w-full appearance-none bg-transparent border-0 border-b-2 ${
                        errors.students?.[index]?.courseId ? 'border-red-500' : 'border-[#002B18]'
                      } py-2 pr-8 text-sm sm:text-base text-gray-900 outline-none transition-colors cursor-pointer`}
                    >
                      <option value={0} className="text-gray-500 bg-white">Selecione um curso...</option>
                      {courses.map((course) => (
                        <option key={course.id} value={course.id} className="text-gray-900 bg-white">
                          {course.name}
                        </option>
                      ))}
                    </select>
                    <ChevronDown className="w-4 h-4 text-[#002B18] absolute right-1 top-1/2 -translate-y-1/2 pointer-events-none" />
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
                    variant="ghost"
                    size="sm"
                    className="text-red-600 hover:text-red-700 hover:bg-red-50 h-10 w-10 p-0 self-end"
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
            className="block text-xs sm:text-sm font-bold text-[#002B18] tracking-wider uppercase mb-1.5"
          >
            Professor Orientador *
          </label>
          <div className="relative" ref={dropdownRef}>
            {/* Input oculto para registrar no react-hook-form */}
            <input type="hidden" {...register('advisorId')} />

            {/* Gatilho customizado que se parece com o campo sublinhado */}
            <button
              id="orientador-trigger"
              type="button"
              onClick={() => setIsSelectOpen((prev) => !prev)}
              className={`w-full flex items-center justify-between text-left bg-transparent border-0 border-b-2 ${
                errors.advisorId ? 'border-red-500' : 'border-[#002B18]'
              } py-2.5 pr-2 text-sm sm:text-base outline-none cursor-pointer transition-colors group`}
            >
              <span className={selectedAdvisor ? 'text-[#002B18] font-semibold' : 'text-gray-400 font-normal'}>
                {selectedAdvisor ? selectedAdvisor.name : 'Selecione o professor orientador...'}
              </span>
              <ChevronDown
                className={`w-4 h-4 text-[#002B18] transition-transform duration-200 ${
                  isSelectOpen ? 'rotate-180' : ''
                }`}
              />
            </button>

            {/* Menu flutuante estilizado na nossa estética (fundo limpo, bordas arredondadas sutis, hover verde) */}
            {isSelectOpen && (
              <div className="absolute left-0 right-0 top-full mt-2 bg-[#FAF9F5] border border-[#DDD9CD] rounded-xl shadow-xl z-50 overflow-hidden py-1.5 animate-in fade-in-50 zoom-in-95 duration-150">
                <div
                  onClick={() => {
                    form.setValue('advisorId', 0, { shouldValidate: true });
                    setIsSelectOpen(false);
                  }}
                  className={`px-4 py-2.5 text-xs sm:text-sm cursor-pointer transition-colors ${
                    !selectedAdvisorId || Number(selectedAdvisorId) === 0
                      ? 'bg-[#E8F3EC] text-[#002B18] font-semibold'
                      : 'text-gray-500 hover:bg-[#F0EEE6]'
                  }`}
                >
                  Selecione o professor orientador...
                </div>

                {advisors.length === 0 ? (
                  <div className="px-4 py-3 text-xs sm:text-sm text-gray-500 italic">
                    Nenhum orientador encontrado para seu campus.
                  </div>
                ) : (
                  advisors.map((advisor) => {
                    const isSelected = Number(selectedAdvisorId) === advisor.id;
                    return (
                      <div
                        key={advisor.id}
                        onClick={() => {
                          form.setValue('advisorId', advisor.id, { shouldValidate: true });
                          setIsSelectOpen(false);
                        }}
                        className={`flex items-center justify-between px-4 py-3 text-sm cursor-pointer transition-colors ${
                          isSelected
                            ? 'bg-[#002B18] text-[#C8FF47] font-semibold'
                            : 'text-[#002B18] font-medium hover:bg-[#E8F3EC]'
                        }`}
                      >
                        <span>{advisor.name}</span>
                        {isSelected && <Check className="w-4 h-4 text-[#C8FF47]" />}
                      </div>
                    );
                  })
                )}
              </div>
            )}
          </div>

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
              className="block text-xs sm:text-sm font-bold text-[#002B18] tracking-wider uppercase"
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
            Tema principal da proposta de trabalho de conclusão de curso (máximo 255 caracteres).
          </p>
        </div>

        {/* 3. CAMPO RESUMO: TÍTULO COM MAIS DESTAQUE + PLACEHOLDER EXATAMENTE NA LINHA + AUTO-EXPANSÃO + CONTADOR */}
        <div>
          <div className="flex items-center justify-between mb-1.5">
            <label
              htmlFor="summary"
              className="block text-xs sm:text-sm font-bold text-[#002B18] tracking-wider uppercase"
            >
              Resumo da Proposta *
            </label>
            <span
              className={`text-[11px] font-mono ${
                (watchedSummary || '').length > 255 ? 'text-red-500 font-bold' : 'text-gray-400'
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
            Recomendado sintetizar os objetivos para facilitar o aceite do orientador (máximo 255 caracteres).
          </p>
        </div>

        {/* BARRA DE AÇÕES: COESA, RESPONSIVA */}
        <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-4">
          <Button
            type="button"
            onClick={() => push('/homePage')}
            variant="cancel"
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
                    : 'Submeter Proposta ao Orientador'
                  : 'Submeter Proposta'}
              </span>
            )}
          </Button>
        </div>
      </form>
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
