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
import type { ProjectListQuery } from '../../api/projects';
import type { Project, ProjectStatus } from '../../api/types';
import { projectStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ErrorState } from '../../components/ErrorState';
import { PageHeader } from '../../components/PageHeader';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteProject, useProjectsQuery } from '../../features/projects/useProjects';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { paths } from '../../routes/paths';
import { formatDate, humanizeEnum } from '../../utils/formatting';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

export function ProjectsListPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 350);
  const [status, setStatus] = useState<ProjectStatus | ''>('');
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: 20,
  });
  const [sortModel, setSortModel] = useState<GridSortModel>([{ field: 'name', sort: 'asc' }]);
  const [pendingDelete, setPendingDelete] = useState<Project | null>(null);

  const query: ProjectListQuery = useMemo(
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

  const { data, isLoading, isError, error, refetch } = useProjectsQuery(query);
  const deleteProject = useDeleteProject();

  // Memoized: DataGrid treats a new columns array as a structural change on
  // every render, which is wasted work (and, combined with Suspense-loaded
  // routes, can trigger a spurious "setState during render" warning).
  const columns: GridColDef<Project>[] = useMemo(
    () => [
      { field: 'name', headerName: 'Name', flex: 1, minWidth: 200 },
      { field: 'client', headerName: 'Client', flex: 1, minWidth: 160, valueGetter: (v) => v || '—' },
      {
        field: 'status',
        headerName: 'Status',
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} />,
      },
      { field: 'employeeCount', headerName: 'Crew', width: 90, type: 'number' },
      {
        field: 'startDate',
        headerName: 'Start',
        width: 120,
        valueFormatter: (value: string | null) => formatDate(value),
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
              <IconButton size="small" onClick={() => navigate(paths.projectDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton size="small" onClick={() => navigate(paths.projectEdit(params.row.id))}>
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
      await deleteProject.mutateAsync(pendingDelete.id);
      setPendingDelete(null);
    } catch {
      // Error surfaced below via the mutation's own state.
    }
  };

  return (
    <Box>
      <PageHeader
        title="Projects"
        subtitle={data ? `${data.totalCount} total` : undefined}
        action={{
          label: 'Add project',
          icon: <AddOutlined />,
          onClick: () => navigate(paths.projectNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField value={search} onChange={setSearch} placeholder="Name, client, address…" />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="project-status-filter-label">Status</InputLabel>
          <Select
            labelId="project-status-filter-label"
            label="Status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value as ProjectStatus | '');
              setPaginationModel((prev) => ({ ...prev, page: 0 }));
            }}
          >
            <MenuItem value="">
              <em>All</em>
            </MenuItem>
            {projectStatuses.map((value) => (
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
            onRowClick={(params) => navigate(paths.projectDetail(params.row.id))}
            sx={{ border: 'none', cursor: 'pointer' }}
          />
        )}
      </Paper>

      <ConfirmDialog
        open={!!pendingDelete}
        title="Delete project?"
        description={
          pendingDelete
            ? `${pendingDelete.name} will be removed from active records. Any tools assigned only to this project will be released.`
            : ''
        }
        confirmLabel="Delete"
        destructive
        loading={deleteProject.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />

      {deleteProject.isError && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {(deleteProject.error as ApiError).message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}
