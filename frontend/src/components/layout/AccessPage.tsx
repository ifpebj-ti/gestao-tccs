import type { ReactNode } from 'react';
import { PageTitle } from './PageTitle';

interface AccessPageProps {
  code: string;
  title: string;
  description: string;
  children: ReactNode;
}

export function AccessPage({
  code,
  title,
  description,
  children
}: AccessPageProps) {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-[#FAF9F5] px-6 py-12 text-center">
      <span
        className="font-[family-name:var(--font-poppins)] text-7xl font-extrabold tracking-tight text-[#006633]"
        aria-hidden="true"
      >
        {code}
      </span>
      <PageTitle>{title}</PageTitle>
      <p className="max-w-md text-sm leading-relaxed text-gray-600">
        {description}
      </p>
      {children}
    </main>
  );
}
