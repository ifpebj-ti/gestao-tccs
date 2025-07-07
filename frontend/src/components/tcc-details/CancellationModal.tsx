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
import { UseFormReturn, SubmitHandler } from 'react-hook-form';
import { CancellationSchemaType } from '@/app/schemas/tccCancellationSchema';

interface CancellationModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  form: UseFormReturn<CancellationSchemaType>;
  onSubmit: SubmitHandler<CancellationSchemaType>;
}

export function CancellationModal({
  isOpen,
  onOpenChange,
  form,
  onSubmit
}: CancellationModalProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting }
  } = form;

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogHeader>
            <DialogTitle>Solicitar Cancelamento do TCC</DialogTitle>
            <DialogDescription>
              Descreva o motivo pelo qual você está solicitando o cancelamento.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <Label htmlFor="reason">Motivo</Label>
            <Textarea
              id="reason"
              {...register('reason')}
              placeholder="Explique detalhadamente o motivo aqui..."
              className="min-h-[100px]"
            />
            {errors.reason && (
              <p className="text-sm text-red-600">{errors.reason.message}</p>
            )}
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
            >
              Fechar
            </Button>
            <Button type="submit" variant="destructive" disabled={isSubmitting}>
              {isSubmitting ? 'Enviando...' : 'Enviar Solicitação'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
