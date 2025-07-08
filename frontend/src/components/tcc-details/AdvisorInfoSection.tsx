'use client';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface Advisor {
  name: string;
  email: string;
}

interface AdvisorInfoSectionProps {
  advisor: Advisor;
}

export function AdvisorInfoSection({ advisor }: AdvisorInfoSectionProps) {
  return (
    <section>
      <h2 className="text-lg font-extrabold uppercase">
        Informações do orientador
      </h2>
      <div className="grid md:grid-cols-2 gap-4 mt-4">
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Nome do orientador</Label>
          <Input value={advisor.name} readOnly />
        </div>
        <div className="grid items-center gap-1.5">
          <Label className="font-semibold">Email do orientador</Label>
          <Input value={advisor.email} readOnly />
        </div>
      </div>
    </section>
  );
}
