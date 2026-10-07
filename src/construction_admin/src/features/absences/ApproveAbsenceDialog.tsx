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
import { planningApi } from '../../api/planning';
import { useQueryClient } from '@tanstack/react-query';
import { AbsenceCoverageChoice, type CoverageChoice } from './AbsenceCoverageChoice';
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
  const queryClient = useQueryClient();
  const [housing, setHousing] = useState<HousingChoice>({});
  const [coverage, setCoverage] = useState<CoverageChoice | null>(null);
  const [approved, setApproved] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const onHousing = useCallback((choice: HousingChoice) => setHousing(choice), []);
  const onCoverage = useCallback((choice: CoverageChoice | null) => setCoverage(choice), []);

  const close = () => {
    setFailure(null);
    setApproved(false);
    onClose();
  };

  const confirm = async () => {
    if (!absence) return;

    setPending(true);
    setFailure(null);
    let granted = approved;

    try {
      // Awaited, so a refusal (a conflict, a lost connection) is shown here instead of closing silently.
      if (!approved) {
        await review.mutateAsync({ id: absence.id, input: { approve: true, ...housing } });
        granted = true;
        setApproved(true);
      }

      if (coverage) {
        await planningApi.assign({
          employeeId: coverage.employeeId,
          projectId: coverage.projectId,
          from: coverage.from,
          to: coverage.to,
          onlyFreeDays: coverage.onlyFreeDays,
        });
        void queryClient.invalidateQueries({ queryKey: ['planning'] });
      }

      setApproved(false);
      onClose();
    } catch (error) {
      const message = toApiError(error).message;
      // The leave is already granted once we get here; say so, so it is not granted twice.
      setFailure(granted ? t('planning.cover.failed', { message }) : message);
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
        {absence && !approved && (
          <AbsenceCoverageChoice
            employeeId={absence.employeeId}
            startDate={absence.startDate}
            endDate={absence.endDate}
            onChange={onCoverage}
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
