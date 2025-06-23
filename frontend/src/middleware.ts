import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';
import { jwtDecode } from "jwt-decode";
interface DecodedToken {
  unique_name: string;
  userId: string;
  role: string;
}

// Rotas protegidas com perfis permitidos (token principal)
const protectedRoutes: Record<string, string[]> = {
  '/homePage': [], // Qualquer usuário logado pode acessar
  '/newTCC': ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR'],
  '/newUser': ['ADMIN', 'COORDINATOR', 'SUPERVISOR'],
  '/ongoingTCCs': ['ADMIN', 'COORDINATOR', 'SUPERVISOR', 'ADVISOR', 'BANK', 'LIBRARY'],
  '/ongoingTCCs/signatures': [],
  '/ongoingTCCs/details': [],
};

// Rotas temporárias com token temporário
const tempProtectedRoutes = ['/autoRegister', '/newPassword'];

export function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;
  const token = request.cookies.get('token')?.value;
  const tempToken = request.cookies.get('access_token_temp')?.value;

  // Verificação de rotas com autenticação principal
  for (const route in protectedRoutes) {
    if (path.startsWith(route)) {
      if (!token) {
        return NextResponse.redirect(new URL('/', request.url));
      }

      const allowedProfiles = protectedRoutes[route];
      if (allowedProfiles.length > 0) {
        try {
          const decoded = jwtDecode<DecodedToken>(token);
          if (!allowedProfiles.includes(decoded.role)) {
            return NextResponse.redirect(new URL('/unauthorized', request.url));
          }          
        } catch (err) {
          console.error('Erro ao decodificar token:', err);
          return NextResponse.redirect(new URL('/', request.url));
        }
      }
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
    '/ongoingTCCs',
  ],
};
