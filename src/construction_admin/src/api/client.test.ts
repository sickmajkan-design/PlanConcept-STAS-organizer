/**
 * @vitest-environment jsdom
 */
import axios, {
  AxiosError,
  AxiosHeaders,
  type AxiosAdapter,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { sessionStore, type Session } from './session';
import type { AuthResponse } from './types';

/**
 * The session machinery, driven through a fake network.
 *
 * This is the piece with the most ways to go quietly wrong. A refresh that is
 * not single-flight fires one per in-flight request and, because the API
 * rotates refresh tokens and treats a replayed one as theft, the second call
 * revokes every session the account has — the operator is thrown out mid-task
 * and nothing in the UI explains why. None of that shows up in a type check.
 *
 * The adapter is installed on `axios.defaults` *before* `./client` is imported,
 * because `axios.create` copies the defaults at creation time. That is also why
 * every test here imports the module fresh: the interceptors and the in-flight
 * refresh promise are module state.
 */
type Call = { method: string; url: string; headers: Record<string, string>; body: unknown };

interface FakeNetwork {
  calls: Call[];
  /** Replies to the next request matching `url`, in the order registered. */
  reply: (url: string, status: number, data?: unknown) => void;
}

function installFakeNetwork(): FakeNetwork {
  const calls: Call[] = [];
  const queues = new Map<string, { status: number; data?: unknown }[]>();

  const adapter: AxiosAdapter = (config) => {
    const url = config.url ?? '';

    calls.push({
      method: (config.method ?? 'get').toUpperCase(),
      url,
      headers: Object.fromEntries(
        Object.entries(AxiosHeaders.from(config.headers).toJSON()).map(
          ([key, value]) => [key.toLowerCase(), String(value)],
        ),
      ),
      body: typeof config.data === 'string' ? JSON.parse(config.data) : config.data,
    });

    const key = [...queues.keys()].find((candidate) => url.includes(candidate));
    const next = key ? queues.get(key)!.shift() : undefined;
    const status = next?.status ?? 200;

    const response: AxiosResponse = {
      status,
      statusText: '',
      headers: {},
      config: config as InternalAxiosRequestConfig,
      data: next?.data ?? {},
    };

    return status >= 200 && status < 300
      ? Promise.resolve(response)
      : Promise.reject(
          new AxiosError(
            `Request failed with status code ${status}`,
            String(status),
            config as InternalAxiosRequestConfig,
            {},
            response,
          ),
        );
  };

  axios.defaults.adapter = adapter;

  return {
    calls,
    reply(url, status, data) {
      const queue = queues.get(url) ?? [];
      queue.push({ status, data });
      queues.set(url, queue);
    },
  };
}

const inMinutes = (minutes: number) =>
  new Date(Date.now() + minutes * 60_000).toISOString();

const user = {
  id: '1',
  email: 'operator@example.test',
  role: 'Admin' as const,
  employeeId: null,
  firstName: null,
  lastName: null,
  lastLoginAt: null,
};

function storedSession(overrides: Partial<Session> = {}): Session {
  const session: Session = {
    accessToken: 'old-access',
    accessTokenExpiresAt: inMinutes(15),
    refreshTokenExpiresAt: inMinutes(60 * 24 * 7),
    user,
    ...overrides,
  };

  sessionStore.write(session);

  return session;
}

const freshTokens: AuthResponse = {
  accessToken: 'new-access',
  accessTokenExpiresAt: inMinutes(15),
  refreshToken: 'new-refresh',
  refreshTokenExpiresAt: inMinutes(60 * 24 * 7),
  user,
};

/** A fresh copy of the module, so its interceptors and in-flight state reset. */
async function loadClient() {
  vi.resetModules();
  return import('./client');
}

let network: FakeNetwork;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

describe('the bearer token', () => {
  it('is attached to an ordinary request', async () => {
    storedSession();

    const { request } = await loadClient();

    await request({ method: 'GET', url: '/api/v1/employees' });

    expect(network.calls[0]?.headers.authorization).toBe('Bearer old-access');
  });

  it('is left off the anonymous endpoints', async () => {
    // A stale token on the login call would be sent to an endpoint that must
    // not see one, and on refresh it would be the wrong credential entirely.
    storedSession();

    const { request } = await loadClient();

    await request({ method: 'POST', url: '/api/v1/auth/login', data: {} });

    expect(network.calls[0]?.headers.authorization).toBeUndefined();
  });

  it('is left off when nobody is signed in', async () => {
    const { request } = await loadClient();

    await request({ method: 'GET', url: '/api/v1/employees' });

    expect(network.calls[0]?.headers.authorization).toBeUndefined();
  });
});

describe('refreshing before the token dies', () => {
  it('renews an expired token and sends the new one', async () => {
    storedSession({ accessToken: 'old-access', accessTokenExpiresAt: inMinutes(-1) });
    network.reply('/api/v1/auth/refresh', 200, freshTokens);

    const { request } = await loadClient();

    await request({ method: 'GET', url: '/api/v1/employees' });

    expect(network.calls.map((call) => call.url)).toEqual([
      '/api/v1/auth/refresh',
      '/api/v1/employees',
    ]);
    expect(network.calls[1]?.headers.authorization).toBe('Bearer new-access');
  });

  it('sends no refresh token in the body and asks for a cookie', async () => {
    // The browser holds its token in an HttpOnly cookie it cannot read. If
    // this call carried one in the body, the token would be in reach of
    // script again and the cookie would be decoration.
    storedSession({ accessTokenExpiresAt: inMinutes(-1) });
    network.reply('/api/v1/auth/refresh', 200, freshTokens);

    const { request } = await loadClient();

    await request({ method: 'GET', url: '/api/v1/employees' });

    const refresh = network.calls.find((call) => call.url.includes('/api/v1/auth/refresh'))!;

    expect(refresh.body).toEqual({});
    expect(refresh.headers['x-auth-mode']).toBe('cookie');
  });

  it('never writes a refresh token to storage, whatever the API returns', async () => {
    storedSession({ accessTokenExpiresAt: inMinutes(-1) });
    network.reply('/api/v1/auth/refresh', 200, {
      ...freshTokens,
      refreshToken: 'a-real-refresh-token',
    });

    const { request } = await loadClient();

    await request({ method: 'GET', url: '/api/v1/employees' });

    const raw = window.localStorage.getItem('construction.admin.session') ?? '';

    expect(raw).not.toContain('a-real-refresh-token');

    // The access token is still rotated and stored — that part has to keep
    // working, or the session ends every fifteen minutes.
    expect(sessionStore.read()?.accessToken).toBe('new-access');
  });

  it('refreshes once for however many requests are waiting', async () => {
    // The whole point of the single-flight promise. The API rotates the
    // refresh token and treats a replayed one as theft, revoking every session
    // for the account — so a second refresh does not merely waste a call, it
    // signs the operator out.
    storedSession({ accessTokenExpiresAt: inMinutes(-1) });
    network.reply('/api/v1/auth/refresh', 200, freshTokens);

    const { request } = await loadClient();

    await Promise.all([
      request({ method: 'GET', url: '/api/v1/employees' }),
      request({ method: 'GET', url: '/api/v1/projects' }),
      request({ method: 'GET', url: '/api/v1/vehicles' }),
    ]);

    const refreshes = network.calls.filter((call) =>
      call.url.includes('/api/v1/auth/refresh'),
    );

    expect(refreshes).toHaveLength(1);
  });
});

describe('refreshing after a 401', () => {
  it('renews and replays the request once', async () => {
    storedSession();
    network.reply('/api/v1/employees', 401);
    network.reply('/api/v1/auth/refresh', 200, freshTokens);
    network.reply('/api/v1/employees', 200, { items: [] });

    const { request } = await loadClient();

    const result = await request({ method: 'GET', url: '/api/v1/employees' });

    expect(result).toEqual({ items: [] });
    expect(network.calls.map((call) => call.url)).toEqual([
      '/api/v1/employees',
      '/api/v1/auth/refresh',
      '/api/v1/employees',
    ]);
    expect(network.calls[2]?.headers.authorization).toBe('Bearer new-access');
  });

  it('gives up rather than looping when the replay fails too', async () => {
    // 401 → refresh → 401 must end there and not go round again, which in a
    // browser would look like a hung page rather than an error. Two things
    // stop it: the replay goes out on the interceptor-free client, and the
    // config is flagged as retried. The first is what actually holds today —
    // removing the flag alone does not reintroduce the loop — so this asserts
    // the outcome rather than either mechanism.
    storedSession();
    network.reply('/api/v1/employees', 401);
    network.reply('/api/v1/auth/refresh', 200, freshTokens);
    network.reply('/api/v1/employees', 401);

    const { request } = await loadClient();

    await expect(request({ method: 'GET', url: '/api/v1/employees' })).rejects.toThrow();

    expect(
      network.calls.filter((call) => call.url.includes('/api/v1/auth/refresh')),
    ).toHaveLength(1);
  });
});

describe('when the session cannot be recovered', () => {
  it('clears it and says so, on a rejected refresh token', async () => {
    storedSession({ accessTokenExpiresAt: inMinutes(-1) });
    network.reply('/api/v1/auth/refresh', 401);

    const { request, setSessionLostHandler } = await loadClient();

    const lost = vi.fn();
    setSessionLostHandler(lost);

    await request({ method: 'GET', url: '/api/v1/employees' });

    expect(sessionStore.read()).toBeNull();
    expect(lost).toHaveBeenCalledOnce();
  });

  it('keeps the session when the refresh only failed to reach the server', async () => {
    // A rejected token is unrecoverable; a network blip is not, and signing
    // somebody out over a dropped connection loses whatever they were typing.
    storedSession({ accessTokenExpiresAt: inMinutes(-1) });
    network.reply('/api/v1/auth/refresh', 503);

    const { request, setSessionLostHandler } = await loadClient();

    const lost = vi.fn();
    setSessionLostHandler(lost);

    await expect(request({ method: 'GET', url: '/api/v1/employees' })).rejects.toThrow();

    expect(sessionStore.read()).not.toBeNull();
    expect(lost).not.toHaveBeenCalled();
  });

  it('does not try to refresh a 401 on the login call itself', async () => {
    // Wrong credentials are a 401 that no refresh can fix, and attempting one
    // would replace "that password is wrong" with a spurious sign-out.
    network.reply('/api/v1/auth/login', 401);

    const { request } = await loadClient();

    await expect(
      request({ method: 'POST', url: '/api/v1/auth/login', data: {} }),
    ).rejects.toThrow();

    expect(network.calls.map((call) => call.url)).toEqual(['/api/v1/auth/login']);
  });
});

describe('errors', () => {
  it('come back as an ApiError, whatever axios threw', async () => {
    storedSession();
    network.reply('/api/v1/employees', 409, { detail: 'Stock would go negative.' });

    const { request } = await loadClient();
    const { ApiError } = await import('./apiError');

    await expect(
      request({ method: 'GET', url: '/api/v1/employees' }),
    ).rejects.toBeInstanceOf(ApiError);
  });
});
