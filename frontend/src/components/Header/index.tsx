'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';

import IFPELogo from '../../../public/IFPE Logo.png';
import UserProfile from '../UserProfile/index';
import { toast } from 'react-toastify';

interface User {
  name: string;
  roles: string[];
}

interface DecodedToken {
  unique_name: string;
  role: string | string[];
}

const roleTranslations: { [key: string]: string } = {
  COORDINATOR: 'Coordenador(a)',
  SUPERVISOR: 'Supervisor(a)',
  ADVISOR: 'Orientador(a)',
  LIBRARY: 'Biblioteca',
  BANKING: 'Banca Examinadora',
  STUDENT: 'Estudante',
  ADMIN: 'Administrador(a)'
};

export default function Header() {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    const token = Cookies.get('token');

    if (token) {
      try {
        const decodedToken: DecodedToken = jwtDecode(token);

        const rolesFromToken = Array.isArray(decodedToken.role)
          ? decodedToken.role
          : [decodedToken.role];

        const translatedRoles = rolesFromToken.map(
          (originalRole) => roleTranslations[originalRole] || originalRole
        );

        const userData: User = {
          name: decodedToken.unique_name,
          roles: translatedRoles
        };

        setUser(userData);
      } catch {
        toast.error('Token inválido ou expirado');
        setUser(null);
      }
    }
  }, []);

  const handleLogout = () => {
    Cookies.remove('token');
    setUser(null);
    window.location.href = '/';
  };

  return (
    <header className="fixed top-0 left-0 w-full bg-white shadow z-50">
      <div className="mx-auto px-4 md:px-10 py-3 flex items-center justify-between gap-2 w-full">
        {/* Logo */}
        <div className="flex-shrink-0">
          <Link href="/">
            <Image
              src={IFPELogo}
              alt="IFPE Logo"
              className="h-8 w-auto md:h-12"
              priority
            />
          </Link>
        </div>

        {/* Título (centralizado) */}
        <div className="flex-1 flex justify-center">
          <h1 className="text-lg md:text-xl font-semibold text-gray-800 text-center">
            Gestão de TCCs
          </h1>
        </div>

        {/* Menu do Usuário (à direita) */}
        <div className="flex-shrink-0">
          {user ? (
            <UserProfile user={user} onLogout={handleLogout} />
          ) : (
            <div className="w-10 h-10"></div>
          )}
        </div>
      </div>
    </header>
  );
}
