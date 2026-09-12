import { useMemo, useState } from 'react';
import type { GridPaginationModel, GridSortModel } from '@mui/x-data-grid';

import type { ListQuery } from '../api/types';
import { useDebouncedValue } from './useDebouncedValue';
import { useSavedViews } from './useSavedViews';

export const PAGE_SIZE_OPTIONS = [10, 20, 50];

const SEARCH_DEBOUNCE_MS = 350;

interface BaseViewState<TFilter> {
  search: string;
  filter: TFilter | '';
  sortModel: GridSortModel;
}

/**
 * Paging, sorting and debounced search for a server-driven grid — the state
 * every list page needs, and the query object it sends.
 *
 * Pass `pageKey` to also get that page's saved-filter views (search, the
 * status filter, and sort — the three fields owned here) for free via the
 * returned `savedViews`. A page with filters beyond these three composes its
 * own `useSavedViews` instead, capturing the extra field itself (see
 * `EmployeesListPage`'s `typeFilter`) and leaves `pageKey` unset here so the
 * two don't both write the same storage key with different shapes.
 *
 * Changing a filter has to reset to page 1: leaving the grid on page 3 of a
 * result set that now has one page shows an empty table, which reads as
 * "no results" rather than "wrong page".
 */
export function useListQueryState<TFilter extends string = string>(
  defaultSortField: string,
  defaultSort: 'asc' | 'desc' = 'asc',
  pageKey?: string,
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

  // Always called (Rules of Hooks) — harmless when `pageKey` is unset, since
  // nothing below ever reads or writes through it in that case.
  const savedViewsStore = useSavedViews<BaseViewState<TFilter>>(pageKey ?? '__unused__');

  const savedViews = pageKey
    ? {
        views: savedViewsStore.views,
        saveCurrentView: (name: string) =>
          savedViewsStore.saveView(name, { search, filter, sortModel }),
        applyView: (state: BaseViewState<TFilter>) => {
          setSearch(state.search);
          setFilterValue(state.filter);
          setSortModel(state.sortModel);
          resetToFirstPage();
        },
        deleteView: savedViewsStore.deleteView,
      }
    : undefined;

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
    savedViews,
  };
}
