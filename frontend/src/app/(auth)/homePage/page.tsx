'use client';

import { CardHome } from '@/components/CardHome';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
import {
  faFileCircleCheck,
  faFileCirclePlus,
  faFileSignature,
  faGraduationCap,
  faUserPlus
} from '@fortawesome/free-solid-svg-icons';
import Link from 'next/link';

export default function HomePage() {
  return (
    <div className="flex flex-col">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 mb-10">
        Página Inicial
      </h1>
      <div className="grid grid-cols-6 gap-6">
        <CardHome
          title="Assinaturas pendentes"
          icon={faFileSignature}
          indicatorNumber={3}
          indicatorColor="bg-red-600"
          className="col-span-2"
        />
        <CardHome
          title="TCCs em andamento"
          icon={faGraduationCap}
          indicatorNumber={7}
          indicatorColor="bg-blue-600"
          className="col-span-2"
        />
        <CardHome
          title="TCCs concluídos"
          icon={faFileCircleCheck}
          className="col-span-2"
        />
        <Link href={'/newTCC'} className="col-span-2 col-start-2">
          <CardHome title="Cadastrar nova proposta" icon={faFileCirclePlus} />
        </Link>
        <Link href={'/newUser'} className="col-span-2">
          <CardHome
            title="Cadastrar novo usuário"
            icon={faUserPlus}
            className="col-span-2"
          />
        </Link>
      </div>
    </div>
  );
}
