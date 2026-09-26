import { AddOutlined } from '@mui/icons-material';
import { Alert, Box, Card, CardContent, Chip, Stack, Tab, Tabs, Typography } from '@mui/material';
import { useState } from 'react';

import type { ArticleOrder, ArticleOrderStatus } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { ArticleOrderActions } from '../../features/articleOrders/ArticleOrderActions';
import { useArticleOrdersQuery } from '../../features/articleOrders/useArticleOrders';
import { useHighlightTarget } from '../../hooks/useHighlightTarget';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';
import { NewArticleOrderDialog } from './NewArticleOrderDialog';

type Tab = 'open' | 'done' | 'all';

const statusColor: Record<ArticleOrderStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Requested: 'warning',
  Ordered: 'info',
  InDelivery: 'info',
  Delivered: 'success',
  Rejected: 'error',
  Cancelled: 'default',
};

export function ArticleOrderStatusChip({ status }: { status: ArticleOrderStatus }) {
  const t = useT();

  return <Chip size="small" color={statusColor[status]} label={t(`articleOrders.status.${status}`)} />;
}

function OrderCard({ order, highlighted }: { order: ArticleOrder; highlighted: boolean }) {
  const t = useT();

  return (
    <Card variant="outlined" sx={highlighted ? { borderColor: 'primary.main', borderWidth: 2 } : undefined}>
      <CardContent>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 1 }}>
          <Typography sx={{ fontWeight: 700, flex: 1, minWidth: 0 }}>{order.requestedByName}</Typography>
          {order.urgent && <Chip size="small" color="error" variant="outlined" label={t('articleOrders.urgentChip')} />}
          <ArticleOrderStatusChip status={order.status} />
        </Stack>

        <Box component="ul" sx={{ m: 0, pl: 2.5 }}>
          {order.items.map((item) => (
            <li key={item.id}>
              <Typography variant="body2">
                {item.quantity} {item.unit} {item.name}
                {item.note ? ` (${item.note})` : ''}
              </Typography>
            </li>
          ))}
        </Box>

        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
          {formatDate(order.createdAt)}
          {order.projectName ? ` · ${order.projectName}` : ''}
        </Typography>
        {order.note && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {order.note}
          </Typography>
        )}
        {order.status === 'Rejected' && order.reviewNote && (
          <Alert severity="error" sx={{ mt: 1 }}>
            {order.reviewNote}
          </Alert>
        )}

        <Box sx={{ mt: 1.5 }}>
          <ArticleOrderActions order={order} />
        </Box>
      </CardContent>
    </Card>
  );
}

/**
 * Requests for articles. Anyone signed in asks; the office orders, sends and declines;
 * the person who asked confirms it arrived. Open ones first, since that is what
 * somebody opens the page to deal with.
 */
export function ArticleOrdersPage() {
  const t = useT();
  const { targetId } = useHighlightTarget();
  const [tab, setTab] = useState<Tab>('open');
  const [creating, setCreating] = useState(false);

  const query = useArticleOrdersQuery({
    pageNumber: 1,
    pageSize: 100,
    openOnly: tab === 'open' || undefined,
    status: tab === 'done' ? 'Delivered' : undefined,
  });
  const items = query.data?.items ?? [];

  return (
    <>
      <PageHeader
        title={t('articleOrders.title')}
        description={t('articleOrders.description')}
        action={{ label: t('articleOrders.new'), icon: <AddOutlined />, onClick: () => setCreating(true) }}
      />

      <Tabs value={tab} onChange={(_, value: Tab) => setTab(value)} sx={{ mb: 2 }}>
        <Tab value="open" label={t('articleOrders.tab.open')} />
        <Tab value="done" label={t('articleOrders.tab.done')} />
        <Tab value="all" label={t('articleOrders.tab.all')} />
      </Tabs>

      {query.isError ? (
        <Alert severity="error">{t('common.somethingWentWrong')}</Alert>
      ) : items.length === 0 && !query.isLoading ? (
        <Typography color="text.secondary">{t('articleOrders.empty')}</Typography>
      ) : (
        <Stack spacing={2}>
          {items.map((order) => (
            <OrderCard key={order.id} order={order} highlighted={order.id === targetId} />
          ))}
        </Stack>
      )}

      <NewArticleOrderDialog open={creating} onClose={() => setCreating(false)} />
    </>
  );
}
