'use client';

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { KeyRound } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger
} from '@/components/ui/dialog';
import { FeatureActionCard } from '@/components/layout/FeatureActionCard';
import { ChangePasswordForm } from './ChangePasswordForm';

export function AccountSecurityCard() {
  const [open, setOpen] = useState(false);
  const params = useSearchParams();
  const router = useRouter();
  useEffect(() => {
    if (params.get('section') === 'password') setOpen(true);
  }, [params]);
  const changeOpen = (value: boolean) => {
    setOpen(value);
    if (!value && params.get('section') === 'password')
      router.replace('/profile', { scroll: false });
  };

  return (
    <Dialog open={open} onOpenChange={changeOpen}>
      <DialogTrigger asChild>
        <FeatureActionCard
          id="change-password-trigger"
          aria-label="Alterar senha"
          icon={KeyRound}
          heading="Cuide do seu acesso."
          description="Confirme sua senha atual para escolher uma nova."
          actionLabel="Alterar senha"
        />
      </DialogTrigger>
      <DialogContent className="max-h-[85dvh] overflow-y-auto border-[#DDD9CD] bg-[#FAF9F5]">
        <DialogHeader>
          <DialogTitle className="text-xl text-[#002B18]">
            Alterar senha
          </DialogTitle>
          <DialogDescription>
            Confirme a senha atual. Após salvar, entre novamente com a nova
            senha.
          </DialogDescription>
        </DialogHeader>
        <ChangePasswordForm />
      </DialogContent>
    </Dialog>
  );
}
