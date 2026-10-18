import { Box, Stack } from '@mui/material';

import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { daysUntil, formatDate } from '../../../utils/formatting';
import { useExpiringDocumentsQuery } from '../../attachments/useAttachments';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { InsetList, InsetRow, MoreLink } from './widgetUi';

const WITHIN_DAYS = 30;
const SHOWN = 8;

/**
 * A week or less left reads as urgent (warning), everything else as informational. Counted in calendar days from the
 * local date: the expiry is a date, not a moment, and as one it is midnight UTC, which would make the count
 * come out a day short or long depending on the hour.
 */
function isUrgent(expiresAt: string | null): boolean {
  if (!expiresAt) return false;
  return daysUntil(expiresAt) <= 7;
}

export function DocumentExpiryWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();

  // The shared query: the same request and key as the "Needs attention" list and the menu's badge, narrowed to the
  // chosen business unit, and dropped the instant a document is renewed or deleted.
  const { data, isLoading, error } = useExpiringDocumentsQuery(WITHIN_DAYS);

  const items = (Array.isArray(data) ? [...data] : [])
    .sort((a, b) => (a.expiresAt ?? '').localeCompare(b.expiresAt ?? ''))
    .slice(0, SHOWN);

  return (
    <WidgetShell
      title={t('dashboard.widget.DocumentExpiry')}
      isLoading={isLoading}
      error={error}
      onRemove={onRemove}
      onExpandWidth={onExpandWidth}
      empty={items.length === 0 ? { text: t('dashboard.documentExpiry.empty') } : undefined}
    >
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        {items.length > 0 && (
          <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
            <InsetList>
              {items.map((attachment) => {
                const urgent = isUrgent(attachment.expiresAt);

                return (
                  <InsetRow
                    key={attachment.id}
                    tone={urgent ? 'bad' : 'warn'}
                    primary={attachment.fileName}
                    secondary={t('dashboard.documentExpiry.expiresOn', { date: formatDate(attachment.expiresAt) })}
                  />
                );
              })}
            </InsetList>
          </Box>
        )}
        <MoreLink to={paths.expiringDocuments}>{t('common.viewAll')}</MoreLink>
      </Stack>
    </WidgetShell>
  );
}
