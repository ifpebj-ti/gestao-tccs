'use client';

import Link from 'next/link';
import { ChevronDown, CircleUserRound, KeyRound, LogOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';

interface UserProfileProps {
  user: { name: string; roles: string[] };
  onLogout: () => void;
}

export default function UserProfile({ user, onLogout }: UserProfileProps) {
  const firstName = user.name.trim().split(/\s+/)[0];
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          aria-label="Abrir menu do usuário"
          className="gap-2 rounded-full px-2 hover:bg-[#ECEAE2]"
        >
          <CircleUserRound
            className="size-6 text-[#002B18]"
            strokeWidth={1.5}
            aria-hidden="true"
          />
          <span className="hidden text-sm font-semibold text-[#002B18] lg:inline">
            {firstName}
          </span>
          <ChevronDown
            className="hidden size-3.5 text-gray-500 sm:block"
            aria-hidden="true"
          />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        className="w-64 rounded-xl border-[#DDD9CD] bg-[#FAF9F5] p-2"
        align="end"
      >
        <DropdownMenuLabel className="px-3 py-3 font-normal">
          <p className="font-[family-name:var(--font-poppins)] text-sm font-semibold text-[#002B18]">
            {user.name}
          </p>
          <p className="mt-1 text-xs text-gray-500">{user.roles.join(' · ')}</p>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link
            href="/profile?section=password"
            className="cursor-pointer gap-2 py-2.5"
          >
            <KeyRound size={16} /> Alterar senha
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={onLogout}
          className="cursor-pointer gap-2 py-2.5 text-red-700"
        >
          <LogOut size={16} /> Sair da conta
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
