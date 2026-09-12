import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import { Box, IconButton, Stack, Tooltip } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';

import type { NotificationGroup } from '../../api/types';
import { BulkActionsBar } from '../../components/BulkActionsBar';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SavedViewsBar } from '../../components/SavedViewsBar';
import { SearchField } from '../../components/SearchField';
import {
  useDeleteNotificationGroup,
  useNotificationGroupsQuery,
} from '../../features/notificationGroups/useNotificationGroups';
import { useBulkDelete } from '../../hooks/useBulkDelete';
import { useBulkSelection } from '../../hooks/useBulkSelection';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

export function NotificationGroupsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const list = useListQueryState('name', 'asc', 'notification-groups');

  const { data, isLoading, isError, error, refetch } = useNotificationGroupsQuery(list.query);
  const deleteGroup = useDeleteNotificationGroup();
  const remove = useDeleteWithConfirm<NotificationGroup>(deleteGroup);
  const bulk = useBulkDelete(deleteGroup);
  const selection = useBulkSelection();

  const columns: GridColDef<NotificationGroup>[] = useMemo(
    () => [
      { field: 'name', headerName: t('notificationGroups.name'), flex: 1, minWidth: 220 },
      {
        field: 'memberCount',
        headerName: t('notificationGroups.members'),
        width: 140,
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
          <Stack direction="row" spacing={0.5}>
            <Tooltip title={t('common.edit')}>
              <IconButton
                size="small"
                onClick={() => navigate(paths.notificationGroupEdit(params.row.id))}
              >
                <EditOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title={t('common.delete')}>
              <IconButton size="small" color="error" onClick={() => remove.request(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [navigate, remove, t],
  );

  return (
    <>
      <PageHeader
        title={t('notificationGroups.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('notificationGroups.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.notificationGroupNew),
        }}
      />

      <Stack sx={{ mb: 2 }}>
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('notificationGroups.searchPlaceholder')}
        />
      </Stack>

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
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('notificationGroups.deleteTitle')}
        description={
          remove.pending ? t('notificationGroups.deleteBody', { name: remove.pending.name }) : ''
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
    </>
  );
}
