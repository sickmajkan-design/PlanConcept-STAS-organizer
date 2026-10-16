import { toApiError } from '../../api/apiError';
import { attachmentsApi, type UploadAttachmentInput } from '../../api/attachments';
import type { AttachmentCategory, AttachmentOwnerType } from '../../api/types';

export interface PendingFile {
  id: string;
  file: File;
  category: AttachmentCategory;
}

const KEY = (ownerId: string) => `documents.unsent.${ownerId}`;

/** How long to wait when the server says it is being sent files too fast, at most; and how many times. */
const MAX_WAIT_SECONDS = 65;
const MAX_TRIES = 3;

const sleep = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));

/**
 * One upload that, when the server asks to slow down (429), waits as long as it says and tries again, a
 * few times. Anything else is the caller's to handle.
 */
export async function uploadPatiently(input: UploadAttachmentInput): Promise<void> {
  for (let attempt = 1; ; attempt += 1) {
    try {
      await attachmentsApi.upload(input);

      return;
    } catch (error) {
      const failure = toApiError(error);

      if (failure.status !== 429 || attempt >= MAX_TRIES) throw error;

      await sleep(Math.min(failure.retryAfterSeconds ?? 10, MAX_WAIT_SECONDS) * 1000 + 250);
    }
  }
}

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
      await uploadPatiently({ ownerType, ownerId, category: item.category, file: item.file });
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
