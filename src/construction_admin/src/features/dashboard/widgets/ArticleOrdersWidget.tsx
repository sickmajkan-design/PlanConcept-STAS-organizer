import { Box, Button, Chip, Divider, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { ArticleOrderActions } from '../../articleOrders/ArticleOrderActions';
import { useArticleOrdersQuery } from '../../articleOrders/useArticleOrders';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const SHOWN = 5;

/**
 * Requests for articles that are not yet in anybody's hands: how many wait for each step,
 * and the newest ones with the next step one press away. The same query and the same
 * actions as the page, so a step taken here shows there at once.
 */
export function ArticleOrdersWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  const query = useArticleOrdersQuery({ pageNumber: 1, pageSize: 100, openOnly: true });
  const orders = query.data?.items ?? [];

  const count = (status: string) => orders.filter((order) => order.status === status).length;
  const shown = orders.slice(0, SHOWN);

  return (
    <WidgetShell
      title={t('dashboard.widget.ArticleOrders')}
      isLoading={query.isLoading}
      error={query.error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1 }}>
        {(['Requested', 'Ordered', 'InDelivery'] as const).map((status) => (
          <Chip
            key={status}
            size="small"
            color={status === 'Requested' && count(status) > 0 ? 'warning' : 'default'}
            label={`${t(`articleOrders.status.${status}`)}: ${count(status)}`}
          />
        ))}
      </Stack>

      {shown.length === 0 && !query.isLoading ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.articleOrders.empty')}
        </Typography>
      ) : (
        <Stack spacing={1} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {shown.map((order, index) => (
            <Box key={order.id}>
              {index > 0 && <Divider sx={{ mb: 1 }} />}
              <Typography variant="body2" sx={{ fontWeight: 700 }} noWrap>
                {order.requestedByName}
                {order.urgent ? ` · ${t('articleOrders.urgentChip')}` : ''}
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                {order.items.map((item) => `${item.quantity} ${item.unit ?? ''} ${item.name}`.replace('  ', ' ')).join(', ')}
              </Typography>
              <ArticleOrderActions order={order} compact />
            </Box>
          ))}
        </Stack>
      )}

      <Typography variant="body2" sx={{ flexShrink: 0, mt: 1 }}>
        <Button component={Link} to={paths.articleOrders} size="small">
          {t('common.viewAll')}
        </Button>
      </Typography>
    </WidgetShell>
  );
}
