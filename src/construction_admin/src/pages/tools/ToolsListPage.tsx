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

import type { ToolListQuery } from '../../api/tools';
import type { Tool, ToolStatus } from '../../api/types';
import { toolStatuses } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { useDeleteTool, useToolsQuery } from '../../features/tools/useTools';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';
import { humanizeEnum } from '../../utils/formatting';

export function ToolsListPage() {
  const navigate = useNavigate();
  const list = useListQueryState<ToolStatus>('name');

  const query: ToolListQuery = useMemo(
    () => ({ ...list.query, status: list.filter || undefined }),
    [list.query, list.filter],
  );

  const { data, isLoading, isError, error, refetch } = useToolsQuery(query);
  const remove = useDeleteWithConfirm<Tool>(useDeleteTool());

  const columns: GridColDef<Tool>[] = useMemo(
    () => [
      { field: 'name', headerName: 'Tool', flex: 1, minWidth: 180 },
      {
        field: 'category',
        headerName: 'Category',
        flex: 1,
        minWidth: 140,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} />,
      },
      {
        field: 'assignedEmployeeName',
        headerName: 'Held by',
        flex: 1,
        minWidth: 150,
        valueGetter: (_value, row) =>
          row.assignedEmployeeName || row.assignedProjectName || '—',
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
              <IconButton size="small" onClick={() => navigate(paths.toolDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton size="small" onClick={() => navigate(paths.toolEdit(params.row.id))}>
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
        title="Tools"
        subtitle={data ? `${data.totalCount} total` : undefined}
        action={{
          label: 'Add tool',
          icon: <AddOutlined />,
          onClick: () => navigate(paths.toolNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder="Name, category, serial number…"
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="tool-status-filter-label">Status</InputLabel>
          <Select
            labelId="tool-status-filter-label"
            label="Status"
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as ToolStatus | '')}
          >
            <MenuItem value="">
              <em>All</em>
            </MenuItem>
            {toolStatuses.map((value) => (
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
        onRowClick={(row) => navigate(paths.toolDetail(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title="Delete tool?"
        description={
          remove.pending ? `${remove.pending.name} will be removed from active records.` : ''
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
