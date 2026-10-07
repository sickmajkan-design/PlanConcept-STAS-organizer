import { Alert, Box, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Button, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { planningApi, type PlanningHistoryEntry } from '../../api/planning';
import { useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime } from '../../utils/formatting';

/** "10.03. – 14.03." for the days a posting covers, open-ended when there is no end. */
function days(entry: PlanningHistoryEntry): string {
  const from = entry.changes.StartDate?.to ?? entry.startDate;
  const to = entry.changes.EndDate?.to ?? entry.endDate;
  if (!from) return '';
  return `${formatDate(from)} \u2013 ${to ? formatDate(to) : '\u2026'}`;
}

function describe(entry: PlanningHistoryEntry, t: ReturnType<typeof useT>): string {
  const who = entry.employeeName ?? t('planning.history.someone');
  const site = entry.projectName ?? t('planning.history.aSite');
  if (entry.action === 'Created') return t('planning.history.created', { name: who, site, days: days(entry) });
  if (entry.action === 'Deleted') return t('planning.history.deleted', { name: who, site, days: days(entry) });

  const parts = (['StartDate', 'EndDate'] as const)
    .filter((key) => entry.changes[key])
    .map((key) => {
      const change = entry.changes[key];
      const label = t(key === 'StartDate' ? 'planning.history.start' : 'planning.history.end');
      return `${label}: ${change.from ? formatDate(change.from) : '\u2026'} \u2192 ${change.to ? formatDate(change.to) : '\u2026'}`;
    });
  return t('planning.history.updated', { name: who, site, change: parts.join(', ') });
}

/** Who changed the schedule and when, newest first, read from the audit trail. */
export function HistoryDialog({ onClose }: { onClose: () => void }) {
  const t = useT();
  const query = useQuery({ queryKey: ['planning', 'history'], queryFn: () => planningApi.history({ take: 100 }) });

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('planning.history.title')}</DialogTitle>
      <DialogContent dividers>
        {query.isLoading && (
          <Box sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
            <CircularProgress />
          </Box>
        )}
        {query.error && <Alert severity="error">{t('planning.history.failed')}</Alert>}
        {query.data && query.data.length === 0 && <Typography color="text.secondary">{t('planning.history.empty')}</Typography>}
        <Stack spacing={1.5}>
          {query.data?.map((entry) => (
            <Box key={entry.id}>
              <Typography variant="body2">{describe(entry, t)}</Typography>
              <Typography variant="caption" color="text.secondary">
                {formatDateTime(entry.occurredAt)} \u00b7 {entry.byEmail ?? t('planning.history.system')}
              </Typography>
            </Box>
          ))}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
