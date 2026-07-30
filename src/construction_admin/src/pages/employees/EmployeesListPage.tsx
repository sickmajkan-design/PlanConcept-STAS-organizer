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
import type { EmployeeListQuery } from '../../api/employees';
import type { Employee, EmployeeStatus } from '../../api/types';
import { employeeStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { StatusChip } from '../../components/StatusChip';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { paths } from '../../routes/paths';
import { formatDate, humanizeEnum } from '../../utils/formatting';
import { useDeleteEmployee, useEmployeesQuery } from '../../features/employees/useEmployees';
import { SearchField } from '../../components/SearchField';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

export function EmployeesListPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 350);
  const [status, setStatus] = useState<EmployeeStatus | ''>('');
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  });
  const [sortModel, setSortModel] = useState<GridSortModel>([{ field: 'lastName', sort: 'asc' }]);
  const [pendingDelete, setPendingDelete] = useState<Employee | null>(null);

  const query: EmployeeListQuery = useMemo(
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

  const { data, isLoading, isError, error, refetch } = useEmployeesQuery(query);
  const deleteEmployee = useDeleteEmployee();

  // Memoized: DataGrid treats a new columns array as a structural change on
  // every render, which is wasted work (and, combined with Suspense-loaded
  // routes, can trigger a spurious "setState during render" warning).
  const columns: GridColDef<Employee>[] = useMemo(
    () => [
      { field: 'employeeNumber', headerName: 'Number', width: 110 },
      { field: 'fullName', headerName: 'Name', flex: 1, minWidth: 180 },
      { field: 'position', headerName: 'Position', flex: 1, minWidth: 150 },
      {
        field: 'status',
        headerName: 'Status',
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} />,
      },
      {
        field: 'employmentDate',
        headerName: 'Employed',
        width: 120,
        valueFormatter: (value: string) => formatDate(value),
      },
      { field: 'phone', headerName: 'Phone', width: 150, valueGetter: (v) => v || '—' },
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
              <IconButton
                size="small"
                onClick={() => navigate(paths.employeeDetail(params.row.id))}
              >
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton
                size="small"
                onClick={() => navigate(paths.employeeEdit(params.row.id))}
              >
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
      await deleteEmployee.mutateAsync(pendingDelete.id);
      setPendingDelete(null);
    } catch {
      // The dialog stays open with the mutation's error surfaced via its own state.
    }
  };

  return (
    <Box>
      <PageHeader
        title="Employees"
        subtitle={data ? `${data.totalCount} total` : undefined}
        action={{
          label: 'Add employee',
          icon: <AddOutlined />,
          onClick: () => navigate(paths.employeeNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={search}
          onChange={setSearch}
          placeholder="Name, number, position…"
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="status-filter-label">Status</InputLabel>
          <Select
            labelId="status-filter-label"
            label="Status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value as EmployeeStatus | '');
              setPaginationModel((prev) => ({ ...prev, page: 0 }));
            }}
          >
            <MenuItem value="">
              <em>All</em>
            </MenuItem>
            {employeeStatuses.map((value) => (
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
            onRowClick={(params) => navigate(paths.employeeDetail(params.row.id))}
            sx={{ border: 'none', cursor: 'pointer' }}
          />
        )}
      </Paper>

      <ConfirmDialog
        open={!!pendingDelete}
        title="Delete employee?"
        description={
          pendingDelete
            ? `${pendingDelete.fullName} (${pendingDelete.employeeNumber}) will be removed from active records. This can be reversed by support if needed.`
            : ''
        }
        confirmLabel="Delete"
        destructive
        loading={deleteEmployee.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />

      {deleteEmployee.isError && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {(deleteEmployee.error as ApiError).message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}
