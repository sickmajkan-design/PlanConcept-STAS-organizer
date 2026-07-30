import { EngineeringOutlined } from '@mui/icons-material';
import { Box, Paper, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

/** Centered card frame shared by every anonymous auth screen. */
export function AuthCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
}) {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        px: 2,
      }}
    >
      <Paper
        elevation={0}
        sx={{ p: { xs: 3, sm: 5 }, width: '100%', maxWidth: 440, border: '1px solid #e0e0e0' }}
      >
        <Stack spacing={3}>
          <Stack spacing={1} sx={{ alignItems: 'center', textAlign: 'center' }}>
            <EngineeringOutlined sx={{ fontSize: 44, color: 'primary.main' }} />
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              {title}
            </Typography>
            {subtitle && (
              <Typography variant="body2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Stack>
          {children}
        </Stack>
      </Paper>
    </Box>
  );
}
