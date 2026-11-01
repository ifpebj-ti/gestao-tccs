import type { ReactNode } from 'react';
import { PageTitle } from '@/components/layout/PageTitle';
import { cn } from '@/lib/utils';

interface PageIntroProps {
  eyebrow?: string;
  title: ReactNode;
  description: ReactNode;
  className?: string;
}

export function PageIntro({
  eyebrow,
  title,
  description,
  className = ''
}: PageIntroProps) {
  return (
    <div className={cn('mb-10 max-w-3xl', className)}>
      {eyebrow && (
        <p className="mb-4 flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.16em] text-[#006633]">
          <span
            aria-hidden="true"
            className="size-1.5 rounded-full bg-[#006633]"
          />{' '}
          {eyebrow}
        </p>
      )}
      <PageTitle>{title}</PageTitle>
      <p className="mt-4 max-w-2xl text-sm leading-relaxed text-gray-600">
        {description}
      </p>
    </div>
  );
}
