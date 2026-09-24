/**
 * @vitest-environment jsdom
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import type { AuthResponse } from './types';
import {
  activityClock,
  IDLE_LIMIT_MS,
  isAccessTokenExpired,
  purgeLegacySession,
  sessionFromAuthResponse,
  sessionStore,
  type Session,
} from './session';

const inMinutes = (minutes: number) =>
  new Date(Date.now() + minutes * 60_000).toISOString();

function sessionWith(overrides: Partial<Session> = {}): Session {
  return {
    accessToken: 'access',
    accessTokenExpiresAt: inMinutes(15),
    refreshTokenExpiresAt: inMinutes(60 * 24 * 7),
    user: {
      id: '1',
      email: 'operator@example.test',
      role: 'Admin',
      employeeId: null,
      firstName: 'Ivan',
      lastName: 'Horvat',
      lastLoginAt: null,
      canViewCustomerTaxDetails: false,
      financeAccess: 'None',
    },
    ...overrides,
  };
}

beforeEach(() => {
  window.localStorage.clear();
  window.sessionStorage.clear();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('sessionStore', () => {
  it('reads back what it wrote', () => {
    const session = sessionWith();

    sessionStore.write(session);

    // Stamped with the version that wrote it.
    expect(sessionStore.read()).toEqual({ ...session, version: 2 });
  });

  it('drops a session stored by an earlier build, so the refresh brings the account as it is now', () => {
    // Written straight to storage, without the stamp an earlier build did not have.
    window.sessionStorage.setItem('construction.admin.session', JSON.stringify(sessionWith()));

    expect(sessionStore.read()).toBeNull();
  });

  it('does not drop a session again after it was written by this build, even if the account lacks a field', () => {
    const session = sessionWith();
    delete (session.user as unknown as Record<string, unknown>).financeAccess;

    sessionStore.write(session);

    expect(sessionStore.read()).not.toBeNull();
  });

  it('keeps the session where closing the browser removes it', () => {
    // The whole point: in localStorage it outlived the browser, and whoever
    // opened the panel next on a shared machine was signed in as the last
    // person to use it.
    sessionStore.write(sessionWith());

    expect(window.sessionStorage.getItem('construction.admin.session')).not.toBeNull();
    expect(window.localStorage.getItem('construction.admin.session')).toBeNull();
  });

  it('has no session before anybody signs in', () => {
    expect(sessionStore.read()).toBeNull();
  });

  it('forgets a session whose refresh token has expired', () => {
    sessionStore.write(sessionWith({ refreshTokenExpiresAt: inMinutes(-1) }));

    // Nothing can revive it, so keeping it would only mean the app starts up
    // believing it is signed in and discovers otherwise on the first request.
    expect(sessionStore.read()).toBeNull();
    expect(window.sessionStorage.length).toBe(0);
  });

  it('discards a stored value that is not a session at all', () => {
    window.sessionStorage.setItem('construction.admin.session', '{not json');

    expect(sessionStore.read()).toBeNull();
    expect(window.sessionStorage.length).toBe(0);
  });

  it('clears on request', () => {
    sessionStore.write(sessionWith());
    sessionStore.clear();

    expect(sessionStore.read()).toBeNull();
  });
});

describe('purgeLegacySession', () => {
  it('removes the session an older version left in localStorage', () => {
    window.localStorage.setItem('construction.admin.session', JSON.stringify(sessionWith()));

    purgeLegacySession();

    expect(window.localStorage.getItem('construction.admin.session')).toBeNull();
  });
});

describe('activityClock', () => {
  it('counts a machine that has never recorded activity as expired', () => {
    // A browser last used by the version with a week-long cookie may still
    // hold one. Expired is what gets that cookie revoked instead of revived.
    expect(activityClock.isExpired()).toBe(true);
  });

  it('is not expired right after signing in', () => {
    activityClock.start();

    expect(activityClock.isExpired()).toBe(false);
  });

  it('expires once the panel has been left alone for the idle limit', () => {
    vi.useFakeTimers();
    activityClock.start();

    vi.advanceTimersByTime(IDLE_LIMIT_MS - 1_000);
    expect(activityClock.isExpired()).toBe(false);

    vi.advanceTimersByTime(1_000);
    expect(activityClock.isExpired()).toBe(true);
  });

  it('restarts the countdown on interaction', () => {
    vi.useFakeTimers();
    activityClock.start();

    vi.advanceTimersByTime(IDLE_LIMIT_MS - 60_000);
    activityClock.touch();
    vi.advanceTimersByTime(IDLE_LIMIT_MS - 60_000);

    expect(activityClock.isExpired()).toBe(false);
  });

  it('does not let a late click revive a session that already timed out', () => {
    vi.useFakeTimers();
    activityClock.start();

    vi.advanceTimersByTime(IDLE_LIMIT_MS);
    activityClock.touch();

    expect(activityClock.isExpired()).toBe(true);
  });

  it('is shared by every tab, so a background tab cannot time out the one in use', () => {
    // localStorage, unlike the session itself: one clock for the browser.
    activityClock.start();

    expect(window.localStorage.getItem('construction.admin.last-active')).not.toBeNull();
  });
});

describe('sessionFromAuthResponse', () => {
  it('keeps the access token and the user, and nothing else', () => {
    const response = {
      accessToken: 'a',
      accessTokenExpiresAt: '2026-08-03T10:15:00Z',
      refreshToken: 'r',
      refreshTokenExpiresAt: '2026-08-10T10:00:00Z',
      user: sessionWith().user,
      // The API may grow fields; they have no business in localStorage.
      somethingNew: 'ignored',
    } as AuthResponse & { somethingNew: string };

    expect(sessionFromAuthResponse(response)).toEqual({
      accessToken: 'a',
      accessTokenExpiresAt: '2026-08-03T10:15:00Z',
      refreshTokenExpiresAt: '2026-08-10T10:00:00Z',
      user: response.user,
    });
  });

  it('never stores a refresh token, even when the API sends one', () => {
    // The assertion the whole change exists for. The API returns it empty in
    // cookie mode, but a response that carried one — a changed header, an
    // older API, a proxy that rewrote the request — must not put a seven-day
    // credential somewhere any script on the page can read it.
    const stored = sessionFromAuthResponse({
      accessToken: 'a',
      accessTokenExpiresAt: '2026-08-03T10:15:00Z',
      refreshToken: 'a-real-refresh-token',
      refreshTokenExpiresAt: '2026-08-10T10:00:00Z',
      user: sessionWith().user,
    });

    expect(JSON.stringify(stored)).not.toContain('a-real-refresh-token');
  });

  it('leaves nothing in storage that looks like a refresh credential', () => {
    sessionStore.write(
      sessionFromAuthResponse({
        accessToken: 'a',
        accessTokenExpiresAt: '2026-08-03T10:15:00Z',
        refreshToken: 'a-real-refresh-token',
        refreshTokenExpiresAt: '2026-08-10T10:00:00Z',
        user: sessionWith().user,
      }),
    );

    const raw = window.sessionStorage.getItem('construction.admin.session') ?? '';

    expect(raw).not.toContain('a-real-refresh-token');
  });
});

describe('isAccessTokenExpired', () => {
  it('is false for a token with plenty of life left', () => {
    expect(isAccessTokenExpired(sessionWith({ accessTokenExpiresAt: inMinutes(5) })))
      .toBe(false);
  });

  it('is true once it has actually expired', () => {
    expect(isAccessTokenExpired(sessionWith({ accessTokenExpiresAt: inMinutes(-1) })))
      .toBe(true);
  });

  it('is true in the last half-minute, before the token dies in flight', () => {
    // The skew that stops a request being sent with a token that expires
    // between leaving the browser and reaching the API.
    const twentySeconds = new Date(Date.now() + 20_000).toISOString();

    expect(isAccessTokenExpired(sessionWith({ accessTokenExpiresAt: twentySeconds })))
      .toBe(true);

    const fortySeconds = new Date(Date.now() + 40_000).toISOString();

    expect(isAccessTokenExpired(sessionWith({ accessTokenExpiresAt: fortySeconds })))
      .toBe(false);
  });
});
