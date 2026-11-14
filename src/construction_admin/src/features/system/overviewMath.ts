import type { HostStatus, Outage } from '../../api/system';
import type { Tone } from './serverUi';

const DAY = 86_400_000;

/** One-minute load per core, as a percentage. 100 means every core was busy; it can go higher. Null when the host does not say. */
export function cpuPercent(host: HostStatus): number | null {
  return host.load1 !== null && host.load1 !== undefined && host.cores > 0 ? (host.load1 / host.cores) * 100 : null;
}

/** Memory in use, as a percentage. Null when the host does not say. */
export function memoryUsedPercent(host: HostStatus): number | null {
  const { memoryTotalBytes: total, memoryAvailableBytes: available } = host;

  return total && available !== null && available !== undefined ? ((total - available) / total) * 100 : null;
}

/** The ring's colour: fine below the first line, a warning above it, urgent above the second. */
export function toneAt(percent: number, warnAt: number, urgentAt: number): Tone {
  return percent >= urgentAt ? 'error' : percent >= warnAt ? 'warning' : 'success';
}

/**
 * One flag per day for the last `days` days, oldest first: true when the application was down for part of it. Days are
 * rolling 24-hour windows ending now, which is how a person reads "the last 30 days".
 */
export function outageDays(outages: Outage[], days: number, now: number): boolean[] {
  return Array.from({ length: days }, (_, index) => {
    const end = now - (days - 1 - index) * DAY;
    const start = end - DAY;

    return outages.some((outage) => Date.parse(outage.startedAtUtc) < end && Date.parse(outage.endedAtUtc) > start);
  });
}

const UNIT_SECONDS: Record<string, [string, number]> = {
  second: ['s', 1],
  minute: ['min', 60],
  hour: ['h', 3600],
  day: ['d', 86_400],
  week: ['d', 7 * 86_400],
  month: ['d', 30 * 86_400],
  year: ['d', 365 * 86_400],
};

/**
 * Docker words a container's age in English ("Up 3 days (healthy)", "Up About an hour"). The panel shows it the way the
 * rest of the page writes durations: `3 d`, `1 h`, `12 min`. Anything it does not recognise is shown as Docker wrote it.
 */
export function containerUptime(status: string): string {
  const match = /^Up (?:(Less than a) |(?:About )?(an?|\d+) )?(second|minute|hour|day|week|month|year)s?/i.exec(status);

  if (!match) {
    return status.replace(/^Up /, '');
  }

  const [, lessThan, count, unit] = match;
  const [label, seconds] = UNIT_SECONDS[unit!.toLowerCase()]!;
  const amount = lessThan ? 1 : count && /^\d+$/.test(count) ? Number(count) : 1;
  const total = amount * seconds;

  if (lessThan) return `< 1 ${label}`;
  if (label === 'd') return `${Math.round(total / 86_400)} d`;

  return `${amount} ${label}`;
}
