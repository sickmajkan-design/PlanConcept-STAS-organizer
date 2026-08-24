import { Card, CardContent, CircularProgress, Divider, Stack, Typography } from '@mui/material';

import { useAuditTrailQuery } from '../features/audit/useAudit';
import { useT } from '../i18n/useI18n';
import { formatDateTime } from '../utils/formatting';

/**
 * A record's change history, read straight from the audit trail. Admin and
 * above only — the API refuses anyone else, and this mirrors that rather than
 * showing a card that always errors for a Foreman.
 *
 * Values are shown as the audit trail stored them — a raw id or enum number,
 * not a resolved employee name or status label. Good enough to answer "did
 * this change, and when," which is what this card is for; turning a stored id
 * into a name is a separate piece of work for if it turns out to matter.
 */
export function AuditHistoryCard({
  entityName,
  entityId,
}: {
  entityName: string;
  entityId: string;
}) {
  const t = useT();
  const { data, isLoading } = useAuditTrailQuery(entityName, entityId);

  const entries = data?.items ?? [];

  return (
    <Card>
      <CardContent>
        <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 700 }}>
          {t('audit.title')}
        </Typography>

        {isLoading ? (
          <CircularProgress size={20} sx={{ mt: 1 }} />
        ) : entries.length === 0 ? (
          <Typography color="text.secondary">{t('audit.empty')}</Typography>
        ) : (
          <Stack divider={<Divider />} spacing={1.25} sx={{ mt: 1 }}>
            {entries.map((entry) => {
              const changeLines = Object.entries(entry.changes).map(
                ([field, change]) => `${field}: ${change.from ?? '—'} → ${change.to ?? '—'}`,
              );

              return (
                <Stack key={entry.id} spacing={0.25}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'baseline' }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {formatDateTime(entry.occurredAt)}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {entry.userEmail ?? t('audit.systemActor')}
                    </Typography>
                  </Stack>
                  {changeLines.length > 0 ? (
                    changeLines.map((line) => (
                      <Typography key={line} variant="caption" color="text.secondary">
                        {line}
                      </Typography>
                    ))
                  ) : (
                    <Typography variant="caption" color="text.secondary">
                      {entry.action}
                    </Typography>
                  )}
                </Stack>
              );
            })}
          </Stack>
        )}
      </CardContent>
    </Card>
  );
}
