import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryKey,
} from '@tanstack/react-query';
import { useRef } from 'react';

import { newIdempotencyKey } from '../api/idempotency';

/**
 * The cache keys for one resource collection.
 *
 * Every feature used to declare this same triple by hand. Building it from the
 * resource name keeps the shape identical across features, which matters
 * because invalidating `all` relies on it being a strict prefix of both `list`
 * and `detail`.
 */
export interface ResourceKeys<TQuery> {
  readonly all: QueryKey;
  readonly list: (query: TQuery) => QueryKey;
  readonly detail: (id: string) => QueryKey;
}

export function createResourceKeys<TQuery>(
  resource: string,
): ResourceKeys<TQuery> {
  const all = [resource] as const;

  return {
    all,
    list: (query: TQuery) => [...all, 'list', query],
    detail: (id: string) => [...all, 'detail', id],
  };
}

/**
 * One page of a list endpoint. `keepPreviousData` holds the current rows on
 * screen while the next page loads, so paging and filtering do not flash an
 * empty grid.
 */
export function useResourceList<TResult, TQuery>(
  keys: ResourceKeys<TQuery>,
  fetchPage: (query: TQuery) => Promise<TResult>,
  query: TQuery,
) {
  return useQuery({
    queryKey: keys.list(query),
    queryFn: () => fetchPage(query),
    placeholderData: keepPreviousData,
  });
}

/**
 * A single record. Disabled until an id is known, so a detail route can render
 * before its parameter has resolved without firing a request for `undefined`.
 */
export function useResourceDetail<TResult, TQuery>(
  keys: ResourceKeys<TQuery>,
  fetchOne: (id: string) => Promise<TResult>,
  id: string | undefined,
) {
  return useQuery({
    queryKey: keys.detail(id ?? ''),
    queryFn: () => fetchOne(id!),
    enabled: !!id,
  });
}

/**
 * A write that refreshes the caches it affects.
 *
 * `invalidate` is explicit rather than inferred because the correct set is a
 * decision, not a default: most writes invalidate their own collection, but
 * assigning an employee to a project also changes what the projects endpoint
 * returns. Stating it at each call site is what stops a screen quietly showing
 * stale data after a successful write.
 *
 * The second argument handed to `mutationFn` is an idempotency key, and its
 * lifetime is the point of it. It stays the same for every attempt at one
 * action and only changes once one of them succeeds — so an operator who
 * presses "Save" again after a failure is retrying, not asking for a second
 * stock movement, and the API can tell the difference. A key minted per call
 * would look like two different actions and would protect nothing, which is
 * the mistake this exists to avoid; see `Idempotent` on the API side.
 *
 * Writes whose endpoint is not marked idempotent send the header anyway. It
 * costs nothing — the server ignores what it does not read — and the
 * alternative is a list of which hooks may pass it, kept in sync by hand.
 */
export function useResourceMutation<TVariables, TResult>(
  mutationFn: (variables: TVariables, idempotencyKey: string) => Promise<TResult>,
  invalidate: readonly QueryKey[],
) {
  const queryClient = useQueryClient();
  const key = useRef(newIdempotencyKey());

  return useMutation({
    mutationFn: (variables: TVariables) => mutationFn(variables, key.current),
    onSuccess: () => {
      // Rotated only here. After a failure the same key is offered again, so
      // a retry of a request that did land — and whose answer was lost on the
      // way back — is answered rather than carried out a second time.
      key.current = newIdempotencyKey();

      for (const queryKey of invalidate) {
        void queryClient.invalidateQueries({ queryKey });
      }
    },
  });
}
