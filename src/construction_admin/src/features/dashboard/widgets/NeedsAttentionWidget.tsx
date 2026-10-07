import { useQuery } from '@tanstack/react-query';
import { Box, Chip, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { attentionApi, type AttentionGroup, type AttentionItem } from '../../../api/attention';
import type { MessageKey } from '../../../i18n/en';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import { useExpiringDocumentsQuery } from '../../attachments/useAttachments';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';

const DOCUMENT_WINDOW_DAYS = 30;

/** Where each kind of thing is dealt with. */
const DESTINATIONS: Record<string, string> = {
  absenceRequests: paths.absences,
  expensesToReview: paths.vehicleExpenses,
  timeEntriesToReview: paths.timeEntries,
  unconfirmedPostings: paths.schedule,
  articleOrders: paths.articleOrders,
  refunds: paths.refunds,
  dkvRows: paths.fuelReconciliation,
  certificatesExpiring: paths.employees,
  vehicleDates: paths.vehicles,
  expiringDocuments: paths.expiringDocuments,
  dataQuality: paths.dataQuality,
};

/** The order they are shown in: what somebody is waiting on first, housekeeping last. */
const ORDER = [
  'absenceRequests',
  'timeEntriesToReview',
  'expensesToReview',
  'articleOrders',
  'refunds',
  'dkvRows',
  'unconfirmedPostings',
  'vehicleDates',
  'expiringDocuments',
  'certificatesExpiring',
  'dataQuality',
];

/**
 * One list of what is waiting for this person, across the platform and limited to what their role can
 * act on: requests to answer, costs to review, statements that do not match, dates about to pass.
 * Each line is a kind of work with how much of it there is, and a click goes to where it is done.
 *
 * Expiring documents come from their own query, as they always have, and are folded into the same list.
 * A failure of either source leaves the other showing instead of emptying the whole widget.
 */
export function NeedsAttentionWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();

  const attention = useQuery({ queryKey: ['attention'], queryFn: attentionApi.get, refetchInterval: 60_000 });
  const documents = useExpiringDocumentsQuery(DOCUMENT_WINDOW_DAYS);

  const documentList = Array.isArray(documents.data) ? documents.data : [];
  const groups: AttentionGroup[] = [...(attention.data?.groups ?? [])];

  if (documentList.length > 0) {
    groups.push({
      key: 'expiringDocuments',
      count: documentList.length,
      items: documentList.slice(0, 3).map((d) => ({ label: d.fileName, kind: null, date: d.expiresAt ?? null })),
    });
  }

  groups.sort((a, b) => ORDER.indexOf(a.key) - ORDER.indexOf(b.key));

  const isLoading = attention.isLoading || documents.isLoading;
  const bothFailed = !!attention.error && !!documents.error;
  const isEmpty = !isLoading && !bothFailed && groups.length === 0;

  const example = (group: AttentionGroup, item: AttentionItem): string => {
    const date = item.date ? formatDate(item.date) : '';

    if (group.key === 'vehicleDates' && item.kind) {
      return `${item.label} · ${t(`attention.vehicleDate.${item.kind}` as MessageKey)} ${date}`;
    }

    if (group.key === 'expiringDocuments') return `${item.label} · ${t('dashboard.needsAttention.documentExpires', { date })}`;
    return date ? `${item.label} · ${date}` : item.label;
  };

  return (
    <WidgetShell
      title={t('dashboard.widget.NeedsAttention')}
      isLoading={isLoading}
      error={bothFailed ? attention.error : undefined}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
    >
      {isEmpty ? (
        <Typography color="text.secondary" variant="body2">
          {t('dashboard.needsAttention.empty')}
        </Typography>
      ) : (
        <Stack spacing={0.5} sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
          {groups.map((group) => (
            <Box
              key={group.key}
              component={Link}
              to={DESTINATIONS[group.key] ?? paths.home}
              sx={{ display: 'block', color: 'inherit', textDecoration: 'none', borderRadius: 1.5, p: 1, '&:hover': { bgcolor: 'action.hover' } }}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Typography variant="body2" sx={{ fontWeight: 700, flex: 1, minWidth: 0 }}>
                  {t(`attention.${group.key}` as MessageKey)}
                </Typography>
                <Chip size="small" color="warning" label={group.count} sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }} />
              </Stack>
              {group.items.map((item, i) => (
                <Typography key={i} variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                  {example(group, item)}
                </Typography>
              ))}
            </Box>
          ))}
        </Stack>
      )}
    </WidgetShell>
  );
}
