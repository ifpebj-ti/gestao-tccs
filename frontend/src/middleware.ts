import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { jwtDecode } from 'jwt-decode';
import { toast } from 'react-toastify';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string | string[];
  isTestUser?: boolean;
  isDefaultPassword?: boolean;
}

const protectedRoutes: Record<string, string[]> = {
  '/homePage': [],
  '/newTCC': ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR'],
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
  '/users': ['ADMIN', 'COORDINATOR', 'SUPERVISOR']
};

const tempProtectedRoutes = [
  '/autoRegister',
  '/newPassword',
  '/updatePassword'
];

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
      toast.error('Erro ao decodificar token');
      return NextResponse.redirect(new URL('/', request.url));
    }
  }

  if (tempProtectedRoutes.some((r) => path.startsWith(r)) && !tempToken) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  const isTempProtectedRoute = tempProtectedRoutes.some((r) =>
    path.startsWith(r)
  );
  if (isTempProtectedRoute) {
    // Permitir acesso se o token temporário existir
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
        toast.error('Erro ao decodificar token');
      }
    }
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/homePage',
    '/newTCC',
    '/newUser',
    '/autoRegister',
    '/newPassword',
    '/updatePassword',
    '/ongoingTCCs/:path*',
    '/myTCC/:path*',
    '/completedTCCs/:path*',
    '/pendingSignatures/:path*',
    '/users/:path*'
  ]
};
