import { useMemo, useState } from 'react';
import type { GridPaginationModel, GridSortModel } from '@mui/x-data-grid';

import type { ListQuery } from '../api/types';
import { useDebouncedValue } from './useDebouncedValue';

export const PAGE_SIZE_OPTIONS = [10, 20, 50];

const SEARCH_DEBOUNCE_MS = 350;

/**
 * Paging, sorting and debounced search for a server-driven grid — the state
 * every list page needs, and the query object it sends.
 *
 * Changing a filter has to reset to page 1: leaving the grid on page 3 of a
 * result set that now has one page shows an empty table, which reads as
 * "no results" rather than "wrong page".
 */
export function useListQueryState<TFilter extends string = string>(
  defaultSortField: string,
  defaultSort: 'asc' | 'desc' = 'asc',
) {
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, SEARCH_DEBOUNCE_MS);

  const [filter, setFilterValue] = useState<TFilter | ''>('');

  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  });

  const [sortModel, setSortModel] = useState<GridSortModel>([
    { field: defaultSortField, sort: defaultSort },
  ]);

  const query: ListQuery = useMemo(
    () => ({
      pageNumber: paginationModel.page + 1,
      pageSize: paginationModel.pageSize,
      search: debouncedSearch,
      sortBy: sortModel[0]?.field,
      sortDescending: sortModel[0]?.sort === 'desc',
    }),
    [paginationModel, debouncedSearch, sortModel],
  );

  const resetToFirstPage = () => setPaginationModel((prev) => ({ ...prev, page: 0 }));

  const setFilter = (value: TFilter | '') => {
    setFilterValue(value);
    resetToFirstPage();
  };

  return {
    search,
    setSearch,
    filter,
    setFilter,
    paginationModel,
    setPaginationModel,
    sortModel,
    setSortModel,
    query,
    resetToFirstPage,
  };
}
