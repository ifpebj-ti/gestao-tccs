import {
  Home,
  FileText,
  Users,
  GraduationCap,
  FileSignature,
  Building2,
  Calendar,
  CircleUserRound,
  CheckCircle2,
  ClipboardList,
  type LucideIcon
} from 'lucide-react';

export type UserRole =
  | 'ADMIN'
  | 'COORDINATOR'
  | 'SUPERVISOR'
  | 'ADVISOR'
  | 'STUDENT'
  | 'BANKING'
  | 'LIBRARY';
export const validRoles: UserRole[] = [
  'STUDENT',
  'ADVISOR',
  'COORDINATOR',
  'SUPERVISOR',
  'LIBRARY',
  'BANKING',
  'ADMIN'
];

export interface StudentNavigation {
  status: 'loading' | 'none' | 'active' | 'completed' | 'error';
  tccId?: number;
}

export interface SidebarMenuItem {
  label: string;
  href: string;
  icon: LucideIcon;
  activePaths?: string[];
}

export const roleLabels: Record<UserRole, string> = {
  ADMIN: 'Administração',
  COORDINATOR: 'Coordenação',
  SUPERVISOR: 'Supervisão',
  ADVISOR: 'Orientação',
  STUDENT: 'Estudante',
  BANKING: 'Banca examinadora',
  LIBRARY: 'Biblioteca'
};

export function studentTccHref(student: StudentNavigation): string | null {
  if (!student.tccId) return null;
  if (student.status === 'completed') return `/completedTCCs/${student.tccId}`;
  if (student.status === 'active')
    return `/myTCC/signatures?id=${student.tccId}`;
  return null;
}

export function getSidebarItems(
  roles: UserRole[],
  student: StudentNavigation
): SidebarMenuItem[] {
  const has = (...allowed: UserRole[]) =>
    roles.some((role) => allowed.includes(role));
  const items: SidebarMenuItem[] = [
    { label: 'Início', href: '/homePage', icon: Home }
  ];
  const myTcc = studentTccHref(student);

  if (has('STUDENT')) {
    if (myTcc) {
      items.push({
        label: 'Meu TCC',
        href: myTcc,
        icon: GraduationCap,
        activePaths: ['/myTCC', `/completedTCCs/${student.tccId}`]
      });
    } else if (student.status === 'none') {
      items.push({ label: 'Proposta de TCC', href: '/newTCC', icon: FileText });
    }
  } else if (has('ADMIN', 'COORDINATOR', 'SUPERVISOR')) {
    items.push({ label: 'Nova proposta', href: '/newTCC', icon: FileText });
  }

  if (has('ADVISOR', 'COORDINATOR', 'SUPERVISOR')) {
    items.push({
      label: 'Propostas recebidas',
      href: '/receivedProposals',
      icon: FileText
    });
  }
  if (
    (has('STUDENT') && myTcc) ||
    (!has('STUDENT') &&
      has('COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY', 'BANKING'))
  ) {
    items.push({
      label: 'Assinaturas',
      href: '/pendingSignatures',
      icon: FileSignature
    });
  }
  if (has('COORDINATOR', 'SUPERVISOR')) {
    items.push({
      label: 'Pendências',
      href: '/coordinationPending',
      icon: ClipboardList
    });
  }
  if (
    has('COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY', 'BANKING', 'ADMIN')
  ) {
    items.push({
      label: 'Em andamento',
      href: '/ongoingTCCs',
      icon: GraduationCap
    });
  }
  if (has('COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY', 'ADMIN')) {
    items.push({
      label: 'Concluídos',
      href: '/completedTCCs',
      icon: CheckCircle2
    });
  }
  if (has('ADMIN', 'COORDINATOR', 'SUPERVISOR')) {
    items.push({ label: 'Usuários', href: '/users', icon: Users });
  }
  if (has('ADMIN')) {
    items.push({
      label: 'Instituições e cursos',
      href: '/instituicoes',
      icon: Building2
    });
    items.push({
      label: 'Semestres letivos',
      href: '/semestres',
      icon: Calendar
    });
  }
  items.push({ label: 'Minha conta', href: '/profile', icon: CircleUserRound });
  return items;
}

export function isSidebarItemActive(
  item: SidebarMenuItem,
  pathname: string
): boolean {
  const paths = item.activePaths ?? [item.href.split('?')[0]];
  return paths.some(
    (path) => pathname === path || pathname.startsWith(`${path}/`)
  );
}

export function activeSidebarHref(
  items: SidebarMenuItem[],
  pathname: string
): string | null {
  const matches = items.filter((item) => isSidebarItemActive(item, pathname));
  const specificity = (item: SidebarMenuItem) =>
    Math.max(
      ...(item.activePaths ?? [item.href.split('?')[0]])
        .filter((path) => pathname === path || pathname.startsWith(`${path}/`))
        .map((path) => path.length)
    );
  return (
    matches.sort((first, second) => specificity(second) - specificity(first))[0]
      ?.href ?? null
  );
}
