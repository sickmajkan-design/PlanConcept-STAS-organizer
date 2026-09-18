import axios, {
  AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';

import { config } from '../config';
import { toApiError } from './apiError';
import {
  isAccessTokenExpired,
  sessionFromAuthResponse,
  sessionStore,
  type Session,
} from './session';
import type { AuthResponse } from './types';

const ANONYMOUS_PATHS = [
  '/api/v1/auth/login',
  '/api/v1/auth/refresh',
  '/api/v1/auth/forgot-password',
  '/api/v1/auth/reset-password',
];

const isAnonymous = (url = ''): boolean =>
  ANONYMOUS_PATHS.some((path) => url.includes(path));

/**
 * Sent on sign-in and refresh to ask the API for a cookie.
 *
 * The API answers by setting an `HttpOnly` refresh cookie and returning an
 * empty `refreshToken` in the body, so the credential never passes through
 * anything a script on this page can read. The mobile app sends no such
 * header and keeps receiving the token in the body, where its platform secure
 * storage can hold it.
 */
export const cookieAuthHeaders = { 'X-Auth-Mode': 'cookie' } as const;

/**
 * `withCredentials` on both clients: without it the browser neither stores the
 * refresh cookie nor sends it back on a cross-origin call, and the session
 * would silently end after fifteen minutes with no way to renew it.
 */
const clientOptions = {
  baseURL: config.apiBaseUrl,
  timeout: 20_000,
  withCredentials: true,
};

/**
 * Plain client used for refreshing and for replaying a request afterwards,
 * so neither can re-enter the auth interceptor.
 */
const plainClient = axios.create(clientOptions);

export const apiClient = axios.create(clientOptions);

/** Notified when the session cannot be recovered, so the app can sign out. */
let onSessionLost: (() => void) | undefined;

export function setSessionLostHandler(handler: () => void): void {
  onSessionLost = handler;
}

/**
 * Refresh is single-flight: however many requests hit a 401 at once, only one
 * refresh call is sent and all of them await its result.
 */
let refreshInFlight: Promise<Session | null> | null = null;

/**
 * Serialises refreshes across every tab of this browser, not just this one.
 *
 * All tabs share one refresh cookie, and the API treats a rotated token being
 * presented again as theft: it revokes every token the user holds, the phone
 * app's included. Two tabs refreshing at the same moment is exactly that
 * pattern — and now that each new tab restores itself from the cookie on
 * start-up, opening two at once would do it. Under the lock the second tab
 * waits, and by the time it runs the browser already holds the rotated cookie.
 */
function withRefreshLock<T>(run: () => Promise<T>): Promise<T> {
  if (!('locks' in navigator)) {
    return run();
  }

  return navigator.locks.request('construction.admin.refresh', run);
}

function postRefresh(): Promise<AuthResponse> {
  return withRefreshLock(async () => {
    // No token in the body — the browser holds it in a cookie it cannot read,
    // and sends it because of `withCredentials`.
    const { data } = await plainClient.post<AuthResponse>(
      '/api/v1/auth/refresh',
      {},
      { headers: cookieAuthHeaders },
    );

    return data;
  });
}

async function performRefresh(): Promise<Session | null> {
  const current = sessionStore.read();

  if (!current) {
    return null;
  }

  try {
    const refreshed = sessionFromAuthResponse(await postRefresh());
    sessionStore.write(refreshed);
    return refreshed;
  } catch (error) {
    const status = (error as AxiosError).response?.status;

    // The refresh token was rejected (expired, revoked, or replayed after
    // rotation) — the session is unrecoverable.
    if (status === 401 || status === 400) {
      sessionStore.clear();
      onSessionLost?.();
      return null;
    }

    // Transport problem: keep the session so the operator can retry.
    throw error;
  }
}

function refreshSession(): Promise<Session | null> {
  refreshInFlight ??= performRefresh().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

/**
 * Revives a session from the refresh cookie alone, with nothing in storage.
 *
 * This is what makes a second tab — or a reload — work now that the session is
 * scoped to the browsing session rather than the machine. The cookie is one
 * too, so it is there while the browser is open and gone once it is closed,
 * which is exactly the line the panel wants to draw. A failure here is the
 * ordinary case, not an error: it means nobody is signed in.
 */
export async function restoreSessionFromCookie(): Promise<Session | null> {
  try {
    const restored = sessionFromAuthResponse(await postRefresh());
    sessionStore.write(restored);
    return restored;
  } catch {
    return null;
  }
}

apiClient.interceptors.request.use(
  async (request: InternalAxiosRequestConfig) => {
    if (isAnonymous(request.url)) {
      return request;
    }

    let session = sessionStore.read();

    if (session && isAccessTokenExpired(session)) {
      session = await refreshSession();
    }

    if (session) {
      request.headers.set('Authorization', `Bearer ${session.accessToken}`);
    }

    return request;
  },
);

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as
      | (AxiosRequestConfig & { _retried?: boolean })
      | undefined;

    const shouldRefresh =
      error.response?.status === 401 &&
      request !== undefined &&
      !request._retried &&
      !isAnonymous(request.url) &&
      sessionStore.read() !== null;

    if (!shouldRefresh) {
      return Promise.reject(error);
    }

    let refreshed: Session | null = null;

    try {
      refreshed = await refreshSession();
    } catch {
      return Promise.reject(error);
    }

    if (!refreshed) {
      return Promise.reject(error);
    }

    // Replay once, on the plain client so this interceptor cannot recurse.
    return plainClient({
      ...request,
      _retried: true,
      headers: {
        ...request.headers,
        Authorization: `Bearer ${refreshed.accessToken}`,
      },
    } as AxiosRequestConfig);
  },
);

/** Runs a request and normalises any failure into an `ApiError`. */
export async function request<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const response = await apiClient.request<T>(config);
    return response.data;
  } catch (error) {
    throw toApiError(error);
  }
}

/** Used by the sign-in flow, which must not carry a stale bearer token. */
export async function anonymousRequest<T>(
  config: AxiosRequestConfig,
): Promise<T> {
  try {
    const response = await plainClient.request<T>(config);
    return response.data;
  } catch (error) {
    throw toApiError(error);
  }
}
