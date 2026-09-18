import type { AuthResponse, User } from './types';

/**
 * What the browser keeps.
 *
 * No refresh token. It lives in an `HttpOnly` cookie the API sets, which no
 * script on this page can read — which is the point: a seven-day credential in
 * storage turns one XSS into a persistent account takeover rather than a
 * session-length one. `refreshTokenExpiresAt` stays because the app needs to
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
 * `sessionStorage`, not `localStorage`: the panel is used by management,
 * administrators and superadmins, often on a shared office machine, and a
 * session in `localStorage` outlives the browser — whoever opened it next
 * landed straight in the previous person's account, with their role. Scoped to
 * the browsing session, closing the browser ends it. A new tab starts empty and
 * revives itself from the refresh cookie instead, so this costs nothing while
 * the browser stays open.
 */
export const sessionStore = {
  read(): Session | null {
    const raw = window.sessionStorage.getItem(STORAGE_KEY);

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
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  },

  clear(): void {
    window.sessionStorage.removeItem(STORAGE_KEY);
  },
};

/**
 * Removes the session an earlier version of the panel left in `localStorage`.
 *
 * Nothing reads that key any more, so left alone it would sit there holding a
 * name, an email and a role until the operator cleared their browser.
 */
export function purgeLegacySession(): void {
  window.localStorage.removeItem(STORAGE_KEY);
}

const LAST_ACTIVE_KEY = 'construction.admin.last-active';

/** How long the panel may sit untouched before it signs itself out. */
export const IDLE_LIMIT_MS = 30 * 60_000;

/**
 * When somebody last actually touched the panel, in any tab.
 *
 * In `localStorage` so every tab shares it: a per-tab clock would let a
 * background tab time out and sign out the person working in the one next to
 * it. It is a timestamp, not a credential, so outliving the browser is
 * harmless — and useful, because a browser that restores its tabs on start-up
 * also restores its session cookies, and a stale clock is what catches that.
 *
 * Only real interaction moves it. Background polling and token refreshes
 * deliberately do not — several screens poll every fifteen seconds, and a clock
 * fed by network activity would keep an empty desk signed in forever.
 */
export const activityClock = {
  /** Restarts the countdown unconditionally. For signing in. */
  start(): void {
    window.localStorage.setItem(LAST_ACTIVE_KEY, String(Date.now()));
  },

  /**
   * Records interaction.
   *
   * Not once the limit has passed: a click in the seconds before the idle
   * check notices would otherwise revive a session that has already ended.
   */
  touch(): void {
    if (!activityClock.isExpired()) {
      activityClock.start();
    }
  },

  /**
   * True once nobody has touched the panel for the idle limit.
   *
   * Also true when there is no record at all. That is the case on a machine
   * that last used a version which kept its refresh cookie for a week: the
   * cookie may still be in the jar, and treating the missing clock as expired
   * is what gets it revoked rather than quietly signed back in with.
   */
  isExpired(): boolean {
    const last = Number(window.localStorage.getItem(LAST_ACTIVE_KEY));
    return !last || Date.now() - last >= IDLE_LIMIT_MS;
  },
};

/**
 * Keeps what the browser is allowed to keep.
 *
 * `response.refreshToken` is deliberately dropped even though the field still
 * exists on the type — the API sends it empty in cookie mode, and copying it
 * anyway would put a credential back in storage the moment somebody changed a
 * header.
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
