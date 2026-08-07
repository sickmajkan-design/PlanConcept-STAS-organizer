import type { AuthResponse, User } from './types';

/**
 * What the browser keeps.
 *
 * No refresh token. It lives in an `HttpOnly` cookie the API sets, which no
 * script on this page can read — which is the point: a seven-day credential in
 * `localStorage` turns one XSS into a persistent account takeover rather than
 * a session-length one. `refreshTokenExpiresAt` stays because the app needs to
 * know when the session is beyond reviving, and a date is not a credential.
 */
export interface Session {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: User;
}

const STORAGE_KEY = 'construction.admin.session';

/**
 * Session persistence for the admin SPA.
 *
 * The access token lives in localStorage so a page reload keeps the operator
 * signed in without a round trip. It is readable by any script on the origin,
 * which is accepted: it lasts fifteen minutes and is rotated on every refresh.
 * The refresh token is not here at all — it is an `HttpOnly` cookie, out of
 * reach of script entirely.
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

/**
 * Keeps what the browser is allowed to keep.
 *
 * `response.refreshToken` is deliberately dropped even though the field still
 * exists on the type — the API sends it empty in cookie mode, and copying it
 * anyway would put a credential back in localStorage the moment somebody
 * changed a header.
 */
export function sessionFromAuthResponse(response: AuthResponse): Session {
  return {
    accessToken: response.accessToken,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
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
