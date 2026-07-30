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
  '/api/auth/login',
  '/api/auth/refresh',
  '/api/auth/forgot-password',
  '/api/auth/reset-password',
];

const isAnonymous = (url = ''): boolean =>
  ANONYMOUS_PATHS.some((path) => url.includes(path));

/**
 * Plain client used for refreshing and for replaying a request afterwards,
 * so neither can re-enter the auth interceptor.
 */
const plainClient = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 20_000,
});

export const apiClient = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 20_000,
});

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

async function performRefresh(): Promise<Session | null> {
  const current = sessionStore.read();

  if (!current) {
    return null;
  }

  try {
    const { data } = await plainClient.post<AuthResponse>('/api/auth/refresh', {
      refreshToken: current.refreshToken,
    });

    const refreshed = sessionFromAuthResponse(data);
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
