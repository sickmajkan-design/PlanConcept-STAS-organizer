import { Box, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

interface StatTileProps {
  icon: ReactNode;
  label: string;
  value: string;
  hint?: string;
  hintColor?: 'warning.main' | 'error.main' | 'success.main' | 'text.secondary';
  accent?: string;
}

/**
 * One KPI tile: icon chip, label, and a big number. Used anywhere a widget
 * needs to lead with "the number that matters" rather than bury it in a
 * sentence — CompanyKpiWidget's whole grid is built from these.
 */
export function StatTile({ icon, label, value, hint, hintColor = 'text.secondary', accent = '#e65100' }: StatTileProps) {
  return (
    <Stack
      spacing={1}
      sx={{
        p: { xs: 1.5, sm: 2 },
        borderRadius: 2.5,
        minWidth: 0,
        // Two to a row on a phone, instead of one tile per row that pushes the rest off the screen.
        flex: { xs: '1 1 calc(50% - 6px)', sm: '1 1 200px' },
        background: `linear-gradient(160deg, ${accent}12 0%, ${accent}05 100%)`,
        border: '1px solid',
        borderColor: `${accent}22`,
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            width: 32,
            height: 32,
            borderRadius: 1.5,
            bgcolor: `${accent}1c`,
            color: accent,
            flexShrink: 0,
          }}
        >
          {icon}
        </Box>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{
            fontWeight: 600,
            minWidth: 0,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            // On a phone a label may take two lines instead of being cut to "Active emplo...".
            whiteSpace: { xs: 'normal', sm: 'nowrap' },
            display: { xs: '-webkit-box', sm: 'block' },
            WebkitLineClamp: { xs: 2, sm: 'unset' },
            WebkitBoxOrient: 'vertical',
            lineHeight: { xs: 1.25, sm: undefined },
          }}
        >
          {label}
        </Typography>
      </Stack>
      <Typography
        sx={{
          fontWeight: 700,
          // A zero is the absence of something, so it recedes on a phone, where the tiles stand shoulder to shoulder.
          color: /^[0\s/]+$/.test(value) ? { xs: 'text.secondary', sm: 'text.primary' } : undefined,
          fontSize: { xs: '1.5rem', sm: 'clamp(1.5rem, 2.4vw, 2.5rem)' },
          lineHeight: 1.05,
          letterSpacing: '-0.02em',
        }}
      >
        {value}
      </Typography>
      {hint && (
        <Typography variant="caption" sx={{ color: hintColor, fontWeight: 600 }}>
          {hint}
        </Typography>
      )}
    </Stack>
  );
}
