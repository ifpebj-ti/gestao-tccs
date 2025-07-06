'use client';

import Link from 'next/link';
import { usePathname, useSearchParams } from 'next/navigation';

export default function TccTabs() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const id = searchParams.get('id') || '';

  const tabs = [
    { label: 'Assinaturas', slug: 'signatures' },
    { label: 'Informações', slug: 'details' }
  ];

  if (!id) return null;

  const basePath = pathname.substring(0, pathname.lastIndexOf('/'));

  const isActive = (slug: string) => pathname.endsWith(`/${slug}`);

  return (
    <div className="w-full border-b border-gray-200 overflow-x-auto mb-5 hidden md:block">
      <ul className="flex min-w-full whitespace-nowrap">
        {tabs.map((tab) => (
          <li key={tab.slug} className="flex-shrink-0">
            <Link
              href={`${basePath}/${tab.slug}?id=${id}`}
              className={`inline-block px-4 py-3 text-sm font-medium border-b-4
                transition-all duration-200
                ${
                  isActive(tab.slug)
                    ? 'border-[#1351B4] text-[#1351B4] font-semibold hover:bg-blue-50'
                    : 'border-transparent hover:text-[#1351B4] hover:bg-blue-50'
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
