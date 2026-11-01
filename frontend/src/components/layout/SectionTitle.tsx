import type { ComponentProps } from 'react';
import { cn } from '@/lib/utils';

export function SectionTitle({ className, ...props }: ComponentProps<'h2'>) {
  return (
    <h2
      className={cn(
        'border-b border-[#ECEAE2] pb-3 font-[family-name:var(--font-poppins)] text-xs font-bold uppercase tracking-[0.06em] text-gray-700',
        className
      )}
      {...props}
    />
  );
}
