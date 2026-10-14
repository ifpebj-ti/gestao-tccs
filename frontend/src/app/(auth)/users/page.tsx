'use client';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';

import { ResponsiveRecordCard } from '@/components/ResponsiveRecordCard';
import { faUser, faPen, faSearch } from '@fortawesome/free-solid-svg-icons';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useEffect, useState, useCallback, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
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
      className: 'bg-green-100 text-green-800'
    };
  }
  return {
    text: 'Inativo',
    className: 'bg-red-100 text-red-800'
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
    <div className="flex flex-col">
      <PageTitle className="mb-6">Usuários</PageTitle>

      {/* --- BARRA DE PESQUISA --- */}
      <div className="mb-8 flex justify-end">
        <div className="w-full max-w-lg">
          <Input
            type="text"
            placeholder="Buscar por nome ou perfil"
            icon={faSearch}
            value={searchQuery}
            onChange={handleSearchChange}
          />
        </div>
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
        <div className="grid grid-cols-1 gap-x-8 md:grid-cols-2">
          {usersToDisplay.map((user) => {
            const statusTag = formatStatusTag(user.status);
            const profileText = formatProfile(user.profile);

            return (
              <ResponsiveRecordCard
                key={user.id}
                onClick={() => push(`/users/details?id=${user.id}`)}
                title={user.name || 'Usuário sem nome'}
                icon={faUser}
                profileText={profileText}
                statusText={statusTag.text}
                statusClass={statusTag.className}
                indicatorColor={
                  statusTag.text === 'Ativo' ? 'bg-green-600' : 'bg-red-600'
                }
              >
                <div className="flex flex-col gap-2">
                  <button
                    onClick={() => push(`/users/details?id=${user.id}`)}
                    className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                  >
                    <FontAwesomeIcon icon={faPen} className="text-[#006633]" />
                    <span>Editar Usuário</span>
                  </button>
                </div>
              </ResponsiveRecordCard>
            );
          })}
        </div>
      )}

      {/* --- Seção de Paginação (Lógica 1-indexed) --- */}
      {!isLoading && totalPages > 1 && !searchQuery && (
        <div className="flex justify-center items-center gap-4 mt-8">
          <Button
            variant="outline"
            onClick={handlePreviousPage}
            disabled={currentPage === 1}
          >
            Anterior
          </Button>

          <span className="text-gray-700">
            Página {currentPage} de {totalPages}
          </span>

          <Button
            variant="outline"
            onClick={handleNextPage}
            disabled={currentPage >= totalPages}
          >
            Próximo
          </Button>
        </div>
      )}
    </div>
  );
}
