'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowLeft } from 'lucide-react';
import IFPELogo from '../../../public/IFPE Logo.png';

interface AuthNavbarProps {
  showBackToLogin?: boolean;
}

export function AuthNavbar({ showBackToLogin = false }: AuthNavbarProps) {
  const [isLogoHovered, setIsLogoHovered] = useState(false);

  return (
    <header className="flex items-center justify-between border-b border-[#ECEAE2] px-4 py-3 sm:px-8 lg:px-12">
      <div className="flex items-center gap-3 sm:gap-4">
        <Image
          src={IFPELogo}
          alt="Instituto Federal de Pernambuco - Campus Belo Jardim"
          height={32}
          className="h-7 w-auto object-contain sm:h-8"
          priority
        />
        <div className="h-5 w-[1px] bg-[#DDD9CD]" />

        {/* Gradus Flow com animação de inversão de cores */}
        <Link
          href="/"
          className="flex cursor-pointer select-none items-baseline gap-1 no-underline"
          onMouseEnter={() => setIsLogoHovered(true)}
          onMouseLeave={() => setIsLogoHovered(false)}
        >
          <span
            className={`text-base font-bold tracking-tight transition-colors duration-200 sm:text-lg ${
              isLogoHovered ? 'text-[#006633]' : 'text-[#002B18]'
            }`}
          >
            Gradus
          </span>
          <span
            className={`text-base font-normal tracking-tight transition-colors duration-200 sm:text-lg ${
              isLogoHovered ? 'text-[#00A355]' : 'text-[#1F2937]'
            }`}
          >
            Flow
          </span>
        </Link>
      </div>

      {showBackToLogin ? (
        <Link
          href="/"
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#002B18] no-underline transition-colors hover:text-[#006633] sm:text-sm"
        >
          <ArrowLeft size={14} />
          <span>Voltar ao Login</span>
        </Link>
      ) : (
        <span className="hidden text-xs font-semibold tracking-wider text-gray-500 sm:inline-block">
          Portal Acadêmico
        </span>
      )}
    </header>
  );
}
