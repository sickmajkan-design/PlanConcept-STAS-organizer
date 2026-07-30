import { ApartmentOutlined, MapOutlined, PeopleOutlined } from '@mui/icons-material';
import { Box, Card, CardActionArea, CardContent, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

import { canViewDirectory, displayName } from '../auth/authHelpers';
import { useAuth } from '../auth/useAuth';
import { paths } from '../routes/paths';

export function HomePage() {
  const { user } = useAuth();

  if (!user) return null;

  const cards = [
    {
      label: 'Live map',
      description: "See where today's crews are, live.",
      icon: <MapOutlined sx={{ fontSize: 32 }} />,
      to: paths.map,
    },
    ...(canViewDirectory(user)
      ? [
          {
            label: 'Employees',
            description: 'Search, add and manage the workforce.',
            icon: <PeopleOutlined sx={{ fontSize: 32 }} />,
            to: paths.employees,
          },
          {
            label: 'Projects',
            description: 'Manage sites and their assigned crews.',
            icon: <ApartmentOutlined sx={{ fontSize: 32 }} />,
            to: paths.projects,
          },
        ]
      : []),
  ];

  return (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 700 }}>
        Welcome, {displayName(user)}
      </Typography>
      <Typography color="text.secondary" sx={{ mb: 4 }}>
        Here is what you can do today.
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
