import { describe, expect, it } from 'vitest';

import { listParams } from './resource';

/**
 * Every list screen sends its filters through this. A parameter dropped here
 * is a filter that silently does nothing, and one kept by mistake is a filter
 * nobody asked for — both look like a working screen returning the wrong rows.
 */
describe('listParams', () => {
  it('keeps the values that mean something', () => {
    const params = listParams({
      pageNumber: 1,
      pageSize: 20,
      search: 'beton',
      status: 'Active',
      sortDescending: true,
    });

    expect(params).toEqual({
      pageNumber: 1,
      pageSize: 20,
      search: 'beton',
      status: 'Active',
      sortDescending: true,
    });
  });

  it('drops the ones the API would read as a filter on nothing', () => {
    const params = listParams({
      pageNumber: 1,
      search: '',
      status: undefined,
      employeeId: null,
      unassignedOnly: false,
    });

    expect(params).toEqual({ pageNumber: 1 });
  });

  it('keeps a zero, which is a filter and not an absence', () => {
    // `maxQuantity: 0` means "out of stock" — a real question, and the one a
    // truthiness check would throw away.
    expect(listParams({ maxQuantity: 0 })).toEqual({ maxQuantity: 0 });
  });

  it('leaves an empty object empty rather than inventing defaults', () => {
    expect(listParams({})).toEqual({});
  });
});
