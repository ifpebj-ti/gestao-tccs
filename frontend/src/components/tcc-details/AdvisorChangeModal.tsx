'use client';

import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter
} from '@/components/ui/dialog';
import { UseFormReturn, SubmitHandler, Controller } from 'react-hook-form';
import { AdvisorChangeSchemaType } from '@/app/schemas/advisorChangeSchema';

interface AdvisorOption {
  id: number;
  name: string;
}

interface AdvisorChangeModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  form: UseFormReturn<AdvisorChangeSchemaType>;
  onSubmit: SubmitHandler<AdvisorChangeSchemaType>;
  type: 'STUDENT_REQUEST' | 'ADVISOR_DEPARTURE';
  availableAdvisors: AdvisorOption[];
  currentAdvisorName?: string;
}

export function AdvisorChangeModal({
  isOpen,
  onOpenChange,
  form,
  onSubmit,
  type,
  availableAdvisors,
  currentAdvisorName
}: AdvisorChangeModalProps) {
  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting }
  } = form;

  const isStudentRequest = type === 'STUDENT_REQUEST';

  const title = isStudentRequest
    ? 'Solicitar Substituição de Orientador'
    : 'Solicitar Afastamento da Orientação';

  const description = isStudentRequest
    ? `Você está solicitando a substituição do(a) orientador(a) ${currentAdvisorName ? `"${currentAdvisorName}"` : 'atual'}. Informe o motivo do descumprimento do termo de compromisso (Art. 19, inciso XII).`
    : 'Você está solicitando seu afastamento da orientação deste TCC. A solicitação deve ser justificada por escrito e requer que um substituto esteja disponível (Art. 18, inciso XVI).';

  const reasonLabel = isStudentRequest
    ? 'Motivo da substituição (descumprimento do termo de compromisso)'
    : 'Justificativa para o afastamento';

  const reasonPlaceholder = isStudentRequest
    ? 'Descreva detalhadamente o descumprimento do termo de compromisso pelo(a) orientador(a)...'
    : 'Justifique por escrito o motivo do afastamento da orientação...';

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-1.5">
              <Label htmlFor="advisor-change-reason">{reasonLabel}</Label>
              <Textarea
                id="advisor-change-reason"
                {...register('reason')}
                placeholder={reasonPlaceholder}
                className="min-h-[120px]"
              />
              {errors.reason && (
                <p className="text-sm text-red-600">{errors.reason.message}</p>
              )}
            </div>

            {availableAdvisors.length > 0 && (
              <div className="grid gap-1.5">
                <Label htmlFor="suggested-advisor">
                  Orientador(a) substituto(a) sugerido(a)
                  {!isStudentRequest && (
                    <span className="text-red-500 ml-1">*</span>
                  )}
                </Label>
                <Controller
                  control={control}
                  name="suggestedAdvisorId"
                  render={({ field }) => (
                    <select
                      id="suggested-advisor"
                      value={field.value ?? 0}
                      onChange={(e) => field.onChange(Number(e.target.value))}
                      onBlur={field.onBlur}
                      className="w-full rounded-md border border-gray-300 bg-white py-2 px-3 text-sm text-gray-900 outline-none focus:ring-2 focus:ring-[#002B18] cursor-pointer"
                    >
                      <option value={0} disabled>
                        Selecione um orientador substituto...
                      </option>
                      {availableAdvisors.map((advisor) => (
                        <option key={advisor.id} value={advisor.id}>
                          {advisor.name}
                        </option>
                      ))}
                    </select>
                  )}
                />
                {errors.suggestedAdvisorId && (
                  <p className="text-sm text-red-600">
                    {errors.suggestedAdvisorId.message}
                  </p>
                )}
              </div>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Enviando...' : 'Enviar Solicitação'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
