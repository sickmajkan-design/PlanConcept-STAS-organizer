import { Box, useMediaQuery, useTheme } from '@mui/material';

import type { AttentionGroup, AttentionItem } from '../../../api/attention';
import type { MessageKey } from '../../../i18n/en';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import { useExpiringDocumentsQuery } from '../../attachments/useAttachments';
import { useAttentionQuery } from '../useAttention';
import { useSinceYesterday } from '../useSinceYesterday';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { InsetList, InsetRow } from './widgetUi';

export const DOCUMENT_WINDOW_DAYS = 30;

/** How many kinds the phone's "Today" card already lists, so the widget under it does not list them again. */
export const SUMMARY_GROUPS = 3;

/** What stops people, or is already late: shown in red rather than amber. */
const URGENT = new Set(['virusScanner', 'lateInvoices', 'lateCosts']);

/** Where each kind of thing is dealt with. */
export const DESTINATIONS: Record<string, string> = {
  virusScanner: paths.help,
  absenceRequests: paths.absences,
  expensesToReview: paths.vehicleExpenses,
  timeEntriesToReview: paths.timeEntries,
  unconfirmedPostings: paths.schedule,
  articleOrders: paths.articleOrders,
  refunds: paths.refunds,
  dkvRows: paths.fuelReconciliation,
  certificatesExpiring: paths.employees,
  vehicleDates: paths.vehicles,
  vehicleFines: paths.vehicles,
  expiringDocuments: paths.expiringDocuments,
  dataQuality: paths.dataQuality,
  lateInvoices: paths.bookReceivables,
  lateCosts: paths.bookReceivables,
};

/** The order they are shown in: what somebody is waiting on first, housekeeping last. */
export const ORDER = [
  // Nothing else on the list stops people working the way this does.
  'virusScanner',
  'absenceRequests',
  'timeEntriesToReview',
  'expensesToReview',
  'articleOrders',
  'refunds',
  'lateInvoices',
  'lateCosts',
  'dkvRows',
  'unconfirmedPostings',
  'vehicleDates',
  'vehicleFines',
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
/** What is waiting for this person, in the order it is shown, shared by the widget and the phone's "Today" summary. */
export function useAttentionGroups() {
  const attention = useAttentionQuery();
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

  // Every kind is recorded, a missing one as zero: a kind that is gone today still has to be remembered as gone,
  // or the day it comes back there would be nothing to compare it with. Nothing is recorded while a source
  // has not answered, so a failed request is never remembered as "nothing waiting".
  const counts: Record<string, number | undefined> = {};
  if (attention.data) {
    for (const key of ORDER) {
      const known = key === 'expiringDocuments' ? Array.isArray(documents.data) : true;
      counts[`attention.${key}`] = known ? (groups.find((g) => g.key === key)?.count ?? 0) : undefined;
    }
  }
  const since = useSinceYesterday(counts);
  // More of what waits is worse.
  const sinceFor = (key: string) => since(`attention.${key}`, 'down');

  return {
    groups,
    sinceFor,
    isLoading: attention.isLoading || documents.isLoading,
    bothFailed: !!attention.error && !!documents.error,
    attentionError: attention.error,
  };
}

export function NeedsAttentionWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();

  const all = useAttentionGroups();
  const { sinceFor, isLoading, bothFailed, attentionError } = all;

  // On a phone the "Today" card above already names the first few kinds, so this widget is what else is waiting.
  const alongsideSummary = useMediaQuery(useTheme().breakpoints.down('sm'));
  const groups = alongsideSummary ? all.groups.slice(SUMMARY_GROUPS) : all.groups;
  const isEmpty = !isLoading && !bothFailed && groups.length === 0;
  const coveredAbove = alongsideSummary && all.groups.length > 0;

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
      title={alongsideSummary ? t('dashboard.needsAttention.stillWaiting') : t('dashboard.widget.NeedsAttention')}
      subtitle={groups.length > 0 ? t('dashboard.needsAttention.count', { count: groups.length }) : undefined}
      isLoading={isLoading}
      error={bothFailed ? attentionError : undefined}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      empty={isEmpty ? { text: coveredAbove ? t('dashboard.needsAttention.allAbove') : t('dashboard.needsAttention.empty') } : undefined}
    >
      {!isEmpty && (
        <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto', display: 'grid', gap: 1.25, alignContent: 'start' }}>
          <InsetList>
            {groups.map((group) => (
              <InsetRow
                key={group.key}
                to={DESTINATIONS[group.key] ?? paths.home}
                tone={URGENT.has(group.key) ? 'bad' : 'warn'}
                primary={t(`attention.${group.key}` as MessageKey)}
                secondary={group.items.length > 0 ? group.items.map((item) => example(group, item)).join(' · ') : undefined}
                badge={group.count}
                since={sinceFor(group.key)}
              />
            ))}
          </InsetList>
        </Box>
      )}
    </WidgetShell>
  );
}
