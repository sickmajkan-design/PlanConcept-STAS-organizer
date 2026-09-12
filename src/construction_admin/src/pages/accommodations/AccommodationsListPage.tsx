import {
  AddOutlined,
  DeleteOutlined,
  EditOutlined,
  HomeWorkOutlined,
} from '@mui/icons-material';
import {
  Box,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';

import type { AccommodationListQuery } from '../../api/accommodations';
import type { Accommodation } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { RowActions } from '../../components/RowActions';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import {
  useAccommodationsQuery,
  useDeleteAccommodation,
} from '../../features/accommodations/useAccommodations';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useI18n, useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { formatMoney } from '../../utils/formatting';

// The company houses at most a few dozen workers at once — everything fits
// on one board, no page numbers to click through. Same call as Projects.
const FULL_LIST: { pageNumber: number; pageSize: number } = { pageNumber: 1, pageSize: 100 };

export function AccommodationsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const list = useListQueryState('address', 'asc', 'accommodations');

  const query: AccommodationListQuery = useMemo(
    () => ({ ...list.query, ...FULL_LIST }),
    [list.query],
  );

  const { data, isLoading, isError, error, refetch } = useAccommodationsQuery(query);
  const remove = useDeleteWithConfirm<Accommodation>(useDeleteAccommodation());

  const rows = data?.items ?? [];

  return (
    <Box>
      <PageHeader
        title={t('accommodations.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('accommodations.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.accommodationNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('accommodations.searchPlaceholder')}
        />
      </Stack>

      {list.savedViews && (
        <Box sx={{ mb: 2 }}>
          <SavedViewsBar
            views={list.savedViews.views}
            onApply={list.savedViews.applyView}
            onSave={list.savedViews.saveCurrentView}
            onDelete={list.savedViews.deleteView}
          />
        </Box>
      )}

      {isError ? (
        <ErrorState error={error} onRetry={() => void refetch()} />
      ) : isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : rows.length === 0 ? (
        <EmptyState message={t('common.noResults')} />
      ) : (
        <Paper variant="outlined">
          {rows.map((row, index) => (
            <Box key={row.id}>
              {index > 0 && <Divider />}
              <AccommodationRow
                accommodation={row}
                locale={locale}
                onOpen={() => navigate(paths.accommodationDetail(row.id))}
                onEdit={() => navigate(paths.accommodationEdit(row.id))}
                onDelete={() => remove.request(row)}
              />
            </Box>
          ))}
        </Paper>
      )}

      <ConfirmDialog
        open={!!remove.pending}
        title={t('accommodations.deleteTitle')}
        description={
          remove.pending ? t('accommodations.deleteBody', { name: remove.pending.address }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {remove.error.message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}

/** One accommodation's row — address up front, current rent (or a "no active rate" flag) at a glance. */
function AccommodationRow({
  accommodation,
  locale,
  onOpen,
  onEdit,
  onDelete,
}: {
  accommodation: Accommodation;
  locale: string;
  onOpen: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const t = useT();
  const hasActiveRate = accommodation.currentMonthlyAmount !== null;

  return (
    <Stack
      direction="row"
      spacing={1.5}
      onClick={onOpen}
      sx={{
        alignItems: 'center',
        py: 1.25,
        px: 2,
        cursor: 'pointer',
        '&:hover': { bgcolor: 'action.hover' },
      }}
    >
      <HomeWorkOutlined fontSize="small" color="primary" />

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
          {accommodation.address}
        </Typography>
        {accommodation.currentProvider && (
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
            {accommodation.currentProvider}
          </Typography>
        )}
      </Box>

      {hasActiveRate ? (
        <Chip
          size="small"
          color="success"
          variant="outlined"
          label={formatMoney(accommodation.currentMonthlyAmount!, locale)}
        />
      ) : (
        <Chip size="small" variant="outlined" label={t('accommodations.noActiveRate')} />
      )}

      <RowActions>
        <Tooltip title={t('common.edit')}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onEdit();
            }}
          >
            <EditOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title={t('common.delete')}>
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              onDelete();
            }}
          >
            <DeleteOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      </RowActions>
    </Stack>
  );
}
