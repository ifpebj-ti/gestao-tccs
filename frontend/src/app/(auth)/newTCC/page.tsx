'use client';

import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { faEnvelope, faPlus, faTrash } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useRouter } from 'next/navigation';
import { useNewTccForm } from '@/app/hooks/useNewTcc';

export default function NewTcc() {
  const { push } = useRouter();
  const {
    form,
    submitForm,
    advisors,
    courses,
    fields,
    append,
    remove,
    isSubmitting
  } = useNewTccForm();
  const {
    register,
    handleSubmit,
    formState: { errors }
  } = form;

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal mb-10">
        Nova Proposta de TCC
      </h1>

      <form className="flex flex-col gap-8" onSubmit={handleSubmit(submitForm)}>
        <div>
          <h2 className="text-lg font-extrabold uppercase mb-4">
            Informações do(s) estudante(s)
          </h2>

          {/* Mapeamento dinâmico dos campos de estudante */}
          {fields.map((field, index) => (
            <div
              key={field.id}
              className="grid grid-cols-1 md:grid-cols-[1fr_1fr_auto] gap-4 mb-4 items-end"
            >
              {/* Campo de Email */}
              <div className="grid items-center gap-1.5">
                <Label
                  className="font-semibold"
                  htmlFor={`students.${index}.studentEmail`}
                >
                  Estudante {index + 1}
                </Label>
                <Input
                  id={`students.${index}.studentEmail`}
                  placeholder="Digite o email do estudante"
                  icon={faEnvelope}
                  errorText={errors.students?.[index]?.studentEmail?.message}
                  {...register(`students.${index}.studentEmail`)}
                />
              </div>

              {/* Campo de Curso */}
              <div className="grid items-center gap-1.5">
                <Label
                  className="font-semibold"
                  htmlFor={`students.${index}.courseId`}
                >
                  Curso do Estudante {index + 1}
                </Label>
                <select
                  id={`students.${index}.courseId`}
                  {...register(`students.${index}.courseId`)}
                  className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
                >
                  <option value={0}>Selecione um curso</option>
                  {courses.map((course) => (
                    <option key={course.id} value={course.id}>
                      {course.name}
                    </option>
                  ))}
                </select>
                {errors.students?.[index]?.courseId && (
                  <p className="text-red-500 text-sm mt-1">
                    {errors.students?.[index]?.courseId?.message}
                  </p>
                )}
              </div>

              {/* Botão de Remover */}
              {fields.length > 1 && (
                <Button
                  type="button"
                  variant="ghost"
                  className="text-red-600 hover:text-red-800"
                  onClick={() => remove(index)}
                >
                  <FontAwesomeIcon icon={faTrash} />
                </Button>
              )}
            </div>
          ))}

          {/* Botão de Adicionar */}
          {fields.length < 4 && (
            <Button
              type="button"
              variant="outline"
              className="w-fit self-end"
              onClick={() => append({ studentEmail: '', courseId: 0 })}
            >
              <FontAwesomeIcon icon={faPlus} className="mr-2" />
              Adicionar estudante
            </Button>
          )}
        </div>

        {/* Informações da Orientação e TCC */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div>
            <h2 className="text-lg font-extrabold uppercase mb-4">
              Informações da orientação
            </h2>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="orientador">
                Orientador
              </Label>
              <select
                id="orientador"
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
                {...register('advisorId')}
              >
                <option value={0}>Selecione um orientador</option>
                {advisors.map((advisor) => (
                  <option key={advisor.id} value={advisor.id}>
                    {advisor.name}
                  </option>
                ))}
              </select>
              {errors.advisorId && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.advisorId.message}
                </p>
              )}
            </div>
          </div>
          <div>
            <h2 className="text-lg font-extrabold uppercase mb-4">
              Informações do TCC
            </h2>
            <div className="flex flex-col gap-4">
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold" htmlFor="titulo">
                  Título
                </Label>
                <Input
                  id="titulo"
                  placeholder="Digite o título do TCC"
                  {...register('title')}
                  errorText={errors.title?.message}
                />
              </div>
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold" htmlFor="summary">
                  Resumo
                </Label>
                <Textarea
                  id="summary"
                  placeholder="Digite o resumo da proposta"
                  {...register('summary')}
                  className="flex items-center min-h-[100px] border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
                />
                {errors.summary && (
                  <p className="text-red-500 text-sm mt-1">
                    {errors.summary.message}
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="flex gap-2 md:self-end mt-4">
          <Button
            onClick={() => push('/homePage')}
            variant="outline"
            className="w-full md:w-fit"
          >
            Cancelar
          </Button>
          <Button
            type="submit"
            className="w-full md:w-fit"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Submetendo...' : 'Submeter Proposta'}
          </Button>
        </div>
      </form>
    </div>
  );
}
