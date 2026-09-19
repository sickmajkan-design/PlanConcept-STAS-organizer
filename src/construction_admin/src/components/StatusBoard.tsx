import { ViewKanbanOutlined, ViewListOutlined } from '@mui/icons-material';
import {
  Box,
  Chip,
  LinearProgress,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef, GridValidRowModel } from '@mui/x-data-grid';
import { useState } from 'react';

import { useAuth } from '../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../hooks/userScopedStorage';
import { useT } from '../i18n/useI18n';
import { RowCard } from './ResourceCardList';

export type ViewMode = 'list' | 'board';

/**
 * List or board, remembered per person and per page. Kept in the browser only:
 * it is a preference about how a screen looks, not data.
 */
export function useViewMode(page: string): [ViewMode, (mode: ViewMode) => void] {
  const { user } = useAuth();
  const scope = storageScope(user);
  const key = `viewMode.${page}`;
  const [mode, setMode] = useState<ViewMode>(() => readScoped<ViewMode>(scope, key, 'list'));

  return [
    mode,
    (next) => {
      setMode(next);
      writeScoped(scope, key, next);
    },
  ];
}

export function ViewModeToggle({
  value,
  onChange,
}: {
  value: ViewMode;
  onChange: (mode: ViewMode) => void;
}) {
  const t = useT();

  return (
    <ToggleButtonGroup
      size="small"
      exclusive
      value={value}
      onChange={(_event, next: ViewMode | null) => next && onChange(next)}
    >
      <ToggleButton value="list" aria-label={t('view.list')}>
        <Tooltip title={t('view.list')}>
          <ViewListOutlined fontSize="small" />
        </Tooltip>
      </ToggleButton>
      <ToggleButton value="board" aria-label={t('view.board')}>
        <Tooltip title={t('view.board')}>
          <ViewKanbanOutlined fontSize="small" />
        </Tooltip>
      </ToggleButton>
    </ToggleButtonGroup>
  );
}

export interface BoardColumn {
  status: string;
  label: string;
  /** Theme palette colour for the header stripe. */
  color: 'warning' | 'success' | 'error' | 'inherit';
}

/**
 * Rows laid out in a column per status, the way the Work Time board groups
 * entries per project. A card is not dragged between columns: moving a row on
 * means an approval or a reason for a refusal, so it happens through the
 * buttons on the card, which already ask for whatever the decision needs.
 */
export function StatusBoard<T extends GridValidRowModel>({
  rows,
  totalCount,
  columns,
  boardColumns,
  getStatus,
  hideFields,
  isLoading,
  onCardClick,
  highlightedId,
}: {
  rows: T[];
  totalCount: number;
  columns: GridColDef<T>[];
  boardColumns: BoardColumn[];
  getStatus: (row: T) => string;
  hideFields?: readonly string[];
  isLoading: boolean;
  onCardClick?: (row: T) => void;
  highlightedId?: string | null;
}) {
  const t = useT();

  return (
    <Box>
      {isLoading && <LinearProgress sx={{ mb: 1 }} />}

      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', md: 'row' },
          gap: 2,
          alignItems: { md: 'flex-start' },
          overflowX: { md: 'auto' },
          pb: 1,
        }}
      >
        {boardColumns.map((column) => {
          const cards = rows.filter((row) => getStatus(row) === column.status);

          return (
            <Box
              key={column.status}
              sx={{
                flex: { md: '1 1 0' },
                minWidth: { md: 280 },
                bgcolor: 'action.hover',
                borderRadius: 1,
                borderTop: 3,
                borderColor: column.color === 'inherit' ? 'divider' : `${column.color}.main`,
                p: 1.5,
              }}
            >
              <Stack
                direction="row"
                spacing={1} useFlexGap
                sx={{ flexWrap: 'wrap', mb: 1.5, alignItems: 'center', justifyContent: 'space-between' }}
              >
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                  {column.label}
                </Typography>
                <Chip size="small" label={cards.length} />
              </Stack>

              <Stack spacing={1.25}>
                {cards.map((row) => {
                  const id = String((row as unknown as { id: string | number }).id);

                  return (
                    <RowCard
                      key={id}
                      row={row}
                      columns={columns}
                      hideFields={hideFields}
                      highlighted={highlightedId === id}
                      onClick={onCardClick ? () => onCardClick(row) : undefined}
                    />
                  );
                })}
                {cards.length === 0 && !isLoading && (
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                    {t('board.empty')}
                  </Typography>
                )}
              </Stack>
            </Box>
          );
        })}
      </Box>

      {totalCount > rows.length && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          {t('board.partial', { shown: rows.length, total: totalCount })}
        </Typography>
      )}
    </Box>
  );
}
