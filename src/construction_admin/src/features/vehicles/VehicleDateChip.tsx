import { Chip } from '@mui/material';

import { useT } from '../../i18n/useI18n';
import { formatDate } from '../../utils/formatting';

/** How many days away a `YYYY-MM-DD` date is from today, negative once it has passed. */
export function daysUntil(isoDate: string, today: Date = new Date()): number {
  const target = new Date(`${isoDate}T00:00:00`);
  const start = new Date(today.getFullYear(), today.getMonth(), today.getDate());

  return Math.round((target.getTime() - start.getTime()) / 86_400_000);
}

/** A date that runs out is red once it has, amber for its last month, otherwise plain. */
export const EXPIRING_SOON_DAYS = 30;

export function dateTone(isoDate: string | null | undefined): 'default' | 'warning' | 'error' {
  if (!isoDate) return 'default';

  const days = daysUntil(isoDate);
  if (days < 0) return 'error';
  if (days <= EXPIRING_SOON_DAYS) return 'warning';

  return 'default';
}

/**
 * "Registered until 31.03.2027", coloured by how close the date is: the vehicle header and the list use
 * it so the date to renew by is seen without opening anything. A date that was never entered says so,
 * instead of leaving the reader to wonder whether it is fine.
 */
export function VehicleDateChip({
  label,
  date,
  size = 'small',
}: {
  label: string;
  date: string | null | undefined;
  size?: 'small' | 'medium';
}) {
  const t = useT();

  if (!date) {
    return <Chip size={size} variant="outlined" label={`${label}: ${t('vehicles.dateMissing')}`} />;
  }

  const days = daysUntil(date);
  const tone = dateTone(date);

  return (
    <Chip
      size={size}
      color={tone}
      variant={tone === 'default' ? 'outlined' : 'filled'}
      label={`${label}: ${formatDate(date)}${
        days < 0 ? ` (${t('vehicles.dateExpired')})` : days <= EXPIRING_SOON_DAYS ? ` (${t('vehicles.dateDaysLeft', { count: days })})` : ''
      }`}
    />
  );
}
