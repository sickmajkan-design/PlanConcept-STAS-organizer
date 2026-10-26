import { MutationCache, QueryClient } from '@tanstack/react-query';

import { ApiError } from './api/apiError';

/**
 * Retries transport failures a couple of times but never retries a request
 * the server has actively rejected (4xx) — retrying a 403 or 404 just wastes
 * time and delays the error the user needs to see.
 */
function shouldRetry(failureCount: number, error: unknown): boolean {
  // "Too many requests" is the one rejection that passes by itself: a board that fires a few dozen calls at once
  // can touch the per-minute allowance, and asking again a little later works. It is waited out, not shown as an error.
  if (error instanceof ApiError && error.status === 429) return failureCount < RATE_LIMIT_DELAYS_MS.length;
  if (failureCount >= 2) return false;
  if (error instanceof ApiError && error.status !== undefined) return false;
  return true;
}

/** How long to wait before each new try after a 429, with a little spread so the widgets do not all return at once. */
const RATE_LIMIT_DELAYS_MS = [4_000, 10_000, 20_000, 30_000];

function retryDelay(failureCount: number, error: unknown): number {
  if (error instanceof ApiError && error.status === 429) {
    const base = RATE_LIMIT_DELAYS_MS[Math.min(failureCount, RATE_LIMIT_DELAYS_MS.length - 1)];
    return base + Math.random() * 3_000;
  }
  return Math.min(1000 * 2 ** failureCount, 30_000);
}

export const queryClient: QueryClient = new QueryClient({
  // Any successful change — an approval, a deletion, a notification read —
  // can alter what the sidebar badges, the notification bell and the open
  // page should show. Marking everything stale refetches only what is on
  // screen, so the platform follows an action live without each mutation
  // having to list every place its effect shows up.
  mutationCache: new MutationCache({
    onSuccess: () => {
      void queryClient.invalidateQueries();
    },
  }),
  defaultOptions: {
    queries: {
      retry: shouldRetry,
      retryDelay,
      staleTime: 15_000,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
});
