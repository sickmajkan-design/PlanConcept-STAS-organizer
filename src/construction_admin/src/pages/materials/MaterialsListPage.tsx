import { AddOutlined, DeleteOutlined, EditOutlined, VisibilityOutlined } from '@mui/icons-material';
import { Box, FormControlLabel, IconButton, Paper, Stack, Switch, Tooltip, Typography } from '@mui/material';
import { DataGrid, type GridColDef, type GridPaginationModel, type GridSortModel } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { ApiError } from '../../api/apiError';
import type { MaterialListQuery } from '../../api/materials';
import type { Material } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { useDeleteMaterial, useMaterialsQuery } from '../../features/materials/useMaterials';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { paths } from '../../routes/paths';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

export function MaterialsListPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 350);
  const [warehouseOnly, setWarehouseOnly] = useState(false);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  });
  const [sortModel, setSortModel] = useState<GridSortModel>([{ field: 'name', sort: 'asc' }]);
  const [pendingDelete, setPendingDelete] = useState<Material | null>(null);

  const query: MaterialListQuery = useMemo(
    () => ({
      pageNumber: paginationModel.page + 1,
      pageSize: paginationModel.pageSize,
      search: debouncedSearch,
      unassignedOnly: warehouseOnly || undefined,
      sortBy: sortModel[0]?.field,
      sortDescending: sortModel[0]?.sort === 'desc',
    }),
    [paginationModel, debouncedSearch, warehouseOnly, sortModel],
  );

  const { data, isLoading, isError, error, refetch } = useMaterialsQuery(query);
  const deleteMaterial = useDeleteMaterial();

  const columns: GridColDef<Material>[] = useMemo(
    () => [
      { field: 'name', headerName: 'Material', flex: 1, minWidth: 180 },
      {
        field: 'quantity',
        headerName: 'Quantity',
        width: 140,
        type: 'number',
        valueGetter: (_value, row) => `${row.quantity} ${row.unit}`,
      },
      { field: 'warehouse', headerName: 'Warehouse', flex: 1, minWidth: 140, valueGetter: (v) => v || '—' },
      {
        field: 'projectName',
        headerName: 'Project',
        flex: 1,
        minWidth: 160,
        valueGetter: (v) => v || 'Warehouse stock',
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
              <IconButton size="small" onClick={() => navigate(paths.materialDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton size="small" onClick={() => navigate(paths.materialEdit(params.row.id))}>
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
      await deleteMaterial.mutateAsync(pendingDelete.id);
      setPendingDelete(null);
    } catch {
      // Error surfaced below via the mutation's own state.
    }
  };

  return (
    <Box>
      <PageHeader
        title="Materials"
        subtitle={data ? `${data.totalCount} total` : undefined}
        action={{
          label: 'Add material',
          icon: <AddOutlined />,
          onClick: () => navigate(paths.materialNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2, alignItems: { sm: 'center' } }}>
        <SearchField value={search} onChange={setSearch} placeholder="Name, warehouse…" />
        <FormControlLabel
          control={
            <Switch
              checked={warehouseOnly}
              onChange={(event) => {
                setWarehouseOnly(event.target.checked);
                setPaginationModel((prev) => ({ ...prev, page: 0 }));
              }}
            />
          }
          label="Warehouse stock only"
        />
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
            onRowClick={(params) => navigate(paths.materialDetail(params.row.id))}
            sx={{ border: 'none', cursor: 'pointer' }}
          />
        )}
      </Paper>

      <ConfirmDialog
        open={!!pendingDelete}
        title="Delete material?"
        description={pendingDelete ? `${pendingDelete.name} will be removed from active records.` : ''}
        confirmLabel="Delete"
        destructive
        loading={deleteMaterial.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />

      {deleteMaterial.isError && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {(deleteMaterial.error as ApiError).message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}
