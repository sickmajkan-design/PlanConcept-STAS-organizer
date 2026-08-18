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
import { RowActions } from '../../components/RowActions';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import { useDeleteEmployee, useEmployeesQuery } from '../../features/employees/useEmployees';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { formatDate } from '../../utils/formatting';

export function EmployeesListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
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
      { field: 'employeeNumber', headerName: t('employees.number'), width: 110 },
      { field: 'fullName', headerName: t('employees.name'), flex: 1, minWidth: 180 },
      { field: 'position', headerName: t('employees.position'), flex: 1, minWidth: 150 },
      {
        field: 'status',
        headerName: t('employees.status'),
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} kind="employeeStatus" />,
      },
      {
        field: 'employmentDate',
        headerName: t('employees.employedOn'),
        width: 120,
        valueFormatter: (value: string) => formatDate(value),
      },
      { field: 'phone', headerName: t('employees.phone'), width: 150, valueGetter: (v) => v || '—' },
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
              <IconButton size="small" onClick={() => navigate(paths.employeeDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.employeeEdit(params.row.id))}>
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
        title={t('employees.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('employees.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.employeeNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('employees.searchPlaceholder')}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="status-filter-label">{t('employees.status')}</InputLabel>
          <Select
            labelId="status-filter-label"
            label={t('employees.status')}
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as EmployeeStatus | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {employeeStatuses.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('employeeStatus', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <StatusLegend kind="employeeStatus" values={employeeStatuses} />
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
        title={t('employees.deleteTitle')}
        description={
          remove.pending
            ? t('employees.deleteBody', {
                name: remove.pending.fullName,
                number: remove.pending.employeeNumber,
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
