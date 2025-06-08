'use client';

import { CardHome } from '@/components/CardHome';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import {
  faFileCircleCheck,
  faFileCirclePlus,
  faFileSignature,
  faGraduationCap,
  faUserPlus
} from '@fortawesome/free-solid-svg-icons';
import Link from 'next/link';
import { jwtDecode } from 'jwt-decode';
import Cookies from 'js-cookie';
import { useEffect, useState } from 'react';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string;
}

export default function HomePage() {
  const [profile, setProfile] = useState<string | null>(null);

  useEffect(() => {
    const token = Cookies.get('token');
    if (token) {
      const decodedToken = jwtDecode<DecodedToken>(token);
      setProfile(decodedToken.role);
    }
  }, []);

  const canView = (roles: string[]) => roles.includes(profile ?? '');

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        Página Inicial
      </h1>

      <div
        className={`grid gap-6 ${
          profile === 'STUDENT' || profile === 'BANKING'
            ? 'grid-cols-2'
            : 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3'
        }`}
      >
        {/* Assinaturas pendentes */}
        <div
          className={
            canView([
              'ADMIN',
              'COORDINATOR',
              'SUPERVISOR',
              'ADVISOR',
              'LIBRARY',
              'BANKING',
              'STUDENT'
            ])
              ? ''
              : 'hidden'
          }
        >
          <CardHome
            title="Assinaturas pendentes"
            icon={faFileSignature}
            indicatorNumber={3}
            indicatorColor="bg-red-600"
          />
        </div>

        {/* TCCs em andamento */}
        <div
          className={
            canView([
              'ADMIN',
              'COORDINATOR',
              'SUPERVISOR',
              'ADVISOR',
              'LIBRARY',
              'BANKING'
            ])
              ? ''
              : 'hidden'
          }
        >
          <CardHome
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={7}
            indicatorColor="bg-blue-600"
          />
        </div>

        {/* TCCs concluídos */}
        <div
          className={
            canView([
              'ADMIN',
              'COORDINATOR',
              'SUPERVISOR',
              'ADVISOR',
              'LIBRARY'
            ])
              ? ''
              : 'hidden'
          }
        >
          <CardHome title="TCCs concluídos" icon={faFileCircleCheck} />
        </div>

        {/* Meu TCC (somente estudante) */}
        <div className={canView(['STUDENT']) ? '' : 'hidden'}>
          <Link href="/meuTCC">
            <CardHome title="Meu TCC" icon={faGraduationCap} />
          </Link>
        </div>

        {/* Cadastrar nova proposta */}
        <div
          className={
            canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR'])
              ? ''
              : 'hidden'
          }
        >
          <Link href="/newTCC">
            <CardHome title="Cadastrar nova proposta" icon={faFileCirclePlus} />
          </Link>
        </div>

        {/* Cadastrar novo usuário */}
        <div
          className={
            canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) ? '' : 'hidden'
          }
        >
          <Link href="/newUser">
            <CardHome title="Cadastrar novo usuário" icon={faUserPlus} />
          </Link>
        </div>
      </div>
    </div>
  );
}
