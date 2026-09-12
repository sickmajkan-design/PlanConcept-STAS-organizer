import {
  AddOutlined,
  DeleteOutlined,
  EditOutlined,
  VisibilityOutlined,
  WarningAmberOutlined,
} from '@mui/icons-material';
import {
  Box,
  FormControlLabel,
  IconButton,
  Stack,
  Switch,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef, GridSortModel } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { MaterialListQuery } from '../../api/materials';
import type { Material } from '../../api/types';
import { BulkActionsBar } from '../../components/BulkActionsBar';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import { useDeleteMaterial, useMaterialsQuery } from '../../features/materials/useMaterials';
import { useBulkDelete } from '../../hooks/useBulkDelete';
import { useBulkSelection } from '../../hooks/useBulkSelection';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useI18n, useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useSavedViews } from '../../hooks/useSavedViews';
import { paths } from '../../routes/paths';
import { formatMoney } from '../../utils/formatting';

interface MaterialViewState {
  search: string;
  sortModel: GridSortModel;
  warehouseOnly: boolean;
  incompleteOnly: boolean;
}

export function MaterialsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const { locale } = useI18n();
  const list = useListQueryState('name');

  // Materials filter on a boolean rather than a status enum, so this one keeps
  // its own state instead of using the hook's single-select filter.
  const [warehouseOnly, setWarehouseOnly] = useState(false);
  const [incompleteOnly, setIncompleteOnly] = useState(false);

  const savedViews = useSavedViews<MaterialViewState>('materials');

  const applyView = (state: MaterialViewState) => {
    list.setSearch(state.search);
    list.setSortModel(state.sortModel);
    setWarehouseOnly(state.warehouseOnly);
    setIncompleteOnly(state.incompleteOnly);
    list.resetToFirstPage();
  };

  const saveCurrentView = (name: string) => {
    savedViews.saveView(name, {
      search: list.search,
      sortModel: list.sortModel,
      warehouseOnly,
      incompleteOnly,
    });
  };

  const query: MaterialListQuery = useMemo(
    () => ({
      ...list.query,
      unassignedOnly: warehouseOnly || undefined,
      incompleteOnly: incompleteOnly || undefined,
    }),
    [list.query, warehouseOnly, incompleteOnly],
  );

  const { data, isLoading, isError, error, refetch } = useMaterialsQuery(query);
  const deleteMaterial = useDeleteMaterial();
  const remove = useDeleteWithConfirm<Material>(deleteMaterial);
  const bulk = useBulkDelete(deleteMaterial);
  const selection = useBulkSelection();

  const columns: GridColDef<Material>[] = useMemo(
    () => [
      {
        field: 'name',
        headerName: t('materials.name'),
        flex: 1,
        minWidth: 180,
        renderCell: (params) => (
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <span>{params.row.name}</span>
            {params.row.unitPrice === null && (
              <Tooltip title={t('materials.incompleteHint')}>
                <WarningAmberOutlined fontSize="small" color="warning" />
              </Tooltip>
            )}
          </Stack>
        ),
      },
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
        field: 'unitPrice',
        headerName: t('materials.unitPrice'),
        width: 130,
        type: 'number',
        valueGetter: (_value, row) =>
          row.unitPrice === null ? '—' : formatMoney(row.unitPrice, locale),
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
    [navigate, remove, t, locale],
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
        <FormControlLabel
          control={
            <Switch
              checked={incompleteOnly}
              onChange={(event) => {
                setIncompleteOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('materials.incompleteOnly')}
        />
      </Stack>

      <Box sx={{ mb: 2 }}>
        <SavedViewsBar
          views={savedViews.views}
          onApply={applyView}
          onSave={saveCurrentView}
          onDelete={savedViews.deleteView}
        />
      </Box>

      <BulkActionsBar
        count={selection.count}
        onDelete={() => bulk.request(selection.selectedIds)}
        onClear={selection.clear}
      />

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
        rowSelectionModel={selection.model}
        onRowSelectionModelChange={selection.setModel}
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
