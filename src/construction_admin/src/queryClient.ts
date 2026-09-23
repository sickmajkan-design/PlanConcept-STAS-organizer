import { MutationCache, QueryClient } from '@tanstack/react-query';

import { ApiError } from './api/apiError';

/**
 * Retries transport failures a couple of times but never retries a request
 * the server has actively rejected (4xx) — retrying a 403 or 404 just wastes
 * time and delays the error the user needs to see.
 */
function shouldRetry(failureCount: number, error: unknown): boolean {
  if (failureCount >= 2) return false;
  if (error instanceof ApiError && error.status !== undefined) return false;
  return true;
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
      staleTime: 15_000,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
});
