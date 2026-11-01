import type { ReactNode } from 'react';
import { LoaderCircle } from 'lucide-react';

interface PageStateProps {
  children: ReactNode;
  loading?: boolean;
}

export function PageState({ children, loading = false }: PageStateProps) {
  return (
    <div
      role="status"
      className="flex flex-col items-center gap-3 border-y border-[#ECEAE2] px-4 py-12 text-center text-sm text-gray-500"
    >
      {loading && (
        <LoaderCircle
          aria-hidden="true"
          className="size-5 animate-spin text-[#006633] motion-reduce:animate-none"
        />
      )}
      <p>{children}</p>
    </div>
  );
}
