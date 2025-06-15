'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

interface TccTabsProps {
  id: string;
}

export default function TccTabs({ id }: TccTabsProps) {
  const pathname = usePathname();

  const tabs = [
    { label: 'Assinaturas', slug: 'signatures' },
    { label: 'Informações', slug: 'details' }
  ];

  const isActive = (slug: string) => pathname.endsWith(`/${slug}`);

  return (
    <div className="w-full border-b border-gray-200 overflow-x-auto hidden lg:block">
      <ul className="flex min-w-full whitespace-nowrap">
        {tabs.map((tab) => (
          <li key={tab.slug} className="flex-shrink-0">
            <Link
              href={`/ongoingTCCs/${id}/${tab.slug}`}
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
