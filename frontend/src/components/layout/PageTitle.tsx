import type { ComponentProps } from 'react';
import { cn } from '@/lib/utils';

export function PageTitle({ className, ...props }: ComponentProps<'h1'>) {
  return (
    <h1
      className={cn(
        'font-[family-name:var(--font-poppins)] text-2xl font-semibold leading-tight tracking-tight text-[#002B18]',
        className
      )}
      {...props}
    />
  );
}
