'use client';

import { CollapseCard } from '@/components/CollapseCard';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { faUser, faPen, faFile } from '@fortawesome/free-solid-svg-icons';
import { useEffect, useState } from 'react';
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

export default function UsersPage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const [users, setUsers] = useState<UserFromApi[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(0);

  useEffect(() => {
    const fetchUsers = async () => {
      setIsLoading(true);
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token de autenticação não encontrado.');
          push('/login');
          return;
        }

        const endpoint = `${API_URL}/User/all?pageNumber=${currentPage}&pageSize=${pageSize}`;

        const res = await fetch(endpoint, {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });

        const responseData: ApiResponse = await res.json();

        setUsers(responseData.data);
        setTotalPages(responseData.totalPages);
      } catch {
        toast.error('Erro ao carregar usuários.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchUsers();
  }, [API_URL, push, currentPage, pageSize]);

  const handlePreviousPage = () => {
    setCurrentPage((prev) => Math.max(1, prev - 1));
  };

  const handleNextPage = () => {
    setCurrentPage((prev) => Math.min(totalPages, prev + 1));
  };

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        Usuários
      </h1>

      {isLoading ? (
        <p className="text-center text-gray-500 mt-4">Carregando...</p>
      ) : users.length === 0 ? (
        <p className="text-center text-gray-500 mt-4">
          Nenhum usuário encontrado.
        </p>
      ) : (
        <>
          {/* Mobile */}
          <div className="md:hidden flex flex-col gap-2">
            {users.map((user) => (
              <div key={user.id}>
                <CollapseCard
                  title={user.name || 'Usuário sem nome'}
                  icon={faUser}
                  profile={user.profile}
                  status={user.status}
                  indicatorColor={
                    user.status === 'ACTIVE' ? 'bg-green-600' : 'bg-red-600'
                  }
                >
                  <div className="flex flex-col gap-2">
                    <button
                      onClick={() => push(`/users/edit?id=${user.id}`)}
                      className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                    >
                      <FontAwesomeIcon
                        icon={faPen}
                        className="text-[#1351B4]"
                      />
                      <span>Editar Usuário</span>
                    </button>
                    <button
                      onClick={() => push(`/users/details?id=${user.id}`)}
                      className="flex items-center gap-2 px-4 py-2 rounded-md bg-gray-100 hover:bg-gray-200 transition hover:cursor-pointer"
                    >
                      <FontAwesomeIcon
                        icon={faFile}
                        className="text-[#1351B4]"
                      />
                      <span>Ver Detalhes</span>
                    </button>
                  </div>
                </CollapseCard>
              </div>
            ))}
          </div>

          {/* Desktop */}
          <div className="hidden md:grid grid-cols-2 lg:grid-cols-3 gap-4">
            {users.map((user) => (
              <div key={user.id}>
                <CollapseCard
                  title={user.name || 'Usuário sem nome'}
                  profile={user.profile}
                  status={user.status}
                  icon={faUser}
                  indicatorColor={
                    user.status === 'ACTIVE' ? 'bg-green-600' : 'bg-red-600'
                  }
                  onClick={() => push(`/users/details?id=${user.id}`)}
                />
              </div>
            ))}
          </div>
        </>
      )}

      {/* --- Seção de Paginação (Lógica 1-indexed) --- */}
      {!isLoading && totalPages > 1 && (
        <div className="flex justify-center items-center gap-4 mt-8">
          <button
            onClick={handlePreviousPage}
            disabled={currentPage === 1} // Desabilita na primeira página (página 1)
            className="px-4 py-2 rounded-md bg-gray-200 hover:bg-gray-300 disabled:bg-gray-100 disabled:text-gray-400 disabled:cursor-not-allowed transition"
          >
            Anterior
          </button>

          <span className="text-gray-700">
            Página {currentPage} de {totalPages}
          </span>

          <button
            onClick={handleNextPage}
            disabled={currentPage >= totalPages} // Desabilita na última página
            className="px-4 py-2 rounded-md bg-gray-200 hover:bg-gray-300 disabled:bg-gray-100 disabled:text-gray-400 disabled:cursor-not-allowed transition"
          >
            Próximo
          </button>
        </div>
      )}
    </div>
  );
}
