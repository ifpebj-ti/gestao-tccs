'use client';

import React from 'react';
import { AuthNavbar } from './AuthNavbar';
import { AuthTicker } from './AuthTicker';

interface AuthLayoutProps {
  children: React.ReactNode;
  showBackToLogin?: boolean;
  gridCols?: string;
}

export function AuthLayout({ children, showBackToLogin = false, gridCols = '1.15fr 0.85fr' }: AuthLayoutProps) {
  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        background: '#FAF9F5',
        color: '#111827',
        fontFamily: 'var(--font-poppins), -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
        overflowX: 'hidden'
      }}
    >
      <AuthNavbar showBackToLogin={showBackToLogin} />

      <main
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          padding: '2.5rem 3rem',
          maxWidth: '1240px',
          width: '100%',
          margin: '0 auto',
          boxSizing: 'border-box'
        }}
      >
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: gridCols,
            gap: '4.5rem',
            width: '100%',
            alignItems: 'center'
          }}
        >
          {children}
        </div>
      </main>

      <AuthTicker />
    </div>
  );
}