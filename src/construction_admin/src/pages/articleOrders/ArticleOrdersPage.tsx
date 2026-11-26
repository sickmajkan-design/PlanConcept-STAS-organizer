import { AddOutlined } from '@mui/icons-material';
import { Alert, Box, Chip, FormControlLabel, Stack, Switch, Tooltip, Typography } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import type { ArticleOrder } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { RecordPreviewDialog } from '../../components/RecordPreviewDialog';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { ArticleOrderActions } from '../../features/articleOrders/ArticleOrderActions';
import { useArticleOrdersQuery } from '../../features/articleOrders/useArticleOrders';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useOpenOnParam } from '../../hooks/useOpenOnParam';
import { useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime } from '../../utils/formatting';
import { NewArticleOrderDialog } from './NewArticleOrderDialog';
import { ProjectLabel } from '../../components/ProjectLabel';

/** "1 par Radne cipele (broj 43), 2 kom Sljem": what was asked for, on one line. */
function itemsSummary(order: ArticleOrder): string {
  return order.items
    .map((item) => {
      const detail = [item.quantity, item.unit].filter((part) => part !== null && part !== '').join(' ');

      return `${detail} ${item.name}${item.note ? ` (${item.note})` : ''}`.trim();
    })
    .join(', ');
}

/**
 * Requests for articles. Anyone signed in asks; the office orders, sends and declines;
 * the person who asked confirms it arrived. Laid out like every other list in the panel:
 * a header, the one filter people open it for, and the table.
 */
export function ArticleOrdersPage() {
  const t = useT();
  const list = useListQueryState('createdAt', 'desc');
  const { targetId, isHighlighted } = useHighlightTarget();

  // The one question the page is opened to answer: what is still on its way.
  const [openOnly, setOpenOnly] = useState(true);
  const [creating, setCreating] = useState(false);
  const [previewingId, setPreviewingId] = useState<string | null>(null);
  useOpenOnParam('new', () => setCreating(true));

  const query = useMemo(
    () => ({
      ...list.query,
      // The API has no text search on this collection.
      search: undefined,
      openOnly: openOnly || undefined,
    }),
    [list.query, openOnly],
  );

  const { data, isLoading, isError, error, refetch } = useArticleOrdersQuery(query);

  // Looked up in the list each time, so a step taken in the preview shows in it at once.
  const previewing = data?.items.find((o) => o.id === previewingId) ?? null;

  const columns: GridColDef<ArticleOrder>[] = useMemo(
    () => [
      { field: 'requestedByName', headerName: t('articleOrders.requestedBy'), flex: 1, minWidth: 160, sortable: false },
      {
        field: 'items',
        headerName: t('articleOrders.items'),
        flex: 2,
        minWidth: 220,
        sortable: false,
        renderCell: (params) => (
          <Tooltip title={itemsSummary(params.row)}>
            <Typography variant="body2" noWrap>
              {itemsSummary(params.row)}
              {params.row.urgent ? ` · ${t('articleOrders.urgentChip')}` : ''}
            </Typography>
          </Tooltip>
        ),
      },
      { field: 'projectName', headerName: t('articleOrders.project'), width: 170, sortable: false, valueGetter: (value) => value ?? '—', renderCell: ({ row, value }) => (row.projectName ? <ProjectLabel name={row.projectName} projectId={row.projectId} variant="stacked" noWrap /> : value) },
      { field: 'createdAt', headerName: t('articleOrders.asked'), width: 120, valueGetter: (value) => formatDate(value) },
      {
        field: 'status',
        headerName: t('articleOrders.statusColumn'),
        width: 150,
        renderCell: (params) => <StatusChip status={params.row.status} kind="articleOrderStatus" />,
      },
      {
        field: 'actions',
        headerName: '',
        width: 330,
        sortable: false,
        filterable: false,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => <ArticleOrderActions order={params.row} compact />,
      },
    ],
    [t],
  );

  return (
    <Box>
      <PageHeader
        title={t('articleOrders.title')}
        subtitle={data ? t('common.total', { count: data.totalCount }) : undefined}
        description={t('articleOrders.description')}
        action={{ label: t('articleOrders.new'), icon: <AddOutlined />, onClick: () => setCreating(true) }}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} useFlexGap sx={{ flexWrap: 'wrap', mb: 2, alignItems: { sm: 'center' } }}>
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
          label={t('articleOrders.openOnly')}
        />
      </Stack>

      {isError && !data ? (
        <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
      ) : null}

      <ResourceDataGrid
        empty={
          openOnly
            ? { title: t('articleOrders.noneOpen'), hint: t('articleOrders.noneOpenHint'), action: { label: t('empty.showAll'), onClick: () => setOpenOnly(false) } }
            : { title: t('articleOrders.emptyTitle'), action: { label: t('articleOrders.new'), onClick: () => setCreating(true) } }
        }
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
        highlightedId={targetId && isHighlighted(targetId) ? targetId : null}
        onRowDoubleClick={(row) => setPreviewingId(row.id)}
      />

      <RecordPreviewDialog
        open={!!previewing}
        title={t('preview.orderTitle')}
        heading={previewing?.requestedByName}
        status={
          previewing && (
            <Stack direction="row" spacing={1}>
              {previewing.urgent && <Chip size="small" color="error" label={t('articleOrders.urgentChip')} />}
              <StatusChip status={previewing.status} kind="articleOrderStatus" />
            </Stack>
          )
        }
        fields={
          previewing
            ? [
                { label: t('articleOrders.project'), value: previewing.projectName },
                { label: t('articleOrders.note'), value: previewing.note },
                { label: t('preview.decisionNote'), value: previewing.reviewNote },
                { label: t('preview.created'), value: formatDateTime(previewing.createdAt) },
                { label: t('preview.handledBy'), value: previewing.handledByName },
                { label: t('preview.ordered'), value: previewing.orderedAt && formatDateTime(previewing.orderedAt) },
                { label: t('preview.shipped'), value: previewing.shippedAt && formatDateTime(previewing.shippedAt) },
                { label: t('preview.delivered'), value: previewing.deliveredAt && formatDateTime(previewing.deliveredAt) },
              ]
            : []
        }
        actions={previewing && <ArticleOrderActions order={previewing} />}
        onClose={() => setPreviewingId(null)}
      >
        {previewing && (
          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('preview.itemsHeading')}
            </Typography>
            {previewing.items.map((item) => (
              <Typography key={item.id} sx={{ overflowWrap: 'anywhere' }}>
                {[item.quantity, item.unit].filter((part) => part !== null && part !== '').join(' ')} {item.name}
                {item.note ? ` (${item.note})` : ''}
              </Typography>
            ))}
          </Box>
        )}
      </RecordPreviewDialog>

      <NewArticleOrderDialog open={creating} onClose={() => setCreating(false)} />
    </Box>
  );
}
