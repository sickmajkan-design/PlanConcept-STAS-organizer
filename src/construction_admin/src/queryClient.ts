import { QueryClient } from '@tanstack/react-query';

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

export const queryClient = new QueryClient({
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
