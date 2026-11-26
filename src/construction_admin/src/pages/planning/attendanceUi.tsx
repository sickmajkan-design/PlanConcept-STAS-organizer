import { memo, type ReactNode } from 'react';
import { Stack, Typography } from '@mui/material';
import { alpha, useTheme, type Theme } from '@mui/material/styles';

import type { PersonAttendance } from '../../features/planning/attendance';
import { FRESH_PING_MINUTES, LATE_AFTER_MINUTES, ON_SITE_METERS } from '../../features/planning/attendance';
import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';
import type { BindCard } from './equipmentUi';

/** The panel's own Material outlined icons (AccessTime, LocationOn), as the inner markup of a 24 px svg. Apache 2.0. */
const SHAPES = {
  clock: `<path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2M12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8m.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z"/>`,
  pin: `<path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7M7 9c0-2.76 2.24-5 5-5s5 2.24 5 5c0 2.88-2.88 7.19-5 9.88C9.92 16.21 7 11.85 7 9"/><circle cx="12" cy="9" r="2.5"/>`,
} as const;

type Kind = keyof typeof SHAPES;
type Tone = 'good' | 'warn' | 'bad' | 'neutral';
type Glyph = 'check' | 'bang' | 'dash' | 'question';

/** How a state looks: its colour and the small mark in the corner (never the colour alone). */
function look(kind: Kind, att: PersonAttendance): { tone: Tone; glyph: Glyph } {
  if (kind === 'clock') {
    switch (att.clock.state) {
      case 'in':
        return { tone: 'good', glyph: 'check' };
      case 'late':
        return { tone: 'warn', glyph: 'bang' };
      case 'out':
        return { tone: 'neutral', glyph: 'check' };
      default:
        return { tone: 'neutral', glyph: 'dash' };
    }
  }

  switch (att.pin?.state) {
    case 'ok':
      return { tone: 'good', glyph: 'check' };
    case 'wrong':
      return { tone: 'bad', glyph: 'bang' };
    case 'unsure':
      return { tone: 'neutral', glyph: 'question' };
    default:
      return { tone: 'neutral', glyph: 'dash' };
  }
}

const toneColor = (tone: Tone, theme: Theme): string => {
  const dark = theme.palette.mode === 'dark';

  if (tone === 'good') return dark ? '#66bb6a' : '#1b7f3b';
  if (tone === 'warn') return '#e07b00';
  if (tone === 'bad') return dark ? '#ef5350' : '#c62828';

  return theme.palette.text.secondary;
};

const IMAGES = new Map<string, string>();

function glyphSvg(glyph: Glyph): string {
  const common = 'fill="none" stroke="#fff" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"';
  if (glyph === 'check') return `<path d="M19.6 19.2l1.7 1.7 3-3.4" ${common}/>`;
  if (glyph === 'dash') return `<path d="M19.8 19h4.4" ${common}/>`;

  return `<text x="22" y="21.6" text-anchor="middle" font-size="7.5" font-weight="700" font-family="sans-serif" fill="#fff">${glyph === 'bang' ? '!' : '?'}</text>`;
}

/** One cached image per kind, tone, mark and colour scheme: the icon with its small mark, so each status is one element. */
function imageFor(kind: Kind, tone: Tone, glyph: Glyph, theme: Theme): string {
  const color = toneColor(tone, theme);
  const ring = theme.palette.background.paper;
  const key = `${kind}|${tone}|${glyph}|${color}|${ring}`;
  let image = IMAGES.get(key);

  if (!image) {
    const dot = tone === 'neutral' ? '#8e8e93' : color;
    const svg =
      `<svg xmlns="http://www.w3.org/2000/svg" width="28" height="26" viewBox="0 0 28 26">` +
      `<g transform="translate(4 3) scale(0.8333)" fill="${color}">${SHAPES[kind]}</g>` +
      `<circle cx="22" cy="19" r="4.6" fill="${dot}" stroke="${ring}" stroke-width="1.2"/>${glyphSvg(glyph)}</svg>`;
    image = `url("data:image/svg+xml,${encodeURIComponent(svg)}")`;
    IMAGES.set(key, image);
  }

  return image;
}

