'use client';

import { Button } from '@/components/ui/button';
import {
  faArrowRightFromBracket,
  faBell
} from '@fortawesome/free-solid-svg-icons';
import Link from 'next/link';
import IFPELogo from '../../../public/IFPE Logo.png';
import Image from 'next/image';

export default function Header() {
  return (
    <header className="fixed top-0 left-0 w-full bg-white shadow z-50">
      <div className="mx-auto px-4 md:px-10 py-3 flex flex-col md:flex-row md:items-center gap-2 w-full">
        {/* Logo */}
        <div className="flex justify-between items-center w-full md:w-1/3">
          <Link href="/">
            <Image
              src={IFPELogo}
              alt="IFPE Logo"
              className="h-8 w-auto md:h-12"
            />
          </Link>

          {/* Botões no mobile ficam à direita */}
          <nav className="flex items-center gap-2 md:hidden">
            <Button variant="ghost" icon={faBell} aria-label="Notificações" />
            <Button variant="ghost" icon={faArrowRightFromBracket}>
              <Link href="/">Sair</Link>
            </Button>
          </nav>
        </div>

        {/* Título mobile */}
        <h1 className="block md:hidden text-center text-lg font-semibold text-gray-800">
          Gestão de TCCs
        </h1>

        {/* Título desktop */}
        <div className="hidden md:flex justify-center w-1/3">
          <h1 className="text-xl font-semibold text-gray-800">
            Gestão de TCCs
          </h1>
        </div>

        {/* Botões desktop */}
        <nav className="hidden md:flex justify-end items-center gap-4 w-1/3">
          <Button variant="ghost" icon={faBell} aria-label="Notificações" />
          <Button variant="ghost" icon={faArrowRightFromBracket}>
            <Link href="/">Sair</Link>
          </Button>
        </nav>
      </div>
    </header>
  );
}
