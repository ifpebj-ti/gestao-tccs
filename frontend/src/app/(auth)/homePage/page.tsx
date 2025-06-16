'use client';

import { CardHome } from '@/components/CardHome';
import { CollapseCard } from '@/components/CollapseCard';
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
import { toast } from 'react-toastify';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string;
}

interface TCCFromApi {
  tccId: number;
  studanteNames: string[];
}

export default function HomePage() {
  const { push } = useRouter();
  const [profile, setProfile] = useState<string | null>(null);
  const [tccs, setTccs] = useState<TCCFromApi[]>([]);

  useEffect(() => {
    const token = Cookies.get('token');
    if (token) {
      const decodedToken = jwtDecode<DecodedToken>(token);
      setProfile(decodedToken.role);
    }
  }, []);

  useEffect(() => {
    const fetchTccs = async () => {
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          return;
        }

        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/Tcc/filter?filter=IN_PROGRESS`,
          {
            headers: {
              Authorization: `Bearer ${token}`
            }
          }
        );

        if (!res.ok) {
          throw new Error('Erro ao buscar TCCs');
        }

        const data: TCCFromApi[] = await res.json();
        setTccs(data);
      } catch {
        toast.error('Erro ao carregar TCCs em andamento.');
      }
    };

    fetchTccs();
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
          <CollapseCard
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
          <CollapseCard
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={tccs.length}
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
          <CollapseCard
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          ></CollapseCard>
        )}

        {canView(['STUDENT']) && (
          <CollapseCard
            title="Meu TCC"
            icon={faGraduationCap}
            onClick={() => push('/TCC/{id}')}
          ></CollapseCard>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR']) && (
          <CollapseCard
            title="Cadastrar nova proposta"
            icon={faFileCirclePlus}
            onClick={() => push('/newTCC')}
          ></CollapseCard>
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCard
            title="Cadastrar novo usuário"
            icon={faUserPlus}
            onClick={() => push('/newUser')}
          ></CollapseCard>
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
            indicatorNumber={tccs.length}
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
