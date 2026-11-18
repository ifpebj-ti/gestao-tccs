'use client';

import React from 'react';
import { AuthNavbar } from './AuthNavbar';
import { AuthTicker } from './AuthTicker';

interface AuthLayoutProps {
  children: React.ReactNode;
  showBackToLogin?: boolean;
  gridCols?: string;
}

export function AuthLayout({
  children,
  showBackToLogin = false,
  gridCols = '1.15fr 0.85fr'
}: AuthLayoutProps) {
  return (
    <div className="flex min-h-dvh flex-col justify-between overflow-x-hidden bg-[#FAF9F5] font-[family-name:var(--font-poppins)] text-gray-900">
      <AuthNavbar showBackToLogin={showBackToLogin} />

      <main className="mx-auto box-border flex w-full max-w-[1240px] flex-1 items-center justify-center p-4 sm:p-8 md:p-10 lg:p-12">
        <div
          className="grid w-full items-center gap-8 sm:gap-12 lg:gap-16 [grid-template-columns:1fr] lg:[grid-template-columns:var(--auth-grid)]"
          style={{ '--auth-grid': gridCols } as React.CSSProperties}
        >
          {children}
        </div>
      </main>

      <AuthTicker />
    </div>
  );
}
