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
import { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string | string[];
}

interface TCCsInformation {
  pendingSignature: number;
  tccInprogress: number;
}

interface UserTCCs {
  tccId: number;
  studanteNames: string[];
}

export default function HomePage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const [profile, setProfile] = useState<string | string[] | null>(null);
  const [tccs, setTccs] = useState<TCCsInformation | null>(null);
  const [userTCCs, setUserTCCs] = useState<UserTCCs[]>([]);
  const [completedUserTCC, setCompletedUserTCC] = useState<UserTCCs[]>([]);

  const checkUserTccStatus = useCallback(
    async (userId: string) => {
      const token = Cookies.get('token');
      if (!token) return;

      try {
        const completedRes = await fetch(
          `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=COMPLETED`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (!completedRes.ok)
          throw new Error('Falha ao verificar TCCs concluídos.');

        const completedData = await completedRes.json();

        if (completedData && completedData.length > 0) {
          setCompletedUserTCC(completedData);
        } else {
          const inProgressRes = await fetch(
            `${API_URL}/Tcc/filter?userId=${userId}&StatusTcc=IN_PROGRESS`,
            { headers: { Authorization: `Bearer ${token}` } }
          );
          if (inProgressRes.ok) {
            const inProgressData = await inProgressRes.json();
            setUserTCCs(inProgressData);
          }
        }
      } catch {
        toast.error('Erro ao verificar o status do seu TCC.');
      }
    },
    [API_URL]
  );

  useEffect(() => {
    const token = Cookies.get('token');
    if (token) {
      const decodedToken = jwtDecode<DecodedToken>(token);
      setProfile(decodedToken.role);

      const userHasStudentRole = Array.isArray(decodedToken.role)
        ? decodedToken.role.includes('STUDENT')
        : decodedToken.role === 'STUDENT';

      if (userHasStudentRole) {
        checkUserTccStatus(decodedToken.userId);
      }
    }
  }, [checkUserTccStatus]);

  useEffect(() => {
    const fetchTccs = async () => {
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          return;
        }

        const res = await fetch(`${API_URL}/Home`, {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });

        if (!res.ok) {
          throw new Error('Erro ao buscar informações dos TCCs.');
        }

        const data: TCCsInformation = await res.json();
        setTccs(data);
      } catch {
        toast.error('Erro ao carregar informações dos TCCs.');
      }
    };

    fetchTccs();
  }, [API_URL]);

  const canView = (allowedRoles: string[]) => {
    if (!profile) return false;
    if (typeof profile === 'string') return allowedRoles.includes(profile);
    if (Array.isArray(profile))
      return profile.some((userRole) => allowedRoles.includes(userRole));
    return false;
  };

  const isLimitedView = () => {
    if (!profile) return false;
    if (typeof profile === 'string')
      return profile === 'STUDENT' || profile === 'BANKING';
    if (Array.isArray(profile)) return false;
    return false;
  };

  const handleMyTccClick = () => {
    if (completedUserTCC.length > 0) {
      push(`/completedTCCs/${completedUserTCC[0].tccId}`);
    } else if (userTCCs.length > 0) {
      push(`/myTCC/signatures?id=${userTCCs[0].tccId}`);
    }
  };

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        Página Inicial
      </h1>

      {/* Mobile (Collapse) */}
      <div className="md:hidden">
        {canView([
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
            indicatorNumber={tccs?.pendingSignature || 0}
            indicatorColor="bg-red-600"
            onClick={() => push('/pendingSignatures')}
          />
        )}

        {canView([
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING'
        ]) && (
          <CollapseCard
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={tccs?.tccInprogress || 0}
            indicatorColor="bg-blue-600"
            onClick={() => push('/ongoingTCCs')}
          />
        )}

        {canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY']) && (
          <CollapseCard
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          />
        )}

        {canView(['STUDENT']) &&
          (userTCCs.length > 0 || completedUserTCC.length > 0) && (
            <CollapseCard
              title="Meu TCC"
              icon={faGraduationCap}
              onClick={handleMyTccClick}
            />
          )}

        {canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR']) && (
          <CollapseCard
            title="Cadastrar nova proposta"
            icon={faFileCirclePlus}
            onClick={() => push('/newTCC')}
          />
        )}

        {canView(['ADMIN', 'COORDINATOR', 'SUPERVISOR']) && (
          <CollapseCard
            title="Cadastrar novo usuário"
            icon={faUserPlus}
            onClick={() => push('/newUser')}
          />
        )}
      </div>

      {/* Desktop (Grid Cards) */}
      <div
        className={`hidden md:grid gap-6 ${
          isLimitedView() ? 'grid-cols-2' : 'md:grid-cols-2 lg:grid-cols-3'
        }`}
      >
        {canView([
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
            indicatorNumber={tccs?.pendingSignature || 0}
            indicatorColor="bg-red-600"
            onClick={() => push('/pendingSignatures')}
          />
        )}

        {canView([
          'COORDINATOR',
          'SUPERVISOR',
          'ADVISOR',
          'LIBRARY',
          'BANKING'
        ]) && (
          <CardHome
            title="TCCs em andamento"
            icon={faGraduationCap}
            indicatorNumber={tccs?.tccInprogress || 0}
            indicatorColor="bg-blue-600"
            onClick={() => push('/ongoingTCCs')}
          />
        )}

        {canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY']) && (
          <CardHome
            title="TCCs concluídos"
            icon={faFileCircleCheck}
            onClick={() => push('/completedTCCs')}
          />
        )}

        {canView(['STUDENT']) &&
          (userTCCs.length > 0 || completedUserTCC.length > 0) && (
            <CardHome
              title="Meu TCC"
              icon={faGraduationCap}
              onClick={handleMyTccClick}
            />
          )}

        {canView(['COORDINATOR', 'SUPERVISOR', 'ADVISOR']) && (
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
