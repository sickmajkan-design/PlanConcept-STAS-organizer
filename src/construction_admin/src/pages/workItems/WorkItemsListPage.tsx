import { AddOutlined, DeleteOutlined, EditOutlined } from '@mui/icons-material';
import {
  Box,
  Chip,
  FormControlLabel,
  IconButton,
  MenuItem,
  Select,
  Stack,
  Switch,
  Tooltip,
  Typography,
} from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import type { WorkItemListQuery } from '../../api/workItems';
import type { WorkItem, WorkItemStatus } from '../../api/types';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { SearchField } from '../../components/SearchField';
import { StatusChip } from '../../components/StatusChip';
import {
  useChangeWorkItemStatus,
  useDeleteWorkItem,
  useWorkItemsQuery,
} from '../../features/workItems/useWorkItems';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { formatDate } from '../../utils/formatting';

/**
 * The moves offered per state, mirroring the API's transition table.
 *
 * Duplicated here on purpose rather than fetched: offering a move the server
 * refuses produces a 409 the user has to read to learn what the row could have
 * shown. The integration tests pin the server side; if the two ever disagree
 * the API still wins, and the worst case is a button that errors rather than a
 * move that should not have happened.
 */
const NEXT_STATES: Record<WorkItemStatus, WorkItemStatus[]> = {
  Open: ['InProgress', 'Resolved', 'Cancelled'],
  InProgress: ['Resolved', 'Open', 'Cancelled'],
  Resolved: ['Closed', 'InProgress'],
  Closed: [],
  Cancelled: [],
};

export function WorkItemsListPage() {
  const navigate = useNavigate();
  const t = useT();
  const enumLabel = useEnumLabel();
  const list = useListQueryState('dueDate');

  const [openOnly, setOpenOnly] = useState(true);
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [defectsOnly, setDefectsOnly] = useState(false);

  const query: WorkItemListQuery = useMemo(
    () => ({
      ...list.query,
      openOnly: openOnly || undefined,
      overdueOnly: overdueOnly || undefined,
      kind: defectsOnly ? 'Defect' : undefined,
    }),
    [list.query, openOnly, overdueOnly, defectsOnly],
  );

  const { data, isLoading, isError, error, refetch } = useWorkItemsQuery(query);
  const remove = useDeleteWithConfirm<WorkItem>(useDeleteWorkItem());

  const columns: GridColDef<WorkItem>[] = useMemo(
    () => [
      {
        field: 'title',
        headerName: t('workItems.itemTitle'),
        flex: 1,
        minWidth: 200,
        renderCell: (params) => (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <span>{params.row.title}</span>
            {params.row.kind === 'Defect' && (
              <Chip
                size="small"
                color="error"
                variant="outlined"
                label={enumLabel('workItemKind', 'Defect')}
              />
            )}
            {params.row.attachmentCount > 0 && (
              <Chip size="small" variant="outlined" label={params.row.attachmentCount} />
            )}
          </Stack>
        ),
      },
      {
        field: 'projectName',
        headerName: t('workItems.project'),
        flex: 1,
        minWidth: 140,
        valueGetter: (v) => v || t('workItems.noProject'),
      },
      {
        field: 'assignedEmployeeName',
        headerName: t('workItems.assignee'),
        flex: 1,
        minWidth: 150,
        sortable: false,
        valueGetter: (v) => v || t('workItems.unassigned'),
      },
      {
        field: 'dueDate',
        headerName: t('workItems.dueDate'),
        width: 140,
        renderCell: (params) => <DueCell item={params.row} />,
      },
      {
        field: 'priority',
        headerName: t('workItems.priority'),
        width: 120,
        valueGetter: (_value, row) => enumLabel('workItemPriority', row.priority),
      },
      {
        field: 'status',
        headerName: t('workItems.status'),
        width: 160,
        renderCell: (params) => (
          <StatusChip status={params.row.status} kind="workItemStatus" />
        ),
      },
      {
        field: 'actions',
        headerName: '',
        width: 190,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <StatusPicker item={params.row} />
            <Tooltip title={t('common.edit')}>
              <span>
                <IconButton
                  size="small"
                  disabled={params.row.isFinished}
                  onClick={() => navigate(paths.workItemEdit(params.row.id))}
                >
                  <EditOutlined fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
            <Tooltip title={t('common.delete')}>
              <IconButton size="small" onClick={() => remove.request(params.row)}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [enumLabel, navigate, remove, t],
  );

  return (
    <Box>
      <PageHeader
        title={t('workItems.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        action={{
          label: t('workItems.add'),
          icon: <AddOutlined />,
          onClick: () => navigate(paths.workItemNew),
        }}
      />

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ mb: 2, alignItems: { sm: 'center' }, flexWrap: 'wrap' }}
      >
        <SearchField
          value={list.search}
          onChange={list.setSearch}
          placeholder={t('workItems.searchPlaceholder')}
        />
        <FormControlLabel
          control={
            <Switch
              checked={openOnly}
              onChange={(event) => {
                setOpenOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('workItems.openOnly')}
        />
        <FormControlLabel
          control={
            <Switch
              checked={overdueOnly}
              onChange={(event) => {
                setOverdueOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('workItems.overdueOnly')}
        />
        <FormControlLabel
          control={
            <Switch
              checked={defectsOnly}
              onChange={(event) => {
                setDefectsOnly(event.target.checked);
                list.resetToFirstPage();
              }}
            />
          }
          label={t('workItems.defectsOnly')}
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
        open={!!remove.pending}
        title={t('workItems.deleteTitle')}
        description={
          remove.pending
            ? t('workItems.deleteBody', { name: remove.pending.title })
            : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Typography variant="body2" color="error" sx={{ mt: 1 }}>
          {remove.error.message}
        </Typography>
      )}
    </Box>
  );
}

/** Red once the deadline has passed and the work is still to do. */
function DueCell({ item }: { item: WorkItem }) {
  const t = useT();

  if (!item.dueDate) {
    return <span>—</span>;
  }

  const overdue =
    !item.isFinished &&
    new Date(`${item.dueDate}T00:00`).getTime() <
      new Date(new Date().toDateString()).getTime();

  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
      <span>{formatDate(item.dueDate)}</span>
      {overdue && <Chip size="small" color="error" label={t('workItems.overdue')} />}
    </Stack>
  );
}

/** Moves the item on, offering only what its current state allows. */
function StatusPicker({ item }: { item: WorkItem }) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const change = useChangeWorkItemStatus(item.id);

  const options = NEXT_STATES[item.status];

  if (options.length === 0) {
    return null;
  }

  return (
    <Select
      size="small"
      value=""
      displayEmpty
      disabled={change.isPending}
      onChange={(event) => change.mutate(event.target.value as WorkItemStatus)}
      renderValue={() => t('workItems.status')}
      sx={{ minWidth: 110 }}
    >
      {options.map((status) => (
        <MenuItem key={status} value={status}>
          {enumLabel('workItemStatus', status)}
        </MenuItem>
      ))}
    </Select>
  );
}
