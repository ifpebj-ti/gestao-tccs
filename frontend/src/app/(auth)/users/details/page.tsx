'use client';

import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Suspense, useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { toast } from 'react-toastify';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';

import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

import { useEditUser } from '@/app/hooks/useEditUser';
import { EditUserSchemaType } from '@/app/schemas/editUserSchema';

import {
  faUser,
  faEnvelope,
  faIdCard,
  faPhone,
  faAddressCard,
  faHashtag,
  faBuilding,
  faGraduationCap,
  faUsers
} from '@fortawesome/free-solid-svg-icons';

interface UserProfile {
  id: number;
  role: string;
}
interface CampiCourse {
  campiId: number;
  campiName: string;
  courseId: number;
  courseName: string;
}
interface UserDetailsApiResponse {
  id: number;
  name: string;
  email: string;
  registration: string;
  cpf: string;
  siape: string;
  status: string;
  phone: string;
  userClass: string;
  shift: string;
  titration: string;
  profile: UserProfile[];
  campiCourse: CampiCourse;
}

function convertApiResponseToFormData(
  data: UserDetailsApiResponse
): EditUserSchemaType {
  const getShiftNumber = (shiftString: string | undefined): number | null => {
    // Convertendo para maiúsculas e removendo espaços para padronizar
    const normalizedShift = shiftString?.toUpperCase().trim() || '';

    switch (normalizedShift) {
      case 'MATUTINO':
        return 1;
      case 'VESPERTINO':
        return 2;
      case 'NOTURNO':
        return 3;
      case 'DIURNO':
        return 4;
      case 'MORNING':
        return 1;
      case 'AFTERNOON':
        return 2;
      case 'NIGHT':
        return 3;
      case 'DAYTIME':
        return 4;
      default:
        return null;
    }
  };

  let primaryProfileRole: string = '';

  if (data.profile && data.profile.length > 0) {
    // Clonar e ordenar os perfis pelo ID (menor ID primeiro)
    const sortedProfiles = [...data.profile].sort((a, b) => a.id - b.id);

    primaryProfileRole = sortedProfiles[0].role;
  }

  return {
    id: data.id,
    name: data.name,
    email: data.email,
    registration: data.registration || '',
    cpf: data.cpf || '',
    siape: data.siape || '',
    phone: data.phone || '',
    userClass: data.userClass || '',
    titration: data.titration || '',
    status: data.status === 'ACTIVE' ? 'ACTIVE' : 'INACTIVE',
    profile: primaryProfileRole ? [primaryProfileRole] : [],
    shift: getShiftNumber(data.shift),
    campiId: data.campiCourse?.campiId || null,
    courseId: data.campiCourse?.courseId || null
  };
}

