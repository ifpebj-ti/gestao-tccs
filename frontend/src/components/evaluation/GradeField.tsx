import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useId } from 'react';

interface GradeFieldProps {
  label: string;
  max: number;
  value: string;
  onChange: (value: string) => void;
}

export function GradeField({ label, max, value, onChange }: GradeFieldProps) {
  const id = useId();

  return (
    <div className="flex items-center justify-between gap-4 border-b border-[#ECEAE2] py-4">
      <Label
        htmlFor={id}
        className="flex-1 normal-case tracking-normal text-sm font-medium"
      >
        {label}
        <span className="mt-1 block text-xs font-normal text-gray-500">
          0 a {max.toString().replace('.', ',')}
        </span>
      </Label>
      <div className="w-20 shrink-0">
        <Input
          id={id}
          type="number"
          step="0.1"
          min={0}
          max={max}
          required
          placeholder="0,0"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          className="text-center font-bold"
        />
      </div>
    </div>
  );
}
