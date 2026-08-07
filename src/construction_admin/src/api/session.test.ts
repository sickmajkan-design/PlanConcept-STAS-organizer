/**
 * @vitest-environment jsdom
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import type { AuthResponse } from './types';
import {
  isAccessTokenExpired,
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
    refreshToken: 'refresh',
    refreshTokenExpiresAt: inMinutes(60 * 24 * 7),
    user: {
      id: '1',
      email: 'operator@example.test',
      role: 'Admin',
      employeeId: null,
      firstName: 'Ivan',
      lastName: 'Horvat',
      lastLoginAt: null,
    },
    ...overrides,
  };
}

beforeEach(() => {
  window.localStorage.clear();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('sessionStore', () => {
  it('reads back what it wrote', () => {
    const session = sessionWith();

    sessionStore.write(session);

    expect(sessionStore.read()).toEqual(session);
  });

  it('has no session before anybody signs in', () => {
    expect(sessionStore.read()).toBeNull();
  });

  it('forgets a session whose refresh token has expired', () => {
    sessionStore.write(sessionWith({ refreshTokenExpiresAt: inMinutes(-1) }));

    // Nothing can revive it, so keeping it would only mean the app starts up
    // believing it is signed in and discovers otherwise on the first request.
    expect(sessionStore.read()).toBeNull();
    expect(window.localStorage.length).toBe(0);
  });

  it('discards a stored value that is not a session at all', () => {
    window.localStorage.setItem('construction.admin.session', '{not json');

    expect(sessionStore.read()).toBeNull();
    expect(window.localStorage.length).toBe(0);
  });

  it('clears on request', () => {
    sessionStore.write(sessionWith());
    sessionStore.clear();

    expect(sessionStore.read()).toBeNull();
  });
});

describe('sessionFromAuthResponse', () => {
  it('keeps the tokens and the user, and nothing else', () => {
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
      refreshToken: 'r',
      refreshTokenExpiresAt: '2026-08-10T10:00:00Z',
      user: response.user,
    });
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
