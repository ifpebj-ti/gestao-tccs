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
    <header
      style={{
        padding: '0.85rem 3rem',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        borderBottom: '1px solid #ECEAE2'
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
        <Image
          src={IFPELogo}
          alt="Instituto Federal de Pernambuco - Campus Belo Jardim"
          height={36}
          style={{ width: 'auto', height: '36px', objectFit: 'contain' }}
          priority
        />
        <div style={{ width: '1px', height: '22px', background: '#DDD9CD' }} />

        {/* Gradus Flow com animação de inversão de cores */}
        <Link
          href="/"
          style={{
            textDecoration: 'none',
            display: 'flex',
            alignItems: 'baseline',
            gap: '0.35rem',
            cursor: 'pointer',
            userSelect: 'none'
          }}
          onMouseEnter={() => setIsLogoHovered(true)}
          onMouseLeave={() => setIsLogoHovered(false)}
        >
          <span
            style={{
              fontSize: '1.1rem',
              fontWeight: 700,
              color: isLogoHovered ? '#006633' : '#002B18',
              letterSpacing: '-0.03em',
              transition: 'color 0.25s ease'
            }}
          >
            Gradus
          </span>
          <span
            style={{
              fontSize: '1.1rem',
              fontWeight: 400,
              color: isLogoHovered ? '#002B18' : '#006633',
              letterSpacing: '-0.03em',
              transition: 'color 0.25s ease'
            }}
          >
            Flow
          </span>
        </Link>
      </div>

      {showBackToLogin ? (
        <Link
          href="/"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.4rem',
            fontSize: '0.8rem',
            fontWeight: 600,
            color: '#002B18',
            textDecoration: 'none',
            transition: 'color 0.15s ease'
          }}
          onMouseEnter={e => (e.currentTarget.style.color = '#006633')}
          onMouseLeave={e => (e.currentTarget.style.color = '#002B18')}
        >
          <ArrowLeft size={14} />
          <span>Voltar para o Login</span>
        </Link>
      ) : (
        <span
          style={{
            fontSize: '0.75rem',
            fontWeight: 600,
            color: '#6B7280',
            letterSpacing: '0.04em'
          }}
        >
          Portal Acadêmico
        </span>
      )}
    </header>
  );
}