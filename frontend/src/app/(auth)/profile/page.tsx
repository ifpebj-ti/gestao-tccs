'use client';

import { Suspense, useState } from 'react';
import { ArrowUpRight, Phone } from 'lucide-react';
import { useMyProfile } from '@/app/hooks/useMyProfile';
import { PageIntro } from '@/components/layout/PageIntro';
import { PageState } from '@/components/layout/PageState';
import { SectionTitle } from '@/components/layout/SectionTitle';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { StudentProfileForm } from '@/components/profile/StudentProfileForm';
import { AccountSecurityCard } from '@/components/profile/AccountSecurityCard';
import { roleLabels, type UserRole } from '@/components/Sidebar/menuConfig';
import { formatPhone, isValidProfilePhone, phoneDigits } from '@/lib/phone';

export default function ProfilePage() {
  const { profile, status, isSaving, loadProfile, savePhone, saveStudent } =
    useMyProfile();
  const [draftPhone, setDraftPhone] = useState<string | null>(null);
  const phone = draftPhone ?? formatPhone(profile?.phone ?? '');
  const changed =
    draftPhone !== null &&
    phoneDigits(phone) !== phoneDigits(profile?.phone ?? '');

  return (
    <div className="max-w-5xl">
      <PageIntro
        title="Minha conta"
        description="Mantenha seus dados pessoais atualizados e cuide do seu acesso."
      />
      {status === 'loading' ? (
        <PageState loading>Carregando suas informações…</PageState>
      ) : status === 'error' || !profile ? (
        <div className="space-y-4">
          <PageState>Não foi possível carregar sua conta.</PageState>
          <Button variant="outline" onClick={() => void loadProfile()}>
            Tentar novamente
          </Button>
        </div>
      ) : (
        <div className="grid gap-12 lg:grid-cols-[minmax(0,1fr)_260px]">
          <div className="space-y-9">
            <section aria-labelledby="profile-identity">
              <SectionTitle id="profile-identity">Identificação</SectionTitle>
              <dl className="grid gap-x-8 gap-y-6 py-6 sm:grid-cols-2">
                {[
                  ...(!profile.roles.includes('STUDENT')
                    ? [['Nome', profile.name]]
                    : []),
                  ['E-mail institucional', profile.email],
                  [
                    'Perfil de acesso',
                    profile.roles
                      .map((role) => roleLabels[role as UserRole] ?? role)
                      .join(' · ')
                  ],
                  ['Campus', profile.campus],
                  ['Curso', profile.course],
                  ...(profile.registration && !profile.roles.includes('STUDENT')
                    ? [['Matrícula', profile.registration]]
                    : []),
                  ...(profile.siape ? [['SIAPE', profile.siape]] : [])
                ].map(([label, value]) => (
                  <div key={label} className="min-w-0">
                    <dt className="text-[10px] font-bold uppercase tracking-[0.08em] text-gray-500">
                      {label}
                    </dt>
                    <dd className="mt-2 break-words text-sm font-medium text-[#002B18]">
                      {value || 'Não informado'}
                    </dd>
                  </div>
                ))}
              </dl>
              <p className="text-xs leading-relaxed text-gray-500">
                Para corrigir campus ou curso, procure a coordenação.
              </p>
            </section>
            {profile.roles.includes('STUDENT') ? (
              <StudentProfileForm profile={profile} save={saveStudent} />
            ) : (
              <section aria-labelledby="profile-contact">
                <SectionTitle id="profile-contact">Contato</SectionTitle>
                <form
                  className="pt-6"
                  onSubmit={async (event) => {
                    event.preventDefault();
                    if (await savePhone(phone)) setDraftPhone(null);
                  }}
                >
                  <div className="max-w-sm">
                    <Label htmlFor="profile-phone">Telefone com DDD</Label>
                    <Input
                      id="profile-phone"
                      type="tel"
                      autoComplete="tel-national"
                      inputMode="tel"
                      icon={<Phone size={16} aria-hidden="true" />}
                      value={phone}
                      onChange={(event) =>
                        setDraftPhone(formatPhone(event.target.value))
                      }
                      disabled={isSaving}
                      placeholder="(81) 99999-9999"
                      errorText={
                        draftPhone !== null && !isValidProfilePhone(phone)
                          ? 'Informe DDD e 10 ou 11 dígitos.'
                          : undefined
                      }
                    />
                  </div>
                  <div className="mt-6 flex flex-wrap items-center gap-4">
                    <Button
                      type="submit"
                      disabled={
                        !changed || !isValidProfilePhone(phone) || isSaving
                      }
                    >
                      {isSaving ? 'Salvando…' : 'Salvar telefone'}
                      <ArrowUpRight size={16} aria-hidden="true" />
                    </Button>
                    {changed && (
                      <Button
                        type="button"
                        variant="ghost"
                        disabled={isSaving}
                        onClick={() => setDraftPhone(null)}
                      >
                        Descartar alteração
                      </Button>
                    )}
                  </div>
                </form>
              </section>
            )}
          </div>
          <Suspense fallback={null}>
            <AccountSecurityCard />
          </Suspense>
        </div>
      )}
    </div>
  );
}
