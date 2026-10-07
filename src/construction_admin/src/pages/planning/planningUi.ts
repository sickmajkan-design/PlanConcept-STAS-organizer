import { alpha } from '@mui/material/styles';

import { addDays, diffDays } from '../../features/planning/planningLogic';

const SITE_COLORS = ['#1c7ed6', '#2f9e44', '#d9480f', '#9c36b5', '#0b7285', '#c2255c', '#8a7a00', '#5f6b7a'];

const assigned = new Map<string, number>();

/**
 * A site keeps its colour for as long as the page is open, and no two sites share one until there are
 * more sites than colours. Handed out in the order sites are first drawn, which a hash of the id cannot
 * promise: with a handful of sites it regularly gives two of them the same colour.
 */
export function siteColor(projectId: string): string {
  let index = assigned.get(projectId);

  if (index === undefined) {
    index = assigned.size;
    assigned.set(projectId, index);
  }

  return SITE_COLORS[index % SITE_COLORS.length];
}

export const siteChipSx = (projectId: string) => ({
  bgcolor: alpha(siteColor(projectId), 0.16),
  color: siteColor(projectId),
});

export const AWAY_COLOR = '#b26a00';

export const awayChipSx = {
  bgcolor: alpha(AWAY_COLOR, 0.14),
  color: AWAY_COLOR,
  backgroundImage: `repeating-linear-gradient(135deg, ${alpha(AWAY_COLOR, 0.2)} 0 6px, transparent 6px 12px)`,
};

export const initials = (name: string): string =>
  name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase();

/** `2026-10-09` as `09.10.` */
export const shortDate = (value: string): string => `${value.slice(8)}.${value.slice(5, 7)}.`;

export const WEEKDAYS = ['Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub', 'Ned'];

/** The window to fetch: the range on screen, the focused day, and the next thirty days for the stand-in list. */
export function fetchWindow(
  range: { from: string; to: string },
  focusDay: string,
  today: string,
  maxDays: number,
): { from: string; to: string } {
  let from = [range.from, focusDay, today].sort()[0];
  let to = [range.to, focusDay, addDays(today, 30)].sort().reverse()[0];

  if (diffDays(from, to) + 1 > maxDays) {
    from = range.from;
    to = to > addDays(from, maxDays - 1) ? addDays(from, maxDays - 1) : to;
  }

  return { from, to };
}
