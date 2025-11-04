'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { useRouter } from 'next/navigation';
import { toast } from 'react-toastify';

import IFPELogo from '../../../public/IFPE Logo.png';
import IFPELogoMini from '../../../public/Logo ifpe mini.svg';
import LogoImage from '../../../public/flat-color-icons_graduation-cap.svg';
import UserProfile from '../UserProfile/index';
import { Button } from '@/components/ui/button';
import { TriangleAlert } from 'lucide-react';

interface User {
  name: string;
  roles: string[];
}

interface DecodedToken {
  unique_name: string;
  role: string | string[];
  isTestUser?: boolean;
  isDefaultPassword?: boolean;
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
  const [isTestUser, setIsTestUser] = useState(false);
  const [isDefaultPassword, setIsDefaultPassword] = useState(false);
  const router = useRouter();

  useEffect(() => {
    const token = Cookies.get('token');

    if (token) {
      try {
        const decodedToken: DecodedToken = jwtDecode(token);

        const testUserFlag = decodedToken.isTestUser === true;
        const defaultPasswordFlag = decodedToken.isDefaultPassword === true;

        setIsTestUser(testUserFlag);
        setIsDefaultPassword(defaultPasswordFlag);

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

    setIsDefaultPassword(true);
    setIsTestUser(true);
  }, []);

  const handleLogout = () => {
    Cookies.remove('token');
    setUser(null);
    window.location.href = '/';
  };

  const handleRedirectToUpdatePassword = () => {
    router.push('/update-password');
  };

  return (
    <header className=" top-0 left-0 w-full bg-white shadow z-50">
      <div className="mx-auto px-4 md:px-10 py-3 flex items-center justify-between gap-2 w-full">
        {/* Logo Desktop */}
        <div className="flex-shrink-0 hidden md:block">
          <Link href="/">
            <Image
              src={IFPELogo}
              alt="IFPE Logo"
              className="h-8 w-auto md:h-12"
              priority
            />
          </Link>
        </div>

        {/* Logo Mobile */}
        <div className="flex-shrink-0 md:hidden">
          <Link href="/">
            <Image
              src={IFPELogoMini}
              alt="IFPE Logo"
              className="h-8 w-auto md:h-12"
              priority
            />
          </Link>
        </div>

        {/* Título (centralizado) */}
        <div className="flex gap-3 justify-center items-center">
          <Image
            src={LogoImage}
            alt="Logo Image"
            className="md:h-10 h-8 w-min"
          />
          <h1 className="md:text-xl font-semibold text-gray-800 text-center">
            Gradus Flow
          </h1>
        </div>

        {/* Menu do Usuário e Botão de Teste (à direita) */}
        <div className="flex-shrink-0 flex items-center gap-2">
          {/* Botão de Usuário Teste */}
          {isTestUser && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleRedirectToUpdatePassword}
            >
              Trocar Senha
            </Button>
          )}

          {/* Menu do Usuário */}
          {user ? (
            <UserProfile user={user} onLogout={handleLogout} />
          ) : (
            <div className="w-10 h-10"></div>
          )}
        </div>
      </div>

      {/* Alerta Condicional */}
      {isDefaultPassword && (
        <div className="px-4 md:px-10 py-2 flex flex-col items-center justify-center gap-2 text-center">
          <div className="w-full text-sm text-yellow-800 font-medium bg-yellow-100 p-2 rounded-md flex items-center justify-center gap-2">
            <TriangleAlert className="flex-shrink-0 w-5 h-5" />
            <span>
              <strong>Alerta:</strong> Sua senha é a padrão. Por favor,
              <Button
                variant="link"
                className="p-0 h-auto ml-1 font-bold text-yellow-800 hover:text-yellow-900"
                onClick={handleRedirectToUpdatePassword}
              >
                altere sua senha
              </Button>{' '}
              para mais segurança.
            </span>
          </div>
        </div>
      )}
    </header>
  );
}
