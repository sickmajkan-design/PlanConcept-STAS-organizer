import { Box, Stack, Typography } from '@mui/material';

import { articleOrdersApi } from '../../../api/articleOrders';
import { ArticleOrderActions } from '../../articleOrders/ArticleOrderActions';
import { useArticleOrdersQuery } from '../../articleOrders/useArticleOrders';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { useCounts } from '../dashboardData';
import { useSinceYesterday } from '../useSinceYesterday';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { MoreLink, Pill, SinceText } from './widgetUi';

const SHOWN = 5;
const ONE = { pageNumber: 1, pageSize: 1 } as const;

/**
 * Requests for articles that are not yet in anybody's hands: how many wait for each step,
 * and the newest ones with the next step one press away. The same query and the same
 * actions as the page, so a step taken here shows there at once.
 */
export function ArticleOrdersWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();
  // The newest few, to act on; the numbers above them are the server's own counts per step, not what a page holds.
  const query = useArticleOrdersQuery({ pageNumber: 1, pageSize: SHOWN, openOnly: true });
  const shown = query.data?.items ?? [];

  const { counts, isLoading: countsLoading, error: countsError } = useCounts('orders', {
    Requested: (unit) => articleOrdersApi.list({ ...ONE, status: 'Requested', ...unit }),
    Ordered: (unit) => articleOrdersApi.list({ ...ONE, status: 'Ordered', ...unit }),
    InDelivery: (unit) => articleOrdersApi.list({ ...ONE, status: 'InDelivery', ...unit }),
  });
  const count = (status: 'Requested' | 'Ordered' | 'InDelivery') => counts[status] ?? 0;
  const open = count('Requested') + count('Ordered') + count('InDelivery');

  const since = useSinceYesterday({ 'orders.requested': counts.Requested });

  return (
    <WidgetShell
      title={t('dashboard.widget.ArticleOrders')}
      isLoading={query.isLoading || countsLoading}
      error={query.error ?? countsError}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      empty={open === 0 ? { text: t('dashboard.articleOrders.empty') } : undefined}
    >
      <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, mb: 1.5 }}>
        {(['Requested', 'Ordered', 'InDelivery'] as const).map((status) => (
          <Pill key={status} tone={status === 'Requested' && count(status) > 0 ? 'warn' : 'neutral'} sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
            {`${t(`articleOrders.status.${status}`)}: ${count(status)}`}
          </Pill>
        ))}
        <SinceText note={since('orders.requested', 'down')} />
      </Stack>

      {shown.length > 0 && (
        <Box
          sx={{
            display: 'grid',
            gap: 1.25,
            alignContent: 'start',
            flex: 1,
            minHeight: 0,
            overflow: 'auto',
            gridTemplateColumns: '1fr',
            '@container widget (min-width: 640px)': { gridTemplateColumns: '1fr 1fr' },
          }}
        >
          {shown.map((order) => (
            <Box
              key={order.id}
              sx={(theme) => ({
                borderRadius: '16px',
                px: 1.75,
                py: 1.4,
                display: 'grid',
                gap: 0.5,
                bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.05)' : 'rgba(31,27,22,0.04)',
              })}
            >
              <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600, flex: 1, minWidth: 0 }} noWrap>
                  {order.requestedByName}
                </Typography>
                {order.urgent && <Pill tone="bad">{t('articleOrders.urgentChip')}</Pill>}
              </Stack>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                {order.items.map((item) => `${item.quantity} ${item.unit ?? ''} ${item.name}`.replace('  ', ' ')).join(', ')}
              </Typography>
              <ArticleOrderActions order={order} compact />
            </Box>
          ))}
        </Box>
      )}

      <MoreLink to={paths.articleOrders}>{t('common.viewAll')}</MoreLink>
    </WidgetShell>
  );
}
