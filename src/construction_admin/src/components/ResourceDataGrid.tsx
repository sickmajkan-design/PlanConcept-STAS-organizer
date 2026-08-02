import { Paper } from '@mui/material';
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridSortModel,
  type GridValidRowModel,
} from '@mui/x-data-grid';

import type { PagedList } from '../api/types';
import { ErrorState } from './ErrorState';
import { useT } from '../i18n/useI18n';
import { PAGE_SIZE_OPTIONS } from '../hooks/useListQueryState';

/**
 * The server-paged grid every list page renders. Paging and sorting are done
 * by the API, so the grid is told the total row count rather than being handed
 * every row to slice itself.
 */
export function ResourceDataGrid<T extends GridValidRowModel>({
  data,
  columns,
  isLoading,
  isError,
  error,
  onRetry,
  paginationModel,
  onPaginationModelChange,
  sortModel,
  onSortModelChange,
  onRowClick,
  height = 600,
}: {
  data: PagedList<T> | undefined;
  columns: GridColDef<T>[];
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  onRetry: () => void;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  sortModel: GridSortModel;
  onSortModelChange: (model: GridSortModel) => void;
  /** Omit for lists with no drill-down, such as user accounts. */
  onRowClick?: (row: T) => void;
  height?: number;
}) {
  const t = useT();

  return (
    <Paper sx={{ height }}>
      {isError ? (
        <ErrorState error={error} onRetry={onRetry} />
      ) : (
        <DataGrid
          rows={data?.items ?? []}
          columns={columns}
          loading={isLoading}
          rowCount={data?.totalCount ?? 0}
          paginationMode="server"
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          pageSizeOptions={PAGE_SIZE_OPTIONS}
          sortingMode="server"
          sortModel={sortModel}
          onSortModelChange={onSortModelChange}
          // MUI ships no Serbian locale, so the grid's own chrome — the
          // pagination footer and empty state — has to be handed over
          // explicitly or it stays English inside a translated page.
          localeText={{
            noRowsLabel: t('common.noRows'),
            paginationRowsPerPage: `${t('common.rowsPerPage')}:`,
            paginationDisplayedRows: ({ from, to, count }) =>
              t('common.displayedRows', {
                from,
                to,
                // -1 means the total is not known yet.
                count: count === -1 ? to : count,
              }),
          }}
          disableColumnMenu
          disableRowSelectionOnClick
          onRowClick={onRowClick ? (params) => onRowClick(params.row) : undefined}
          // Only offer the affordance when a click actually goes somewhere.
          sx={{ border: 'none', cursor: onRowClick ? 'pointer' : 'default' }}
        />
      )}
    </Paper>
  );
}
