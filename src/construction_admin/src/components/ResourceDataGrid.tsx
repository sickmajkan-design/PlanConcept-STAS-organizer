import { InboxOutlined, ViewColumnOutlined } from '@mui/icons-material';
import { Box, Button, Checkbox, Divider, ListItemIcon, ListItemText, Menu, MenuItem, Paper, Stack, Typography, useMediaQuery, useTheme } from '@mui/material';
import { useEffect, useMemo, useRef, useState } from 'react';
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
import { ResourceCardList } from './ResourceCardList';
import { useT } from '../i18n/useI18n';
import { PAGE_SIZE_OPTIONS } from '../hooks/useListQueryState';
import { ALL_COLUMNS_FROM_PX, ESSENTIAL_COLUMNS } from './gridColumnDefaults';

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
  columnsKey,
  empty,
  filtered,
  onClearFilters,
}: {
  /**
   * Names the list in `gridColumnDefaults` and in storage: turns on the "Columns" button, with the list's
   * important columns shown first on a narrow screen and the person's own choice remembered.
   */
  columnsKey?: string;
  /**
   * What an empty list says when nothing has been entered yet: a title and the one thing to do about it.
   * Without it the grid says the plain "no rows".
   */
  empty?: { title: string; hint?: string; action?: { label: string; onClick: () => void }; secondary?: { label: string; onClick: () => void } };
  /** A search or filter is on: an empty list then means "nothing matches", and offers to clear the filters. */
  filtered?: boolean;
  onClearFilters?: () => void;
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
  //
  // Memoised, like the locale text below: the grid compares these by reference, and a fresh
  // object on every render makes it update its own state while this component renders.
  const wide = useMediaQuery(`(min-width:${ALL_COLUMNS_FROM_PX}px)`);
  const essential = columnsKey ? ESSENTIAL_COLUMNS[columnsKey] : undefined;
  const storageKey = columnsKey ? `grid.columns.${columnsKey}` : null;

  // A person's own choice of columns for this list, if they ever made one.
  const [chosen, setChosen] = useState<Record<string, boolean> | null>(() => {
    if (!storageKey) return null;

    try {
      const raw = window.localStorage.getItem(storageKey);
      return raw ? (JSON.parse(raw) as Record<string, boolean>) : null;
    } catch {
      return null;
    }
  });

  // The columns that can be switched: the ones with a name. Pictures and the row's own buttons always stay.
  const switchable = useMemo(
    () => columns.filter((column) => column.field !== 'actions' && column.field !== '__check__' && !!column.headerName),
    [columns],
  );

  const visible = (field: string): boolean => {
    if (chosen && field in chosen) return chosen[field]!;
    if (essential && !wide) return essential.includes(field);
    return true;
  };

  const columnVisibilityModel = useMemo<Record<string, boolean>>(
    () => {
      if (isCompact && compactHiddenFields) {
        return Object.fromEntries(compactHiddenFields.map((field) => [field, false]));
      }

      if (!essential) return {};

      return Object.fromEntries(
        switchable.map((column) => [
          column.field,
          chosen && column.field in chosen ? chosen[column.field]! : wide || essential.includes(column.field),
        ]),
      );
    },
    [isCompact, compactHiddenFields, essential, switchable, chosen, wide],
  );

  const [columnsAnchor, setColumnsAnchor] = useState<HTMLElement | null>(null);

  const toggleColumn = (field: string) => {
    const next = { ...Object.fromEntries(switchable.map((column) => [column.field, visible(column.field)])), [field]: !visible(field) };

    setChosen(next);
    if (storageKey) {
      try {
        window.localStorage.setItem(storageKey, JSON.stringify(next));
      } catch {
        // Storage blocked: the choice holds until the page is left, which is all it can do.
      }
    }
  };

  const resetColumns = () => {
    setChosen(null);
    if (storageKey) {
      try {
        window.localStorage.removeItem(storageKey);
      } catch {
        // See above.
      }
    }
    setColumnsAnchor(null);
  };

  // What the grid shows when it has no rows. A new component only when what it says changes.
  const NoRows = useMemo(
    () =>
      function NoRows() {
        const nothingMatches = !!filtered;

        return (
          <Stack spacing={1} sx={{ height: '100%', alignItems: 'center', justifyContent: 'center', textAlign: 'center', px: 2, py: 4 }}>
            <InboxOutlined sx={{ fontSize: 36, color: 'text.disabled' }} />
            {nothingMatches ? (
              <>
                <Typography sx={{ fontWeight: 600 }}>{t('empty.filteredTitle')}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('empty.filteredHint')}
                </Typography>
                {onClearFilters && (
                  <Button variant="outlined" size="small" onClick={onClearFilters} sx={{ mt: 1 }}>
                    {t('empty.clearFilters')}
                  </Button>
                )}
              </>
            ) : empty ? (
              <>
                <Typography sx={{ fontWeight: 600 }}>{empty.title}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {empty.hint ?? t('empty.createHint')}
                </Typography>
                <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                  {empty.action && (
                    <Button variant="contained" size="small" onClick={empty.action.onClick}>
                      {empty.action.label}
                    </Button>
                  )}
                  {empty.secondary && (
                    <Button variant="outlined" size="small" onClick={empty.secondary.onClick}>
                      {empty.secondary.label}
                    </Button>
                  )}
                </Stack>
              </>
            ) : (
              <Typography color="text.secondary">{t('common.noRows')}</Typography>
            )}
          </Stack>
        );
      },
    [filtered, empty, onClearFilters, t],
  );

  const localeText = useMemo(
    () => ({
      noRowsLabel: t('common.noRows'),
      checkboxSelectionHeaderName: t('grid.checkboxColumn'),
      checkboxSelectionSelectAllRows: t('grid.selectAllRows'),
      checkboxSelectionUnselectAllRows: t('grid.unselectAllRows'),
      checkboxSelectionSelectRow: t('grid.selectRow'),
      checkboxSelectionUnselectRow: t('grid.unselectRow'),
      columnHeaderSortIconLabel: t('grid.sortIcon'),
    }),
    [t],
  );

  // A grid wider than its card scrolls sideways inside it; a soft edge on the side that has more says so.
  const frameRef = useRef<HTMLDivElement | null>(null);
  const [edges, setEdges] = useState({ left: false, right: false });

  useEffect(() => {
    const frame = frameRef.current;
    if (!frame) return;

    let scroller: HTMLElement | null = null;

    const measure = () => {
      scroller = frame.querySelector<HTMLElement>('.MuiDataGrid-virtualScroller');
      if (!scroller) return;

      const left = scroller.scrollLeft > 2;
      const right = scroller.scrollLeft + scroller.clientWidth < scroller.scrollWidth - 2;

      setEdges((current) => (current.left === left && current.right === right ? current : { left, right }));
    };

    const onScroll = () => measure();
    // Absent in a test browser (jsdom); the soft edge is only a hint, so it simply is not drawn there.
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(measure);
    observer?.observe(frame);
    // The grid builds its scroller after the first paint, and the rows (so the width) after the data.
    const timer = window.setInterval(() => {
      measure();
      if (scroller && !scroller.dataset.edgeWatched) {
        scroller.dataset.edgeWatched = 'true';
        scroller.addEventListener('scroll', onScroll, { passive: true });
      }
    }, 400);

    return () => {
      observer?.disconnect();
      window.clearInterval(timer);
      scroller?.removeEventListener('scroll', onScroll);
    };
  }, [isCompact, isError]);

  return (
    <Paper
      sx={isCompact ? { p: 1.5 } : { height, display: 'flex', flexDirection: 'column' }}
    >
      {isError ? (
        <ErrorState error={error} onRetry={onRetry} />
      ) : isCompact ? (
        <ResourceCardList
          rows={data?.items ?? []}
          columns={columns}
          isLoading={isLoading}
          sortModel={sortModel}
          onSortModelChange={onSortModelChange}
          onRowClick={onRowClick ?? onRowDoubleClick}
          rowSelectionModel={rowSelectionModel}
          onRowSelectionModelChange={onRowSelectionModelChange}
          highlightedId={highlightedId}
        />
      ) : (
        <Box ref={frameRef} sx={{ flex: 1, minHeight: 0, position: 'relative', display: 'flex', flexDirection: 'column' }}>
        {essential && (
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', px: 1, pt: 0.75 }}>
            <Button size="small" color="inherit" startIcon={<ViewColumnOutlined fontSize="small" />} onClick={(event) => setColumnsAnchor(event.currentTarget)}>
              {t('grid.columns')}
            </Button>
            <Menu anchorEl={columnsAnchor} open={!!columnsAnchor} onClose={() => setColumnsAnchor(null)}>
              {switchable.map((column) => (
                <MenuItem key={column.field} dense onClick={() => toggleColumn(column.field)}>
                  <ListItemIcon sx={{ minWidth: 32 }}>
                    <Checkbox edge="start" size="small" checked={visible(column.field)} tabIndex={-1} disableRipple sx={{ p: 0 }} />
                  </ListItemIcon>
                  <ListItemText primary={column.headerName} />
                </MenuItem>
              ))}
              <Divider />
              <MenuItem dense onClick={resetColumns}>
                <ListItemText primary={t('grid.columnsReset')} slotProps={{ primary: { color: 'primary' } }} />
              </MenuItem>
            </Menu>
          </Box>
        )}
        <Box sx={{ flex: 1, minHeight: 0, position: 'relative' }}>
        <EdgeShadow side="left" show={edges.left} />
        <EdgeShadow side="right" show={edges.right} />
        <DataGrid
          rows={data?.items ?? []}
          columns={columns}
          loading={isLoading}
          columnVisibilityModel={columnVisibilityModel}
          rowCount={data?.totalCount ?? 0}
          paginationMode="server"
          // The grid's own default is 25/50/100, and ours starts at 20: a size that is not one of
          // its options makes it correct the model while it renders.
          pageSizeOptions={PAGE_SIZE_OPTIONS}
          paginationModel={paginationModel}
          onPaginationModelChange={onPaginationModelChange}
          hideFooter
          sortingMode="server"
          sortModel={sortModel}
          onSortModelChange={onSortModelChange}
          // MUI ships no Serbian locale, so the grid's own chrome — the
          // pagination footer and empty state — has to be handed over
          // explicitly or it stays English inside a translated page.
          localeText={localeText}
          slots={{ noRowsOverlay: NoRows }}
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

/** A soft shadow on one edge of the grid, there only while there is more to scroll to on that side. */
function EdgeShadow({ side, show }: { side: 'left' | 'right'; show: boolean }) {
  return (
    <Box
      aria-hidden
      sx={{
        position: 'absolute',
        top: 0,
        bottom: 0,
        [side]: 0,
        width: 22,
        zIndex: 2,
        pointerEvents: 'none',
        opacity: show ? 1 : 0,
        transition: 'opacity 160ms ease',
        '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
        background: `linear-gradient(to ${side === 'right' ? 'left' : 'right'}, rgba(0, 0, 0, 0.12), rgba(0, 0, 0, 0))`,
      }}
    />
  );
}
