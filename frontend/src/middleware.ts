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
    'BANKING'
  ],
  '/myTCC': ['STUDENT'],
  '/completedTCCs': [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR'
  ],
  '/pendingSignatures': [
    'ADMIN',
    'COORDINATOR',
    'SUPERVISOR',
    'ADVISOR',
    'STUDENT',
    'BANKING'
  ],
  '/libraryPending': ['LIBRARY', 'ADMIN'],
  '/coordinationPending': ['COORDINATOR', 'SUPERVISOR'],
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

function continueWithContentSecurityPolicy(request: NextRequest) {
  const nonce = crypto.randomUUID().replaceAll('-', '');
  const runtime = process.env;
  const configuredApi = runtime.NEXT_PUBLIC_API_URL;
  const api = configuredApi && URL.canParse(configuredApi) ? new URL(configuredApi) : null;
  const apiOrigin = api && ['https:', 'http:'].includes(api.protocol) ? ` ${api.origin}` : '';
  const development = process.env.NODE_ENV === 'development';
  const policy = [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}'${development ? " 'unsafe-eval'" : ''}`,
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
    "font-src 'self' https://fonts.gstatic.com",
    `connect-src 'self'${apiOrigin}${development ? ' ws: wss:' : ''}`,
    `img-src 'self' data: blob:${apiOrigin}`,
    "frame-src 'self' blob:",
    "worker-src 'self' blob:",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'"
  ].join('; ');

  const headers = new Headers(request.headers);
  headers.set('x-nonce', nonce);
  headers.set('Content-Security-Policy', policy);
  const response = NextResponse.next({ request: { headers } });
  response.headers.set('Content-Security-Policy', policy);
  response.headers.set('Cache-Control', 'private, no-store');
  return response;
}

export function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;
  const token = request.cookies.get('token')?.value;
  const tempToken = request.cookies.get('access_token_temp')?.value;

  const protectedRoute = Object.keys(protectedRoutes).find((route) =>
    path === route || path.startsWith(`${route}/`)
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
      return continueWithContentSecurityPolicy(request);
    }
    // Verificar o token principal se o temporário não existir
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        // Permitir acesso se for usuário de teste ou estiver com senha padrão
        if (decoded.isTestUser || decoded.isDefaultPassword) {
          return continueWithContentSecurityPolicy(request);
        }
      } catch {
        // Ignora erro de decodificação e redireciona
      }
    }
    return NextResponse.redirect(new URL('/', request.url));
  }

  return continueWithContentSecurityPolicy(request);
}

export const config = {
  matcher: [
    '/((?!api/|_next/static|_next/image|.*\\.[^/]+$).*)'
  ]
};
