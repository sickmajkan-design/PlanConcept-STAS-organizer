import { ChevronRightOutlined } from '@mui/icons-material';
import { alpha, Box, ButtonBase, Stack, Typography, type SxProps, type Theme } from '@mui/material';
import type { ElementType, ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';

/**
 * The small set of pieces the dashboard widgets are drawn from, so they read as one family: a big number with
 * a quiet caption, a soft pill, an inset list, a rounded bar. Every colour comes from the theme, and "state" is
 * always a word or a number as well as a colour.
 */
export type Tone = 'neutral' | 'accent' | 'good' | 'warn' | 'bad';

export function toneColor(theme: Theme, tone: Tone): string {
  switch (tone) {
    case 'good':
      return theme.palette.success.main;
    case 'warn':
      return theme.palette.warning.main;
    case 'bad':
      return theme.palette.error.main;
    case 'accent':
      return theme.palette.primary.main;
    default:
      return theme.palette.text.secondary;
  }
}

const NUMERIC = { fontVariantNumeric: 'tabular-nums' } as const;

/** The figure the widget is about: large, tight, with an optional small unit or caption beside it. */
export function BigNumber({ value, caption, tone = 'neutral', size = 'lg' }: { value: ReactNode; caption?: ReactNode; tone?: Tone; size?: 'md' | 'lg' }) {
  return (
    <Stack direction="row" sx={{ alignItems: 'baseline', gap: 1, flexWrap: 'wrap', minWidth: 0 }}>
      <Typography
        component="span"
        sx={(theme) => ({
          ...NUMERIC,
          fontWeight: 800,
          lineHeight: 1,
          letterSpacing: '-0.03em',
          // From the width the widget was given, not the screen's: a wide card gets a bigger figure.
          fontSize: size === 'lg' ? 'clamp(1.9rem, 9cqw, 3.2rem)' : 'clamp(1.4rem, 6cqw, 2rem)',
          color: tone === 'neutral' ? 'text.primary' : toneColor(theme, tone),
          overflowWrap: 'anywhere',
          minWidth: 0,
        })}
      >
        {value}
      </Typography>
      {caption && (
        <Typography component="span" variant="body2" color="text.secondary">
          {caption}
        </Typography>
      )}
    </Stack>
  );
}

/** A small rounded label: a change, a state, a count. */
export function Pill({ children, tone = 'neutral', sx }: { children: ReactNode; tone?: Tone; sx?: SxProps<Theme> }) {
  return (
    <Box
      component="span"
      sx={[
        (theme) => ({
          ...NUMERIC,
          display: 'inline-flex',
          alignItems: 'center',
          // A pill is as wide as its words, also inside a column that would stretch it.
          alignSelf: 'flex-start',
          // It may take two lines when the room is narrow, but it never runs past the edge of what holds it.
          minWidth: 0,
          maxWidth: '100%',
          overflowWrap: 'anywhere',
          textAlign: 'left',
          borderRadius: 999,
          px: 1.1,
          py: 0.15,
          fontSize: '0.78rem',
          fontWeight: 600,
          lineHeight: 1.35,
          color: tone === 'neutral' ? theme.palette.text.secondary : toneColor(theme, tone),
          bgcolor:
            tone === 'neutral'
              ? theme.palette.mode === 'dark'
                ? 'rgba(255,255,255,0.07)'
                : 'rgba(31,27,22,0.06)'
              : alpha(toneColor(theme, tone), theme.palette.mode === 'dark' ? 0.22 : 0.12),
        }),
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      {children}
    </Box>
  );
}

/** A figure with a label above it, on a soft tile. Tiles flow into as many columns as the widget is wide. */
export function MiniStat({ label, value, tone = 'neutral', hint }: { label: ReactNode; value: ReactNode; tone?: Tone; hint?: ReactNode }) {
  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: '16px',
        px: 1.75,
        py: 1.25,
        bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.05)' : 'rgba(31,27,22,0.04)',
        display: 'grid',
        gap: 0.25,
      })}
    >
      <Typography variant="caption" color="text.secondary" sx={{ lineHeight: 1.25 }}>
        {label}
      </Typography>
      <Typography
        component="span"
        sx={(theme) => ({
          ...NUMERIC,
          fontWeight: 800,
          letterSpacing: '-0.02em',
          lineHeight: 1.1,
          fontSize: 'clamp(1.2rem, 5.2cqw, 1.7rem)',
          color: tone === 'neutral' ? 'text.primary' : toneColor(theme, tone),
          overflowWrap: 'anywhere',
        })}
      >
        {value}
      </Typography>
      {hint}
    </Box>
  );
}

