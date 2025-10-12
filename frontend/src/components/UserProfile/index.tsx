'use client';

import { CircleUserRound, LogOut, ShieldCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';

interface User {
  name: string;
  roles: string[];
}

interface UserProfileProps {
  user: User;
  onLogout: () => void;
}

export default function UserProfile({ user, onLogout }: UserProfileProps) {
  return (
    <DropdownMenu>
      {/* O ícone que o usuário irá clicar */}
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="default" className="rounded-full">
          <CircleUserRound className="h-6 w-6" />
        </Button>
      </DropdownMenuTrigger>

      {/* O conteúdo que aparece ao clicar */}
      <DropdownMenuContent className="w-56" align="end" forceMount>
        {/* Nome do usuário */}
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col space-y-1">
            <p className="text-xs font-semibold mb-2 text-muted-foreground">
              Usuário:
            </p>
            <p className="text-sm font-medium leading-none">{user.name}</p>
          </div>
        </DropdownMenuLabel>

        {/* Separador visual */}
        <DropdownMenuSeparator />

        {/* Lista de perfis/roles do usuário */}
        <div className="px-2 py-1.5">
          <p className="text-xs font-semibold mb-2 text-muted-foreground">
            Seus Perfis:
          </p>
          {user.roles.map((role) => (
            <div
              key={role}
              className="flex items-center text-sm text-gray-800 mb-1 last:mb-0"
            >
              <ShieldCheck className="mr-2 h-4 w-4 text-green-600 flex-shrink-0" />
              <span>{role}</span>
            </div>
          ))}
        </div>

        <DropdownMenuSeparator />

        {/* Item de Logout */}
        <DropdownMenuItem onClick={onLogout} className="cursor-pointer">
          <LogOut className="mr-2 h-4 w-4" />
          <span>Sair</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
