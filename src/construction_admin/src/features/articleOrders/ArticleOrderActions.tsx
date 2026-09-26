import { Button, Stack } from '@mui/material';
import { useState } from 'react';

import type { ArticleOrder, ArticleOrderStatus } from '../../api/types';
import { ReasonDialog } from '../../components/ReasonDialog';
import { useAuth } from '../../auth/useAuth';
import { canManageArticleOrders } from '../../auth/authHelpers';
import { useT } from '../../i18n/useI18n';
import { useSetArticleOrderStatus } from './useArticleOrders';

/**
 * The one step this account may take on a request, and declining or withdrawing it.
 * The office orders and sends; the person who asked confirms it arrived (the office
 * may too); the person who asked may withdraw it until it is ordered. Shared by the
 * page and the dashboard widget so the two never offer different things.
 */
export function ArticleOrderActions({ order, compact = false }: { order: ArticleOrder; compact?: boolean }) {
  const t = useT();
  const { user } = useAuth();
  const setStatus = useSetArticleOrderStatus();
  const [declining, setDeclining] = useState(false);

  const manages = canManageArticleOrders(user);
  const own = !!user && order.requestedByUserId === user.id;
  const size = compact ? 'small' : 'medium';

  const move = (status: ArticleOrderStatus) => setStatus.mutate({ id: order.id, status });

  return (
    <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
      {manages && order.status === 'Requested' && (
        <Button size={size} variant="contained" disabled={setStatus.isPending} onClick={() => move('Ordered')}>
          {t('articleOrders.action.order')}
        </Button>
      )}
      {manages && order.status === 'Ordered' && (
        <Button size={size} variant="contained" disabled={setStatus.isPending} onClick={() => move('InDelivery')}>
          {t('articleOrders.action.ship')}
        </Button>
      )}
      {(own || manages) && order.status === 'InDelivery' && (
        <Button size={size} variant="contained" disabled={setStatus.isPending} onClick={() => move('Delivered')}>
          {own ? t('articleOrders.action.received') : t('articleOrders.action.delivered')}
        </Button>
      )}
      {manages && (order.status === 'Requested' || order.status === 'Ordered') && (
        <Button size={size} color="error" disabled={setStatus.isPending} onClick={() => setDeclining(true)}>
          {t('articleOrders.action.decline')}
        </Button>
      )}
      {own && order.status === 'Requested' && (
        <Button size={size} disabled={setStatus.isPending} onClick={() => move('Cancelled')}>
          {t('articleOrders.action.withdraw')}
        </Button>
      )}

      <ReasonDialog
        open={declining}
        title={t('articleOrders.declineTitle')}
        hint={t('articleOrders.declineHint')}
        label={t('articleOrders.declineReason')}
        submitLabel={t('articleOrders.action.decline')}
        onClose={() => setDeclining(false)}
        onSubmit={async (note) => {
          await setStatus.mutateAsync({ id: order.id, status: 'Rejected', note });
          setDeclining(false);
        }}
      />
    </Stack>
  );
}
