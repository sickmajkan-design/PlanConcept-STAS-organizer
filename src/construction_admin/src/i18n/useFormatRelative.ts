import { useCallback } from 'react';

import { formatDate } from '../utils/formatting';
import { useT } from './useI18n';

/**
 * "How long ago" for the live map's last GPS fix.
 *
 * Lives here rather than in `utils/formatting` because, unlike a date, it is
 * words — and in Serbian those words change with the number ("pre 1 dan" vs
 * "pre 5 dana"), which is the plural machinery in the provider.
 */
export function useFormatRelative() {
  const t = useT();

  return useCallback(
    (value: string | null | undefined): string => {
      if (!value) {
        return t('time.never');
      }

      const date = new Date(value);

      if (Number.isNaN(date.getTime())) {
        return t('time.never');
      }

      const elapsedMs = Date.now() - date.getTime();

      if (elapsedMs < 0 || elapsedMs < 60_000) {
        return t('time.justNow');
      }

      const minutes = Math.floor(elapsedMs / 60_000);

      if (minutes < 60) {
        return t('time.minutesAgo', { count: minutes });
      }

      const hours = Math.floor(minutes / 60);

      if (hours < 24) {
        return t('time.hoursAgo', { count: hours });
      }

      const days = Math.floor(hours / 24);

      if (days < 7) {
        return t('time.daysAgo', { count: days });
      }

      return formatDate(value);
    },
    [t],
  );
}
