import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { setCompanyTimeZone } from '../../utils/companyTime';
import { useCompanyBrandingQuery } from './useCompanySettings';

/**
 * Keeps the panel's calendar in the zone the organisation chose.
 *
 * The zone is part of the organisation's public branding, so it is known before anyone signs in. When it differs from
 * the one the panel was using (the first visit, or after someone changed it), every figure that was counted by days is
 * stale, so everything is asked for again. Renders nothing.
 */
export function CompanyTimeZoneSync() {
  const queryClient = useQueryClient();
  const { data } = useCompanyBrandingQuery();
  const zone = data?.timeZoneId;

  useEffect(() => {
    if (setCompanyTimeZone(zone)) {
      void queryClient.invalidateQueries();
    }
  }, [zone, queryClient]);

  return null;
}
