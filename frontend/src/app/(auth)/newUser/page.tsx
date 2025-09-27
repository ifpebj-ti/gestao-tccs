'use client';

import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  faEnvelope,
  faGraduationCap,
  faIdCard,
  faUser
} from '@fortawesome/free-solid-svg-icons';
import { useRouter } from 'next/navigation';
import { useNewUserForm } from '@/app/hooks/useNewUser';

export default function NewUser() {
  const { push } = useRouter();
  const { errors, handleSubmit, register, submitForm, isSubmitting } =
    useNewUserForm();

  const formatCPF = (value: string) => {
    const numericValue = value.replace(/\D/g, '');
    const limitedValue = numericValue.slice(0, 11);
    let formattedValue = limitedValue;
    if (limitedValue.length > 3) {
      formattedValue = formattedValue.replace(/(\d{3})(\d)/, '$1.$2');
    }
    if (limitedValue.length > 6) {
      formattedValue = formattedValue.replace(
        /(\d{3})\.(\d{3})(\d)/,
        '$1.$2.$3'
      );
    }
    if (limitedValue.length > 9) {
      formattedValue = formattedValue.replace(
        /(\d{3})\.(\d{3})\.(\d{3})(\d{1,2})/,
        '$1.$2.$3-$4'
      );
    }
    return formattedValue;
  };

  const { onChange: onCpfChange, ...cpfRest } = register('cpf');

  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal mb-10">
        Novo usuário
      </h1>

      <form className="flex flex-col gap-4" onSubmit={handleSubmit(submitForm)}>
        <h2 className="text-lg font-extrabold uppercase">
          Informações do usuário
        </h2>
        <div className="grid md:grid-cols-2 gap-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="name">
              Nome
            </Label>
            <Input
              id="name"
              type="text"
              placeholder="Digite o nome do usuário"
              icon={faUser}
              errorText={errors.name?.message?.toString()}
              {...register('name')}
            />
          </div>
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="profile">
              Perfil
            </Label>
            <select
              id="profile"
              required
              {...register('profile')}
              className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all cursor-pointer"
            >
              <option value="">Selecione um perfil</option>
              <option value="ADMIN">Administrador</option>
              <option value="COORDINATOR">Coordenador</option>
              <option value="SUPERVISOR">Supervisor</option>
              <option value="ADVISOR">Orientador</option>
              <option value="BANKING">Banca</option>
              <option value="LIBRARY">Biblioteca</option>
            </select>
          </div>
        </div>
        <div className="grid md:grid-cols-3 gap-4">
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="email">
              Email
            </Label>
            <Input
              id="email"
              type="email"
              placeholder="Digite o email do usuário"
              icon={faEnvelope}
              errorText={errors.email?.message?.toString()}
              {...register('email')}
            />
          </div>
          {/* campo cpf */}
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="cpf">
              CPF
            </Label>
            <Input
              placeholder="Digite o CPF do usuário"
              icon={faIdCard}
              errorText={errors.cpf?.message?.toString()}
              maxLength={14}
              {...cpfRest}
              onChange={(e) => {
                e.target.value = formatCPF(e.target.value);
                onCpfChange(e);
              }}
            />
          </div>
          {/* campo siape */}
          <div className="grid items-center gap-1.5">
            <Label className="font-semibold" htmlFor="siape">
              SIAPE
            </Label>
            <Input
              id="siape"
              type="text"
              placeholder="Digite o SIAPE do usuário"
              icon={faGraduationCap}
              errorText={errors.siape?.message?.toString()}
              {...register('siape')}
            />
          </div>
        </div>
        <div className="flex gap-2 md:self-end">
          <Button
            onClick={() => push('/homePage')}
            variant="outline"
            className="w-full md:w-fit"
          >
            Cancelar
          </Button>
          <Button type="submit" className="w-full md:w-fit">
            {isSubmitting ? 'Cadastrando...' : 'Cadastrar usuário'}
          </Button>
        </div>
      </form>
    </div>
  );
}
