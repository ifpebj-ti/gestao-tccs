'use client';

import { CardHome } from '@/components/CardHome';
import { CollapseCardMobile } from '@/components/CollapseCardMobile';
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
import { useRouter } from 'next/navigation';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string;
}

export default function HomePage() {
  const { push } = useRouter();
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

      {/* Mobile (Collapse) */}
      <div className="md:hidden">
        {canView([
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING',
          'STUDENT'
        ]) && (
          <CollapseCardMobile
            title="Assinaturas pendentes"
            icon={faFileSignature}
            indicatorNumber={3}
            indicatorColor="bg-red-600"
            onClick={() => push('/pendingSignatures')}
          />
        )}

        {canView([
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING'
        ]) && (
          <CollapseCardMobile
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={7}
            indicatorColor="bg-blue-600"
            onClick={() => push('/ongoingTCCs')}
          />
        )}

        {canView([
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY'
        ]) && (
          <CollapseCardMobile
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          ></CollapseCardMobile>
        )}

        {canView(['STUDENT']) && (
          <CollapseCardMobile
            title="Meu TCC"
            icon={faGraduationCap}
            onClick={() => push('/TCC/{id}')}
          ></CollapseCardMobile>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR']) && (
          <CollapseCardMobile
            title="Cadastrar nova proposta"
            icon={faFileCirclePlus}
            onClick={() => push('/newTCC')}
          ></CollapseCardMobile>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCardMobile
            title="Cadastrar novo usuário"
            icon={faUserPlus}
            onClick={() => push('/newUser')}
          ></CollapseCardMobile>
        )}
      </div>

      {/* Desktop (Grid Cards) */}
      <div
        className={`hidden md:grid gap-6 ${
          profile === 'STUDENT' || profile === 'BANKING'
            ? 'grid-cols-2'
            : 'md:grid-cols-2 lg:grid-cols-3'
        }`}
      >
        {canView([
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING',
          'STUDENT'
        ]) && (
          <CardHome
            title="Assinaturas pendentes"
            icon={faFileSignature}
            indicatorNumber={3}
            indicatorColor="bg-red-600"
            onClick={() => push('/pendingSignatures')}
          />
        )}

        {canView([
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING'
        ]) && (
          <CardHome
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={7}
            indicatorColor="bg-blue-600"
            onClick={() => push('/ongoingTCCs')}
          />
        )}

        {canView([
          'ADMIN',
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY'
        ]) && (
          <CardHome
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          />
        )}

        {canView(['STUDENT']) && (
          <Link href="/meuTCC">
            <CardHome
              title="Meu TCC"
              icon={faGraduationCap}
              onClick={() => push('/TCC/{id}')}
            />
          </Link>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR']) && (
          <Link href="/newTCC">
            <CardHome
              title="Cadastrar nova proposta"
              icon={faFileCirclePlus}
              onClick={() => push('newTCC')}
            />
          </Link>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <Link href="/newUser">
            <CardHome
              title="Cadastrar novo usuário"
              icon={faUserPlus}
              onClick={() => push('newUser')}
            />
          </Link>
        )}
      </div>
    </div>
  );
}
