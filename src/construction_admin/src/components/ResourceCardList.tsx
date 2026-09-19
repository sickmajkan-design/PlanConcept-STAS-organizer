import { ArrowDownward, ArrowUpward } from '@mui/icons-material';
import {
  Box,
  Checkbox,
  FormControl,
  IconButton,
  InputLabel,
  LinearProgress,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import type {
  GridColDef,
  GridRowSelectionModel,
  GridSortModel,
  GridValidRowModel,
} from '@mui/x-data-grid';
import type { ReactNode } from 'react';

import { useT } from '../i18n/useI18n';

type AnyFn = (...args: unknown[]) => unknown;

const rowId = (row: unknown) => String((row as { id: string | number }).id);

const isBlank = (value: unknown) => value === null || value === undefined || value === '';

/**
 * A grid column's cell as plain content, without a grid around it. Column
 * definitions only read the row and value, so the same definitions that draw
 * the desktop table draw the phone cards and the two can never drift apart.
 */
function cellContent<T extends GridValidRowModel>(column: GridColDef<T>, row: T): ReactNode {
  const stubApi = { current: {} } as never;
  let value: unknown = (row as Record<string, unknown>)[column.field];

  if (column.valueGetter) {
    value = (column.valueGetter as unknown as AnyFn)(
      value,
      row,
      column,
      stubApi,
    );
  }

  const formatted = column.valueFormatter
    ? (column.valueFormatter as unknown as AnyFn)(
        value,
        row,
        column,
        stubApi,
      )
    : value;

  if (column.renderCell) {
    return column.renderCell({
      id: rowId(row),
      row,
      field: column.field,
      value,
      formattedValue: formatted,
      colDef: column,
      hasFocus: false,
      tabIndex: -1,
      api: stubApi,
      rowNode: {} as never,
      cellMode: 'view',
      isEditable: false,
    } as never);
  }

  return isBlank(formatted) ? null : (formatted as ReactNode);
}

/**
 * The phone-width view of a list: one card per row instead of a table that
 * scrolls sideways. The first column is the card's title, the rest are
 * labelled lines, and the row's action buttons sit along the bottom.
 */
export function ResourceCardList<T extends GridValidRowModel>({
  rows,
  columns,
  isLoading,
  sortModel,
  onSortModelChange,
  onRowClick,
  rowSelectionModel,
  onRowSelectionModelChange,
  highlightedId,
}: {
  rows: T[];
  columns: GridColDef<T>[];
  isLoading: boolean;
  sortModel: GridSortModel;
  onSortModelChange: (model: GridSortModel) => void;
  onRowClick?: (row: T) => void;
  rowSelectionModel?: GridRowSelectionModel;
  onRowSelectionModelChange?: (model: GridRowSelectionModel) => void;
  highlightedId?: string | null;
}) {
  const t = useT();

  const actionsColumn = columns.find((column) => column.field === 'actions');
  const dataColumns = columns.filter((column) => column.field !== 'actions');
  const [titleColumn, ...detailColumns] = dataColumns;
  const sortable = dataColumns.filter((column) => column.sortable !== false && column.headerName);

  const currentSort = sortModel[0];
  const selectable = !!onRowSelectionModelChange;
  const selectedIds =
    rowSelectionModel?.type === 'include' ? rowSelectionModel.ids : new Set<string | number>();

  const toggle = (id: string) => {
    const next = new Set(selectedIds);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    onRowSelectionModelChange?.({ type: 'include', ids: next });
  };

  return (
    <Box>
      {sortable.length > 0 && (
        <Stack direction="row" spacing={1} sx={{ mb: 1.5, alignItems: 'center' }}>
          <FormControl size="small" sx={{ flex: 1, minWidth: 0 }}>
            <InputLabel>{t('common.sortBy')}</InputLabel>
            <Select
              label={t('common.sortBy')}
              value={currentSort?.field ?? ''}
              onChange={(event) =>
                onSortModelChange([{ field: event.target.value, sort: currentSort?.sort ?? 'asc' }])
              }
            >
              {sortable.map((column) => (
                <MenuItem key={column.field} value={column.field}>
                  {column.headerName}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <IconButton
            aria-label={t('common.sortDirection')}
            disabled={!currentSort}
            onClick={() =>
              currentSort &&
              onSortModelChange([
                { field: currentSort.field, sort: currentSort.sort === 'asc' ? 'desc' : 'asc' },
              ])
            }
          >
            {currentSort?.sort === 'desc' ? <ArrowDownward /> : <ArrowUpward />}
          </IconButton>
        </Stack>
      )}

      {isLoading && <LinearProgress sx={{ mb: 1 }} />}

      {!isLoading && rows.length === 0 && (
        <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          {t('common.noRows')}
        </Typography>
      )}

      <Stack spacing={1.25}>
        {rows.map((row) => {
          const id = rowId(row);
          const details = detailColumns
            .map((column) => ({ column, content: cellContent(column, row) }))
            .filter(({ content }) => !isBlank(content));

          return (
            <Box
              key={id}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              sx={{
                border: 1,
                borderColor: 'divider',
                borderRadius: 1,
                bgcolor: highlightedId === id ? 'action.hover' : 'background.paper',
                p: 1.5,
                cursor: onRowClick ? 'pointer' : 'default',
              }}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                {selectable && (
                  <Checkbox
                    size="small"
                    checked={selectedIds.has(id)}
                    onClick={(event) => event.stopPropagation()}
                    onChange={() => toggle(id)}
                    sx={{ p: 0.5, ml: -0.5 }}
                  />
                )}
                <Box sx={{ fontWeight: 600, minWidth: 0, flex: 1, overflowWrap: 'anywhere' }}>
                  {titleColumn ? cellContent(titleColumn, row) : id}
                </Box>
              </Stack>

              {details.length > 0 && (
                <Box
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: 'minmax(96px, 38%) 1fr',
                    columnGap: 1.5,
                    rowGap: 0.5,
                    mt: 1,
                  }}
                >
                  {details.map(({ column, content }) => (
                    <Box key={column.field} sx={{ display: 'contents' }}>
                      <Typography variant="caption" color="text.secondary" sx={{ pt: 0.25 }}>
                        {column.headerName}
                      </Typography>
                      <Box sx={{ typography: 'body2', minWidth: 0, overflowWrap: 'anywhere' }}>
                        {content}
                      </Box>
                    </Box>
                  ))}
                </Box>
              )}

              {actionsColumn && (
                <Box
                  onClick={(event) => event.stopPropagation()}
                  sx={{ display: 'flex', justifyContent: 'flex-end', mt: 1 }}
                >
                  {cellContent(actionsColumn, row)}
                </Box>
              )}
            </Box>
          );
        })}
      </Stack>
    </Box>
  );
}
