import type { AuthResponse, User } from './types';

export interface Session {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: User;
}

const STORAGE_KEY = 'construction.admin.session';

/**
 * Session persistence for the admin SPA.
 *
 * The tokens live in localStorage so a page reload keeps the operator signed
 * in. That is the usual trade-off for a browser SPA: it survives reloads but
 * is readable by any script running on the origin, so the access token is
 * deliberately short-lived (15 minutes) and rotated on every refresh.
 */
export const sessionStore = {
  read(): Session | null {
    const raw = window.localStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return null;
    }

    try {
      const session = JSON.parse(raw) as Session;

      // A session whose refresh token has expired can no longer be revived.
      if (new Date(session.refreshTokenExpiresAt) <= new Date()) {
        sessionStore.clear();
        return null;
      }

      return session;
    } catch {
      sessionStore.clear();
      return null;
    }
  },

  write(session: Session): void {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  },

  clear(): void {
    window.localStorage.removeItem(STORAGE_KEY);
  },
};

export function sessionFromAuthResponse(response: AuthResponse): Session {
  return {
    accessToken: response.accessToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    refreshToken: response.refreshToken,
    refreshTokenExpiresAt: response.refreshTokenExpiresAt,
    user: response.user,
  };
}

/**
 * Treats the access token as expired slightly early, so a request is never
 * sent with a token that dies in flight.
 */
export function isAccessTokenExpired(session: Session): boolean {
  const expiresAt = new Date(session.accessTokenExpiresAt).getTime();
  return Date.now() >= expiresAt - 30_000;
}
