'use client';

import { PatternSelect } from '@/components/ui/form-select';

export function SemesterFilter({
  semesters,
  value,
  onChange
}: {
  semesters: { id: number; name: string; isActive: boolean }[];
  value: number | null;
  onChange: (value: number | null) => void;
}) {
  return (
    <div className="w-full space-y-1 sm:max-w-64">
      <label
        htmlFor="semester-filter"
        className="text-xs font-semibold text-[#183E2D]"
      >
        Semestre letivo
      </label>
      <PatternSelect
        id="semester-filter"
        value={value ?? ''}
        onValueChange={(selected) =>
          onChange(selected ? Number(selected) : null)
        }
      >
        <option value="">Todos os semestres</option>
        {semesters.map((semester) => (
          <option key={semester.id} value={semester.id}>
            {semester.name}
            {semester.isActive ? ' (Ativo)' : ''}
          </option>
        ))}
      </PatternSelect>
    </div>
  );
}
