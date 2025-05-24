import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('access_token_temp')?.value;

  // bloqueia acesso se não tiver o cookie
  if (
    (request.nextUrl.pathname.startsWith('/autoRegister') || request.nextUrl.pathname.startsWith('/newPassword')) &&
    !token
  ) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/autoRegister', '/newPassword'],
};