function EditUserForm() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const { push } = useRouter();
  const searchParams = useSearchParams();
  const id = searchParams.get('id');

  const [isLoading, setIsLoading] = useState(true);
  const [apiData, setApiData] = useState<UserDetailsApiResponse | null>(null);

  const { form, submitForm, isSubmitting } = useEditUser(() => {
    push('/users');
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
    watch
  } = form;

  useEffect(() => {
    if (!id) {
      toast.error('ID do usuário não fornecido.');
      push('/users');
      return;
    }

    const fetchUser = async () => {
      setIsLoading(true);
      try {
        const token = Cookies.get('token');
        if (!token) {
          toast.error('Token não encontrado.');
          push('/login');
          return;
        }

        const endpoint = `${API_URL}/User/${id}`;
        const res = await fetch(endpoint, {
          headers: { Authorization: `Bearer ${token}` }
        });

        if (!res.ok) {
          toast.error('Erro ao carregar usuário.');
          push('/users');
          return;
        }

        const userData: UserDetailsApiResponse = await res.json();

        setApiData(userData);

        const formData = convertApiResponseToFormData(userData);
        reset(formData);
      } catch {
        toast.error('Erro ao carregar usuário.');
        push('/users');
      } finally {
        setIsLoading(false);
      }
    };

    fetchUser();
  }, [API_URL, id, push, reset]);

  const watchedProfile = watch('profile');

  if (isLoading) {
    return (
      <p className="text-center text-gray-500 mt-4">
        Carregando dados do usuário...
      </p>
    );
  }

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        Editar Usuário: {watch('name') || '...'}
      </h1>

      <form onSubmit={handleSubmit(submitForm)} className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-4 p-6 border rounded-lg bg-white shadow-sm">
          {/* Coluna 1 */}
          <div className="space-y-4">
            <div>
              <label
                htmlFor="name"
                className="block text-sm font-semibold text-gray-700 mb-1"
              >
                Nome Completo
              </label>
              <Input
                id="name"
                icon={faUser}
                {...register('name')}
                errorText={errors.name?.message}
              />
            </div>

            <div>
              <label
                htmlFor="email"
                className="block text-sm font-semibold text-gray-700 mb-1"
              >
                Email
              </label>
              <Input
                id="email"
                icon={faEnvelope}
                {...register('email')}
                errorText={errors.email?.message}
              />
            </div>

            <div className="grid items-center gap-1.5">
              <label
                className="font-semibold text-sm gray-700"
                htmlFor="profile"
              >
                Perfil
              </label>
              <select
                id="profile"
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
                {...register('profile', { setValueAs: (v: string) => [v] })}
              >
                <option value="STUDENT">Estudante</option>
                <option value="BANKING">Banca</option>
                <option value="LIBRARY">Bibliotecário</option>
                <option value="ADVISOR">Orientador</option>
                <option value="SUPERVISOR">Supervisor</option>
                <option value="COORDINATOR">Coordenação</option>
                <option value="ADMIN">Administrador</option>
              </select>
              {errors.profile && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.profile.message}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="cpf"
                className="block text-sm font-semibold text-gray-700 mb-1"
              >
                CPF
              </label>
              <Input
                id="cpf"
                icon={faIdCard}
                {...register('cpf')}
                errorText={errors.cpf?.message}
              />
            </div>

            <div>
              <label
                htmlFor="phone"
                className="block text-sm font-semibold text-gray-700 mb-1"
              >
                Telefone
              </label>
              <Input
                id="phone"
                icon={faPhone}
                {...register('phone')}
                errorText={errors.phone?.message}
              />
            </div>

            <div className="grid items-center gap-1.5">
              <label className="font-semibold text-sm gray-700" htmlFor="shift">
                Turno
              </label>
              <select
                id="shift"
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
                {...register('shift', {
                  setValueAs: (v: string | null) =>
                    v === '' || v === null ? null : Number(v)
                })}
              >
                <option value="">Selecione um turno</option>
                <option value="1">Matutino</option>
                <option value="2">Vespertino</option>
                <option value="3">Noturno</option>
                <option value="4">Diurno</option>
              </select>
              {errors.shift && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.shift.message}
                </p>
              )}
            </div>
          </div>

          {/* Coluna 2 */}
          <div className="space-y-4">
            <div className="grid items-center gap-1.5">
              <label
                className="font-semibold text-sm gray-700"
                htmlFor="status"
              >
                Status
              </label>
              <select
                id="status"
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
                {...register('status')}
              >
                <option value="ACTIVE">Ativo</option>
                <option value="INACTIVE">Inativo</option>
              </select>
              {errors.status && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.status.message}
                </p>
              )}
            </div>

            {watchedProfile?.[0] === 'STUDENT' && (
              <div>
                <label
                  htmlFor="registration"
                  className="block text-sm font-semibold text-gray-700 mb-1"
                >
                  Matrícula
                </label>
                <Input
                  id="registration"
                  icon={faAddressCard}
                  {...register('registration')}
                  errorText={errors.registration?.message}
                />
              </div>
            )}

            {watchedProfile?.[0] === 'STUDENT' && (
              <div>
                <label
                  htmlFor="userClass"
                  className="block text-sm font-semibold text-gray-700 mb-1"
                >
                  Turma
                </label>
                <Input
                  helperText="Ex: 2025.2"
                  id="userClass"
                  icon={faUsers}
                  {...register('userClass')}
                  errorText={errors.userClass?.message}
                />
              </div>
            )}

            {watchedProfile?.[0] !== 'STUDENT' && (
              <div>
                <label
                  htmlFor="siape"
                  className="block text-sm font-semibold text-gray-700 mb-1"
                >
                  SIAPE
                </label>
                <Input
                  id="siape"
                  icon={faHashtag}
                  {...register('siape')}
                  errorText={errors.siape?.message}
                />
              </div>
            )}

            {watchedProfile?.[0] !== 'STUDENT' && (
              <div>
                <label
                  htmlFor="titration"
                  className="block text-sm font-semibold text-gray-700 mb-1"
                >
                  Titulação
                </label>
                <Input
                  id="titration"
                  icon={faGraduationCap}
                  {...register('titration')}
                  errorText={errors.titration?.message}
                />
              </div>
            )}

            {/* Campos de Somente Leitura */}
            <div>
              <label
                htmlFor="campus"
                className="block text-sm font-semibold text-gray-700 mb-1"
              >
                Campus (Somente Leitura)
              </label>
              <Input
                id="campus"
                icon={faBuilding}
                value={apiData?.campiCourse.campiName || 'N/A'}
                disabled
                readOnly
              />
            </div>
            <div>
              <label
                htmlFor="course"
                className="block text-sm font-semibold text-gray-700 mb-1"
              >
                Curso (Somente Leitura)
              </label>
              <Input
                id="course"
                icon={faGraduationCap}
                value={apiData?.campiCourse.courseName || 'N/A'}
                disabled
                readOnly
              />
            </div>
          </div>
        </div>

        {/* Botões de Ação */}
        <div className="flex justify-end gap-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => push('/users')}
          >
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Salvando...' : 'Salvar Alterações'}
          </Button>
        </div>
      </form>
    </div>
  );
}

// --- Componente da Página (Default) ---
export default function EditUserPage() {
  return (
    <Suspense
      fallback={<p className="text-center text-gray-500 mt-4">Carregando...</p>}
    >
      <EditUserForm />
    </Suspense>
  );
}
