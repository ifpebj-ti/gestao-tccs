import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string | string[];
  exp?: number;
  isTestUser?: boolean;
  isDefaultPassword?: boolean;
}

const protectedRoutes: Record<string, string[]> = {
  '/homePage': [],
  '/profile': [],
  '/instituicoes': ['ADMIN'],
  '/newTCC': ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'STUDENT'],
  '/receivedProposals': ['ADVISOR', 'COORDINATOR', 'SUPERVISOR'],
  '/newUser': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/ongoingTCCs': [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'BANKING',
    'LIBRARY'
  ],
  '/myTCC': ['STUDENT'],
  '/completedTCCs': [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'LIBRARY'
  ],
  '/pendingSignatures': [],
  '/users': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/semestres': ['ADMIN', 'COORDINATOR', 'SUPERVISOR']
};

const tempProtectedRoutes: string[] = [];

function hasPermission(
  userRoles: string | string[],
  allowedRoles: string[]
): boolean {
  if (allowedRoles.length === 0) {
    return true;
  }
  if (typeof userRoles === 'string') {
    return allowedRoles.includes(userRoles);
  }
  if (Array.isArray(userRoles)) {
    return userRoles.some((role) => allowedRoles.includes(role));
  }
  return false;
}

export function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;
  const token = request.cookies.get('token')?.value;
  const tempToken = request.cookies.get('access_token_temp')?.value;

  const protectedRoute = Object.keys(protectedRoutes).find((route) =>
    path.startsWith(route)
  );

  if (protectedRoute) {
    if (!token) {
      return NextResponse.redirect(new URL('/', request.url));
    }

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const userRoles = decoded.role;
      const roles = Array.isArray(userRoles) ? userRoles : [userRoles];
      const knownRoles = [
        'ADMIN',
        'COORDINATOR',
        'SUPERVISOR',
        'ADVISOR',
        'STUDENT',
        'BANKING',
        'LIBRARY'
      ];
      // Esta checagem melhora a navegação; a API continua validando assinatura e acesso.
      if (
        !Number.isSafeInteger(Number(decoded.userId)) ||
        Number(decoded.userId) <= 0 ||
        typeof decoded.exp !== 'number' ||
        !Number.isFinite(decoded.exp) ||
        decoded.exp <= Date.now() / 1000 ||
        !roles.some((role) => knownRoles.includes(role))
      ) {
        return NextResponse.redirect(new URL('/', request.url));
      }

      const allowedProfiles = protectedRoutes[protectedRoute];
      const hasGeneralPermission = hasPermission(userRoles, allowedProfiles);

      if (!hasGeneralPermission) {
        const isStudent = Array.isArray(userRoles)
          ? userRoles.includes('STUDENT')
          : userRoles === 'STUDENT';
        const isAccessingSpecificCompletedTcc =
          protectedRoute === '/completedTCCs' && path !== '/completedTCCs';

        // Se for um aluno tentando acessar uma página de detalhes de TCC concluído, permita.
        if (isStudent && isAccessingSpecificCompletedTcc) {
        } else {
          // Se não for essa exceção, então o usuário realmente não tem permissão.
          return NextResponse.redirect(new URL('/unauthorized', request.url));
        }
      }
    } catch {
      return NextResponse.redirect(new URL('/', request.url));
    }
  }

  const isTempProtectedRoute = tempProtectedRoutes.some((r) =>
    path.startsWith(r)
  );

  if (isTempProtectedRoute) {
    // Permitir acesso se o cookie temporário existir (código de verificação validado)
    if (tempToken) {
      return NextResponse.next();
    }
    // Verificar o token principal se o temporário não existir
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        // Permitir acesso se for usuário de teste ou estiver com senha padrão
        if (decoded.isTestUser || decoded.isDefaultPassword) {
          return NextResponse.next();
        }
      } catch {
        // Ignora erro de decodificação e redireciona
      }
    }
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/homePage',
    '/profile',
    '/instituicoes/:path*',
    '/newTCC',
    '/receivedProposals',
    '/newUser',
    '/newPassword',
    '/ongoingTCCs/:path*',
    '/myTCC/:path*',
    '/completedTCCs/:path*',
    '/pendingSignatures/:path*',
    '/users/:path*',
    '/semestres/:path*'
  ]
};
