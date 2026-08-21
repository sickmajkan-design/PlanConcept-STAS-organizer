import {
  AddOutlined,
  DeleteOutlined,
  EditOutlined,
  HandymanOutlined,
  VisibilityOutlined,
} from '@mui/icons-material';
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
import { exportsApi } from '../../api/exports';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { ExportButton } from '../../components/ExportButton';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { RowActions } from '../../components/RowActions';
import { RowPhotoCell } from '../../components/RowPhotoCell';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import { StatusLegend } from '../../components/StatusLegend';
import { useDeleteTool, useToolsQuery } from '../../features/tools/useTools';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { useListQueryState } from '../../hooks/useListQueryState';
import { paths } from '../../routes/paths';

export function ToolsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState<ToolStatus>('name');

  const query: ToolListQuery = useMemo(
    () => ({ ...list.query, status: list.filter || undefined }),
    [list.query, list.filter],
  );

  const { data, isLoading, isError, error, refetch } = useToolsQuery(query);
  const remove = useDeleteWithConfirm<Tool>(useDeleteTool());

  const columns: GridColDef<Tool>[] = useMemo(
    () => [
      {
        field: 'photo',
        headerName: '',
        width: 56,
        sortable: false,
        filterable: false,
        renderCell: (params) => (
          <RowPhotoCell
            ownerType="Tool"
            ownerId={params.row.id}
            icon={<HandymanOutlined fontSize="small" />}
          />
        ),
      },
      { field: 'name', headerName: t('tools.tool'), flex: 1, minWidth: 180 },
      {
        field: 'category',
        headerName: t('tools.category'),
        flex: 1,
        minWidth: 140,
        valueGetter: (v) => v || '—',
      },
      {
        field: 'status',
        headerName: t('tools.status'),
        width: 130,
        renderCell: (params) => <StatusChip status={params.value as string} kind="toolStatus" />,
      },
      {
        field: 'assignedEmployeeName',
        headerName: t('tools.heldBy'),
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
          <RowActions>
            <Tooltip title={t('common.view')}>
              <IconButton size="small" onClick={() => navigate(paths.toolDetail(params.row.id))}>
                <VisibilityOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <IconButton size="small" onClick={() => navigate(paths.toolEdit(params.row.id))}>
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
        title={t('tools.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('tools.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.toolNew),
        }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('tools.searchPlaceholder')}
        />
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="tool-status-filter-label">{t('tools.status')}</InputLabel>
          <Select
            labelId="tool-status-filter-label"
            label={t('tools.status')}
            value={list.filter}
            onChange={(event) => list.setFilter(event.target.value as ToolStatus | '')}
          >
            <MenuItem value="">
              <em>{t('common.all')}</em>
            </MenuItem>
            {toolStatuses.map((value) => (
              <MenuItem key={value} value={value}>
                {enumLabel('toolStatus', value)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <StatusLegend kind="toolStatus" values={toolStatuses} />

        <ExportButton
          onExport={(language) =>
            exportsApi.tools({ search: list.search, status: list.filter, language })
          }
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
        onRowClick={(row) => navigate(paths.toolDetail(row.id))}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('tools.deleteTitle')}
        description={
          remove.pending ? t('tools.deleteBody', { name: remove.pending.name }) : ''
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
