'use client';

import { FormSelect } from '@/components/ui/form-select';

import { PageTitle } from '@/components/layout/PageTitle';
import { SectionTitle } from '@/components/layout/SectionTitle';

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
    control,
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
      <PageTitle className="mb-10">Novo usuário</PageTitle>

      <form className="flex flex-col gap-8" onSubmit={handleSubmit(submitForm)}>
        {/* --- SEÇÃO 1: INFORMAÇÕES PESSOAIS --- */}
        <div>
          <SectionTitle className="mb-4">Informações do usuário</SectionTitle>
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
              <FormSelect
                id="profile"
                control={control}
                name={'profile'}
                className="w-full min-w-0 border-0 border-b-2 border-[#002B18] bg-transparent py-2.5 text-sm text-gray-800 outline-none focus-visible:border-[#006633] disabled:opacity-50"
              >
                <option value="">Selecione um perfil</option>
                <option value="ADMIN">Administrador</option>
                <option value="COORDINATOR">Coordenador</option>
                <option value="SUPERVISOR">Supervisor</option>
                <option value="ADVISOR">Orientador</option>
                <option value="BANKING">Banca</option>
                <option value="LIBRARY">Bibliotecário</option>
              </FormSelect>
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
          <SectionTitle className="mb-4">Informações Acadêmicas</SectionTitle>
          <div className="grid md:grid-cols-2 gap-4">
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold" htmlFor="campusId">
                Campus
              </Label>
              <FormSelect
                id="campusId"
                control={control}
                name={'campusId'}
                className="w-full min-w-0 border-0 border-b-2 border-[#002B18] bg-transparent py-2.5 text-sm text-gray-800 outline-none focus-visible:border-[#006633] disabled:opacity-50"
              >
                <option value="0">Selecione um campus</option>
                {campus.map((campi) => (
                  <option key={campi.id} value={campi.id}>
                    {campi.name}
                  </option>
                ))}
              </FormSelect>
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
              <FormSelect
                id="courseId"
                control={control}
                name={'courseId'}
                disabled={!campusValue || courses.length === 0}
                className="w-full min-w-0 border-0 border-b-2 border-[#002B18] bg-transparent py-2.5 text-sm text-gray-800 outline-none focus-visible:border-[#006633] disabled:opacity-50"
              >
                <option value="0">Selecione um curso</option>
                {courses.map((course) => (
                  <option key={course.id} value={course.id}>
                    {course.name}
                  </option>
                ))}
              </FormSelect>
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
            variant="destructive"
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
