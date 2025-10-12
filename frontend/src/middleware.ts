import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { jwtDecode } from "jwt-decode";
import { toast } from 'react-toastify';

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string | string[];
}

const protectedRoutes: Record<string, string[]> = {
  '/homePage': [], 
  '/newTCC': ['COORDINATOR', 'SUPERVISOR', 'ADVISOR'],
  '/newUser': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/ongoingTCCs': ['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'BANKING', 'LIBRARY'],
  '/myTCC': ['STUDENT'],
  '/completedTCCs': ['COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'LIBRARY'],
  '/pendingSignatures': [], 
};

const tempProtectedRoutes = ['/autoRegister', '/newPassword'];

function hasPermission(userRoles: string | string[], allowedRoles: string[]): boolean {
  if (allowedRoles.length === 0) {
    return true;
  }
  if (typeof userRoles === 'string') {
    return allowedRoles.includes(userRoles);
  }
  if (Array.isArray(userRoles)) {
    return userRoles.some(role => allowedRoles.includes(role));
  }
  return false;
}

export function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;
  const token = request.cookies.get('token')?.value;
  const tempToken = request.cookies.get('access_token_temp')?.value;

  const protectedRoute = Object.keys(protectedRoutes).find(route => path.startsWith(route));

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
        const isStudent = (Array.isArray(userRoles) ? userRoles.includes('STUDENT') : userRoles === 'STUDENT');
        const isAccessingSpecificCompletedTcc = protectedRoute === '/completedTCCs' && path !== '/completedTCCs';

        // Se for um aluno tentando acessar uma página de detalhes de TCC concluído, permita.
        if (isStudent && isAccessingSpecificCompletedTcc) {
        } else {
          // Se não for essa exceção, então o usuário realmente não tem permissão.
          return NextResponse.redirect(new URL('/unauthorized', request.url));
        }
      }
    } catch {
      toast.error('Erro ao decodificar token')
      return NextResponse.redirect(new URL('/', request.url));
    }
  }

  if (tempProtectedRoutes.some((r) => path.startsWith(r)) && !tempToken) {
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
    '/ongoingTCCs/:path*',
    '/myTCC/:path*',
    '/completedTCCs/:path*',
    '/pendingSignatures/:path*',
  ],
};