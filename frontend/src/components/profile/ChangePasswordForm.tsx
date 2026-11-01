'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import Cookies from 'js-cookie';
import { env } from 'next-runtime-env';
import { toast } from 'react-toastify';
import { useRouter } from 'next/navigation';
import {
  changeOwnPasswordSchema,
  type ChangeOwnPasswordValues
} from '@/app/schemas/changeOwnPasswordSchema';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';

export function ChangePasswordForm() {
  const router = useRouter();
  const form = useForm<ChangeOwnPasswordValues>({
    resolver: zodResolver(changeOwnPasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' }
  });
  const { errors, isSubmitting } = form.formState;
  const submit = async ({
    currentPassword,
    newPassword
  }: ChangeOwnPasswordValues) => {
    const token = Cookies.get('token');
    if (!token) {
      router.replace('/');
      return;
    }
    try {
      const response = await fetch(
        `${env('NEXT_PUBLIC_API_URL')}/User/me/password`,
        {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({ currentPassword, newPassword })
        }
      );
      if (response.status === 401) {
        router.replace('/');
        return;
      }
      if (!response.ok) {
        const data = await response.json().catch(() => null);
        toast.error(
          response.status === 429
            ? 'Aguarde um minuto antes de tentar novamente.'
            : (data?.detail ??
                data?.message ??
                'Não foi possível alterar a senha. Confira a senha atual.')
        );
        return;
      }
      form.reset();
      Cookies.remove('token');
      toast.success('Senha alterada. Entre novamente com a nova senha.');
      router.replace('/');
    } catch {
      toast.error('Não foi possível conectar ao servidor. Tente novamente.');
    }
  };
  return (
    <section id="profile-password">
      <form
        noValidate
        onSubmit={form.handleSubmit(submit)}
        className="space-y-5"
      >
        {[
          {
            name: 'currentPassword',
            label: 'Senha atual',
            autoComplete: 'current-password'
          },
          {
            name: 'newPassword',
            label: 'Nova senha',
            autoComplete: 'new-password'
          },
          {
            name: 'confirmPassword',
            label: 'Confirme a nova senha',
            autoComplete: 'new-password'
          }
        ].map(({ name, label, autoComplete }) => {
          const field = name as keyof ChangeOwnPasswordValues;
          return (
            <div key={name}>
              <Label htmlFor={`password-${name}`}>{label}</Label>
              <Input
                id={`password-${name}`}
                isPassword
                autoComplete={autoComplete}
                {...form.register(field)}
                errorText={errors[field]?.message}
                disabled={isSubmitting}
              />
            </div>
          );
        })}
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Alterando…' : 'Salvar nova senha'}
        </Button>
      </form>
    </section>
  );
}
