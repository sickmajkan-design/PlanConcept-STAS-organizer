import { AddOutlined, DeleteOutlined, EditOutlined, VisibilityOutlined } from '@mui/icons-material';
import {
  Box,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { ApiError } from '../../api/apiError';
import type { VehicleListQuery } from '../../api/vehicles';
import type { Vehicle, VehicleStatus } from '../../api/types';
import { vehicleStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteVehicle, useVehiclesQuery } from '../../features/vehicles/useVehicles';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

export function VehiclesListPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 350);
  const [status, setStatus] = useState<VehicleStatus | ''>('');
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  });
  const [sortModel, setSortModel] = useState<GridSortModel>([{ field: 'brand', sort: 'asc' }]);
  const [pendingDelete, setPendingDelete] = useState<Vehicle | null>(null);

  const query: VehicleListQuery = useMemo(
    () => ({
      pageNumber: paginationModel.page + 1,
      pageSize: paginationModel.pageSize,
      search: debouncedSearch,
      status: status || undefined,
      sortBy: sortModel[0]?.field,
      sortDescending: sortModel[0]?.sort === 'desc',
    }),
    [paginationModel, debouncedSearch, status, sortModel],
  );

  const { data, isLoading, isError, error, refetch } = useVehiclesQuery(query);
  const deleteVehicle = useDeleteVehicle();

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
              <IconButton size="small" onClick={() => setPendingDelete(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [navigate],
  );

  const confirmDelete = async () => {
    if (!pendingDelete) return;

    try {
      await deleteVehicle.mutateAsync(pendingDelete.id);
      setPendingDelete(null);
    } catch {
      // Error surfaced below via the mutation's own state.
    }
  };

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
        <SearchField value={search} onChange={setSearch} placeholder="Brand, model, registration…" />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="vehicle-status-filter-label">Status</InputLabel>
          <Select
            labelId="vehicle-status-filter-label"
            label="Status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value as VehicleStatus | '');
              setPaginationModel((prev) => ({ ...prev, page: 0 }));
            }}
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

      <Paper sx={{ height: 600 }}>
        {isError ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : (
          <DataGrid
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading}
            rowCount={data?.totalCount ?? 0}
            paginationMode="server"
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            pageSizeOptions={PAGE_SIZE_OPTIONS}
            sortingMode="server"
            sortModel={sortModel}
            onSortModelChange={setSortModel}
            disableColumnMenu
            disableRowSelectionOnClick
            onRowClick={(params) => navigate(paths.vehicleDetail(params.row.id))}
            sx={{ border: 'none', cursor: 'pointer' }}
          />
        )}
      </Paper>

      <ConfirmDialog
        open={!!pendingDelete}
        title="Delete vehicle?"
        description={
          pendingDelete
            ? `${pendingDelete.brand} ${pendingDelete.model} (${pendingDelete.registrationNumber}) will be removed from active records.`
            : ''
        }
        confirmLabel="Delete"
        destructive
        loading={deleteVehicle.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />

      {deleteVehicle.isError && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {(deleteVehicle.error as ApiError).message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}
