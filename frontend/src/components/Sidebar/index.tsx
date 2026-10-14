'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useEffect, useState } from 'react';
import { AppBrand } from '@/components/layout/AppBrand';
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import {
  getSidebarItems,
  activeSidebarHref,
  type StudentNavigation,
  type UserRole
} from './menuConfig';

interface SidebarProps {
  roles: UserRole[];
  student: StudentNavigation;
  compact?: boolean;
  onNavigate?: () => void;
  onToggle?: () => void;
}

export default function Sidebar({
  roles,
  student,
  compact = false,
  onNavigate,
  onToggle
}: SidebarProps) {
  const pathname = usePathname();
  const [selection, setSelection] = useState<{
    from: string;
    href: string;
  } | null>(null);
  const items = getSidebarItems(roles, student);
  const currentHref = activeSidebarHref(items, pathname);
  useEffect(() => {
    setSelection(null);
  }, [pathname]);

  return (
    <aside className="flex min-h-dvh min-w-0 flex-col border-r border-[#ECEAE2] bg-[#FAF9F5] text-[#002B18] md:sticky md:top-0 md:h-dvh md:overflow-y-auto">
      <AppBrand compact={compact} />
      <nav aria-label="Menu principal" className="flex flex-col gap-1 p-3">
        {items.map((item) => {
          const Icon = item.icon;
          const pending = selection?.from === pathname ? selection.href : null;
          const active = pending
            ? pending === item.href
            : currentHref === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              aria-current={currentHref === item.href ? 'page' : undefined}
              onClick={(event) => {
                if (
                  !event.metaKey &&
                  !event.ctrlKey &&
                  !event.shiftKey &&
                  !event.altKey
                ) {
                  setSelection({ from: pathname, href: item.href });
                  onNavigate?.();
                }
              }}
              aria-label={compact ? item.label : undefined}
              title={compact ? item.label : undefined}
              className={`flex shrink-0 items-center gap-3 rounded-lg px-4 py-3 text-sm font-medium transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#006633] ${active ? 'bg-[#002B18] text-[#FAF9F5]' : 'text-[#374151] hover:bg-[#ECEAE2] hover:text-[#002B18]'}`}
            >
              <Icon size={18} strokeWidth={1.7} aria-hidden="true" />
              <span className={compact ? 'sr-only' : ''}>{item.label}</span>
            </Link>
          );
        })}
      </nav>
      {onToggle && (
        <button
          type="button"
          aria-label={compact ? 'Expandir menu' : 'Recolher menu'}
          aria-expanded={!compact}
          onClick={onToggle}
          className="mx-3 mt-auto mb-6 flex cursor-pointer items-center justify-center gap-2 rounded-lg px-3 py-3 text-sm hover:bg-[#002B18] hover:text-[#C8FF47] focus-visible:outline-2 focus-visible:outline-[#006633]"
        >
          {compact ? <PanelLeftOpen size={18} /> : <PanelLeftClose size={18} />}
          {!compact && <span>Recolher menu</span>}
        </button>
      )}
      {roles.includes('STUDENT') && student.status === 'loading' && (
        <p role="status" className="px-8 py-3 text-xs text-gray-500">
          Consultando seu TCC…
        </p>
      )}
      {roles.includes('STUDENT') && student.status === 'error' && (
        <p
          role="status"
          className="px-8 py-3 text-xs leading-relaxed text-gray-500"
        >
          Não foi possível consultar seu TCC. Tente novamente ao atualizar a
          página.
        </p>
      )}
    </aside>
  );
}
