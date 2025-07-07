'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface TccInfoSectionProps {
  infoTcc: {
    title: string;
    summary: string;
    presentationDate: string;
    presentationTime: string;
    presentationLocation: string;
  };
}

export function TccInfoSection({ infoTcc }: TccInfoSectionProps) {
  return (
    <div>
      <h2 className="text-lg font-extrabold uppercase">Informações do TCC</h2>
      <div className="grid md:grid-cols-2 gap-4 mt-4">
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Título da proposta</Label>
          <Input value={infoTcc.title} readOnly />
        </div>
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Resumo da proposta</Label>
          <Input value={infoTcc.summary} readOnly />
        </div>
        {infoTcc.presentationDate && (
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Data da apresentação</Label>
            <Input value={infoTcc.presentationDate} readOnly />
          </div>
        )}
        {infoTcc.presentationTime && (
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold">Hora da apresentação</Label>
            <Input value={infoTcc.presentationTime} readOnly />
          </div>
        )}
        {infoTcc.presentationLocation && (
          <div className="grid items-center gap-1.5 md:col-span-2">
            <Label className="font-semibold">Local da apresentação</Label>
            <Input value={infoTcc.presentationLocation} readOnly />
          </div>
        )}
      </div>
    </div>
  );
}
