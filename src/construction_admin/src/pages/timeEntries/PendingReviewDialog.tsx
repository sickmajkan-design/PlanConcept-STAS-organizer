import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  Typography,
} from '@mui/material';

import type { TimeEntry } from '../../api/types';
import { EmptyState } from '../../components/EmptyState';
import { ErrorState } from '../../components/ErrorState';
import { useTimeEntriesQuery } from '../../features/timeEntries/useTimeEntries';
import type { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate, toLocalDateOnly } from '../../utils/formatting';
import { TimeEntryCard } from './TimeEntryCard';

/**
 * The API refuses anything above 100 (`PagedQuery.DefaultMaxPageSize`) — and
 * a review queue with more than a hundred entries waiting is the summary
 * report's job, not a dialog's.
 */
const MAX_ENTRIES = 100;

/** What's actually wrong with an entry, beyond "it's Submitted" — the card's own status chip already says that. */
function problemTags(entry: TimeEntry, t: ReturnType<typeof useT>): string[] {
  const tags: string[] = [];
  if (entry.locationCorrect === false) tags.push(t('timeEntries.checkInWrongLocation'));
  if (entry.timeCorrect === false) tags.push(t('timeEntries.checkInWrongTime'));
  return tags;
}

interface DayGroup {
  date: string;
  entries: TimeEntry[];
}

/**
 * Every Submitted entry across every day, not just today's board — the nav
 * badge counts them regardless of date, so this is what it should actually
 * open onto. Grouped by day, each entry tagged with what specifically needs
 * fixing (a wrong check-in location or time, on top of the auto-closed flag
 * the card already shows), so a reviewer can act on all of them from one
 * place instead of paging through the day-by-day board looking for them.
 */
export function PendingReviewDialog({
  open,
  onClose,
  enumLabel,
  currentEmployeeId,
  canReview,
  canEdit,
  canDelete,
  onEdit,
  onDelete,
  onApprove,
  onReject,
}: {
  open: boolean;
  onClose: () => void;
  enumLabel: ReturnType<typeof useEnumLabel>;
  currentEmployeeId: string | null;
  canReview: boolean;
  canEdit: boolean;
  canDelete: boolean;
  onEdit: (entry: TimeEntry) => void;
  onDelete: (entry: TimeEntry) => void;
  onApprove: (entry: TimeEntry) => void;
  onReject: (entry: TimeEntry) => void;
}) {
  const t = useT();

  const { data, isLoading, isError, error, refetch } = useTimeEntriesQuery(
    {
      pageNumber: 1,
      pageSize: MAX_ENTRIES,
      sortBy: 'startedAt',
      sortDescending: false,
      status: 'Submitted',
    },
    open,
  );

  const days: DayGroup[] = [];
  for (const entry of data?.items ?? []) {
    const date = toLocalDateOnly(entry.startedAt);
    const group = days.at(-1);
    if (group?.date === date) {
      group.entries.push(entry);
    } else {
      days.push({ date, entries: [entry] });
    }
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>
        {t('timeEntries.reviewQueue')}
        {typeof data?.totalCount === 'number' && (
          <Chip size="small" label={data.totalCount} sx={{ ml: 1 }} />
        )}
      </DialogTitle>
      <DialogContent dividers sx={{ maxHeight: '70vh' }}>
        {isError && <ErrorState error={error} onRetry={() => void refetch()} />}

        {isLoading && !isError && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        )}

        {!isLoading && !isError && days.length === 0 && (
          <EmptyState message={t('timeEntries.noPendingAnywhere')} />
        )}

        {!isLoading && !isError && days.length > 0 && (
          <Stack spacing={2.5}>
            {days.map((group, index) => (
              <Box key={group.date}>
                {index > 0 && <Divider sx={{ mb: 2.5 }} />}
                <Stack
                  direction="row"
                  spacing={1} useFlexGap
                  sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 1.25 }}
                >
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {formatDate(group.date)}
                  </Typography>
                  <Chip size="small" variant="outlined" label={group.entries.length} />
                </Stack>
                <Stack spacing={1}>
                  {group.entries.map((entry) => (
                    <TimeEntryCard
                      key={entry.id}
                      entry={entry}
                      workTypeLabel={enumLabel('workType', entry.workType)}
                      canReview={canReview}
                      isOwn={currentEmployeeId != null && entry.employeeId === currentEmployeeId}
                      canEdit={canEdit}
                      canDelete={canDelete}
                      showProject
                      extraTags={problemTags(entry, t)}
                      onEdit={() => onEdit(entry)}
                      onDelete={() => onDelete(entry)}
                      onApprove={() => onApprove(entry)}
                      onReject={() => onReject(entry)}
                    />
                  ))}
                </Stack>
              </Box>
            ))}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
