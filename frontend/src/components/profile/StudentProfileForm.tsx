'use client';

import { FormSelect } from '@/components/ui/form-select';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  studentProfileSchema,
  type StudentProfileValues
} from '@/app/schemas/studentProfileSchema';
import type { MyProfile } from '@/app/hooks/useMyProfile';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { SectionTitle } from '@/components/layout/SectionTitle';
import { formatPhone } from '@/lib/phone';

const shifts = ['Matutino', 'Vespertino', 'Noturno', 'Diurno'];
const fields = [
  {
    name: 'name',
    label: 'Nome completo',
    autoComplete: 'name',
    maxLength: 100
  },
  {
    name: 'phone',
    label: 'Telefone com DDD',
    autoComplete: 'tel-national',
    maxLength: 20
  },
  { name: 'cpf', label: 'CPF', autoComplete: 'off', maxLength: 14 },
  {
    name: 'registration',
    label: 'Matrícula',
    autoComplete: 'off',
    maxLength: 50
  },
  { name: 'userClass', label: 'Turma', autoComplete: 'off', maxLength: 50 }
] as const;

export function StudentProfileForm({
  profile,
  save
}: {
  profile: MyProfile;
  save: (data: StudentProfileValues) => Promise<boolean>;
}) {
  const form = useForm<StudentProfileValues>({
    resolver: zodResolver(studentProfileSchema),
    defaultValues: {
      name: profile.name,
      phone: formatPhone(profile.phone),
      cpf: profile.cpf ?? '',
      registration: profile.registration ?? '',
      userClass: profile.userClass ?? '',
      shift: shifts.indexOf(profile.shift ?? '') + 1
    }
  });
  const { errors, isDirty, isSubmitting } = form.formState;
  return (
    <section aria-labelledby="profile-personal">
      <SectionTitle id="profile-personal">Dados pessoais</SectionTitle>
      <form
        noValidate
        className="space-y-6 pt-6"
        onSubmit={form.handleSubmit(async (data) => {
          if (await save(data)) form.reset(data);
        })}
      >
        <div className="grid gap-6 sm:grid-cols-2">
          {fields.map((field) => (
            <div key={field.name}>
              <Label htmlFor={`profile-${field.name}`}>{field.label}</Label>
              <Input
                id={`profile-${field.name}`}
                autoComplete={field.autoComplete}
                maxLength={field.maxLength}
                type={field.name === 'phone' ? 'tel' : 'text'}
                {...form.register(field.name)}
                errorText={errors[field.name]?.message}
                disabled={isSubmitting}
              />
            </div>
          ))}
          <div>
            <Label htmlFor="profile-shift">Turno</Label>
            <FormSelect
              id="profile-shift"
              control={form.control}
              name={'shift'}
              disabled={isSubmitting}
              aria-invalid={!!errors.shift}
              aria-describedby={
                errors.shift ? 'profile-shift-error' : undefined
              }
              className="w-full border-b-2 border-[#002B18] bg-transparent py-3 text-sm"
            >
              <option value={0}>Selecione um turno</option>
              {shifts.map((shift, index) => (
                <option key={shift} value={index + 1}>
                  {shift}
                </option>
              ))}
            </FormSelect>
            {errors.shift && (
              <p
                id="profile-shift-error"
                role="alert"
                className="mt-2 text-xs text-red-600"
              >
                {errors.shift.message}
              </p>
            )}
          </div>
        </div>
        <div className="flex flex-wrap gap-4">
          <Button type="submit" disabled={!isDirty || isSubmitting}>
            {isSubmitting ? 'Salvando…' : 'Salvar alterações'}
          </Button>
          {isDirty && (
            <Button
              type="button"
              variant="ghost"
              disabled={isSubmitting}
              onClick={() => form.reset()}
            >
              Descartar alterações
            </Button>
          )}
        </div>
      </form>
    </section>
  );
}
