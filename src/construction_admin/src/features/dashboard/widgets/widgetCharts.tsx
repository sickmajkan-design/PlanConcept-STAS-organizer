import { Box, Stack, Typography, useTheme } from '@mui/material';
import { useId, useState, type ReactNode } from 'react';

/**
 * The few charts the widgets draw themselves, in SVG: a ring (a share of a whole), a donut (a whole in parts) and
 * a trend line you can run a finger along. They fill whatever box the widget has been given, so they grow with
 * a resize and fit a phone; every number they show is also text, so nothing depends on seeing the picture.
 */

/** A share of a whole as a ring with the figure in the middle. */
export function Ring({
  value,
  total,
  center,
  caption,
  color,
  size = 112,
}: {
  value: number;
  total: number;
  center: ReactNode;
  caption?: ReactNode;
  color?: string;
  size?: number;
}) {
  const theme = useTheme();
  const R = 50;
  const C = 2 * Math.PI * R;
  const share = total > 0 ? Math.max(0, Math.min(1, value / total)) : 0;
  const stroke = color ?? theme.palette.primary.main;

  return (
    <Box sx={{ position: 'relative', width: size, height: size, flexShrink: 0 }} role="img" aria-label={`${value} / ${total}`}>
      <svg viewBox="0 0 120 120" width="100%" height="100%">
        <circle cx="60" cy="60" r={R} fill="none" stroke={theme.palette.action.hover} strokeWidth="14" />
        <circle
          cx="60"
          cy="60"
          r={R}
          fill="none"
          stroke={stroke}
          strokeWidth="14"
          strokeLinecap="round"
          strokeDasharray={`${share * C} ${C}`}
          transform="rotate(-90 60 60)"
          style={{ transition: 'stroke-dasharray 1s cubic-bezier(0.22, 1, 0.36, 1)' }}
        />
      </svg>
      <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center', textAlign: 'center' }}>
        <Typography component="span" sx={{ fontWeight: 800, fontSize: size * 0.3, lineHeight: 1, letterSpacing: '-0.03em', fontVariantNumeric: 'tabular-nums' }}>
          {center}
        </Typography>
        {caption && (
          <Typography component="span" variant="caption" color="text.secondary" sx={{ lineHeight: 1.2 }}>
            {caption}
          </Typography>
        )}
      </Stack>
    </Box>
  );
}

export interface DonutPart {
  label: string;
  value: number;
  color: string;
  display: ReactNode;
}

/** A whole in parts: the ring, and a legend that says each part in words and figures. */
export function Donut({ parts, center }: { parts: DonutPart[]; center?: ReactNode }) {
  const theme = useTheme();
  const total = parts.reduce((sum, part) => sum + part.value, 0);
  const R = 46;
  const C = 2 * Math.PI * R;
  let offset = 0;

  return (
    <Box
      sx={{
        display: 'flex',
        gap: 2.5,
        alignItems: 'center',
        flexWrap: 'wrap',
        // Beside each other when the widget is wide, one under the other when it is narrow.
        '@container widget (max-width: 380px)': { justifyContent: 'center' },
      }}
    >
      <Box sx={{ position: 'relative', width: 132, height: 132, flexShrink: 0 }}>
        <svg viewBox="0 0 120 120" width="100%" height="100%" aria-hidden="true">
          <circle cx="60" cy="60" r={R} fill="none" stroke={theme.palette.action.hover} strokeWidth="14" />
          {parts.map((part) => {
            const length = total > 0 ? (part.value / total) * C : 0;
            const arc = (
              <circle
                key={part.label}
                cx="60"
                cy="60"
                r={R}
                fill="none"
                stroke={part.color}
                strokeWidth="14"
                strokeDasharray={`${Math.max(0, length - 3)} ${C}`}
                strokeDashoffset={-offset}
                transform="rotate(-90 60 60)"
              />
            );
            offset += length;
            return arc;
          })}
        </svg>
        {center && (
          <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }}>
            <Typography component="span" sx={{ fontWeight: 800, fontSize: '1.15rem', letterSpacing: '-0.02em', fontVariantNumeric: 'tabular-nums' }}>
              {center}
            </Typography>
          </Stack>
        )}
      </Box>
      <Box sx={{ display: 'grid', gap: 0.9, flex: '1 1 160px', minWidth: 0 }}>
        {parts.map((part) => (
          <Stack key={part.label} direction="row" sx={{ alignItems: 'center', gap: 1 }}>
            <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: part.color, flexShrink: 0 }} />
            <Typography variant="body2" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
              {part.label}
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums', whiteSpace: 'nowrap' }}>
              {part.display}
            </Typography>
          </Stack>
        ))}
      </Box>
    </Box>
  );
}

