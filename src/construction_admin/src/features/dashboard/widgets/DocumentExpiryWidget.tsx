import { Box, Stack } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { attachmentsApi } from '../../../api/attachments';
import { useT } from '../../../i18n/useI18n';
import { paths } from '../../../routes/paths';
import { formatDate } from '../../../utils/formatting';
import type { DashboardWidgetProps } from '../widgetTypes';
import { WidgetShell } from './WidgetShell';
import { EmptyNote, InsetList, InsetRow, MoreLink } from './widgetUi';

const WITHIN_DAYS = 30;
const SHOWN = 8;

/** Under a week left reads as urgent (warning), everything else as informational. */
function isUrgent(expiresAt: string | null): boolean {
  if (!expiresAt) return false;
  const days = (new Date(expiresAt).getTime() - Date.now()) / 86_400_000;
  return days <= 7;
}

export function DocumentExpiryWidget({ instanceId: _instanceId, onRemove, onExpandWidth }: DashboardWidgetProps) {
  const t = useT();

  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard', 'attachments', 'expiring', WITHIN_DAYS] as const,
    queryFn: () => attachmentsApi.expiring(WITHIN_DAYS),
  });

  const items = (Array.isArray(data) ? [...data] : [])
    .sort((a, b) => (a.expiresAt ?? '').localeCompare(b.expiresAt ?? ''))
    .slice(0, SHOWN);

  return (
    <WidgetShell title={t('dashboard.widget.DocumentExpiry')} isLoading={isLoading} error={error} onRemove={onRemove} onExpandWidth={onExpandWidth}>
      <Stack spacing={1.5} sx={{ flex: 1, minHeight: 0 }}>
        {items.length === 0 ? (
          <EmptyNote>{t('dashboard.documentExpiry.empty')}</EmptyNote>
        ) : (
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
