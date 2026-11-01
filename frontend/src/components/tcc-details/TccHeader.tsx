'use client';

import { PageTitle } from '@/components/layout/PageTitle';

import { Badge } from '@/components/ui/badge';

interface TccHeaderProps {
  cancellationRequested: boolean;
  title?: string;
}

export function TccHeader({ cancellationRequested, title }: TccHeaderProps) {
  return (
    <div className="flex flex-col md:flex-row md:items-center md:gap-4">
      <div className="min-w-0">
        <PageTitle className="break-words text-2xl md:text-3xl">
          {title || 'TCC'}
        </PageTitle>
      </div>
      {cancellationRequested && (
        <Badge variant="destructive" className="text-sm w-fit mt-2 md:mt-0">
          Cancelamento solicitado
        </Badge>
      )}
    </div>
  );
}