export interface TrendPoint {
  label: string;
  value: number;
}

/**
 * A line with a soft fill. Run a finger or the pointer along it and the figure above it becomes that point's; let
 * go and it goes back to the latest. `reference` draws a dashed level (an average). Fills the height it is given.
 */
export function TrendChart({
  points,
  format,
  color,
  reference,
  referenceLabel,
  minHeight = 140,
}: {
  points: TrendPoint[];
  format: (value: number) => string;
  color?: string;
  reference?: number;
  referenceLabel?: string;
  minHeight?: number;
}) {
  const theme = useTheme();
  const gradientId = useId();
  const stroke = color ?? theme.palette.primary.main;
  const [active, setActive] = useState<number | null>(null);

  if (points.length === 0) return null;

  const W = 320;
  const H = 140;
  const padX = 8;
  const top = 10;
  const bottom = 16;
  const values = points.map((p) => p.value);
  const max = Math.max(...values, reference ?? 0, 1) * 1.08;
  const x = (i: number) => (points.length === 1 ? W / 2 : padX + (i * (W - 2 * padX)) / (points.length - 1));
  const y = (v: number) => H - bottom - (v / max) * (H - top - bottom);
  const line = points.map((p, i) => `${i === 0 ? 'M' : 'L'}${x(i).toFixed(1)} ${y(p.value).toFixed(1)}`).join(' ');
  const area = `${line} L${x(points.length - 1)} ${H - bottom} L${x(0)} ${H - bottom} Z`;
  const shown = active ?? points.length - 1;

  const move = (event: { clientX: number; currentTarget: Element }) => {
    const box = event.currentTarget.getBoundingClientRect();
    const ratio = (event.clientX - box.left) / box.width;
    const index = Math.round(((ratio * W - padX) / (W - 2 * padX)) * (points.length - 1));
    setActive(Math.max(0, Math.min(points.length - 1, index)));
  };

  return (
    <Box sx={{ flex: 1, minHeight, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
      {/* The figure for the point under the finger; at rest the widget's own headline says the latest, so this
          keeps its place (no jump) and stays empty. */}
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1, minHeight: 28 }}>
        <Typography variant="caption" color="text.secondary">
          {active !== null ? points[shown]!.label : ''}
        </Typography>
        <Typography component="span" sx={{ fontWeight: 800, fontSize: '1.15rem', letterSpacing: '-0.02em', fontVariantNumeric: 'tabular-nums' }}>
          {active !== null ? format(points[shown]!.value) : ''}
        </Typography>
      </Stack>
      <Box sx={{ flex: 1, minHeight: 0, position: 'relative' }}>
        <svg
          viewBox={`0 0 ${W} ${H}`}
          preserveAspectRatio="none"
          style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', touchAction: 'pan-y' }}
          role="img"
          aria-label={points.map((p) => `${p.label}: ${format(p.value)}`).join(', ')}
          onPointerDown={(event) => {
            event.currentTarget.setPointerCapture(event.pointerId);
            move(event);
          }}
          onPointerMove={(event) => {
            if (event.buttons || event.pointerType === 'mouse') move(event);
          }}
          onPointerLeave={() => setActive(null)}
          onPointerUp={() => setActive(null)}
        >
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor={stroke} stopOpacity="0.28" />
              <stop offset="1" stopColor={stroke} stopOpacity="0" />
            </linearGradient>
          </defs>
          {[0.33, 0.66].map((f) => (
            <line key={f} x1="0" x2={W} y1={H - bottom - f * (H - top - bottom)} y2={H - bottom - f * (H - top - bottom)} stroke={theme.palette.divider} strokeWidth="1" vectorEffect="non-scaling-stroke" />
          ))}
          {reference !== undefined && (
            <line x1="0" x2={W} y1={y(reference)} y2={y(reference)} stroke={theme.palette.text.secondary} strokeWidth="1.5" strokeDasharray="4 4" vectorEffect="non-scaling-stroke">
              {referenceLabel && <title>{referenceLabel}</title>}
            </line>
          )}
          <path d={area} fill={`url(#${gradientId})`} />
          <path d={line} fill="none" stroke={stroke} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
          {active !== null && (
            <line x1={x(active)} x2={x(active)} y1={top - 4} y2={H - bottom} stroke={theme.palette.text.secondary} strokeWidth="1" strokeDasharray="3 3" vectorEffect="non-scaling-stroke" />
          )}
        </svg>
        {/* The marker is HTML, not SVG, so a stretched viewBox cannot turn the dot into an ellipse. */}
        <Box
          sx={{
            position: 'absolute',
            left: `${(x(shown) / W) * 100}%`,
            top: `${(y(points[shown]!.value) / H) * 100}%`,
            width: 11,
            height: 11,
            ml: '-5.5px',
            mt: '-5.5px',
            borderRadius: '50%',
            bgcolor: 'background.paper',
            border: `2.5px solid ${stroke}`,
            pointerEvents: 'none',
          }}
        />
      </Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
        <Typography variant="caption" color="text.secondary">
          {points[0]!.label}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {points[points.length - 1]!.label}
        </Typography>
      </Stack>
    </Box>
  );
}

