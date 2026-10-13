/** Why a file was not taken. */
export type RejectReason = 'size' | 'type' | 'duplicate' | 'count' | 'empty';

export interface Rejection {
  file: File;
  reason: RejectReason;
}

export interface FileRules {
  /** Comma separated extensions with the dot, as the `accept` attribute takes them. */
  accept: string;
  maxBytes: number;
  /** Most files in the queue at once, counting `existing`. Unlimited when omitted. */
  maxFiles?: number;
  /** Files already taken, so a repeat and the count are judged against them. */
  existing?: readonly File[];
}

const sameFile = (a: File, b: File) =>
  a.name === b.name && a.size === b.size && a.lastModified === b.lastModified;

const extensionOf = (name: string) => {
  const dot = name.lastIndexOf('.');

  return dot < 0 ? '' : name.slice(dot).toLowerCase();
};

/**
 * Sorts dropped or picked files into those to take and those to leave, with the reason for each. The
 * server checks the same things again; doing it here refuses a 30 MB file before it is sent.
 * A folder arrives as an empty, untyped "file" and is turned away as empty.
 */
export function validateFiles(
  files: readonly File[],
  rules: FileRules,
): { accepted: File[]; rejected: Rejection[] } {
  const allowed = new Set(
    rules.accept
      .split(',')
      .map((e) => e.trim().toLowerCase())
      .filter(Boolean),
  );

  const taken: File[] = [...(rules.existing ?? [])];
  const accepted: File[] = [];
  const rejected: Rejection[] = [];

  for (const file of files) {
    let reason: RejectReason | null = null;

    if (!allowed.has(extensionOf(file.name))) {
      reason = file.size === 0 && file.type === '' ? 'empty' : 'type';
    } else if (file.size === 0) {
      reason = 'empty';
    } else if (file.size > rules.maxBytes) {
      reason = 'size';
    } else if (taken.some((t) => sameFile(t, file))) {
      reason = 'duplicate';
    } else if (rules.maxFiles !== undefined && taken.length >= rules.maxFiles) {
      reason = 'count';
    }

    if (reason) {
      rejected.push({ file, reason });
    } else {
      taken.push(file);
      accepted.push(file);
    }
  }

  return { accepted, rejected };
}
