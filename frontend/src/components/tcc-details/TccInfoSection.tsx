'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { EditTccSchemaType } from '@/app/schemas/editTccSchema';
import { Pencil, X, Check } from 'lucide-react';

interface TccInfoSectionProps {
  infoTcc: {
    title: string;
    summary: string;
    presentationDate: string | null;
    presentationTime: string | null;
    presentationLocation: string;
  };

  isEditingInfo?: boolean;
  onToggleEditInfo?: (val: boolean) => void;
  editForm?: UseFormReturn<EditTccSchemaType>;
  onEditSubmit?: SubmitHandler<EditTccSchemaType>;
}

export function TccInfoSection({
  infoTcc,
  isEditingInfo = false,
  onToggleEditInfo,
  editForm,
  onEditSubmit
}: TccInfoSectionProps) {
  const canEdit = !!editForm && !!onToggleEditInfo && !!onEditSubmit;

  const registerEdit = editForm?.register;
  const handleSubmitEdit = editForm?.handleSubmit;
  const formStateEdit = editForm?.formState;
  const resetEdit = editForm?.reset;

  const isSubmittingEdit = formStateEdit?.isSubmitting;
  const errorsEdit = formStateEdit?.errors;



  const handleCancelEdit = () => {
    if (resetEdit && onToggleEditInfo) {
      resetEdit({ title: infoTcc.title ?? '', summary: infoTcc.summary ?? '' });
      onToggleEditInfo(false);
    }
  };

  return (
    <section>
      <div className="flex justify-between items-center">
        <h2 className="text-lg font-extrabold uppercase">Informações do TCC</h2>

        {canEdit &&
          (!isEditingInfo ? (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onToggleEditInfo && onToggleEditInfo(true)}
            >
              <Pencil className="w-4 h-4 mr-2" />
              Editar
            </Button>
          ) : (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleCancelEdit}
              className="text-red-500 hover:text-red-700 hover:bg-red-50"
            >
              <X className="w-4 h-4 mr-2" />
              Cancelar Edição
            </Button>
          ))}
      </div>

      <form
        id="edit-tcc-form"
        onSubmit={
          handleSubmitEdit && onEditSubmit
            ? handleSubmitEdit(onEditSubmit)
            : undefined
        }
        className="grid md:grid-cols-2 gap-4 mt-4"
      >
        <div className="grid items-center gap-1.5 md:col-span-2">
          <Label className="font-semibold" htmlFor="tcc-title">
            Título da proposta
            {isEditingInfo && <span className="text-red-500">*</span>}
          </Label>

          {isEditingInfo && registerEdit ? (
            <div className="flex flex-col gap-1">
              <Input
                key="title-edit"
                id="tcc-title"
                errorText={errorsEdit?.title?.message || ''}
                {...registerEdit('title')}
              />
            </div>
          ) : (
            <Input
              key="title-view"
              id="tcc-title-view"
              readOnly
              value={infoTcc.title || ''}
            />
          )}
        </div>

        <div className="grid items-center gap-1.5 md:col-span-2">
          <Label className="font-semibold" htmlFor="tcc-summary">
            Resumo da proposta
            {isEditingInfo && <span className="text-red-500">*</span>}
          </Label>
          <div className="relative">
            {isEditingInfo && registerEdit ? (
              <div className="flex flex-col gap-1">
                <Input
                  key="summary-edit"
                  id="tcc-summary"
                  errorText={errorsEdit?.summary?.message || ''}
                  {...registerEdit('summary')}
                />
              </div>
            ) : (
              <Input
                key="summary-view"
                id="tcc-summary-view"
                readOnly
                value={infoTcc.summary || ''}
              />
            )}
          </div>
        </div>

        {isEditingInfo && (
          <div className="md:col-span-2 flex justify-end mt-2">
            <Button type="submit" disabled={isSubmittingEdit}>
              {isSubmittingEdit ? (
                'Salvando...'
              ) : (
                <>
                  <Check className="w-4 h-4 mr-2" /> Salvar Alterações
                </>
              )}
            </Button>
          </div>
        )}
      </form>

    </section>
  );
}