/** Tiles in a grid that adds columns as the widget gets wider. */
export function StatGrid({ children, min = 132 }: { children: ReactNode; min?: number }) {
  return <Box sx={{ display: 'grid', gridTemplateColumns: `repeat(auto-fit, minmax(${min}px, 1fr))`, gap: 1.25 }}>{children}</Box>;
}

/** A list of rows on one soft surface, with hairlines between. */
export function InsetList({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={(theme) => ({
        borderRadius: '16px',
        overflow: 'hidden',
        bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.05)' : 'rgba(31,27,22,0.04)',
        '& > * + *': { borderTop: '1px solid', borderColor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.07)' : 'rgba(31,27,22,0.07)' },
      })}
    >
      {children}
    </Box>
  );
}

interface InsetRowProps {
  /** A coloured dot in front: the state of the row, repeated in `badge` as a word or a number. */
  tone?: Tone;
  primary: ReactNode;
  secondary?: ReactNode;
  badge?: ReactNode;
  badgeTone?: Tone;
  /** A router link or a click: the row then shows a chevron and presses in. */
  to?: string;
  onClick?: () => void;
  component?: ElementType;
  action?: ReactNode;
  leading?: ReactNode;
}

export function InsetRow({ tone, primary, secondary, badge, badgeTone, to, onClick, component, action, leading }: InsetRowProps) {
  const interactive = !!(to || onClick);
  // A link goes through the router, so a row press never reloads the page.
  const Component: ElementType = component ?? ButtonBase;

  const content = (
    <>
      {leading}
      {tone && !leading && (
        <Box component="span" sx={(theme) => ({ width: 9, height: 9, borderRadius: '50%', flexShrink: 0, bgcolor: toneColor(theme, tone) })} />
      )}
      {/* The words keep a readable width of their own; whatever else is on the row (a state, a number, buttons)
          goes under them when the row is too narrow for both, instead of squeezing them into a column. */}
      <Box component="span" sx={{ flex: '1 1 140px', minWidth: 0, display: 'grid', textAlign: 'left' }}>
        <Typography component="span" variant="body2" sx={{ fontWeight: 500, lineHeight: 1.35, overflowWrap: 'anywhere' }}>
          {primary}
        </Typography>
        {secondary && (
          <Typography component="span" variant="caption" color="text.secondary" sx={{ lineHeight: 1.3, overflowWrap: 'anywhere' }}>
            {secondary}
          </Typography>
        )}
      </Box>
      {action}
      {badge !== undefined && badge !== null && <Pill tone={badgeTone ?? tone ?? 'neutral'}>{badge}</Pill>}
      {interactive && <ChevronRightOutlined fontSize="small" sx={{ color: 'text.disabled', flexShrink: 0 }} />}
    </>
  );

  const sx: SxProps<Theme> = (theme) => ({
    display: 'flex',
    flexWrap: 'wrap',
    alignItems: 'center',
    columnGap: 1.5,
    rowGap: 0.75,
    width: '100%',
    px: 1.75,
    py: 1.4,
    color: 'inherit',
    textDecoration: 'none',
    font: 'inherit',
    ...(interactive && {
      cursor: 'pointer',
      transition: 'background-color 0.12s ease-out',
      '&:active': { bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.08)' : 'rgba(31,27,22,0.07)' },
      '&:focus-visible': { outline: `2px solid ${theme.palette.primary.main}`, outlineOffset: -2 },
    }),
  });

  if (!interactive) {
    return <Box sx={sx}>{content}</Box>;
  }

  return (
    <Component {...(to ? { component: RouterLink, to } : {})} onClick={onClick} sx={sx}>
      {content}
    </Component>
  );
}

