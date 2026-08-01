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

import type { ProjectListQuery } from '../../api/projects';
import type { Project, ProjectStatus } from '../../api/types';
import { projectStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteProject, useProjectsQuery } from '../../features/projects/useProjects';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { formatDate, humanizeEnum } from '../../utils/formatting';

export function ProjectsListPage() {
  const navigate = useNavigate();
  const list = useListQueryState<ProjectStatus>('name');

  const query: ProjectListQuery = useMemo(
    () => ({ ...list.query, status: list.filter || undefined }),
    [list.query, list.filter],
  );

  const { data, isLoading, isError, error, refetch } = useProjectsQuery(query);
  const remove = useDeleteWithConfirm<Project>(useDeleteProject());

  // Memoized: DataGrid treats a new columns array as a structural change on
  // every render, which is wasted work.
  const columns: GridColDef<Project>[] = useMemo(
    () => [
      { field: 'name', headerName: 'Name', flex: 1, minWidth: 200 },
      {
        field: 'client',
        headerName: 'Client',
        flex: 1,
        minWidth: 160,
        valueGetter: (v) => v || '—',
      },
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
        title="Projects"
        subtitle={data ? `${data.totalCount} total` : undefined}
        action={{
          label: 'Add project',
          icon: <AddOutlined />,
          onClick: () => navigate(paths.projectNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder="Name, client, address…"
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="project-status-filter-label">Status</InputLabel>
          <Select
            labelId="project-status-filter-label"
            label="Status"
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as ProjectStatus | '')}
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
        onRowClick={(row) => navigate(paths.projectDetail(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title="Delete project?"
        description={
          remove.pending
            ? `${remove.pending.name} will be removed from active records. Any tools assigned only to this project will be released.`
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
