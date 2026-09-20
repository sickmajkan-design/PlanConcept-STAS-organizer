import { ChevronRightOutlined, DeleteOutlined } from '@mui/icons-material';
import { Box, Button, IconButton, Paper, Stack, Tooltip, Typography, alpha } from '@mui/material';
import { useMemo, useState, type ReactNode } from 'react';

import { EmptyState } from '../EmptyState';
import { ErrorState } from '../ErrorState';
import { useI18n, useT } from '../../i18n/useI18n';
import { formatMoney } from '../../utils/formatting';
import { CostsLoading } from './CostsLoading';
import { numeric } from './costUi';

export interface LedgerRow<T> {
  item: T;
  id: string;
  /** `YYYY-MM-DD`; entries are grouped by its month. */
  date: string;
  icon?: ReactNode;
  title: string;
  subtitle?: string;
  chips?: ReactNode;
  amount: number | null;
  /** A small line under the amount (quantity × price, hours). */
  meta?: string;
  /** Shown as a muted row, for entries that do not count. */
  muted?: boolean;
}

/** How many entries to fetch: 50 to start, "show more" goes to the server's limit of 100. */
export function useLedgerWindow(initial = 50, max = 100) {
  const [pageSize, setPageSize] = useState(initial);

  return {
    pageSize,
    showMore: () => setPageSize(max),
    canShowMore: pageSize < max,
    max,
  };
}

/**
 * The cost ledgers as cards grouped by month, each with its own subtotal —
 * every entry visible at a glance, one click to open it. Replaces the data
 * grid, which showed the same facts as a row of cells.
 */
export function CostLedgerBoard<T>({
  rows,
  totalCount,
  isLoading,
  isError,
  error,
  onRetry,
  onOpen,
  onDelete,
  window,
}: {
  rows: LedgerRow<T>[];
  totalCount: number;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  onRetry: () => void;
  onOpen: (item: T) => void;
  onDelete?: (item: T) => void;
  window: ReturnType<typeof useLedgerWindow>;
}) {
  const t = useT();
  const { locale } = useI18n();

  const groups = useMemo(() => {
    const byMonth = new Map<string, LedgerRow<T>[]>();

    for (const row of rows) {
      const key = row.date.slice(0, 7);
      byMonth.set(key, [...(byMonth.get(key) ?? []), row]);
    }

    return [...byMonth.entries()].map(([key, items]) => ({
      key,
      items,
      total: items.reduce((sum, r) => sum + (r.muted ? 0 : (r.amount ?? 0)), 0),
    }));
  }, [rows]);

  if (isError) return <ErrorState error={error as Error} onRetry={onRetry} />;
  if (isLoading) return <CostsLoading />;
  if (rows.length === 0) return <EmptyState message={t('common.noResults')} />;

  const monthName = (key: string) => {
    const [year, month] = key.split('-').map(Number);
    return new Date(year, month - 1, 1).toLocaleDateString(locale === 'sr' ? 'sr-Latn' : 'en-GB', {
      month: 'long',
      year: 'numeric',
    });
  };

  return (
    <Stack spacing={2}>
      {groups.map((group) => (
        <Paper key={group.key} variant="outlined" sx={{ overflow: 'hidden' }}>
          <Stack
            direction="row"
            spacing={2}
            sx={(theme) => ({
              px: 2,
              py: 1.25,
              alignItems: 'center',
              justifyContent: 'space-between',
              bgcolor: alpha(theme.palette.text.primary, 0.03),
              borderBottom: 1,
              borderColor: 'divider',
            })}
          >
            <Typography variant="subtitle2" sx={{ fontWeight: 700, textTransform: 'capitalize' }}>
              {monthName(group.key)}
              <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                {group.items.length}
              </Typography>
            </Typography>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, ...numeric }}>
              {formatMoney(group.total, locale)}
            </Typography>
          </Stack>

          <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>
            {group.items.map((row) => (
              <Box
                key={row.id}
                role="button"
                tabIndex={0}
                onClick={() => onOpen(row.item)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    onOpen(row.item);
                  }
                }}
                sx={{
                  px: 2,
                  py: 1.5,
                  cursor: 'pointer',
                  opacity: row.muted ? 0.6 : 1,
                  transition: 'background-color 120ms',
                  '&:hover, &:focus-visible': { bgcolor: 'action.hover', outline: 'none' },
                  '&:active': { bgcolor: 'action.selected' },
                }}
              >
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                  {row.icon && <Box sx={{ color: 'text.secondary', display: 'flex' }}>{row.icon}</Box>}

                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {row.title}
                      </Typography>
                      {row.chips}
                    </Stack>
                    {row.subtitle && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        {row.subtitle}
                      </Typography>
                    )}
                  </Box>

                  <Box sx={{ textAlign: 'right', flexShrink: 0 }}>
                    <Typography variant="body1" sx={{ fontWeight: 700, whiteSpace: 'nowrap', ...numeric }}>
                      {row.amount === null ? '—' : formatMoney(row.amount, locale)}
                    </Typography>
                    {row.meta && (
                      <Typography variant="caption" color="text.secondary" sx={numeric}>
                        {row.meta}
                      </Typography>
                    )}
                  </Box>

                  {onDelete && (
                    <Tooltip title={t('common.delete')}>
                      <IconButton
                        size="small"
                        aria-label={t('common.delete')}
                        onClick={(event) => {
                          event.stopPropagation();
                          onDelete(row.item);
                        }}
                      >
                        <DeleteOutlined fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                  <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled' }} />
                </Stack>
              </Box>
            ))}
          </Stack>
        </Paper>
      ))}

      {totalCount > rows.length && (
        <Stack spacing={1} sx={{ alignItems: 'center', py: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {t('costs.ledgerShowing', { shown: rows.length, total: totalCount })}
          </Typography>
          {window.canShowMore ? (
            <Button variant="outlined" onClick={window.showMore}>
              {t('costs.ledgerShowMore')}
            </Button>
          ) : (
            <Typography variant="caption" color="text.secondary">
              {t('costs.ledgerNarrow')}
            </Typography>
          )}
        </Stack>
      )}
    </Stack>
  );
}
