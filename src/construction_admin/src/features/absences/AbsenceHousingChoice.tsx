import { useEffect, useState } from 'react';
import { Alert, Checkbox, FormControlLabel, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

import { absencesApi } from '../../api/absences';
import type { AbsenceType } from '../../api/types';
import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

/** What the office decided about housing while granting a leave. Empty when the person is not housed. */
export interface HousingChoice {
  releaseAccommodation?: boolean;
  returnToAccommodation?: boolean;
}

function addDays(isoDate: string, days: number): string {
  const date = new Date(`${isoDate}T00:00:00`);
  date.setDate(date.getDate() + days);

  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Asks, when somebody who lives in company housing goes on leave, whether they come off it for those
 * days. Annual leave takes them off by default and any other kind does not, since the office decides
 * those case by case. Renders nothing for a person who is not housed.
 *
 * Reports the choice upwards as it changes (and once when it first has an answer), so whichever dialog
 * hosts it can send it with the approval.
 */
export function AbsenceHousingChoice({
  employeeId,
  startDate,
  endDate,
  type,
  onChange,
}: {
  employeeId: string | undefined;
  startDate: string | undefined;
  endDate: string | undefined;
  type: AbsenceType;
  onChange: (choice: HousingChoice) => void;
}) {
  const t = useT();
  const enabled = !!employeeId && !!startDate && !!endDate && endDate >= startDate;

  const impact = useQuery({
    queryKey: ['absenceHousingImpact', employeeId, startDate, endDate],
    queryFn: () => absencesApi.housingImpact(employeeId!, startDate!, endDate!),
    enabled,
  });

  const stay = impact.data?.hasStay ? impact.data : null;
  const [release, setRelease] = useState(type === 'AnnualLeave');
  const [returnAfter, setReturnAfter] = useState(true);

  // A different kind of leave starts from that kind's default again.
  useEffect(() => {
    setRelease(type === 'AnnualLeave');
  }, [type]);

  useEffect(() => {
    onChange(stay ? { releaseAccommodation: release, returnToAccommodation: release && returnAfter } : {});
  }, [stay, release, returnAfter, onChange]);

  if (!stay || !startDate || !endDate) {
    return null;
  }

  const comeBack = formatDate(addDays(endDate, 1));
  // Nothing to book back when the stay was due to end before they return.
  const staysPastLeave = !stay.stayEndDate || stay.stayEndDate >= addDays(endDate, 1);

  return (
    <Stack spacing={0.5}>
      <Alert severity="info">
        {t('absences.housing.lives', { place: stay.accommodationName ?? '' })}{' '}
        {type === 'AnnualLeave' ? t('absences.housing.annualHint') : t('absences.housing.otherHint')}
      </Alert>
      <FormControlLabel
        control={<Checkbox checked={release} onChange={(event) => setRelease(event.target.checked)} />}
        label={t('absences.housing.release', { date: formatDate(startDate) })}
      />
      {staysPastLeave && (
        <FormControlLabel
          sx={{ pl: 3 }}
          control={
            <Checkbox
              checked={release && returnAfter}
              disabled={!release}
              onChange={(event) => setReturnAfter(event.target.checked)}
            />
          }
          label={
            <Typography variant="body2">{t('absences.housing.return', { date: comeBack })}</Typography>
          }
        />
      )}
    </Stack>
  );
}
