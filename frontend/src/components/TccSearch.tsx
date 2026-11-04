'use client';

import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';

export function TccSearch({
  value,
  onChange,
  includeAdvisor = true,
  advisorSearchUnavailable
}: {
  value: string;
  onChange: (value: string) => void;
  includeAdvisor?: boolean;
  advisorSearchUnavailable: boolean;
}) {
  return (
    <div className="min-w-0 w-full space-y-1">
      <label
        htmlFor="tcc-search"
        className="text-xs font-semibold text-[#183E2D]"
      >
        Buscar TCC
      </label>
      <Input
        id="tcc-search"
        type="search"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={
          includeAdvisor
            ? 'Nome do estudante, título do TCC ou professor'
            : 'Nome do estudante ou título do TCC'
        }
        icon={<Search size={18} aria-hidden="true" />}
        helperText={
          includeAdvisor && advisorSearchUnavailable
            ? 'Não foi possível carregar os professores. Você ainda pode buscar por estudante ou título do TCC.'
            : undefined
        }
      />
    </div>
  );
}
