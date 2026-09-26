import { AddOutlined } from '@mui/icons-material';
import { Alert, Box, FormControlLabel, Stack, Switch, Tooltip, Typography } from '@mui/material';
import type { GridColDef } from '@mui/x-data-grid';
import { useMemo, useState } from 'react';

import type { ArticleOrder } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { ResourceDataGrid } from '../../components/ResourceDataGrid';
import { StatusChip } from '../../components/StatusChip';
import { ArticleOrderActions } from '../../features/articleOrders/ArticleOrderActions';
import { useArticleOrdersQuery } from '../../features/articleOrders/useArticleOrders';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useListQueryState } from '../../hooks/useListQueryState';
import { useOpenOnParam } from '../../hooks/useOpenOnParam';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { NewArticleOrderDialog } from './NewArticleOrderDialog';

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
      { field: 'projectName', headerName: t('articleOrders.project'), width: 170, sortable: false, valueGetter: (value) => value ?? '—' },
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
      />

      <NewArticleOrderDialog open={creating} onClose={() => setCreating(false)} />
    </Box>
  );
}
