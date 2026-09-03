import { AddOutlined, DeleteOutlined, EditOutlined, VisibilityOutlined } from '@mui/icons-material';
import { Box, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';

import type { AccommodationListQuery } from '../../api/accommodations';
import type { Accommodation } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
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

export function AccommodationsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const list = useListQueryState('address');

  const query: AccommodationListQuery = list.query;

  const { data, isLoading, isError, error, refetch } = useAccommodationsQuery(query);
  const remove = useDeleteWithConfirm<Accommodation>(useDeleteAccommodation());

  const columns: GridColDef<Accommodation>[] = useMemo(
    () => [
      { field: 'address', headerName: t('accommodations.address'), flex: 1, minWidth: 220 },
      {
        field: 'currentMonthlyAmount',
        headerName: t('accommodations.currentMonthlyAmount'),
        width: 150,
        type: 'number',
        valueGetter: (value) =>
          value === null ? '—' : formatMoney(value as number, locale),
      },
      {
        field: 'currentProvider',
        headerName: t('accommodations.currentProvider'),
        flex: 1,
        minWidth: 160,
        valueGetter: (value) => value || '—',
      },
      {
        field: 'actions',
        headerName: '',
        width: 130,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <RowActions>
            <Tooltip title={t('common.view')}>
              <IconButton size="small" onClick={() => navigate(paths.accommodationDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.accommodationEdit(params.row.id))}>
                <EditOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.delete')}>
              <IconButton size="small" onClick={() => remove.request(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </RowActions>
        ),
      },
    ],
    [navigate, remove, t, locale],
  );

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

      <ResourceDataGrid
        data={data}
        columns={columns}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={() => void refetch()}
        paginationModel={list.paginationModel}
        onPaginationModelChange={list.setPaginationModel}
        sortModel={list.sortModel}
        onSortModelChange={list.setSortModel}
        onRowClick={(row) => navigate(paths.accommodationDetail(row.id))}
      />

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
