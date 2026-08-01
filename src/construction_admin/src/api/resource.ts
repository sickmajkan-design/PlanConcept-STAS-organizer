import { request } from './client';
import type { ListQuery, PagedList } from './types';

/**
 * Drops the query parameters the API should not receive.
 *
 * Every list endpoint treats an absent parameter as "no filter" and applies
 * its own default, so sending an empty or unset value would filter on a blank
 * instead. Dropped: `undefined`, `null`, `''` and `false` — the last because
 * the boolean filters (`unassigned`, `unassignedOnly`, `sortDescending`) are
 * switches that only mean something when on.
 *
 * `0` is deliberately kept: `maxQuantity: 0` means "out of stock", which is a
 * real filter and not the absence of one.
 */
export function listParams(params: object): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(params).filter(
      ([, value]) =>
        value !== undefined && value !== null && value !== '' && value !== false,
    ),
  );
}

/**
 * The five CRUD calls every resource collection exposes, built from its base
 * path. Each resource module wraps this and adds the endpoints specific to it
 * (assignment, stock adjustment, QR lookup), so the shared shape stays in one
 * place while the differences stay visible in the module that owns them.
 *
 * The type parameters are all distinct because the shapes genuinely differ: a
 * list returns summaries, a detail read may return more (`EmployeeDetail`), a
 * write takes an input model, and each resource accepts its own filters.
 */
export function createCrudApi<
  TListItem,
  TDetail,
  TInput,
  TQuery extends ListQuery,
>(basePath: string) {
  return {
    list: (query: TQuery) =>
      request<PagedList<TListItem>>({
        method: 'GET',
        url: basePath,
        params: listParams(query),
      }),

    get: (id: string) =>
      request<TDetail>({ method: 'GET', url: `${basePath}/${id}` }),

    create: (input: TInput) =>
      request<TListItem>({ method: 'POST', url: basePath, data: input }),

    update: (id: string, input: TInput) =>
      request<TListItem>({
        method: 'PUT',
        url: `${basePath}/${id}`,
        data: input,
      }),

    remove: (id: string) =>
      request<void>({ method: 'DELETE', url: `${basePath}/${id}` }),
  };
}
