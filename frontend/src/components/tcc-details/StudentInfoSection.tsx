'use client';

import React from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faPaperPlane,
  faInfoCircle,
  faSpinner
} from '@fortawesome/free-solid-svg-icons';

interface Student {
  name: string;
  registration: string;
  cpf: string;
  course: string;
  email: string;
}

interface StudentInfoSectionProps {
  students: Student[];
  canResendInvite?: boolean;
  onResendInvite?: (email: string) => void;
  resendingInviteTo?: string | null;
}

export function StudentInfoSection({
  students,
  canResendInvite = false,
  onResendInvite = () => {},
  resendingInviteTo = null
}: StudentInfoSectionProps) {
  if (students.length === 0) {
    return (
      <section>
        <h2 className="text-lg font-extrabold uppercase">
          Informações do(s) estudante(s)
        </h2>
        <p className="text-gray-600 italic mt-4">
          Nenhum estudante associado a esta proposta.
        </p>
      </section>
    );
  }

  return (
    <section>
      <h2 className="text-lg font-extrabold uppercase">
        Informações do(s) estudante(s)
      </h2>
      {students.map((student, i) => {
        const isThisStudentLoading = resendingInviteTo === student.email;

        return (
          <div
            key={i}
            className="mt-4 border-t pt-4 first:border-t-0 first:pt-0"
          >
            {student.name ? (
              // SE O ALUNO JÁ SE CADASTROU
              <>
                <div className="flex justify-between items-center mb-4">
                  <p className="font-semibold text-gray-700">
                    Estudante {i + 1}
                  </p>
                </div>
                <div className="grid md:grid-cols-2 gap-4">
                  <div className="grid items-center gap-1.5">
                    <Label>Nome</Label>
                    <Input value={student.name} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label>Matrícula</Label>
                    <Input value={student.registration} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label>CPF</Label>
                    <Input value={student.cpf} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5">
                    <Label>Curso</Label>
                    <Input value={student.course} readOnly />
                  </div>
                  <div className="grid items-center gap-1.5 md:col-span-2">
                    <Label>Email</Label>
                    <Input value={student.email} readOnly />
                  </div>
                </div>
              </>
            ) : (
              // SE O ALUNO AINDA NÃO SE CADASTROU
              <>
                <div className="flex justify-between items-center mb-2">
                  <p className="font-semibold text-gray-700">
                    Estudante {i + 1} (Pendente)
                  </p>
                  {canResendInvite && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => onResendInvite(student.email)}
                      disabled={!!resendingInviteTo}
                    >
                      {isThisStudentLoading ? (
                        <FontAwesomeIcon
                          icon={faSpinner}
                          spin
                          className="mr-2 h-3.5 w-3.5"
                        />
                      ) : (
                        <FontAwesomeIcon
                          icon={faPaperPlane}
                          className="mr-2 h-3.5 w-3.5"
                        />
                      )}
                      {isThisStudentLoading
                        ? 'Reenviando...'
                        : 'Reenviar Convite'}
                    </Button>
                  )}
                </div>
                <div className="grid items-center gap-1.5">
                  <Label>Email Convidado</Label>
                  <Input
                    value={student.email}
                    readOnly
                    className="bg-yellow-50 border-yellow-200"
                  />
                </div>
                <p className="text-xs text-gray-500 italic mt-2 flex items-center gap-1">
                  <FontAwesomeIcon icon={faInfoCircle} />
                  Aguardando o aluno realizar o primeiro acesso e completar o
                  cadastro.
                </p>
              </>
            )}
          </div>
        );
      })}
    </section>
  );
}
