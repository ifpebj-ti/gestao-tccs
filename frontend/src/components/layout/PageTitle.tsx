import type { ComponentProps } from 'react';
import { cn } from '@/lib/utils';

export function PageTitle({ className, ...props }: ComponentProps<'h1'>) {
  return (
    <h1
      className={cn(
        'font-[family-name:var(--font-poppins)] text-3xl font-extrabold leading-[1.1] tracking-[-0.035em] text-[#002B18] md:text-4xl',
        className
      )}
      {...props}
    />
  );
}
