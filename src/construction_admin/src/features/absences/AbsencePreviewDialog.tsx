import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

import type { Absence } from '../../api/types';
import { StatusChip } from '../../components/StatusChip';
import { useEnumLabel } from '../../i18n/enumLabels';
import { useT } from '../../i18n/useI18n';
import { formatDate, formatDateTime } from '../../utils/formatting';
import { useAbsenceBalanceQuery } from './useAbsences';

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography sx={{ overflowWrap: 'anywhere', whiteSpace: 'pre-wrap' }}>{children}</Typography>
    </Box>
  );
}

/** Everything the office needs to decide a request, without leaving the list. */
export function AbsencePreviewDialog({
  absence,
  onClose,
  actions,
}: {
  absence: Absence | null;
  onClose: () => void;
  /** Decision buttons, only passed to those who may press them. */
  actions?: ReactNode;
}) {
  const t = useT();
  const enumLabel = useEnumLabel();
  const annual = absence?.type === 'AnnualLeave';
  const balance = useAbsenceBalanceQuery(
    annual ? absence.employeeId : undefined,
    absence ? Number(absence.startDate.slice(0, 4)) : undefined,
  );

  return (
    <Dialog open={!!absence} onClose={onClose} fullWidth maxWidth="sm">
      {absence && (
        <>
          <DialogTitle>{t('absences.preview.title')}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 0.5 }}>
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 1 }}>
                <Typography variant="h6">{absence.employeeName}</Typography>
                <StatusChip status={absence.status} kind="absenceStatus" />
              </Stack>
              <Field label={t('absences.type')}>{enumLabel('absenceType', absence.type)}</Field>
              <Field label={t('absences.preview.dates')}>
                {formatDate(absence.startDate)} – {formatDate(absence.endDate)} · {t('absences.days', { count: absence.dayCount })}
              </Field>
              <Field label={t('absences.preview.reason')}>
                {absence.reason?.trim() || t('absences.preview.noReason')}
              </Field>
              {absence.hasPendingEdit && (
                <Field label={t('absences.preview.proposal')}>
                  {formatDate(absence.proposedStartDate)} – {formatDate(absence.proposedEndDate)}
                  {absence.proposedReason ? `\n${absence.proposedReason}` : ''}
                </Field>
              )}
              {annual && balance.data && (
                <Field label={t('absences.preview.balance')}>
                  {t(balance.data.remainingDays < 0 ? 'absences.balanceOver' : 'absences.balance', {
                    used: balance.data.usedDays,
                    allowance: balance.data.allowanceDays,
                    remaining: Math.abs(balance.data.remainingDays),
                  })}
                </Field>
              )}
              {absence.requestedByName && (
                <Field label={t('absences.preview.requestedBy')}>
                  {absence.requestedByName} · {formatDateTime(absence.createdAt)}
                </Field>
              )}
              {absence.reviewedByName && (
                <Field label={t('absences.preview.reviewedBy')}>
                  {absence.reviewedByName}
                  {absence.reviewedAt ? ` · ${formatDateTime(absence.reviewedAt)}` : ''}
                </Field>
              )}
              {absence.reviewNote && <Field label={t('absences.preview.reviewNote')}>{absence.reviewNote}</Field>}
            </Stack>
          </DialogContent>
          <DialogActions sx={{ flexWrap: 'wrap', gap: 1 }}>
            <Button onClick={onClose}>{t('common.close')}</Button>
            {actions}
          </DialogActions>
        </>
      )}
    </Dialog>
  );
}
