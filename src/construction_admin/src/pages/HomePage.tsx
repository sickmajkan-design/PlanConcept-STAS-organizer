import { ApartmentOutlined, MapOutlined, PeopleOutlined } from '@mui/icons-material';
import { Box, Card, CardActionArea, CardContent, Stack, Typography, useMediaQuery, useTheme } from '@mui/material';
import { Link } from 'react-router-dom';

import { canConfigureDashboard, canViewDirectory, displayName } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { DashboardGrid } from '../features/dashboard/DashboardGrid';
import { TodaySummary } from '../features/dashboard/TodaySummary';
import { SetupChecklistCard } from '../features/onboarding/SetupChecklistCard';
import { useI18n, useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';
import { ijekavianDate } from '../utils/formatting';
import { companyNow } from '../utils/companyTime';

export function HomePage() {
  const { user } = useAuth();
  const t = useT();
  const { locale } = useI18n();
  const isPhone = useMediaQuery(useTheme().breakpoints.down('sm'));

  if (!user) return null;

  // An account with no name on file would be greeted by its whole e-mail address.
  const name = displayName(user);
  const welcome = name.includes('@') ? t('home.welcomeShort') : t('home.welcome', { name });

  if (canConfigureDashboard(user)) {
    return (
      <Box>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, overflowWrap: 'anywhere', display: { xs: 'none', sm: 'block' } }}>
          {welcome}
        </Typography>
        {/* On a phone the greeting gives way to the day: the screen is about what is going on today. */}
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, display: { xs: 'block', sm: 'none' } }}>
          {t('home.today')}
          <Typography component="span" color="text.secondary" sx={{ ml: 1, fontSize: '0.95rem', fontWeight: 400 }}>
            {ijekavianDate(new Intl.DateTimeFormat(locale === 'sr' ? 'sr-Latn' : 'en', { weekday: 'long', day: 'numeric', month: 'long' }).format(companyNow()))}
          </Typography>
        </Typography>
        {isPhone && <TodaySummary />}
        <SetupChecklistCard />
        <DashboardGrid />
      </Box>
    );
  }

  const cards = [
    {
      label: t('nav.liveMap'),
      description: t('home.mapCard'),
      icon: <MapOutlined sx={{ fontSize: 32 }} />,
      to: paths.map,
    },
    ...(canViewDirectory(user)
      ? [
          {
            label: t('nav.employees'),
            description: t('home.employeesCard'),
            icon: <PeopleOutlined sx={{ fontSize: 32 }} />,
            to: paths.employees,
          },
          {
            label: t('nav.projects'),
            description: t('home.projectsCard'),
            icon: <ApartmentOutlined sx={{ fontSize: 32 }} />,
            to: paths.projects,
          },
        ]
      : []),
  ];

  return (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, overflowWrap: 'anywhere' }}>
        {welcome}
      </Typography>
      <Typography color="text.secondary" sx={{ mb: 4 }}>
        {t('home.subtitle')}
      </Typography>

      <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 2 }}>
        {cards.map((card) => (
          <Card key={card.to} sx={{ width: 260 }}>
            <CardActionArea component={Link} to={card.to} sx={{ height: '100%' }}>
              <CardContent>
                <Stack spacing={1.5}>
                  <Box sx={{ color: 'primary.main' }}>{card.icon}</Box>
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                    {card.label}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {card.description}
                  </Typography>
                </Stack>
              </CardContent>
            </CardActionArea>
          </Card>
        ))}
      </Stack>
    </Box>
  );
}