export interface BarGroup {
  label: string;
  values: number[];
}

/**
 * Grouped bars drawn in plain HTML and CSS, so the chart never measures its box: a chart that is told its own
 * size from the box it sits in can feed on itself and grow without end in a card that is as tall as its content
 * (which is what a card on a phone is). Heights are shares of the largest value; the first, last and every few
 * labels in between are written, so they never run into each other. Press or point at a group to read it.
 */
export function BarsChart({
  groups,
  names,
  colors,
  format,
  minHeight = 180,
}: {
  groups: BarGroup[];
  names: string[];
  colors: string[];
  format: (value: number) => string;
  minHeight?: number;
}) {
  const theme = useTheme();
  const [active, setActive] = useState<number | null>(null);

  if (groups.length === 0) return null;

  const max = Math.max(1, ...groups.flatMap((group) => group.values));
  const step = Math.max(1, Math.ceil(groups.length / 6));
  const shown = active ?? null;

  return (
    <Box sx={{ flex: 1, minHeight, display: 'flex', flexDirection: 'column', gap: 1, minWidth: 0 }}>
      <Stack direction="row" sx={{ gap: 2, flexWrap: 'wrap', alignItems: 'baseline', minHeight: 24 }}>
        {shown === null ? (
          names.map((name, index) => (
            <Stack key={name} direction="row" sx={{ alignItems: 'center', gap: 0.75 }}>
              <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: colors[index] }} />
              <Typography variant="caption" color="text.secondary">
                {name}
              </Typography>
            </Stack>
          ))
        ) : (
          <>
            <Typography variant="caption" color="text.secondary">
              {groups[shown]!.label}
            </Typography>
            {groups[shown]!.values.map((value, index) => (
              <Typography key={names[index]} variant="caption" sx={{ fontWeight: 700, color: colors[index], fontVariantNumeric: 'tabular-nums' }}>
                {names[index]}: {format(value)}
              </Typography>
            ))}
          </>
        )}
      </Stack>

      <Box sx={{ position: 'relative', flex: 1, minHeight: 0 }}>
        {[0.25, 0.5, 0.75, 1].map((f) => (
          <Box key={f} sx={{ position: 'absolute', left: 0, right: 0, bottom: `${f * 100}%`, borderTop: `1px solid ${theme.palette.divider}` }} />
        ))}
        <Box
          sx={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'flex-end', gap: groups.length > 20 ? '2px' : '6px', touchAction: 'pan-y' }}
          onPointerLeave={() => setActive(null)}
        >
          {groups.map((group, index) => (
            <Box
              key={group.label + index}
              onPointerEnter={() => setActive(index)}
              onPointerDown={() => setActive(index)}
              aria-label={`${group.label}: ${group.values.map((value, i) => `${names[i]} ${format(value)}`).join(', ')}`}
              sx={{ flex: 1, minWidth: 0, height: '100%', display: 'flex', alignItems: 'flex-end', gap: '2px', opacity: shown === null || shown === index ? 1 : 0.45, transition: 'opacity 0.15s' }}
            >
              {group.values.map((value, i) => (
                <Box
                  key={i}
                  sx={{
                    flex: 1,
                    minWidth: 2,
                    height: `${(value / max) * 100}%`,
                    minHeight: value > 0 ? 2 : 0,
                    bgcolor: colors[i],
                    borderRadius: '4px 4px 0 0',
                    transition: 'height 0.6s cubic-bezier(0.22, 1, 0.36, 1)',
                    '@media (prefers-reduced-motion: reduce)': { transition: 'none' },
                  }}
                />
              ))}
            </Box>
          ))}
        </Box>
      </Box>

      <Box sx={{ display: 'flex', gap: groups.length > 20 ? '2px' : '6px', minHeight: 16 }}>
        {groups.map((group, index) => (
          <Box key={group.label + index} sx={{ flex: 1, minWidth: 0, textAlign: 'center', overflow: 'visible' }}>
            {(index % step === 0 || index === groups.length - 1) && (
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: '0.68rem', whiteSpace: 'nowrap' }}>
                {group.label}
              </Typography>
            )}
          </Box>
        ))}
      </Box>
    </Box>
  );
}
