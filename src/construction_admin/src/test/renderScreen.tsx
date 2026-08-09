import { ThemeProvider } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import axios, {
  AxiosError,
  AxiosHeaders,
  type AxiosAdapter,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import type { ReactElement, ReactNode } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

import type { Role, User } from '../api/types';
import { AuthContext, type AuthContextValue } from '../auth/authContextInstance';
import { I18nProvider } from '../i18n/I18nProvider';
import { theme } from '../theme';

/**
 * Renders a whole screen against a fake network.
 *
 * The suite already covers the layers underneath — the client, the guards, the
 * dictionaries. What it did not cover is a screen: a form that stops
 * validating, a field error that lands on the wrong input, a grid that renders
 * nothing. Those are silent, and they are what an operator actually touches.
 *
 * The providers are the same ones `main.tsx` wires, in the same order, because
 * a screen that only works under a different provider stack is not the screen
 * that ships.
 */

export type Call = {
  method: string;
  url: string;
  params: Record<string, unknown>;
  body: unknown;
  /** Lower-cased, so a test does not have to guess the casing axios used. */
  headers: Record<string, string>;
};

export interface FakeNetwork {
  /** Every request the screen made, in order. */
  calls: Call[];
  /** Answers one request. Called by the adapter installed below. */
  handle: AxiosAdapter;
  /**
   * Answers requests whose URL contains `match`.
   *
   * Queued answers are consumed in order, and the last one then stands for
   * every further request to that URL. Sticky rather than one-shot because a
   * screen refetches — React Query alone will fire the same list query twice
   * on mount — and a one-shot reply would have the second fetch overwrite real
   * rows with an empty default, which reads as "the API returned nothing".
   *
   * A URL with nothing registered gets 200 and an empty object, which keeps a
   * test focused on the one call it is about.
   */
  reply: (match: string, status: number, data?: unknown) => void;
}

/**
 * The network currently answering, swapped per test.
 *
 * One adapter is installed on `axios.defaults` when this module initialises,
 * and it delegates here. That indirection is the point: the alternative —
 * installing a fresh adapter per test and resetting the module cache — gives
 * the harness and the screen two copies of every module, so the screen's
 * `useI18n` looks for a context the harness never provided and throws. Kept as
 * a note because the failure reads as a missing provider rather than as two
 * module graphs.
 */
let current: FakeNetwork | null = null;

axios.defaults.adapter = ((config) => {
  if (!current) {
    return Promise.reject(new Error('No fake network installed for this test.'));
  }

  return current.handle(config);
}) as AxiosAdapter;

export function installFakeNetwork(): FakeNetwork {
  const calls: Call[] = [];
  const queues = new Map<string, { status: number; data?: unknown }[]>();

  /** The most recent answer per URL, replayed once its queue runs out. */
  const last = new Map<string, { status: number; data?: unknown }>();

  const handle: AxiosAdapter = (config) => {
    const url = config.url ?? '';

    calls.push({
      method: (config.method ?? 'get').toUpperCase(),
      url,
      params: (config.params ?? {}) as Record<string, unknown>,
      body: typeof config.data === 'string' ? JSON.parse(config.data) : config.data,
      headers: Object.fromEntries(
        Object.entries(config.headers ?? {})
          .filter(([, value]) => typeof value === 'string' || typeof value === 'number')
          .map(([name, value]) => [name.toLowerCase(), String(value)]),
      ),
    });

    // Longest match first, so a queue for '/employees/123' is not consumed by
    // a rule registered for '/employees'.
    const key = [...queues.keys()]
      .filter((candidate) => url.includes(candidate))
      .sort((a, b) => b.length - a.length)[0];

    const queued = key ? queues.get(key)! : undefined;
    const next = queued?.length ? queued.shift() : key ? last.get(key) : undefined;

    if (key && next) {
      last.set(key, next);
    }

    const status = next?.status ?? 200;

    const response: AxiosResponse = {
      data: next?.data ?? {},
      status,
      statusText: status >= 400 ? 'Error' : 'OK',
      headers: new AxiosHeaders(),
      config: config as InternalAxiosRequestConfig,
    };

    // A real AxiosError, not a plain Error wearing `isAxiosError`. The app's
    // `toApiError` narrows with `instanceof AxiosError`, so a lookalike falls
    // through to the generic branch and loses the problem-details body — which
    // would make every error-path screen test quietly assert the wrong thing.
    return status >= 400
      ? Promise.reject(
          new AxiosError(
            `Request failed with status code ${status}`,
            String(status),
            config as InternalAxiosRequestConfig,
            null,
            response,
          ),
        )
      : Promise.resolve(response);
  };

  const network: FakeNetwork = {
    calls,
    handle,
    reply(match, status, data) {
      const queue = queues.get(match) ?? [];
      queue.push({ status, data });
      queues.set(match, queue);
    },
  };

  current = network;
  return network;
}

export function signedIn(role: Role = 'Admin'): User {
  return {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    email: 'operator@example.test',
    role,
    employeeId: null,
    firstName: 'Ope',
    lastName: 'Rator',
    lastLoginAt: null,
  };
}

export interface ScreenOptions {
  /** The URL the router starts at — how a screen learns its `:id`. */
  route?: string;
  /** The route pattern the screen is mounted on. */
  path?: string;
  user?: User | null;
  /** Rendered for any other path, so a navigation away is observable. */
  elsewhere?: ReactNode;
}

/**
 * A fresh QueryClient per render.
 *
 * Retries off: a screen under test that hits a 409 should show the error, not
 * pause three times first. And a shared cache would leak one test's rows into
 * the next, which is the kind of failure that only appears when the whole file
 * runs.
 */
function newQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  });
}

export function renderScreen(ui: ReactElement, options: ScreenOptions = {}) {
  const {
    route = '/',
    path = '*',
    user = signedIn(),
    elsewhere = <div data-testid="elsewhere" />,
  } = options;

  const auth: AuthContextValue = {
    user,
    isAuthenticated: !!user,
    signIn: async () => {},
    signOut: async () => {},
    refreshProfile: async () => {},
  };

  return render(
    <I18nProvider>
      <ThemeProvider theme={theme}>
        <QueryClientProvider client={newQueryClient()}>
          <MemoryRouter initialEntries={[route]}>
            <AuthContext.Provider value={auth}>
              <Routes>
                <Route path={path} element={ui} />
                {path !== '*' && <Route path="*" element={elsewhere} />}
              </Routes>
            </AuthContext.Provider>
          </MemoryRouter>
        </QueryClientProvider>
      </ThemeProvider>
    </I18nProvider>,
  );
}
