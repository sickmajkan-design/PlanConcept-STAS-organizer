import { Box, Stack, Typography, alpha, useTheme } from '@mui/material';
import type { ReactNode } from 'react';

/** Money and quantities line up in columns when the digits share a width. */
export const numeric = { fontVariantNumeric: 'tabular-nums' } as const;

export interface Segment {
  key: string;
  label: string;
  value: number;
  color: string;
}

/**
 * One restrained palette for what a cost is made of, used everywhere a total
 * is broken down, so the same colour always means the same thing.
 */
export function useCostColors() {
  const theme = useTheme();

  return {
    labour: theme.palette.primary.main,
    material: '#3B4658',
    other: '#8B95A5',
    housing: '#2F7D6D',
    fuel: theme.palette.primary.main,
    service: '#3B4658',
    rental: '#2F7D6D',
  };
}

/** A stacked bar: how a total splits into its parts. */
export function CompositionBar({ segments, height = 10 }: { segments: Segment[]; height?: number }) {
  const total = segments.reduce((sum, s) => sum + Math.max(0, s.value), 0);

  return (
    <Box
      role="img"
      aria-label={segments.map((s) => `${s.label} ${Math.round((s.value / (total || 1)) * 100)}%`).join(', ')}
      sx={(theme) => ({
        display: 'flex',
        width: '100%',
        height,
        borderRadius: height / 2,
        overflow: 'hidden',
        bgcolor: alpha(theme.palette.text.primary, 0.07),
        gap: '2px',
      })}
    >
      {total > 0 &&
        segments
          .filter((s) => s.value > 0)
          .map((s) => (
            <Box key={s.key} sx={{ width: `${(s.value / total) * 100}%`, bgcolor: s.color, minWidth: 3 }} />
          ))}
    </Box>
  );
}

/** The key to a [CompositionBar]: colour dot, name, amount. */
export function CompositionLegend({
  segments,
  format,
  showShare = false,
}: {
  segments: Segment[];
  format: (value: number) => string;
  showShare?: boolean;
}) {
  const total = segments.reduce((sum, s) => sum + Math.max(0, s.value), 0);

  return (
    <Stack direction="row" useFlexGap sx={{ flexWrap: 'wrap', columnGap: 2.5, rowGap: 0.75 }}>
      {segments
        .filter((s) => s.value > 0)
        .map((s) => (
          <Stack key={s.key} direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <Box sx={{ width: 9, height: 9, borderRadius: '50%', bgcolor: s.color, flexShrink: 0 }} />
            <Typography variant="caption" color="text.secondary">
              {s.label}
            </Typography>
            <Typography variant="caption" sx={{ fontWeight: 700, ...numeric }}>
              {format(s.value)}
            </Typography>
            {showShare && total > 0 && (
              <Typography variant="caption" color="text.disabled" sx={numeric}>
                {Math.round((s.value / total) * 100)}%
              </Typography>
            )}
          </Stack>
        ))}
    </Stack>
  );
}

/** A headline figure with a rule in the accent colour. */
export function Stat({
  label,
  value,
  hint,
  accent = 'primary.main',
}: {
  label: string;
  value: string;
  hint?: string;
  accent?: string;
}) {
  return (
    <Box sx={{ flex: '1 1 160px', minWidth: 0, py: 0.5, px: 2, borderLeft: 3, borderColor: accent }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: -0.3, ...numeric }}>
        {value}
      </Typography>
      {hint && (
        <Typography variant="caption" color="text.secondary">
          {hint}
        </Typography>
      )}
    </Box>
  );
}

/** A boxed figure: smaller than [Stat], for a row of facts inside a dialog. */
export function Tile({
  label,
  value,
  hint,
  tone,
}: {
  label: string;
  value: string;
  hint?: string;
  tone?: 'warn';
}) {
  return (
    <Box
      sx={(theme) => ({
        flex: '1 1 140px',
        minWidth: 0,
        p: 1.75,
        borderRadius: 2,
        border: 1,
        borderColor: tone === 'warn' ? alpha(theme.palette.warning.main, 0.5) : 'divider',
        bgcolor: tone === 'warn' ? alpha(theme.palette.warning.main, 0.06) : 'background.paper',
      })}
    >
      <Typography variant="caption" color="text.secondary" sx={{ letterSpacing: 0.2 }}>
        {label}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1.25, ...numeric }}>
        {value}
      </Typography>
      {hint && (
        <Typography variant="caption" color="text.secondary">
          {hint}
        </Typography>
      )}
    </Box>
  );
}

/** A titled group of itemised lines with its own subtotal. */
export function CostSection({
  title,
  color,
  total,
  share,
  note,
  children,
}: {
  title: string;
  color?: string;
  total?: string;
  /** Percent of the grand total, 0–100. */
  share?: number;
  note?: string;
  children: ReactNode;
}) {
  return (
    <Box>
      <Stack
        direction="row"
        spacing={1.25}
        sx={{ alignItems: 'baseline', justifyContent: 'space-between', pb: 0.75, borderBottom: 1, borderColor: 'divider' }}
      >
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
          {color && <Box sx={{ width: 9, height: 9, borderRadius: '50%', bgcolor: color, flexShrink: 0 }} />}
          <Typography variant="overline" sx={{ fontWeight: 700, letterSpacing: 0.8, lineHeight: 1.6 }} noWrap>
            {title}
          </Typography>
        </Stack>
        {total && (
          <Typography variant="body2" sx={{ fontWeight: 700, whiteSpace: 'nowrap', ...numeric }}>
            {total}
            {share !== undefined && (
              <Typography component="span" variant="caption" color="text.disabled" sx={{ ml: 1 }}>
                {Math.round(share)}%
              </Typography>
            )}
          </Typography>
        )}
      </Stack>
      {note && (
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
          {note}
        </Typography>
      )}
      <Box>{children}</Box>
    </Box>
  );
}

/** One itemised cost: what it is, the detail underneath, the amount on the right. */
export function CostLine({
  primary,
  secondary,
  amount,
  meta,
  onClick,
}: {
  primary: ReactNode;
  secondary?: ReactNode;
  amount: string;
  /** A small right-aligned line under the amount (quantity × price, hours). */
  meta?: ReactNode;
  onClick?: () => void;
}) {
  return (
    <Stack
      direction="row"
      spacing={2}
      onClick={onClick}
      sx={{
        py: 1,
        alignItems: 'center',
        justifyContent: 'space-between',
        borderBottom: 1,
        borderColor: 'divider',
        '&:last-child': { borderBottom: 0 },
        cursor: onClick ? 'pointer' : 'default',
        '&:hover': onClick ? { bgcolor: 'action.hover' } : undefined,
      }}
    >
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {primary}
        </Typography>
        {secondary && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            {secondary}
          </Typography>
        )}
      </Box>
      <Box sx={{ textAlign: 'right', flexShrink: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 700, ...numeric }}>
          {amount}
        </Typography>
        {meta && (
          <Typography variant="caption" color="text.secondary" sx={numeric}>
            {meta}
          </Typography>
        )}
      </Box>
    </Stack>
  );
}

export function EmptyLine({ children }: { children: ReactNode }) {
  return (
    <Typography variant="body2" color="text.secondary" sx={{ py: 1.25 }}>
      {children}
    </Typography>
  );
}