/** A thick rounded bar with a soft gradient; `marker` draws a thin tick (a warning level, a target). */
export function SoftBar({ value, tone = 'accent', marker, label }: { value: number; tone?: Tone; marker?: number; label?: string }) {
  const clamped = Math.max(0, Math.min(100, value));

  return (
    <Box
      role="progressbar"
      aria-valuenow={Math.round(clamped)}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={label}
      sx={(theme) => ({
        position: 'relative',
        height: 12,
        borderRadius: 6,
        bgcolor: theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.07)' : 'rgba(31,27,22,0.07)',
        overflow: 'visible',
      })}
    >
      <Box
        sx={(theme) => ({
          position: 'absolute',
          insetBlock: 0,
          left: 0,
          width: `${clamped}%`,
          borderRadius: 6,
          background: `linear-gradient(90deg, ${toneColor(theme, tone)}, ${alpha(toneColor(theme, tone), 0.7)})`,
          transition: 'width 0.8s cubic-bezier(0.22, 1, 0.36, 1)',
          '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
        })}
      />
      {marker !== undefined && (
        <Box sx={{ position: 'absolute', top: -3, bottom: -3, left: `${marker}%`, width: 2, borderRadius: 1, bgcolor: 'text.primary', opacity: 0.5 }} />
      )}
    </Box>
  );
}

/** One row of "name … value" above a bar. */
export function BarRow({ label, value, percent, tone = 'accent', marker }: { label: ReactNode; value: ReactNode; percent: number; tone?: Tone; marker?: number }) {
  return (
    <Box sx={{ display: 'grid', gap: 0.75 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', columnGap: 1.5, rowGap: 0.25, alignItems: 'baseline', flexWrap: 'wrap' }}>
        <Typography variant="body2" sx={{ fontWeight: 500, minWidth: 0, overflowWrap: 'anywhere' }}>
          {label}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ ...NUMERIC, whiteSpace: 'nowrap' }}>
          {value}
        </Typography>
      </Stack>
      <SoftBar value={percent} tone={tone} marker={marker} />
    </Box>
  );
}

/** A stacked bar of a whole in parts, with a legend. `parts` colours are theme tones or a fixed colour. */
export function Segments({ parts }: { parts: { label: string; value: number; color: string; display?: ReactNode }[] }) {
  const total = parts.reduce((sum, part) => sum + part.value, 0);

  return (
    <Box sx={{ display: 'grid', gap: 1.5 }}>
      <Box sx={{ display: 'flex', height: 14, borderRadius: 7, overflow: 'hidden', gap: '2px', bgcolor: 'action.hover' }}>
        {total > 0 &&
          parts
            .filter((part) => part.value > 0)
            .map((part) => <Box key={part.label} sx={{ flex: part.value, bgcolor: part.color, minWidth: 4 }} />)}
      </Box>
      <Box sx={{ display: 'grid', gap: 0.75 }}>
        {parts.map((part) => (
          <Stack key={part.label} direction="row" sx={{ alignItems: 'center', gap: 1 }}>
            <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: part.color, flexShrink: 0 }} />
            <Typography variant="body2" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
              {part.label}
            </Typography>
            <Typography variant="body2" sx={{ ...NUMERIC, fontWeight: 600 }}>
              {part.display ?? part.value}
            </Typography>
          </Stack>
        ))}
      </Box>
    </Box>
  );
}

/** "Show all" at the foot of a widget. */
export function MoreLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Typography
      component={RouterLink}
      to={to}
      variant="body2"
      sx={{ color: 'primary.main', fontWeight: 600, textDecoration: 'none', width: 'fit-content', mt: 1, '&:hover': { textDecoration: 'underline' } }}
    >
      {children} ›
    </Typography>
  );
}

/** The quiet line a widget shows when it has nothing to list. */
export function EmptyNote({ children }: { children: ReactNode }) {
  return (
    <Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
      {children}
    </Typography>
  );
}
