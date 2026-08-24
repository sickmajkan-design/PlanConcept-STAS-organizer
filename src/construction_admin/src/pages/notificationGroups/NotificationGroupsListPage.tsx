import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import { IconButton, Stack, Tooltip } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { NotificationGroup } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SearchField } from '../../components/SearchField';
import {
  useDeleteNotificationGroup,
  useNotificationGroupsQuery,
} from '../../features/notificationGroups/useNotificationGroups';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

export function NotificationGroupsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const list = useListQueryState('name');
  const [pendingDelete, setPendingDelete] = useState<NotificationGroup | null>(null);

  const { data, isLoading, isError, error, refetch } = useNotificationGroupsQuery(list.query);
  const deleteGroup = useDeleteNotificationGroup();

  const confirmDelete = () => {
    if (!pendingDelete) {
      return;
    }

    deleteGroup.mutate(pendingDelete.id, { onSuccess: () => setPendingDelete(null) });
  };

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
              <IconButton
                size="small"
                color="error"
                onClick={() => setPendingDelete(params.row)}
              >
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [navigate, t],
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
      />

      <ConfirmDialog
        open={!!pendingDelete}
        title={t('notificationGroups.deleteTitle')}
        description={
          pendingDelete ? t('notificationGroups.deleteBody', { name: pendingDelete.name }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={deleteGroup.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setPendingDelete(null)}
      />
    </>
  );
}
