import { AddOutlined, DeleteOutlined, EditOutlined, VisibilityOutlined } from '@mui/icons-material';
import {
  Box,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';

import type { VehicleListQuery } from '../../api/vehicles';
import type { Vehicle, VehicleStatus } from '../../api/types';
import { vehicleStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteVehicle, useVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

export function VehiclesListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState<VehicleStatus>('brand');

  const query: VehicleListQuery = useMemo(
    () => ({ ...list.query, status: list.filter || undefined }),
    [list.query, list.filter],
  );

  const { data, isLoading, isError, error, refetch } = useVehiclesQuery(query);
  const remove = useDeleteWithConfirm<Vehicle>(useDeleteVehicle());

  // Memoized: DataGrid treats a new columns array as a structural change on
  // every render, which is wasted work.
  const columns: GridColDef<Vehicle>[] = useMemo(
    () => [
      {
        field: 'brand',
        headerName: t('vehicles.vehicle'),
        flex: 1,
        minWidth: 180,
        valueGetter: (_value, row) => `${row.brand} ${row.model}`,
      },
      { field: 'registrationNumber', headerName: t('vehicles.registrationShort'), width: 140 },
      {
        field: 'fuelType',
        headerName: t('vehicles.fuelShort'),
        width: 110,
        valueFormatter: (value: string) => humanizeEnum(value),
      },
      {
        field: 'status',
        headerName: t('vehicles.status'),
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} kind="vehicleStatus" />,
      },
      {
        field: 'assignedEmployeeName',
        headerName: t('vehicles.assignedTo'),
        flex: 1,
        minWidth: 160,
        valueGetter: (v) => v || '—',
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
              <IconButton size="small" onClick={() => navigate(paths.vehicleDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.vehicleEdit(params.row.id))}>
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
    [navigate, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('vehicles.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('vehicles.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.vehicleNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('vehicles.searchPlaceholder')}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="vehicle-status-filter-label">{t('vehicles.status')}</InputLabel>
          <Select
            labelId="vehicle-status-filter-label"
            label={t('vehicles.status')}
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as VehicleStatus | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {vehicleStatuses.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('vehicleStatus', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
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
        onRowClick={(row) => navigate(paths.vehicleDetail(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('vehicles.deleteTitle')}
        description={
          remove.pending
            ? t('vehicles.deleteBody', {
                name: `${remove.pending.brand} ${remove.pending.model} (${remove.pending.registrationNumber})`,
              })
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
