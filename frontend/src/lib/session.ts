import { jwtDecode } from 'jwt-decode';
import { validRoles, type UserRole } from '@/components/Sidebar/menuConfig';

export interface SessionIdentity {
  userId: string;
  name: string;
  roles: UserRole[];
  isTestUser: boolean;
  isDefaultPassword: boolean;
}

interface SessionClaims {
  userId: string;
  unique_name?: string;
  role?: string | string[];
  isTestUser?: boolean;
  isDefaultPassword?: boolean;
}

export function decodeSessionIdentity(token: string): SessionIdentity {
  const claims = jwtDecode<SessionClaims>(token);
  const roles = Array.isArray(claims.role)
    ? claims.role
    : claims.role
      ? [claims.role]
      : [];
  return {
    userId: claims.userId,
    name: claims.unique_name ?? '',
    roles: roles.filter((role): role is UserRole =>
      validRoles.includes(role as UserRole)
    ),
    isTestUser: claims.isTestUser === true,
    isDefaultPassword: claims.isDefaultPassword === true
  };
}
