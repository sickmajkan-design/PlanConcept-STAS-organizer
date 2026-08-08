import { AddOutlined, DeleteOutlined, EditOutlined, VisibilityOutlined } from '@mui/icons-material';
import {
  Box,
  FormControlLabel,
  IconButton,
  Stack,
  Switch,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { MaterialListQuery } from '../../api/materials';
import type { Material } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { SearchField } from '../../components/SearchField';
import { useDeleteMaterial, useMaterialsQuery } from '../../features/materials/useMaterials';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';

export function MaterialsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const list = useListQueryState('name');

  // Materials filter on a boolean rather than a status enum, so this one keeps
  // its own state instead of using the hook's single-select filter.
  const [warehouseOnly, setWarehouseOnly] = useState(false);

  const query: MaterialListQuery = useMemo(
    () => ({ ...list.query, unassignedOnly: warehouseOnly || undefined }),
    [list.query, warehouseOnly],
  );

  const { data, isLoading, isError, error, refetch } = useMaterialsQuery(query);
  const remove = useDeleteWithConfirm<Material>(useDeleteMaterial());

  const columns: GridColDef<Material>[] = useMemo(
    () => [
      { field: 'name', headerName: t('materials.name'), flex: 1, minWidth: 180 },
      {
        field: 'quantity',
        headerName: t('materials.quantity'),
        width: 140,
        type: 'number',
        valueGetter: (_value, row) => `${row.quantity} ${row.unit}`,
      },
      {
        field: 'warehouse',
        headerName: t('materials.warehouse'),
        flex: 1,
        minWidth: 140,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'projectName',
        headerName: t('materials.project'),
        flex: 1,
        minWidth: 160,
        valueGetter: (v) => v || t('materials.warehouseStock'),
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
              <IconButton size="small" onClick={() => navigate(paths.materialDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.materialEdit(params.row.id))}>
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
        title={t('materials.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('materials.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.materialNew),
        }}
      />

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <SearchField value={list.search} onChange={list.setSearch} placeholder={t('materials.searchPlaceholder')} />
        <FormControlLabel
          control={
            <Switch
              checked={warehouseOnly}
              onChange={(event) => {
                setWarehouseOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('materials.warehouseOnly')}
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
        onRowClick={(row) => navigate(paths.materialDetail(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('materials.deleteTitle')}
        description={
          remove.pending ? t('materials.deleteBody', { name: remove.pending.name }) : ''
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
