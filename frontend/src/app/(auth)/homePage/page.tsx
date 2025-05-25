'use client';

import { BreadcrumbAuto } from '@/components/ui/breadcrumb';

export default function HomePage() {
  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold text-gray-800">
        Página Inicial
      </h1>
    </div>
  );
}
