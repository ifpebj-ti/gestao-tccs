'use client';

import Link from 'next/link';
import { TriangleAlert } from 'lucide-react';
import { AppBrand } from '@/components/layout/AppBrand';
import UserProfile from '@/components/UserProfile';
import { roleLabels } from '@/components/Sidebar/menuConfig';
import type { SessionIdentity } from '@/lib/session';
import type { ReactNode } from 'react';

interface HeaderProps {
  user: SessionIdentity | null;
  onLogout: () => void;
  navigation?: ReactNode;
}

export default function Header({ user, onLogout, navigation }: HeaderProps) {
  return (
    <header className="relative z-30 bg-[#FAF9F5]">
      <div className="flex min-h-14 items-center justify-between px-3.5 sm:px-6 md:px-9 lg:px-12">
        <div className="flex items-center gap-1 sm:gap-2">
          {navigation}
          <div className="md:hidden">
            <AppBrand className="h-14 px-1 sm:px-3" />
          </div>
        </div>
        <div className="flex min-w-0 items-center justify-end gap-2 sm:gap-4">
          <div className="flex shrink-0 items-center gap-2 sm:gap-3">
            {user && (
              <UserProfile
                user={{
                  name: user.name,
                  roles: user.roles.map((role) => roleLabels[role])
                }}
                onLogout={onLogout}
              />
            )}
          </div>
        </div>
      </div>
      {user?.isDefaultPassword && (
        <div className="flex items-center justify-center gap-2 border-t border-amber-200 bg-amber-50 px-4 py-2 text-xs text-amber-800">
          <TriangleAlert className="size-4 shrink-0" aria-hidden="true" />
          <span>
            <strong>Alerta:</strong> Sua senha é a padrão. Por favor,{' '}
            <Link
              href="/profile?section=password"
              className="font-bold underline underline-offset-2"
            >
              altere sua senha
            </Link>{' '}
            para mais segurança.
          </span>
        </div>
      )}
    </header>
  );
}
