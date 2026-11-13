'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';

import Link from 'next/link';
import {
  UserRound,
  Search,
  ArrowRight,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useEffect, useState, useCallback, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';

interface Campus {
  id: number;
  name: string;
  course: {
    id: number;
    name: string;
  };
}

interface UserFromApi {
  id: number;
  name: string;
  email: string;
  profile: string;
  registration: string;
  cpf: string;
  siape: string;
  phone: string;
  userClass: string;
  shift: string;
  titration: string;
  campus: Campus;
  status: string;
}

interface ApiResponse {
  data: UserFromApi[];
  totalRecords: number;
  currentPageNumber: number;
  currentPageSize: number;
  totalPages: number;
}

// FUNÇÃO PARA FORMATAR O STATUS
const formatStatusTag = (
  status?: string
): { text: string; className: string } => {
  const statusUpperCase = status?.toUpperCase().trim();

  if (statusUpperCase === 'ACTIVE') {
    return {
      text: 'Ativo',
      className: 'text-[#24563B]'
    };
  }
  return {
    text: 'Inativo',
    className: 'text-red-800'
  };
};

// FUNÇÃO PARA FORMATAR O PERFIL
const formatProfile = (profile?: string): string => {
  if (!profile) return '';

  switch (profile.toUpperCase()) {
    case 'ADMIN':
      return 'Admin';
    case 'COORDINATOR':
      return 'Coordenador';
    case 'SUPERVISOR':
      return 'Supervisor';
    case 'ADVISOR':
      return 'Orientador';
    case 'STUDENT':
      return 'Estudante';
    case 'BANKING':
      return 'Banca';
    case 'LIBRARY':
      return 'Biblioteca';
    default:
      return profile.charAt(0).toUpperCase() + profile.slice(1).toLowerCase();
  }
};

export default function UsersPage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();

  const [apiUsers, setApiUsers] = useState<UserFromApi[]>([]);

  const [isLoading, setIsLoading] = useState(true);

  const [searchQuery, setSearchQuery] = useState('');

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(0);

  const fetchUsers = useCallback(
    async (page: number) => {
      setIsLoading(true);
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          push('/login');
          return;
        }

        const endpoint = `${API_URL}/User/all?pageNumber=${page}&pageSize=${pageSize}`;

        const res = await fetch(endpoint, {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });

        if (!res.ok) {
          throw new Error('Falha ao buscar usuários');
        }

        const responseData: ApiResponse = await res.json();

        setApiUsers(responseData.data);
        setTotalPages(responseData.totalPages);
      } catch {
        toast.error('Erro ao carregar usuários.');
      } finally {
        setIsLoading(false);
      }
    },
    [API_URL, pageSize, push]
  );

  useEffect(() => {
    fetchUsers(currentPage);
  }, [fetchUsers, currentPage]);

  const filteredUsers = useMemo(() => {
    if (!searchQuery) {
      return apiUsers;
    }

    const normalizedQuery = searchQuery.toLowerCase();

    return apiUsers.filter((user) => {
      // Cria uma string concatenada com os campos relevantes para a busca
      const searchData = [
        user.name,
        formatProfile(user.profile) // Inclui o perfil formatado na busca
      ]
        .join(' ')
        .toLowerCase();

      return searchData.includes(normalizedQuery);
    });
  }, [apiUsers, searchQuery]);

  // Função para lidar com a mudança na busca
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
  };

  const handlePreviousPage = () => {
    setSearchQuery('');
    setCurrentPage((prev) => Math.max(1, prev - 1));
  };

  const handleNextPage = () => {
    setSearchQuery('');
    setCurrentPage((prev) => Math.min(totalPages, prev + 1));
  };

  const usersToDisplay = filteredUsers;

  return (
    <div className="flex w-full max-w-6xl flex-col gap-8">
      <div>
        <PageTitle>
          Usuários
        </PageTitle>
        <p className="mt-3 text-sm leading-relaxed text-[#617265]">
          Consulte os usuários e acesse seus dados cadastrais.
        </p>
      </div>

      {/* --- BARRA DE PESQUISA --- */}
      <div className="w-full max-w-2xl space-y-2">
        <label
          htmlFor="users-search"
          className="text-xs font-semibold text-[#183E2D]"
        >
          Buscar usuário
        </label>
        <Input
          id="users-search"
          type="text"
          placeholder="Buscar por nome ou perfil"
          icon={<Search size={18} aria-hidden="true" />}
          value={searchQuery}
          onChange={handleSearchChange}
        />
      </div>

      {isLoading ? (
        <PageState loading>Carregando...</PageState>
      ) : usersToDisplay.length === 0 && !searchQuery ? (
        // Se não houver usuários E a busca estiver vazia
        <PageState>Nenhum usuário encontrado.</PageState>
      ) : usersToDisplay.length === 0 && searchQuery ? (
        // Se não houver usuários MAS a busca estiver ativa
        <PageState>
          Nenhum resultado encontrado para &quot;{searchQuery}&quot;.
        </PageState>
      ) : (
        <div>
          <div
            aria-hidden="true"
            className="hidden grid-cols-[minmax(0,1fr)_10rem_6rem_1.5rem] gap-8 border-b border-[#DDD9CD] pb-3 text-xs font-semibold text-[#617265] lg:grid"
          >
            <span>Nome e e-mail</span>
            <span>Perfil</span>
            <span>Situação</span>
            <span className="sr-only">Acessar cadastro</span>
          </div>
          <ul
            aria-label="Lista de usuários"
            className="divide-y divide-[#ECEAE2]"
          >
            {usersToDisplay.map((user) => {
              const statusTag = formatStatusTag(user.status);
              const profileText = formatProfile(user.profile);

              return (
                <li key={user.id}>
                  <Link
                    href={`/users/details?id=${user.id}`}
                    className="group grid min-w-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-x-4 gap-y-2 py-5 text-sm text-[#002B18] transition-colors hover:text-[#006633] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#006633] lg:grid-cols-[minmax(0,1fr)_10rem_6rem_1.5rem] lg:gap-x-8 lg:py-4"
                  >
                    <span className="flex min-w-0 items-center gap-3">
                      <UserRound
                        className="size-5 shrink-0"
                        strokeWidth={1.6}
                        aria-hidden="true"
                      />
                      <span className="min-w-0">
                        <span className="block truncate font-semibold group-hover:underline group-hover:underline-offset-4">
                          {user.name || 'Usuário sem nome'}
                        </span>
                        <span className="mt-1 block truncate text-xs text-[#617265]">
                          {user.email}
                        </span>
                      </span>
                    </span>
                    <span className="col-span-2 flex flex-wrap items-center gap-x-4 gap-y-1 pl-8 text-xs lg:contents">
                      <span className="text-[#617265] lg:text-sm">
                        <span className="sr-only">Perfil: </span>
                        {profileText}
                      </span>
                      <span
                        className={`inline-flex items-center gap-2 ${statusTag.className}`}
                      >
                        <span
                          aria-hidden="true"
                          className={`size-1.5 shrink-0 rounded-full ${statusTag.text === 'Ativo' ? 'bg-[#24563B]' : 'bg-red-800'}`}
                        />
                        <span className="sr-only">Situação: </span>
                        {statusTag.text}
                      </span>
                    </span>
                    <ArrowRight
                      className="col-start-2 row-start-1 size-4 text-[#617265] transition-colors group-hover:text-[#006633] lg:col-start-auto lg:row-start-auto"
                      aria-hidden="true"
                    />
                  </Link>
                </li>
              );
            })}
          </ul>
        </div>
      )}

      {/* --- Seção de Paginação (Lógica 1-indexed) --- */}
      {!isLoading && totalPages > 1 && !searchQuery && (
        <nav
          aria-label="Paginação de usuários"
          className="flex flex-wrap items-center justify-between gap-5 border-t border-[#DDD9CD] pt-5"
        >
          <Button
            variant="outline"
            onClick={handlePreviousPage}
            disabled={currentPage === 1}
            className="h-auto rounded-none border-0 px-0 py-2 shadow-none hover:bg-transparent"
          >
            <ChevronLeft size={16} aria-hidden="true" />
            Anterior
          </Button>

          <span aria-live="polite" className="text-xs text-[#617265]">
            Página {currentPage} de {totalPages}
          </span>

          <Button
            variant="outline"
            onClick={handleNextPage}
            disabled={currentPage >= totalPages}
            className="h-auto rounded-none border-0 px-0 py-2 shadow-none hover:bg-transparent"
          >
            Próximo
            <ChevronRight size={16} aria-hidden="true" />
          </Button>
        </nav>
      )}
    </div>
  );
}
