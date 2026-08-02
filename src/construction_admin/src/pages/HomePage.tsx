import { ApartmentOutlined, MapOutlined, PeopleOutlined } from '@mui/icons-material';
import { Box, Card, CardActionArea, CardContent, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { canViewDirectory, displayName } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { useT } from '../i18n/useI18n';
import { paths } from '../routes/paths';

export function HomePage() {
  const { user } = useAuth();
  const t = useT();

  if (!user) return null;

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
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 700 }}>
        {t('home.welcome', { name: displayName(user) })}
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
