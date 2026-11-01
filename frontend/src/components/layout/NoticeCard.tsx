import type { ReactNode } from 'react';
import { Info } from 'lucide-react';
import { cn } from '@/lib/utils';

export function NoticeCard({
  title,
  children,
  className
}: {
  title: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <aside
      className={cn('rounded-2xl bg-[#183E2D] p-5 text-[#FAF9F5]', className)}
    >
      <div className="mb-3 flex items-center gap-2 text-[#C8FF47]">
        <Info className="size-5 shrink-0" aria-hidden="true" />
        <h3 className="text-sm font-bold">{title}</h3>
      </div>
      <div className="text-sm leading-relaxed">{children}</div>
    </aside>
  );
}
