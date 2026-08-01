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
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteVehicle, useVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

export function VehiclesListPage() {
  const navigate = useNavigate();
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
        headerName: 'Vehicle',
        flex: 1,
        minWidth: 180,
        valueGetter: (_value, row) => `${row.brand} ${row.model}`,
      },
      { field: 'registrationNumber', headerName: 'Registration', width: 140 },
      {
        field: 'fuelType',
        headerName: 'Fuel',
        width: 110,
        valueFormatter: (value: string) => humanizeEnum(value),
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} />,
      },
      {
        field: 'assignedEmployeeName',
        headerName: 'Assigned to',
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
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="View">
              <IconButton size="small" onClick={() => navigate(paths.vehicleDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton size="small" onClick={() => navigate(paths.vehicleEdit(params.row.id))}>
                <EditOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Delete">
              <IconButton size="small" onClick={() => remove.request(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [navigate, remove],
  );

  return (
    <Box>
      <PageHeader
        title="Vehicles"
        subtitle={data ? `${data.totalCount} total` : undefined}
        action={{
          label: 'Add vehicle',
          icon: <AddOutlined />,
          onClick: () => navigate(paths.vehicleNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder="Brand, model, registration…"
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="vehicle-status-filter-label">Status</InputLabel>
          <Select
            labelId="vehicle-status-filter-label"
            label="Status"
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as VehicleStatus | '')}
          >
            <MenuItem value="">
              <em>All</em>
            </MenuItem>
            {vehicleStatuses.map((value) => (
              <MenuItem key={value} value={value}>
                {humanizeEnum(value)}
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
        title="Delete vehicle?"
        description={
          remove.pending
            ? `${remove.pending.brand} ${remove.pending.model} (${remove.pending.registrationNumber}) will be removed from active records.`
            : ''
        }
        confirmLabel="Delete"
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
