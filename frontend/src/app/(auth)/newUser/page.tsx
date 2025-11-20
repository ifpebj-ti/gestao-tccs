'use client';

import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  faEnvelope,
  faGraduationCap,
  faIdCard,
  faUser,
  faPhone
} from '@fortawesome/free-solid-svg-icons';
import { useRouter } from 'next/navigation';
import { useNewUserForm } from '@/app/hooks/useNewUser';

export default function NewUser() {
  const { push } = useRouter();
  const {
    errors,
    handleSubmit,
    register,
    submitForm,
    isSubmitting,
    watch,
    campus,
    courses
  } = useNewUserForm();

  const profileValue = watch('profile');
  const campusValue = watch('campusId');

  // --- Máscaras ---
  const formatCPF = (value: string) => {
    const numericValue = value.replace(/\D/g, '');
    const limitedValue = numericValue.slice(0, 11);
    let formattedValue = limitedValue;
    if (limitedValue.length > 3)
      formattedValue = formattedValue.replace(/(\d{3})(\d)/, '$1.$2');
    if (limitedValue.length > 6)
      formattedValue = formattedValue.replace(
        /(\d{3})\.(\d{3})(\d)/,
        '$1.$2.$3'
      );
    if (limitedValue.length > 9)
      formattedValue = formattedValue.replace(
        /(\d{3})\.(\d{3})\.(\d{3})(\d{1,2})/,
        '$1.$2.$3-$4'
      );
    return formattedValue;
  };

  const formatPhone = (value: string) => {
    if (!value) return '';
    const numericValue = value.replace(/\D/g, '');
    const limitedValue = numericValue.slice(0, 11);
    let formattedValue = limitedValue;
    if (limitedValue.length > 0)
      formattedValue = formattedValue.replace(/^(\d{2})/, '($1');
    if (limitedValue.length > 2)
      formattedValue = formattedValue.replace(/^(\(\d{2})(\d)/, '$1) $2');
    if (limitedValue.length > 7) {
      if (limitedValue.length === 11)
        formattedValue = formattedValue.replace(
          /^(\(\d{2}\) \d{5})(\d{4})/,
          '$1-$2'
        );
      else
        formattedValue = formattedValue.replace(
          /^(\(\d{2}\) \d{4})(\d{0,4})/,
          '$1-$2'
        );
    }
    return formattedValue;
  };

  const { onChange: onCpfChange, ...cpfRest } = register('cpf');
  const { onChange: onPhoneChange, ...phoneRest } = register('phone');

  // Listas de controle de exibição
  const profilesWithSiape = [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'LIBRARY',
    'BANKING'
  ];
  const profilesWithTitration = [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'BANKING'
  ];

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal mb-10">
        Novo usuário
      </h1>

      <form className="flex flex-col gap-8" onSubmit={handleSubmit(submitForm)}>
        {/* --- SEÇÃO 1: INFORMAÇÕES PESSOAIS --- */}
        <div>
          <h2 className="text-lg font-extrabold uppercase mb-4">
            Informações do usuário
          </h2>
          <div className="grid md:grid-cols-2 gap-4">
            {/* NOME */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="name">
                Nome
              </Label>
              <Input
                id="name"
                type="text"
                placeholder="Digite o nome do usuário"
                icon={faUser}
                errorText={errors.name?.message}
                {...register('name')}
              />
            </div>

            {/* PERFIL */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="profile">
                Perfil
              </Label>
              <select
                id="profile"
                {...register('profile')}
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
              >
                <option value="">Selecione um perfil</option>
                <option value="ADMIN">Administrador</option>
                <option value="COORDINATOR">Coordenador</option>
                <option value="SUPERVISOR">Supervisor</option>
                <option value="ADVISOR">Orientador</option>
                <option value="BANKING">Banca</option>
                <option value="LIBRARY">Bibliotecário</option>
              </select>
              {errors.profile && (
                <p className="text-red-500 text-sm">{errors.profile.message}</p>
              )}
            </div>

            {/* EMAIL */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="email">
                Email
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="Digite o email do usuário"
                icon={faEnvelope}
                errorText={errors.email?.message}
                {...register('email')}
              />
            </div>

            {/* CPF */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="cpf">
                CPF
              </Label>
              <Input
                id="cpf"
                placeholder="Digite o CPF (XXX.XXX.XXX-XX)"
                icon={faIdCard}
                errorText={errors.cpf?.message}
                maxLength={14}
                {...cpfRest}
                onChange={(e) => {
                  e.target.value = formatCPF(e.target.value);
                  onCpfChange(e);
                }}
              />
            </div>

            {/* TELEFONE */}
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="phone">
                Telefone
              </Label>
              <Input
                id="phone"
                placeholder="(XX) 9XXXX-XXXX"
                icon={faPhone}
                errorText={errors.phone?.message}
                maxLength={15}
                {...phoneRest}
                onChange={(e) => {
                  e.target.value = formatPhone(e.target.value);
                  onPhoneChange(e);
                }}
              />
            </div>

            {/* CAMPO SIAPE */}
            {profilesWithSiape.includes(profileValue || '') && (
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold" htmlFor="siape">
                  SIAPE
                </Label>
                <Input
                  id="siape"
                  type="text"
                  placeholder="Digite o SIAPE"
                  icon={faGraduationCap}
                  errorText={errors.siape?.message}
                  {...register('siape')}
                />
              </div>
            )}

            {/* CAMPO TITULAÇÃO */}
            {profilesWithTitration.includes(profileValue || '') && (
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold" htmlFor="titration">
                  Titulação
                </Label>
                <Input
                  id="titration"
                  type="text"
                  placeholder="Ex: Doutor, Mestre, Especialista"
                  icon={faGraduationCap}
                  errorText={errors.titration?.message}
                  {...register('titration')}
                />
              </div>
            )}
          </div>
        </div>

        {/* --- SEÇÃO 2: INFORMAÇÕES ACADÊMICAS --- */}
        <div>
          <h2 className="text-lg font-extrabold uppercase mb-4">
            Informações Acadêmicas
          </h2>
          <div className="grid md:grid-cols-2 gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="campusId">
                Campus
              </Label>
              <select
                id="campusId"
                {...register('campusId')}
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
              >
                <option value="0">Selecione um campus</option>
                {campus.map((campi) => (
                  <option key={campi.id} value={campi.id}>
                    {campi.name}
                  </option>
                ))}
              </select>
              {errors.campusId && (
                <p className="text-red-500 text-sm">
                  {errors.campusId.message}
                </p>
              )}
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="courseId">
                Curso
              </Label>
              <select
                id="courseId"
                {...register('courseId')}
                disabled={!campusValue || courses.length === 0}
                className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer disabled:cursor-not-allowed disabled:opacity-50"
              >
                <option value="0">Selecione um curso</option>
                {courses.map((course) => (
                  <option key={course.id} value={course.id}>
                    {course.name}
                  </option>
                ))}
              </select>
              {errors.courseId && (
                <p className="text-red-500 text-sm">
                  {errors.courseId.message}
                </p>
              )}
            </div>
          </div>
        </div>

        <div className="flex gap-2 md:self-end mt-4">
          <Button
            onClick={() => push('/homePage')}
            variant="outline"
            className="w-full md:w-fit"
            type="button"
          >
            Cancelar
          </Button>
          <Button
            type="submit"
            className="w-full md:w-fit"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Cadastrando...' : 'Cadastrar usuário'}
          </Button>
        </div>
      </form>
    </div>
  );
}
