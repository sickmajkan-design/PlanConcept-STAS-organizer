import { attachmentsApi } from '../../api/attachments';
import type { AttachmentCategory, AttachmentOwnerType } from '../../api/types';

export interface PendingFile {
  id: string;
  file: File;
  category: AttachmentCategory;
}

const KEY = (ownerId: string) => `documents.unsent.${ownerId}`;

/**
 * Sends files that were picked while a record was still being made, now that it exists. One at a time and
 * never throwing: the record is already saved, so a file that fails is reported, not allowed to undo it.
 * Returns the names of the files that did not go.
 */
export async function uploadAfterCreate(
  ownerType: AttachmentOwnerType,
  ownerId: string,
  files: readonly PendingFile[],
): Promise<string[]> {
  const failed: string[] = [];

  for (const item of files) {
    try {
      await attachmentsApi.upload({ ownerType, ownerId, category: item.category, file: item.file });
    } catch {
      failed.push(item.file.name);
    }
  }

  return failed;
}

/** What did not go, kept for the page the person lands on next, which says so and asks them to add it there. */
export function rememberUnsent(ownerId: string, names: readonly string[]): void {
  try {
    if (names.length === 0) window.sessionStorage.removeItem(KEY(ownerId));
    else window.sessionStorage.setItem(KEY(ownerId), JSON.stringify(names));
  } catch {
    // Storage blocked: the person is simply not told, and finds the card without those files.
  }
}

/** The names left unsent for this record, once: reading them takes them out. */
export function takeUnsent(ownerId: string): string[] {
  try {
    const raw = window.sessionStorage.getItem(KEY(ownerId));

    if (!raw) return [];

    window.sessionStorage.removeItem(KEY(ownerId));
    const parsed: unknown = JSON.parse(raw);

    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : [];
  } catch {
    return [];
  }
}
