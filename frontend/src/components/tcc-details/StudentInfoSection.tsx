'use client';

import React from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faPaperPlane } from '@fortawesome/free-solid-svg-icons';

interface Student {
  name: string;
  registration: string;
  cpf: string;
  course: string;
  email: string;
}

interface StudentInfoSectionProps {
  students: Student[];
  canResendInvite: boolean;
  onResendInvite: (email: string) => void;
}

export function StudentInfoSection({
  students,
  canResendInvite,
  onResendInvite
}: StudentInfoSectionProps) {
  if (students.length === 0) {
    return (
      <section>
        <h2 className="text-lg font-extrabold uppercase">
          Informações do(s) estudante(s)
        </h2>
        <p className="text-gray-600 italic mt-4">
          Estudante ainda não se cadastrou na plataforma.
        </p>
      </section>
    );
  }

  return (
    <section>
      <h2 className="text-lg font-extrabold uppercase">
        Informações do(s) estudante(s)
      </h2>
      {students.map((student, i) => (
        <div key={i} className="mt-4 border-t pt-4 first:border-t-0 first:pt-0">
          <div className="flex justify-between items-center mb-4">
            <p className="font-semibold text-gray-700">Estudante {i + 1}</p>
            {canResendInvite && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onResendInvite(student.email)}
              >
                <FontAwesomeIcon icon={faPaperPlane} />
                Reenviar Convite
              </Button>
            )}
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
        </div>
      ))}
    </section>
  );
}