/** The clock or the pin of one worker, with its state in the corner. One element, memoised on plain values. */
export const AttendanceIcon = memo(function AttendanceIcon({
  kind,
  att,
  bind,
}: {
  kind: Kind;
  att: PersonAttendance;
  bind: BindCard;
}) {
  const t = useT();
  const theme = useTheme();
  const { tone, glyph } = look(kind, att);
  const title = kind === 'clock' ? clockTitle(att, t) : pinTitle(att, t);

  return (
    <span
      tabIndex={0}
      role="img"
      aria-label={title}
      {...bind(() => (kind === 'clock' ? <ClockCard att={att} /> : <PinCard att={att} />))}
      onClick={(event) => event.stopPropagation()}
      style={{
        display: 'inline-block',
        flex: 'none',
        width: 28,
        height: 26,
        borderRadius: 8,
        backgroundColor: tone === 'neutral' ? theme.palette.action.hover : alpha(toneColor(tone, theme), 0.14),
        backgroundImage: imageFor(kind, tone, glyph, theme),
        backgroundRepeat: 'no-repeat',
      }}
    />
  );
});

type T = (key: MessageKey, values?: Record<string, string | number>) => string;

const key = (k: string) => k as MessageKey;

function clockTitle(att: PersonAttendance, t: T): string {
  const c = att.clock;

  if (c.state === 'in') return t(key('planning.att.in'), { time: c.at ?? '' });
  if (c.state === 'out') return t(key('planning.att.out'), { from: c.at ?? '', to: c.until ?? '' });
  if (c.state === 'late') return t(key('planning.att.late'), { minutes: c.minutesLate ?? 0 });

  return t(key('planning.att.wait'));
}

function pinTitle(att: PersonAttendance, t: T): string {
  return t(key(`planning.att.pin.${att.pin?.state ?? 'none'}`));
}

function clockLines(att: PersonAttendance, t: T): string[] {
  const c = att.clock;

  if (c.state === 'late')
    return [t(key('planning.att.notIn')), t(key('planning.att.lateLine'), { start: c.start ?? '', minutes: c.minutesLate ?? 0 }), t(key('planning.att.limit'), { minutes: LATE_AFTER_MINUTES, limit: c.limit ?? '' })];
  if (c.state === 'wait')
    return c.start ? [t(key('planning.att.starts'), { start: c.start }), t(key('planning.att.notLateYet'))] : [t(key('planning.att.noStart')), t(key('planning.att.noStartNote'))];
  if (c.state === 'in' && c.start) return [t(key('planning.att.starts'), { start: c.start })];

  return [];
}

function pinLines(att: PersonAttendance, t: T): string[] {
  const p = att.pin;
  if (!p) return [];

  const ago = p.pingAgo != null ? t(key('planning.att.pingAgo'), { minutes: p.pingAgo }) : null;

  switch (p.state) {
    case 'ok':
    case 'wrong': {
      const first =
        p.source === 'clockIn'
          ? t(key('planning.att.fromClockIn'), { meters: p.meters ?? 0 })
          : t(key('planning.att.fromSite'), { meters: p.meters ?? 0 });

      return [first, ...(p.state === 'wrong' ? [t(key('planning.att.allowed'), { meters: ON_SITE_METERS })] : []), ...(ago ? [ago] : [])];
    }
    case 'unsure':
      return [t(key('planning.att.accuracy'), { accuracy: p.accuracy ?? 0, meters: ON_SITE_METERS }), ...(ago ? [ago] : [])];
    case 'nosite':
      return [t(key('planning.att.noCoordinates'))];
    default:
      return [t(key('planning.att.noPing'), { minutes: FRESH_PING_MINUTES }), ...(ago ? [ago] : [])];
  }
}

function Card({ title, lines, tone }: { title: string; lines: string[]; tone: Tone }): ReactNode {
  const theme = useTheme();

  return (
    <Stack spacing={0.5}>
      <Typography variant="subtitle2" sx={{ fontWeight: 700, color: toneColor(tone, theme) }}>
        {title}
      </Typography>
      {lines.map((line) => (
        <Typography key={line} variant="body2" color="text.secondary">
          {line}
        </Typography>
      ))}
    </Stack>
  );
}

export function ClockCard({ att }: { att: PersonAttendance }) {
  const t = useT() as T;

  return <Card title={clockTitle(att, t)} lines={clockLines(att, t)} tone={look('clock', att).tone} />;
}

export function PinCard({ att }: { att: PersonAttendance }) {
  const t = useT() as T;

  return <Card title={pinTitle(att, t)} lines={pinLines(att, t)} tone={look('pin', att).tone} />;
}
