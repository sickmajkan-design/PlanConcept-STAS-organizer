import { Box, Stack, Typography } from '@mui/material';

import { ArticleOrderActions } from '../../articleOrders/ArticleOrderActions';
import { useArticleOrdersQuery } from '../../articleOrders/useArticleOrders';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, MoreLink, Pill } from './widgetUi';

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
      <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', gap: 1, mb: 1.5 }}>
        {(['Requested', 'Ordered', 'InDelivery'] as const).map((status) => (
          <Pill key={status} tone={status === 'Requested' && count(status) > 0 ? 'warn' : 'neutral'} sx={{ fontSize: '0.85rem', px: 1.5, py: 0.25 }}>
            {`${t(`articleOrders.status.${status}`)}: ${count(status)}`}
          </Pill>
        ))}
      </Stack>

      {shown.length === 0 && !query.isLoading ? (
        <EmptyNote>{t('dashboard.articleOrders.empty')}</EmptyNote>
      ) : (
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
