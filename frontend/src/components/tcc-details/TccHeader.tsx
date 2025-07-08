'use client';

import { Badge } from '@/components/ui/badge';

interface Student {
  name: string;
}

interface TccHeaderProps {
  infoStudent: Student[];
  cancellationRequested: boolean;
}

export function TccHeader({
  infoStudent,
  cancellationRequested
}: TccHeaderProps) {
  const formatStudentName = (fullName: string) => {
    const [first, second] = fullName.trim().split(' ');
    return `${first ?? ''} ${second ?? ''}`;
  };

  const studentNamesTitle =
    infoStudent?.map((s) => s.name).join(', ') || 'Carregando...';
  const studentNamesDisplay = infoStudent?.length
    ? infoStudent.map((s) => formatStudentName(s.name)).join(', ')
    : 'Aguardando cadastro do estudante';

  return (
    <div className="flex flex-col md:flex-row md:items-center md:gap-4">
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800 pb-2 truncate max-w-full">
        TCC - <span title={studentNamesTitle}>{studentNamesDisplay}</span>
      </h1>
      {cancellationRequested && (
        <Badge variant="destructive" className="text-sm w-fit mt-2 md:mt-0">
          Cancelamento solicitado
        </Badge>
      )}
    </div>
  );
}
