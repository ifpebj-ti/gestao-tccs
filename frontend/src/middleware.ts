import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { jwtDecode } from "jwt-decode";

interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string | string[];
}

const protectedRoutes: Record<string, string[]> = {
  '/homePage': [], 
  '/newTCC': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/newUser': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/ongoingTCCs': ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'BANKING', 'LIBRARY'],
  '/myTCC': ['STUDENT'],
};

const tempProtectedRoutes = ['/autoRegister', '/newPassword'];

function hasPermission(userRoles: string | string[], allowedRoles: string[]): boolean {
  // Se a rota não exige perfis específicos, o acesso é permitido.
  if (allowedRoles.length === 0) {
    return true;
  }
  
  // Se o usuário tem uma única role (string)
  if (typeof userRoles === 'string') {
    return allowedRoles.includes(userRoles);
  }

  // Se o usuário tem múltiplas roles (array), verifica se pelo menos uma delas é permitida.
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

    const allowedProfiles = protectedRoutes[protectedRoute];

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      
      if (!hasPermission(decoded.role, allowedProfiles)) {
        return NextResponse.redirect(new URL('/unauthorized', request.url));
      }
    } catch (err) {
      console.error('Erro ao decodificar token:', err);
      return NextResponse.redirect(new URL('/', request.url));
    }
  }

  // Verificação de rotas com autenticação temporária
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
    '/ongoingTCCs/:path*', // Usar :path* para cobrir sub-rotas como /signatures e /details
    '/myTCC/:path*',
  ],
};