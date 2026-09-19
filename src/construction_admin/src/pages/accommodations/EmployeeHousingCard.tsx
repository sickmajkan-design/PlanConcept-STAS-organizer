import { HomeWorkOutlined } from '@mui/icons-material';
import { Button, Card, CardContent, Chip, Link as MuiLink, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';

import { useAccommodationStaysQuery } from '../../features/accommodations/useAccommodations';
import { useT } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';
import { dateOnlyOffset, formatDate } from '../../utils/formatting';
import { StayDialog } from './AccommodationDetailPage';

/** Where this person lives now, and where they have lived. Placing them is a click away. */
export function EmployeeHousingCard({ employeeId }: { employeeId: string }) {
  const t = useT();
  const [placing, setPlacing] = useState(false);

  const query = useMemo(
    () => ({ employeeId, pageNumber: 1, pageSize: 6, sortBy: 'startDate', sortDescending: true }),
    [employeeId],
  );
  const { data } = useAccommodationStaysQuery(query);

  const today = dateOnlyOffset(0);
  const stays = data?.items ?? [];
  const current = stays.find(
    (stay) => stay.startDate <= today && (stay.endDate === null || stay.endDate >= today),
  );
  const earlier = stays.filter((stay) => stay !== current);

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          useFlexGap
          sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', justifyContent: 'space-between' }}
        >
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <HomeWorkOutlined fontSize="small" color="primary" />
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('accommodations.currentAccommodation')}
            </Typography>
          </Stack>
          <Button size="small" onClick={() => setPlacing(true)}>
            {t('accommodations.placeInHousing')}
          </Button>
        </Stack>

        {current ? (
          <Stack spacing={0.5} sx={{ mt: 1.5 }}>
            <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
              <MuiLink component={RouterLink} to={paths.accommodationDetail(current.accommodationId)} sx={{ fontWeight: 600 }}>
                {current.accommodationName}
              </MuiLink>
              <Chip size="small" color="success" variant="outlined" label={t('rates.active')} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {t('accommodations.since')} {formatDate(current.startDate)}
              {current.endDate ? ` · ${t('accommodations.until')} ${formatDate(current.endDate)}` : ''}
              {current.projectName ? ` · ${current.projectName}` : ''}
            </Typography>
          </Stack>
        ) : (
          <Typography color="text.secondary" sx={{ mt: 1.5 }}>
            {t('accommodations.noStay')}
          </Typography>
        )}

        {earlier.length > 0 && (
          <Stack spacing={0.25} sx={{ mt: 2 }}>
            <Typography variant="caption" color="text.secondary">
              {t('accommodations.stayEarlier')}
            </Typography>
            {earlier.map((stay) => (
              <Typography key={stay.id} variant="body2">
                <MuiLink component={RouterLink} to={paths.accommodationDetail(stay.accommodationId)}>
                  {stay.accommodationName}
                </MuiLink>{' '}
                <Typography component="span" variant="body2" color="text.secondary">
                  {formatDate(stay.startDate)} – {stay.endDate ? formatDate(stay.endDate) : t('rates.open')}
                </Typography>
              </Typography>
            ))}
          </Stack>
        )}
      </CardContent>

      <StayDialog open={placing} employeeId={employeeId} onClose={() => setPlacing(false)} />
    </Card>
  );
}
