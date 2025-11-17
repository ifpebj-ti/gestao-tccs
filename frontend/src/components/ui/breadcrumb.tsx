'use client';

import * as React from 'react';
import { usePathname } from 'next/navigation';
import { Slot } from '@radix-ui/react-slot';
import { ChevronRight, MoreHorizontal } from 'lucide-react';

import { cn } from '@/lib/utils';

function Breadcrumb({ ...props }: React.ComponentProps<'nav'>) {
  return <nav aria-label="breadcrumb" data-slot="breadcrumb" {...props} />;
}

function BreadcrumbList({ className, ...props }: React.ComponentProps<'ol'>) {
  return (
    <ol
      data-slot="breadcrumb-list"
      className={cn(
        'text-muted-foreground flex flex-wrap items-center gap-1.5 text-sm break-words sm:gap-2.5',
        className
      )}
      {...props}
    />
  );
}

function BreadcrumbItem({ className, ...props }: React.ComponentProps<'li'>) {
  return (
    <li
      data-slot="breadcrumb-item"
      className={cn('inline-flex items-center gap-1.5', className)}
      {...props}
    />
  );
}

function BreadcrumbLink({
  asChild,
  className,
  ...props
}: React.ComponentProps<'a'> & {
  asChild?: boolean;
}) {
  const Comp = asChild ? Slot : 'a';

  return (
    <Comp
      data-slot="breadcrumb-link"
      className={cn('hover:text-foreground transition-colors', className)}
      {...props}
    />
  );
}

function BreadcrumbPage({ className, ...props }: React.ComponentProps<'span'>) {
  return (
    <span
      data-slot="breadcrumb-page"
      role="link"
      aria-disabled="true"
      aria-current="page"
      className={cn('text-foreground font-normal', className)}
      {...props}
    />
  );
}

function BreadcrumbSeparator({
  children,
  className,
  ...props
}: React.ComponentProps<'li'>) {
  return (
    <li
      data-slot="breadcrumb-separator"
      role="presentation"
      aria-hidden="true"
      className={cn('[&>svg]:size-3.5', className)}
      {...props}
    >
      {children ?? <ChevronRight />}
    </li>
  );
}

function BreadcrumbEllipsis({
  className,
  ...props
}: React.ComponentProps<'span'>) {
  return (
    <span
      data-slot="breadcrumb-ellipsis"
      role="presentation"
      aria-hidden="true"
      className={cn('flex size-9 items-center justify-center', className)}
      {...props}
    >
      <MoreHorizontal className="size-4" />
      <span className="sr-only">More</span>
    </span>
  );
}

// Novo componente dinâmico baseado no mapeamento
export function BreadcrumbAuto({ map = {} }: { map?: Record<string, string> }) {
  const pathname = usePathname();

  const breadcrumbMap: Record<string, string> = {
    homePage: 'Início',
    newTCC: 'Novo TCC',
    newUser: 'Novo Usuário',
    ongoingTCCs: 'TCCs em Andamento',
    signatures: 'Assinaturas',
    details: 'Detalhes',
    pendingSignatures: 'Assinaturas Pendentes',
    signature: 'Assinatura',
    myTCC: 'Meu TCC',
    completedTCCs: 'TCCs Concluídos',
    users: 'Usuários'
  };

  const segments = pathname.split('/').filter(Boolean);

  return (
    <Breadcrumb className="mb-4">
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink href="/homePage">
            {map['homePage'] || 'Início'}
          </BreadcrumbLink>
        </BreadcrumbItem>

        {segments.map((segment, index) => {
          const fullPath = '/' + segments.slice(0, index + 1).join('/');
          const isLast = index === segments.length - 1;
          const label =
            breadcrumbMap[segment] || toTitleCase(decodeURIComponent(segment));
          const isIDPage = segments[index - 1] === 'ongoingTCCs' && !isLast;

          return (
            <React.Fragment key={fullPath}>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                {isLast || isIDPage ? (
                  <BreadcrumbPage className="text-gray-500 select-none">
                    {label}
                  </BreadcrumbPage>
                ) : (
                  <BreadcrumbLink href={fullPath}>{label}</BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </React.Fragment>
          );
        })}
      </BreadcrumbList>
    </Breadcrumb>
  );
}

function toTitleCase(str: string) {
  return str
    .split('-')
    .map((word) => word[0].toUpperCase() + word.slice(1))
    .join(' ');
}

export {
  Breadcrumb,
  BreadcrumbList,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbPage,
  BreadcrumbSeparator,
  BreadcrumbEllipsis
};
