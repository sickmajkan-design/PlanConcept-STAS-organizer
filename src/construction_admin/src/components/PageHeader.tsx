import { Box, Button, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

export function PageHeader({
  title,
  subtitle,
  description,
  action,
}: {
  title: string;
  /** Usually the row count. Rendered small, under the description if there is one. */
  subtitle?: string;
  /** What this screen is and how it connects to the rest of the app — a sentence, not a count. */
  description?: string;
  action?: { label: string; icon?: ReactNode; onClick: () => void };
}) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={2}
      sx={{
        mb: 3,
        justifyContent: 'space-between',
        alignItems: { xs: 'flex-start', sm: 'center' },
      }}
    >
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
        {description && (
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ mt: 0.5, maxWidth: 560 }}
          >
            {description}
          </Typography>
        )}
        {subtitle && (
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ display: 'block', mt: description ? 0.5 : 0 }}
          >
            {subtitle}
          </Typography>
        )}
      </Box>
      {action && (
        <Button variant="contained" startIcon={action.icon} onClick={action.onClick}>
          {action.label}
        </Button>
      )}
    </Stack>
  );
}
