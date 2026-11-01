'use client';

import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import Header from '@/components/Header';
import Sidebar from '@/components/Sidebar';
import { PageState } from '@/components/layout/PageState';
import { Button } from '@/components/ui/button';
import { studentTccHref } from '@/components/Sidebar/menuConfig';
import { decodeSessionIdentity, type SessionIdentity } from '@/lib/session';
import { useStudentNavigation } from '@/app/hooks/useStudentNavigation';
import { Menu } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogTrigger
} from '@/components/ui/dialog';

export default function AuthRoutesLayout({
  children
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [compact, setCompact] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [session, setSession] = useState<SessionIdentity | null>(null);
  const [updatedName, setUpdatedName] = useState<{
    userId: string;
    name: string;
  } | null>(null);
  useEffect(() => {
    const updateName = (event: Event) =>
      setUpdatedName((event as CustomEvent).detail);
    window.addEventListener('profile-updated', updateName);
    return () => window.removeEventListener('profile-updated', updateName);
  }, []);
  const displayedSession =
    session && updatedName?.userId === session.userId
      ? { ...session, name: updatedName.name }
      : session;

  useEffect(() => {
    try {
      const token = Cookies.get('token');
      setSession(token ? decodeSessionIdentity(token) : null);
    } catch {
      setSession(null);
    }
  }, [pathname]);

  const isStudent = session?.roles.includes('STUDENT') ?? false;
  const student = useStudentNavigation(
    session?.userId ?? null,
    isStudent,
    pathname
  );
  const tccHref = studentTccHref(student);
  const redirectToTcc = isStudent && pathname === '/newTCC' && tccHref !== null;

  useEffect(() => {
    if (redirectToTcc && tccHref) router.replace(tccHref);
  }, [redirectToTcc, tccHref, router]);

  const handleLogout = () => {
    Cookies.remove('token');
    window.location.href = '/';
  };
  const waitingForTcc =
    isStudent && pathname === '/newTCC' && student.status === 'loading';
  const failedTccCheck =
    isStudent && pathname === '/newTCC' && student.status === 'error';

  return (
    <div className="min-h-screen bg-[#FAF9F5] font-[family-name:var(--font-poppins)] text-gray-700">
      <div
        className={
          session?.roles.length
            ? `grid grid-cols-1 ${compact ? 'md:grid-cols-[80px_minmax(0,1fr)]' : 'md:grid-cols-[240px_minmax(0,1fr)]'}`
            : ''
        }
      >
        {!!session?.roles.length && (
          <div className="hidden md:block">
            <Sidebar
              roles={session.roles}
              student={student}
              compact={compact}
              onToggle={() => setCompact(!compact)}
            />
          </div>
        )}
        <div className="min-w-0">
          <Header
            user={displayedSession}
            onLogout={handleLogout}
            navigation={
              !!session?.roles.length && (
                <>
                  <Dialog open={mobileOpen} onOpenChange={setMobileOpen}>
                    <DialogTrigger asChild>
                      <button
                        type="button"
                        aria-label="Abrir navegação"
                        className="cursor-pointer rounded-lg p-2 hover:bg-[#EAF2E8] md:hidden"
                      >
                        <Menu size={20} />
                      </button>
                    </DialogTrigger>
                    <DialogContent
                      aria-describedby={undefined}
                      className="left-0 top-0 h-dvh w-[280px] max-w-[85vw] translate-x-0 translate-y-0 gap-0 overflow-y-auto rounded-none border-0 bg-[#FAF9F5] p-0 font-[family-name:var(--font-poppins)] md:hidden"
                    >
                      <DialogTitle className="sr-only">
                        Menu principal
                      </DialogTitle>
                      <Sidebar
                        roles={session.roles}
                        student={student}
                        onNavigate={() => setMobileOpen(false)}
                      />
                    </DialogContent>
                  </Dialog>
                </>
              )
            }
          />
          <main className="min-w-0 px-6 pb-12 pt-3 sm:px-9 md:px-9 lg:px-12">
            <div className="mx-auto w-full max-w-[1200px]">
              {failedTccCheck ? (
                <div className="space-y-4">
                  <PageState>
                    Não foi possível consultar seu TCC. Tente novamente antes de
                    cadastrar uma proposta.
                  </PageState>
                  <Button
                    variant="outline"
                    onClick={() => window.location.reload()}
                  >
                    Tentar novamente
                  </Button>
                </div>
              ) : waitingForTcc || redirectToTcc ? (
                <PageState loading>
                  {redirectToTcc ? 'Abrindo seu TCC…' : 'Consultando seu TCC…'}
                </PageState>
              ) : (
                children
              )}
            </div>
          </main>
        </div>
      </div>
    </div>
  );
}
