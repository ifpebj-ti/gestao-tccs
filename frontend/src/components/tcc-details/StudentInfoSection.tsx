// app/components/tcc-details/StudentInfoSection.tsx
'use client';

import React from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface Student {
  name: string;
  registration: string;
  cpf: string;
  course: string;
  email: string;
}

interface StudentInfoSectionProps {
  students: Student[];
}

export function StudentInfoSection({ students }: StudentInfoSectionProps) {
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
      <div className="grid md:grid-cols-2 gap-4 mt-4">
        {students.map((student, i) => (
          <React.Fragment key={i}>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Nome</Label>
              <Input value={student.name} readOnly />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Matrícula</Label>
              <Input value={student.registration} readOnly />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">CPF</Label>
              <Input value={student.cpf} readOnly />
            </div>
            <div className="grid items-center gap-1.5">
              <Label className="font-semibold">Curso</Label>
              <Input value={student.course} readOnly />
            </div>
            <div className="grid items-center gap-1.5 md:col-span-2">
              <Label className="font-semibold">Email</Label>
              <Input value={student.email} readOnly />
            </div>
          </React.Fragment>
        ))}
      </div>
    </section>
  );
}
