import type { ComponentProps, ReactNode } from 'react';
import { CollapseCard } from '@/components/CollapseCard';

type ResponsiveRecordCardProps = Omit<
  ComponentProps<typeof CollapseCard>,
  'children' | 'variant'
> & {
  children: ReactNode;
};

// Mantém as ações expansíveis no celular e o acesso direto no desktop.
export function ResponsiveRecordCard({
  children,
  onClick,
  ...props
}: ResponsiveRecordCardProps) {
  return (
    <div className="min-w-0">
      <div className="md:hidden">
        <CollapseCard {...props} variant="flat">
          {children}
        </CollapseCard>
      </div>
      <div className="hidden md:block">
        <CollapseCard {...props} variant="flat" onClick={onClick} />
      </div>
    </div>
  );
}
