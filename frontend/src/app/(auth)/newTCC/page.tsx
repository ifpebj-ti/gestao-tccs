'use client';

import { useState, useEffect } from 'react';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { faEnvelope, faPlus, faTrash } from '@fortawesome/free-solid-svg-icons';
import { useNewTccForm } from '@/app/hooks/useNewTcc';

export default function NewTcc() {
  const { form, submitForm, advisors } = useNewTccForm();
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors }
  } = form;

  const [activeFields, setActiveFields] = useState(1);

  useEffect(() => {
    const allEmails = [];
    for (let i = 0; i < activeFields; i++) {
      allEmails.push('');
    }
    setValue('studentEmails', allEmails);
  }, [activeFields, setValue]);

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal mb-10">
        Nova Proposta de TCC
      </h1>

      <form className="flex flex-col gap-4" onSubmit={handleSubmit(submitForm)}>
        <h2 className="text-lg font-extrabold uppercase">
          Informações do(s) estudante(s)
        </h2>

        {/* Campo 1 */}
        <div className="flex items-center gap-2">
          <div className="grid items-center gap-1.5 w-full">
            <Label className="font-semibold" htmlFor="email">
              Estudante 1
            </Label>
            <Input
              id="studentEmails.0"
              placeholder="Digite o email do estudante"
              icon={faEnvelope}
              errorText={errors.studentEmails?.[0]?.message}
              {...register('studentEmails.0')}
            />
          </div>
        </div>

        {/* Campo 2 */}
        {activeFields > 1 && (
          <div className="flex items-center gap-2">
            <div className="grid items-center gap-1.5 w-full">
              <Label className="font-semibold" htmlFor="email">
                Estudante 2
              </Label>
              <Input
                id="studentEmails.1"
                placeholder="Digite o email do estudante"
                icon={faEnvelope}
                errorText={errors.studentEmails?.[1]?.message}
                {...register('studentEmails.1')}
              />
            </div>
            <Button
              type="button"
              variant="ghost"
              className="text-red-600 hover:text-red-800 mt-5"
              onClick={() => setActiveFields((prev) => prev - 1)}
              icon={faTrash}
            />
          </div>
        )}

        {/* Campo 3 */}
        {activeFields > 2 && (
          <div className="flex items-center gap-2">
            <div className="grid items-center gap-1.5 w-full">
              <Label className="font-semibold" htmlFor="email">
                Estudante 3
              </Label>
              <Input
                id="studentEmails.2"
                placeholder="Digite o email do estudante"
                icon={faEnvelope}
                errorText={errors.studentEmails?.[2]?.message}
                {...register('studentEmails.2')}
              />
            </div>
            <Button
              type="button"
              variant="ghost"
              className="text-red-600 hover:text-red-800 mt-5"
              onClick={() => setActiveFields((prev) => prev - 1)}
              icon={faTrash}
            />
          </div>
        )}

        {/* Campo 4 */}
        {activeFields > 3 && (
          <div className="flex items-center gap-2">
            <div className="grid items-center gap-1.5 w-full">
              <Label className="font-semibold" htmlFor="email">
                Estudante 4
              </Label>
              <Input
                id="studentEmails.3"
                placeholder="Digite o email do estudante"
                icon={faEnvelope}
                errorText={errors.studentEmails?.[3]?.message}
                {...register('studentEmails.3')}
              />
            </div>
            <Button
              type="button"
              variant="ghost"
              className="text-red-600 hover:text-red-800 mt-5"
              onClick={() => setActiveFields((prev) => prev - 1)}
              icon={faTrash}
            />
          </div>
        )}

        {/* Botão de adicionar */}
        {activeFields < 4 && (
          <Button
            type="button"
            variant="outline"
            icon={faPlus}
            className="w-min self-center md:self-end"
            onClick={() => setActiveFields((prev) => prev + 1)}
          >
            Adicionar estudante
          </Button>
        )}

        {/* Informações da orientação */}
        <h2 className="text-lg font-extrabold uppercase">
          Informações da orientação
        </h2>
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold" htmlFor="orientador">
            Orientador
          </Label>
          <select
            id="orientador"
            className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
            {...register('advisorId', { valueAsNumber: true })}
          >
            <option value={0} className="">
              Selecione um orientador
            </option>
            {advisors.map((advisor) => (
              <option key={advisor.id} value={advisor.id}>
                {advisor.name}
              </option>
            ))}
          </select>
        </div>

        {/* Informações do TCC */}
        <h2 className="text-lg font-extrabold uppercase">Informações do TCC</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
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
          <div className="flex flex-col gap-1.5">
            <Label className="font-semibold" htmlFor="descricao">
              Descrição
            </Label>
            <Input
              id="descricao"
              placeholder="Digite a descrição do TCC"
              {...register('summary')}
              errorText={errors.summary?.message}
            />
          </div>
        </div>

        <div className="flex gap-2 md:self-end">
          <Button variant="outline" className="w-full md:w-fit">
            Cancelar
          </Button>
          <Button type="submit" className="w-full md:w-fit">
            Submeter
          </Button>
        </div>
      </form>
    </div>
  );
}
