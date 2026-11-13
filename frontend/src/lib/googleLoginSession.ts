import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { decodeSessionIdentity } from '@/lib/session';

interface GoogleLoginClaims {
  exp?: number;
}

/** Confirms that the callback cookie contains a usable local session and persists it. */
export function persistGoogleLoginSession(token?: string): boolean {
  if (!token || typeof window === 'undefined') return false;

  try {
    const identity = decodeSessionIdentity(token);
    const claims = jwtDecode<GoogleLoginClaims>(token);

    if (
      !Number.isSafeInteger(Number(identity.userId)) ||
      Number(identity.userId) <= 0 ||
      identity.roles.length === 0 ||
      typeof claims.exp !== 'number' ||
      !Number.isFinite(claims.exp) ||
      claims.exp <= Date.now() / 1000
    ) {
      return false;
    }

    Cookies.set('token', token, {
      expires: 1,
      path: '/',
      sameSite: 'lax',
      secure: window.location.protocol === 'https:'
    });
    return Cookies.get('token') === token;
  } catch {
    return false;
  }
}
