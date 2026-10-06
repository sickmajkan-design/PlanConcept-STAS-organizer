import { useCallback, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';

import { toApiError } from '../../api/apiError';
import type { Absence } from '../../api/types';
import { useT } from '../../i18n/useI18n';
import { AbsenceHousingChoice, type HousingChoice } from './AbsenceHousingChoice';
import { useReviewAbsence } from './useAbsences';

/**
 * Granting a request for time off. Beyond the plain confirmation it asks about housing when the person
 * lives in company accommodation: the same dialog on the absences page and on the dashboard, so the
 * question is never skipped by approving from the other place.
 */
export function ApproveAbsenceDialog({
  absence,
  description,
  onClose,
}: {
  absence: Absence | null;
  /** The sentence under the title; the page may add the leave balance to it. */
  description?: string;
  onClose: () => void;
}) {
  const t = useT();
  const review = useReviewAbsence();
  const [housing, setHousing] = useState<HousingChoice>({});
  const [failure, setFailure] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const onHousing = useCallback((choice: HousingChoice) => setHousing(choice), []);

  const close = () => {
    setFailure(null);
    onClose();
  };

  const confirm = async () => {
    if (!absence) return;

    setPending(true);
    setFailure(null);

    try {
      // Awaited, so a refusal (a conflict, a lost connection) is shown here instead of closing silently.
      await review.mutateAsync({ id: absence.id, input: { approve: true, ...housing } });
      onClose();
    } catch (error) {
      setFailure(toApiError(error).message);
    } finally {
      setPending(false);
    }
  };

  return (
    <Dialog open={!!absence} onClose={close} fullWidth maxWidth="sm">
      <DialogTitle>{t('absences.approveTitle')}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>{description ?? t('absences.approveBody')}</DialogContentText>
        {absence && (
          <AbsenceHousingChoice
            employeeId={absence.employeeId}
            startDate={absence.startDate}
            endDate={absence.endDate}
            type={absence.type}
            onChange={onHousing}
          />
        )}
        {failure && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {failure}
          </Alert>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={close} disabled={pending}>
          {t('common.cancel')}
        </Button>
        <Button variant="contained" loading={pending} onClick={() => void confirm()}>
          {t('absences.approve')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
