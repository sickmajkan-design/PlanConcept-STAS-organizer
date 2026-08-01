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

import type { EmployeeListQuery } from '../../api/employees';
import type { Employee, EmployeeStatus } from '../../api/types';
import { employeeStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteEmployee, useEmployeesQuery } from '../../features/employees/useEmployees';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { formatDate, humanizeEnum } from '../../utils/formatting';

export function EmployeesListPage() {
  const navigate = useNavigate();
  const list = useListQueryState<EmployeeStatus>('lastName');

  const query: EmployeeListQuery = useMemo(
    () => ({ ...list.query, status: list.filter || undefined }),
    [list.query, list.filter],
  );

  const { data, isLoading, isError, error, refetch } = useEmployeesQuery(query);
  const remove = useDeleteWithConfirm<Employee>(useDeleteEmployee());

  // Memoized: DataGrid treats a new columns array as a structural change on
  // every render, which is wasted work.
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
              <IconButton size="small" onClick={() => navigate(paths.employeeDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton size="small" onClick={() => navigate(paths.employeeEdit(params.row.id))}>
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
          value={list.search}
          onChange={list.setSearch}
          placeholder="Name, number, position…"
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="status-filter-label">Status</InputLabel>
          <Select
            labelId="status-filter-label"
            label="Status"
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as EmployeeStatus | '')}
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
        onRowClick={(row) => navigate(paths.employeeDetail(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title="Delete employee?"
        description={
          remove.pending
            ? `${remove.pending.fullName} (${remove.pending.employeeNumber}) will be removed from active records. This can be reversed by support if needed.`
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
