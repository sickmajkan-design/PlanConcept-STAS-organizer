import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import { Box, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';

import type { CustomerListQuery } from '../../api/customers';
import type { Customer } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { SearchField } from '../../components/SearchField';
import { useCustomersQuery, useDeleteCustomer } from '../../features/customers/useCustomers';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';

export function CustomersListPage() {
  const navigate = useNavigate();
  const t = useT();
  const list = useListQueryState('name');

  const query: CustomerListQuery = list.query;

  const { data, isLoading, isError, error, refetch } = useCustomersQuery(query);
  const remove = useDeleteWithConfirm<Customer>(useDeleteCustomer());

  const columns: GridColDef<Customer>[] = useMemo(
    () => [
      { field: 'name', headerName: t('customers.name'), flex: 1, minWidth: 200 },
      {
        field: 'contactPerson',
        headerName: t('customers.contactPerson'),
        flex: 1,
        minWidth: 160,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'phone',
        headerName: t('customers.phone'),
        width: 150,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'email',
        headerName: t('customers.email'),
        flex: 1,
        minWidth: 180,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'projectCount',
        headerName: t('customers.projectCount'),
        width: 110,
        type: 'number',
      },
      {
        field: 'actions',
        headerName: '',
        width: 100,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <RowActions>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.customerEdit(params.row.id))}>
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
        title={t('customers.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('customers.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.customerNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('customers.searchPlaceholder')}
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
        onRowClick={(row) => navigate(paths.customerEdit(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('customers.deleteTitle')}
        description={
          remove.pending ? t('customers.deleteBody', { name: remove.pending.name }) : ''
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
