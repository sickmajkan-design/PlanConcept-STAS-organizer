import { Box, Stack, Switch, Typography, type SwitchProps } from '@mui/material';
import { alpha, styled, useTheme } from '@mui/material/styles';
import { useEffect, useState, type ReactNode } from 'react';

/** Cards on this page are soft, large and unframed: structure comes from space and a hairline, not from borders. */
export const PANEL_RADIUS = '24px';
const SHADOW = '0 1px 2px rgba(0, 0, 0, 0.04), 0 8px 28px rgba(0, 0, 0, 0.05)';

export type Tone = 'success' | 'warning' | 'error';

/**
 * The card every section of the server page sits in. Rows inside it (marked `data-row`) get a hairline between them
 * instead of a box each, which is what keeps a page of lists calm.
 */
export function Panel({
  title,
  description,
  action,
  fill = true,
  children,
}: {
  title?: string;
  description?: string;
  action?: ReactNode;
  /** Stretch to the height of the row (default), or keep the height of the content. */
  fill?: boolean;
  children: ReactNode;
}) {
  return (
    <Box
      component="section"
      sx={(theme) => ({
        bgcolor: 'background.paper',
        borderRadius: PANEL_RADIUS,
        p: { xs: 2.5, md: 3.5 },
        boxShadow: theme.palette.mode === 'dark' ? 'none' : SHADOW,
        height: fill ? '100%' : 'auto',
        boxSizing: 'border-box',
        minWidth: 0,
        '& [data-row] + [data-row]': { borderTop: 1, borderColor: 'divider' },
      })}
    >
      {(title || action) && (
        <Stack direction="row" sx={{ alignItems: 'center', gap: 2, flexWrap: 'wrap', mb: description ? 0.5 : 1.5 }}>
          <Typography component="h2" sx={{ fontSize: 22, fontWeight: 600, letterSpacing: '-0.02em', lineHeight: 1.2, flexGrow: 1 }}>
            {title}
          </Typography>
          {action}
        </Stack>
      )}
      {description && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
          {description}
        </Typography>
      )}
      {children}
    </Box>
  );
}

/** A figure that is the point of a card: large, tight, with the digits lined up. */
export function BigNumber({ children, size = 44 }: { children: ReactNode; size?: number }) {
  return (
    <Typography
      component="div"
      sx={{ fontSize: { xs: Math.min(size, 30), sm: size }, lineHeight: 1, fontWeight: 600, letterSpacing: '-0.03em', fontVariantNumeric: 'tabular-nums' }}
    >
      {children}
    </Typography>
  );
}

export function StatusDot({ tone, size = 8 }: { tone: Tone | 'none'; size?: number }) {
  return (
    <Box
      component="span"
      aria-hidden="true"
      sx={{ width: size, height: size, borderRadius: '50%', flexShrink: 0, display: 'inline-block', bgcolor: tone === 'none' ? 'transparent' : `${tone}.main` }}
    />
  );
}

/** A ring that fills to a share. It grows from empty once when it appears, unless the person asked for less motion. */
export function Ring({ percent, tone, label, size = 76 }: { percent: number; tone: Tone; label: string; size?: number }) {
  const theme = useTheme();
  const [shown, setShown] = useState(false);

  useEffect(() => {
    const frame = requestAnimationFrame(() => setShown(true));
    return () => cancelAnimationFrame(frame);
  }, []);

  const circumference = 2 * Math.PI * 34;
  const filled = (circumference * Math.max(0, Math.min(100, percent))) / 100;

  return (
    <Box
      component="svg"
      width={size}
      height={size}
      viewBox="0 0 80 80"
      role="img"
      aria-label={label}
      sx={{ flexShrink: 0, '@media (prefers-reduced-motion: reduce)': { '& circle': { transition: 'none !important' } } }}
    >
      <circle cx="40" cy="40" r="34" fill="none" stroke={alpha(theme.palette.text.primary, 0.08)} strokeWidth="8" />
      <circle
        cx="40"
        cy="40"
        r="34"
        fill="none"
        stroke={theme.palette[tone].main}
        strokeWidth="8"
        strokeLinecap="round"
        strokeDasharray={`${shown ? filled : 0} ${circumference}`}
        transform="rotate(-90 40 40)"
        style={{ transition: 'stroke-dasharray 700ms cubic-bezier(0.23, 1, 0.32, 1)' }}
      />
    </Box>
  );
}

/** The switch from iOS: 51 by 31, the knob slides with a strong ease-out. */
export const IosSwitch = styled((props: SwitchProps) => <Switch disableRipple {...props} />)(({ theme }) => ({
  width: 51,
  height: 31,
  padding: 0,
  flexShrink: 0,
  '& .MuiSwitch-switchBase': {
    padding: 0,
    margin: 2,
    transitionDuration: '220ms',
    transitionTimingFunction: 'cubic-bezier(0.23, 1, 0.32, 1)',
    '&.Mui-checked': {
      transform: 'translateX(20px)',
      color: '#ffffff',
      '& + .MuiSwitch-track': { backgroundColor: theme.palette.success.main, opacity: 1, border: 0 },
    },
    '&.Mui-focusVisible .MuiSwitch-thumb': { outline: `2px solid ${theme.palette.primary.main}`, outlineOffset: 2 },
  },
  '& .MuiSwitch-thumb': { boxSizing: 'border-box', width: 27, height: 27, boxShadow: '0 2px 4px rgba(0, 0, 0, 0.25)' },
  '& .MuiSwitch-track': {
    borderRadius: 31 / 2,
    backgroundColor: theme.palette.mode === 'dark' ? '#3a3a3c' : '#e5e5ea',
    opacity: 1,
    transition: 'background-color 220ms cubic-bezier(0.23, 1, 0.32, 1)',
  },
  '@media (prefers-reduced-motion: reduce)': {
    '& .MuiSwitch-switchBase': { transitionDuration: '0ms' },
    '& .MuiSwitch-track': { transition: 'none' },
  },
}));
