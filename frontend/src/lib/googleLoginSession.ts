import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { decodeSessionIdentity } from '@/lib/session';

interface GoogleLoginClaims {
  exp?: number;
}

export function isGoogleLoginTokenUsable(token?: string): boolean {
  if (!token) return false;

  try {
    const identity = decodeSessionIdentity(token);
    const claims = jwtDecode<GoogleLoginClaims>(token);

    return (
      Number.isSafeInteger(Number(identity.userId)) &&
      Number(identity.userId) > 0 &&
      identity.roles.length > 0 &&
      typeof claims.exp === 'number' &&
      Number.isFinite(claims.exp) &&
      claims.exp > Date.now() / 1000
    );
  } catch {
    return false;
  }
}

/** Confirms that the callback cookie contains a usable local session and persists it. */
export function persistGoogleLoginSession(
  token?: string,
  cookieStore = Cookies,
  secure = typeof window !== 'undefined' && window.location.protocol === 'https:'
): boolean {
  if (!token || !isGoogleLoginTokenUsable(token)) return false;

  try {
    cookieStore.set('token', token, {
      expires: 1,
      path: '/',
      sameSite: 'lax',
      secure
    });
    return cookieStore.get('token') === token;
  } catch {
    return false;
  }
}
