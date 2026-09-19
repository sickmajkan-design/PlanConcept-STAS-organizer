import { Box, Button, Paper, Typography, useMediaQuery, useTheme } from '@mui/material';
import {
  DataGrid,
  type GridColDef,
  type GridPaginationModel,
  type GridRowSelectionModel,
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
  onRowDoubleClick,
  height = 600,
  rowSelectionModel,
  onRowSelectionModelChange,
  highlightedId,
  compactHiddenFields,
}: {
  /**
   * Columns to drop on a phone-width screen, so the ones that matter fit
   * without scrolling sideways. Opt-in per page: only the page knows which of
   * its columns are the ones a person came for. Omit it and nothing changes.
   */
  compactHiddenFields?: readonly string[];
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
  /** Opens an edit dialog in place, for ledgers with no detail page of their own. */
  onRowDoubleClick?: (row: T) => void;
  height?: number;
  /**
   * Passing both turns on row checkboxes for bulk actions (see
   * `BulkActionsBar`). Omit both — the common case — for a grid with no bulk
   * actions, which is the same as before this existed.
   */
  rowSelectionModel?: GridRowSelectionModel;
  onRowSelectionModelChange?: (model: GridRowSelectionModel) => void;
  /** The row a notification deep-link points at — briefly flashed so it isn't lost in the page. */
  highlightedId?: string | null;
}) {
  const t = useT();
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('sm'));

  // No page numbers: the list grows in place. Underneath it is still one
  // server page, just a bigger one, so every row is always fresh - a row
  // approved on screen cannot linger as "pending" from an older page load.
  const total = data?.totalCount ?? 0;
  const shown = data?.items?.length ?? 0;
  const nextPageSize = PAGE_SIZE_OPTIONS.find((size) => size > paginationModel.pageSize);

  // Always a model, empty when nothing is hidden: switching a grid between
  // controlled and uncontrolled as the window is resized draws a warning.
  const columnVisibilityModel: Record<string, boolean> =
    isCompact && compactHiddenFields
      ? Object.fromEntries(compactHiddenFields.map((field) => [field, false]))
      : {};

  return (
    <Paper sx={{ height, display: 'flex', flexDirection: 'column' }}>
      {isError ? (
        <ErrorState error={error} onRetry={onRetry} />
      ) : (
        <Box sx={{ flex: 1, minHeight: 0 }}>
        <DataGrid
          rows={data?.items ?? []}
          columns={columns}
          loading={isLoading}
          columnVisibilityModel={columnVisibilityModel}
          rowCount={data?.totalCount ?? 0}
          paginationMode="server"
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          hideFooter
          sortingMode="server"
          sortModel={sortModel}
          onSortModelChange={onSortModelChange}
          // MUI ships no Serbian locale, so the grid's own chrome — the
          // pagination footer and empty state — has to be handed over
          // explicitly or it stays English inside a translated page.
          localeText={{
            noRowsLabel: t('common.noRows'),
          }}
          disableColumnMenu
          disableRowSelectionOnClick
          // The app's own BulkActionsBar already shows the selected count
          // (translated); MUI's built-in footer count ships English-only
          // ("N row(s) selected") with no locale text override available for
          // it, so it stays hidden rather than showing untranslated text
          // whenever checkbox selection is on.
          hideFooterSelectedRowCount
          checkboxSelection={!!onRowSelectionModelChange}
          rowSelectionModel={rowSelectionModel}
          onRowSelectionModelChange={onRowSelectionModelChange}
          onRowClick={onRowClick ? (params) => onRowClick(params.row) : undefined}
          onRowDoubleClick={
            onRowDoubleClick ? (params) => onRowDoubleClick(params.row) : undefined
          }
          getRowClassName={(params) =>
            highlightedId && params.id === highlightedId ? 'row-highlight' : ''
          }
          // Only offer the affordance when a click actually goes somewhere.
          sx={{
            border: 'none',
            cursor: onRowClick || onRowDoubleClick ? 'pointer' : 'default',
            '& .row-highlight': { bgcolor: 'action.hover' },
          }}
        />
        </Box>
      )}
      {!isError && total > 0 && (
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 2,
            px: 2,
            py: 1,
            borderTop: 1,
            borderColor: 'divider',
            flexWrap: 'wrap',
          }}
        >
          <Typography variant="body2" color="text.secondary">
            {t('common.shownOfTotal', { shown, total })}
          </Typography>
          {shown < total && nextPageSize && (
            <Button
              size="small"
              onClick={() => onPaginationModelChange({ page: 0, pageSize: nextPageSize })}
            >
              {t('common.showMore')}
            </Button>
          )}
          {shown < total && !nextPageSize && (
            <Typography variant="body2" color="text.secondary">
              {t('common.narrowSearch')}
            </Typography>
          )}
        </Box>
      )}
    </Paper>
  );
}
