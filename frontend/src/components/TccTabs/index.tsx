'use client';

import Link from 'next/link';
import { usePathname, useSearchParams } from 'next/navigation';

export default function TccTabs({ tccId }: { tccId?: string } = {}) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const id = tccId ?? searchParams.get('id') ?? '';
  const completed = pathname.startsWith('/completedTCCs');

  const tabs = [
    ...(!completed ? [{ label: 'Assinaturas', slug: 'signatures' }] : []),
    { label: 'Informações', slug: 'details' },
    { label: 'Arquivos', slug: 'files' }
  ];

  if (!id) return null;

  const basePath = pathname.substring(0, pathname.lastIndexOf('/'));

  const isActive = (slug: string) =>
    completed && slug === 'details'
      ? !pathname.endsWith('/files')
      : pathname.endsWith(`/${slug}`);

  return (
    <div className="w-full border-b border-gray-200 overflow-x-auto mb-5">
      <ul className="flex min-w-full whitespace-nowrap">
        {tabs.map((tab) => (
          <li key={tab.slug} className="flex-shrink-0">
            <Link
              href={
                completed && tab.slug === 'details'
                  ? `/completedTCCs/${id}`
                  : `${basePath}/${tab.slug}?id=${id}`
              }
              aria-current={isActive(tab.slug) ? 'page' : undefined}
              className={`inline-block px-4 py-3 text-sm font-medium border-b-4
                transition-colors duration-150 duration-200
                ${
                  isActive(tab.slug)
                    ? 'border-[#006633] text-[#006633] font-semibold'
                    : 'border-transparent hover:text-[#006633]'
                }`}
            >
              {tab.label}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
