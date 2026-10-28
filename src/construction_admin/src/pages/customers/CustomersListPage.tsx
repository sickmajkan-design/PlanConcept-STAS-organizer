import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
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
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { CustomerListQuery } from '../../api/customers';
import type { Customer } from '../../api/types';
import { BulkActionsBar } from '../../components/BulkActionsBar';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import { FilterBar } from '../../components/FilterBar';
import { useCustomersQuery, useDeleteCustomer } from '../../features/customers/useCustomers';
import { useBulkDelete } from '../../hooks/useBulkDelete';
import { useBulkSelection } from '../../hooks/useBulkSelection';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { CountryFlag } from '../../components/CountryFlag';
import { CountrySuggestionDialog } from '../../features/customers/CountrySuggestionDialog';
import { COUNTRIES, countryLabel } from '../../data/countries';

export function CustomersListPage() {
  const navigate = useNavigate();
  const t = useT();
  const list = useListQueryState('name', 'asc', 'customers');

  const [country, setCountry] = useState('');
  const [suggesting, setSuggesting] = useState(false);

  const query: CustomerListQuery = useMemo(() => ({ ...list.query, country: country || undefined }), [list.query, country]);
  const withoutCountry = useCustomersQuery({ pageNumber: 1, pageSize: 1, country: 'none' }).data?.totalCount ?? 0;

  const { data, isLoading, isError, error, refetch } = useCustomersQuery(query);
  const deleteCustomer = useDeleteCustomer();
  const remove = useDeleteWithConfirm<Customer>(deleteCustomer);
  const bulk = useBulkDelete(deleteCustomer);
  const selection = useBulkSelection();

  const columns: GridColDef<Customer>[] = useMemo(
    () => [
      { field: 'name', headerName: t('customers.name'), flex: 1, minWidth: 200 },
      {
        field: 'countryCode',
        headerName: t('customers.country'),
        width: 210,
        valueGetter: (v) => countryLabel(v as string | null),
        renderCell: (params) =>
          params.row.countryCode ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CountryFlag code={params.row.countryCode} />
              <span>{countryLabel(params.row.countryCode)}</span>
            </Stack>
          ) : (
            '—'
          ),
      },
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

      <FilterBar
        activeCount={country ? 1 : 0}
        search={
          <SearchField
            value={list.search}
            onChange={list.setSearch}
            placeholder={t('customers.searchPlaceholder')}
          />
        }
      >
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel id="customer-country-filter">{t('customers.country')}</InputLabel>
          <Select
            labelId="customer-country-filter"
            label={t('customers.country')}
            value={country}
            onChange={(event) => {
              setCountry(event.target.value);
              list.resetToFirstPage();
            }}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            <MenuItem value="none">{t('customers.countryNone')}</MenuItem>
            {COUNTRIES.map((c) => (
              <MenuItem key={c.code} value={c.code}>
                {c.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </FilterBar>

      {withoutCountry > 0 && (
        <Alert
          severity="warning"
          sx={{ mb: 2 }}
          action={
            <Stack direction="row" spacing={1}>
              <Button color="inherit" size="small" onClick={() => setSuggesting(true)}>
                {t('customers.suggestFromProjects')}
              </Button>
              <Button color="inherit" size="small" onClick={() => setCountry('none')}>
                {t('customers.showWithoutCountry')}
              </Button>
            </Stack>
          }
        >
          {t('customers.withoutCountry', { count: withoutCountry })}
        </Alert>
      )}

      {suggesting && <CountrySuggestionDialog onClose={() => setSuggesting(false)} />}

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

      <BulkActionsBar
        count={selection.count}
        onDelete={() => bulk.request(selection.selectedIds)}
        onClear={selection.clear}
      />

      <ResourceDataGrid
        filtered={list.hasFilters}
        onClearFilters={list.clearFilters}
        empty={{ title: t('customers.emptyTitle'), action: { label: t('customers.add'), onClick: () => navigate(paths.customerNew) } }}
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
        rowSelectionModel={selection.model}
        onRowSelectionModelChange={selection.setModel}
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

      <ConfirmDialog
        open={!!bulk.pendingIds}
        title={t('bulk.deleteConfirmTitle')}
        description={
          bulk.pendingIds ? t('bulk.deleteConfirmBody', { count: bulk.pendingIds.length }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        onConfirm={() => {
          void bulk.confirm();
          selection.clear();
        }}
        onCancel={bulk.cancel}
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
