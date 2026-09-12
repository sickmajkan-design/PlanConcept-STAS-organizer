import { CheckOutlined, UndoOutlined } from '@mui/icons-material';
import { IconButton, Tooltip } from '@mui/material';

import type { TimeEntry } from '../../api/types';
import { useT } from '../../i18n/useI18n';

/**
 * Approve and send-back, shown only where they make sense.
 *
 * A running shift has nothing to review yet, and an approved one is already
 * decided — the API refuses both, so offering the buttons would only produce a
 * 409 the supervisor has to read to learn what the grid could have shown.
 * Likewise for a role the API's `ProjectManagerAndAbove` policy refuses
 * outright, and for the reviewer's own entry — "never your own" is enforced
 * server-side, but a live button that always 403s is worse than no button.
 */
export function ReviewButtons({
  entry,
  canReview,
  isOwn,
  onApprove,
  onReject,
}: {
  entry: TimeEntry;
  canReview: boolean;
  isOwn: boolean;
  onApprove: () => void;
  onReject: () => void;
}) {
  const t = useT();

  if (entry.status === 'InProgress' || !canReview || isOwn) {
    return null;
  }

  return (
    <>
      {entry.status !== 'Approved' && (
        <Tooltip title={t('timeEntries.approve')}>
          <IconButton size="small" color="success" onClick={onApprove}>
            <CheckOutlined fontSize="small" />
          </IconButton>
        </Tooltip>
      )}
      <Tooltip title={t('timeEntries.reject')}>
        <IconButton size="small" color="warning" onClick={onReject}>
          <UndoOutlined fontSize="small" />
        </IconButton>
      </Tooltip>
    </>
  );
}
