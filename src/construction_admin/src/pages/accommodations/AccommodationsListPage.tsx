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
  FormControlLabel,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { AccommodationListQuery } from '../../api/accommodations';
import { accommodationTypes, type Accommodation, type AccommodationType } from '../../api/types';
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
import { useEnumLabel } from '../../i18n/enumLabels';
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
  const enumLabel = useEnumLabel();
  const list = useListQueryState('address', 'asc', 'accommodations');
  const [type, setType] = useState<AccommodationType | ''>('');
  const [activeOnly, setActiveOnly] = useState(true);

  const query: AccommodationListQuery = useMemo(
    () => ({
      ...list.query,
      ...FULL_LIST,
      type: type || undefined,
      isActive: activeOnly ? true : undefined,
    }),
    [list.query, type, activeOnly],
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

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('accommodations.searchPlaceholder')}
        />
        <TextField
          select
          size="small"
          label={t('accommodations.type')}
          value={type}
          onChange={(event) => setType(event.target.value as AccommodationType | '')}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">{t('accommodations.allTypes')}</MenuItem>
          {accommodationTypes.map((value) => (
            <MenuItem key={value} value={value}>
              {enumLabel('accommodationType', value)}
            </MenuItem>
          ))}
        </TextField>
        <FormControlLabel
          control={<Switch checked={activeOnly} onChange={(event) => setActiveOnly(event.target.checked)} />}
          label={t('accommodations.activeOnly')}
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
          remove.pending
            ? t('accommodations.deleteBody', { name: remove.pending.name || remove.pending.address })
            : ''
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
  const enumLabel = useEnumLabel();
  const hasActiveRate = accommodation.currentMonthlyAmount !== null;
  const beds = accommodation.beds;
  const over = beds !== null && accommodation.currentOccupants > beds;

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
        opacity: accommodation.isActive ? 1 : 0.6,
        '&:hover': { bgcolor: 'action.hover' },
      }}
    >
      <HomeWorkOutlined fontSize="small" color="primary" />

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
          {accommodation.name || accommodation.address}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
          {[
            accommodation.name ? accommodation.address : null,
            accommodation.city,
            accommodation.currentProvider,
          ]
            .filter(Boolean)
            .join(' · ')}
        </Typography>
      </Box>

      <Chip
        size="small"
        variant="outlined"
        label={enumLabel('accommodationType', accommodation.type)}
        sx={{ display: { xs: 'none', sm: 'inline-flex' } }}
      />

      <Tooltip title={over ? t('accommodations.overCapacity') : t('accommodations.occupancy')}>
        <Chip
          size="small"
          variant={accommodation.currentOccupants > 0 ? 'filled' : 'outlined'}
          color={over ? 'error' : 'default'}
          label={beds === null ? accommodation.currentOccupants : `${accommodation.currentOccupants} / ${beds}`}
        />
      </Tooltip>

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
