import {
  DeleteOutlined,
  EditOutlined,
  HelpOutlineOutlined,
  LocationOffOutlined,
  ReportOutlined,
  ScheduleOutlined,
  TaskAltOutlined,
} from '@mui/icons-material';
import { Avatar, Box, Chip, IconButton, Paper, Stack, Tooltip, Typography } from '@mui/material';

import type { TimeEntry } from '../../api/types';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import { StatusChip } from '../../components/StatusChip';
import { formatTimeOfDay, splitMinutes } from '../../utils/formatting';
import { ReviewButtons } from './ReviewButtons';

/** Two-letter initials from a full "First Last" name, for the card avatar. */
export function employeeInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? '' : '';
  return (first + last).toUpperCase() || '?';
}

/** One of the four check-in states, or "unknown" — resolved once so the icon and the legend never drift apart. */
export type CheckInState = 'unknown' | 'bothWrong' | 'wrongLocation' | 'wrongTime' | 'bothCorrect';

export function checkInState(locationCorrect: boolean | null, timeCorrect: boolean | null): CheckInState {
  if (locationCorrect === null && timeCorrect === null) return 'unknown';

  const locationWrong = locationCorrect === false;
  const timeWrong = timeCorrect === false;

  if (locationWrong && timeWrong) return 'bothWrong';
  if (locationWrong) return 'wrongLocation';
  if (timeWrong) return 'wrongTime';
  return 'bothCorrect';
}

export const CHECK_IN_PRESENTATION: Record<
  CheckInState,
  { Icon: typeof TaskAltOutlined; color: string; bgcolor: string; labelKey: MessageKey }
> = {
  bothCorrect: {
    Icon: TaskAltOutlined,
    color: 'success.dark',
    bgcolor: 'success.light',
    labelKey: 'timeEntries.checkInBothCorrect',
  },
  wrongLocation: {
    Icon: LocationOffOutlined,
    color: 'warning.dark',
    bgcolor: 'warning.light',
    labelKey: 'timeEntries.checkInWrongLocation',
  },
  wrongTime: {
    Icon: ScheduleOutlined,
    color: 'warning.dark',
    bgcolor: 'warning.light',
    labelKey: 'timeEntries.checkInWrongTime',
  },
  bothWrong: {
    Icon: ReportOutlined,
    color: 'error.dark',
    bgcolor: 'error.light',
    labelKey: 'timeEntries.checkInBothWrong',
  },
  unknown: {
    Icon: HelpOutlineOutlined,
    color: 'text.disabled',
    bgcolor: 'action.hover',
    labelKey: 'timeEntries.checkInUnknown',
  },
};

/**
 * Whether the clock-in was at the right place and time, as a small coloured
 * badge — a bare icon reads as decoration, a filled circle reads as status.
 */
export function CheckInIcon({
  locationCorrect,
  timeCorrect,
}: {
  locationCorrect: boolean | null;
  timeCorrect: boolean | null;
}) {
  const t = useT();
  const { Icon, color, bgcolor, labelKey } = CHECK_IN_PRESENTATION[
    checkInState(locationCorrect, timeCorrect)
  ];

  return (
    <Tooltip title={t(labelKey)}>
      <Box
        sx={{
          width: 28,
          height: 28,
          borderRadius: '50%',
          bgcolor,
          color,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexShrink: 0,
        }}
      >
        <Icon fontSize="small" sx={{ color: 'inherit' }} />
      </Box>
    </Tooltip>
  );
}

/**
 * One worker's shift, styled like a small roster card — shared by the day
 * board (`TimeEntriesListPage`) and the cross-day `PendingReviewDialog`, so
 * the two never drift into showing the same entry differently.
 */
export function TimeEntryCard({
  entry,
  workTypeLabel,
  canReview,
  isOwn,
  canEdit,
  canDelete,
  onEdit,
  onDelete,
  onApprove,
  onReject,
  highlighted = false,
  /** The board groups by project already; the review queue shows it here instead. */
  showProject = false,
  /** Extra chips after the status/auto-closed ones — the review queue's "what's wrong with it" tags. */
  extraTags,
}: {
  entry: TimeEntry;
  workTypeLabel: string;
  canReview: boolean;
  isOwn: boolean;
  canEdit: boolean;
  canDelete: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onApprove: () => void;
  onReject: () => void;
  highlighted?: boolean;
  showProject?: boolean;
  extraTags?: string[];
}) {
  const t = useT();
  const locked = entry.status === 'Approved';

  const range = entry.endedAt
    ? `${formatTimeOfDay(entry.startedAt)}–${formatTimeOfDay(entry.endedAt)}`
    : `${formatTimeOfDay(entry.startedAt)}–…`;
  const worked =
    entry.workedMinutes === null
      ? t('timeEntries.running')
      : t('timeEntries.hoursShort', splitMinutes(entry.workedMinutes));

  return (
    <Paper
      ref={(element: HTMLDivElement | null) => {
        if (highlighted) element?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }}
      variant="outlined"
      sx={{
        p: 1.25,
        transition: 'background-color 1.5s ease',
        bgcolor: highlighted ? 'action.hover' : undefined,
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
        <Avatar sx={{ width: 30, height: 30, fontSize: '0.8rem' }}>
          {employeeInitials(entry.employeeName)}
        </Avatar>
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
            {entry.employeeName}
          </Typography>
          {showProject && (
            <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
              {entry.projectName ?? t('timeEntries.noProject')}
            </Typography>
          )}
          <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5, mt: 0.25 }}>
            <Chip size="small" variant="outlined" label={workTypeLabel} />
            <StatusChip status={entry.status} kind="timeEntryStatus" size="small" />
            {entry.autoClosed && (
              <Tooltip title={t('timeEntries.autoClosedHint')}>
                <Chip size="small" color="warning" variant="outlined" label={t('timeEntries.autoClosed')} />
              </Tooltip>
            )}
            {extraTags?.map((tag) => (
              <Chip key={tag} size="small" color="warning" variant="outlined" label={tag} />
            ))}
          </Stack>
        </Box>
        <CheckInIcon locationCorrect={entry.locationCorrect} timeCorrect={entry.timeCorrect} />
      </Stack>

      <Stack sx={{ mt: 1, pl: 4.75 }}>
        <Typography variant="caption" color="text.secondary">
          {range} · {worked}
        </Typography>
      </Stack>

      <Stack direction="row" spacing={0.25} sx={{ mt: 0.5, justifyContent: 'flex-end' }}>
        <ReviewButtons
          entry={entry}
          canReview={canReview}
          isOwn={isOwn}
          onApprove={onApprove}
          onReject={onReject}
        />
        {canEdit && (
          <Tooltip title={locked ? t('timeEntries.locked') : t('common.edit')}>
            {/* A disabled button swallows its own events, so the tooltip
                needs a wrapper that still receives them. */}
            <span>
              <IconButton size="small" disabled={locked} onClick={onEdit}>
                <EditOutlined fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        )}
        {canDelete && (
          <Tooltip title={locked ? t('timeEntries.locked') : t('common.delete')}>
            <span>
              <IconButton size="small" disabled={locked} onClick={onDelete}>
                <DeleteOutlined fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        )}
      </Stack>
    </Paper>
  );
}
