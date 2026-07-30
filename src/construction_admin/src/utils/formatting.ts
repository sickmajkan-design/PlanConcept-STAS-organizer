/** Turns API enum names such as `ProjectManager` into `Project Manager`. */
export function humanizeEnum(value: string): string {
  return value.replace(/(?<=[a-z])[A-Z]/g, (match) => ` ${match}`).trim();
}

/** Day-first date, the convention on site paperwork. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  return `${day}.${month}.${date.getFullYear()}.`;
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';

  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${formatDate(value)} ${hours}:${minutes}`;
}

/** Compact "how long ago" label, used for the last GPS fix on the live map. */
export function formatRelative(value: string | null | undefined): string {
  if (!value) return 'never';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'never';

  const elapsedMs = Date.now() - date.getTime();

  if (elapsedMs < 0 || elapsedMs < 60_000) return 'just now';

  const minutes = Math.floor(elapsedMs / 60_000);
  if (minutes < 60) return `${minutes} min ago`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} h ago`;

  const days = Math.floor(hours / 24);
  if (days < 7) return `${days} d ago`;

  return formatDate(value);
}

/** Avatar initials from a name, falling back to the first letter of an email. */
export function initialsOf(
  firstName?: string | null,
  lastName?: string | null,
  fallback?: string | null,
): string {
  const first = firstName?.trim() ?? '';
  const last = lastName?.trim() ?? '';

  if (first && last) return `${first[0]}${last[0]}`.toUpperCase();

  const single = first || last;
  if (single) return single[0]!.toUpperCase();

  const alt = fallback?.trim() ?? '';
  return alt ? alt[0]!.toUpperCase() : '?';
}
