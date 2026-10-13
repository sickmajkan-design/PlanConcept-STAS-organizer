import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useRef, useState } from 'react';

import {
  attachmentsApi,
  type UploadAttachmentInput,
  type UploadOptions,
} from '../../api/attachments';
import type { Attachment } from '../../api/types';
import { attachmentKeys } from '../attachments/useAttachments';

export type QueueStatus = 'queued' | 'uploading' | 'done' | 'error' | 'cancelled';

export interface QueueItem {
  id: string;
  file: File;
  status: QueueStatus;
  /** 0..1 while uploading. */
  progress: number;
  error: string | null;
  /** A local picture of an image file, for the row; null for anything a browser cannot draw. */
  previewUrl: string | null;
}

type Uploader = (input: UploadAttachmentInput, options?: UploadOptions) => Promise<Attachment>;

/** Browsers draw these; a HEIC photo gets an icon instead. */
const PREVIEWABLE = /^image\/(jpeg|png|webp|gif)$/;

let counter = 0;

/**
 * The files of one upload, each with its own state.
 *
 * Several go out at once (three by default), each reports its own progress, and one failing never stops
 * the rest: it is marked, kept, and can be sent again by itself. Cancelling aborts what is in flight and
 * leaves the finished ones alone. Files can be added while others are still going.
 */
export function useUploadQueue(options: { concurrency?: number; uploader?: Uploader } = {}) {
  const concurrency = options.concurrency ?? 3;
  const uploader = options.uploader ?? attachmentsApi.upload;
  const queryClient = useQueryClient();

  const [items, setItems] = useState<QueueItem[]>([]);
  const [started, setStarted] = useState(false);
  const itemsRef = useRef<QueueItem[]>([]);
  const builder = useRef<((file: File) => UploadAttachmentInput) | null>(null);
  const controllers = useRef(new Map<string, AbortController>());

  const commit = useCallback((next: QueueItem[]) => {
    itemsRef.current = next;
    setItems(next);
  }, []);

  const patch = useCallback(
    (id: string, change: Partial<QueueItem>) =>
      commit(itemsRef.current.map((item) => (item.id === id ? { ...item, ...change } : item))),
    [commit],
  );

  const pump = useCallback(() => {
    if (!builder.current) return;

    const build = builder.current;

    while (
      controllers.current.size < concurrency &&
      itemsRef.current.some((item) => item.status === 'queued')
    ) {
      const item = itemsRef.current.find((i) => i.status === 'queued')!;
      const controller = new AbortController();
      controllers.current.set(item.id, controller);
      patch(item.id, { status: 'uploading', progress: 0, error: null });

      void uploader(build(item.file), {
        signal: controller.signal,
        onProgress: (fraction) => patch(item.id, { progress: fraction }),
      })
        .then(() => {
          patch(item.id, { status: 'done', progress: 1 });
        })
        .catch((error: unknown) => {
          const aborted = controller.signal.aborted;
          patch(item.id, {
            status: aborted ? 'cancelled' : 'error',
            error: aborted ? null : error instanceof Error ? error.message : String(error),
          });
        })
        .finally(() => {
          controllers.current.delete(item.id);

          if (controllers.current.size === 0) {
            // One refresh when a burst ends rather than one per file.
            void queryClient.invalidateQueries({ queryKey: attachmentKeys.all });
          }

          pump();
        });
    }
  }, [concurrency, patch, queryClient, uploader]);

  const add = useCallback(
    (files: readonly File[]) => {
      const added: QueueItem[] = files.map((file) => ({
        id: `f${(counter += 1)}`,
        file,
        status: 'queued',
        progress: 0,
        error: null,
        previewUrl: PREVIEWABLE.test(file.type) ? URL.createObjectURL(file) : null,
      }));

      commit([...itemsRef.current, ...added]);
      // Files dropped while an upload is running join it at once.
      pump();
    },
    [commit, pump],
  );

  const release = (item: QueueItem) => {
    if (item.previewUrl) URL.revokeObjectURL(item.previewUrl);
  };

  const remove = useCallback(
    (id: string) => {
      controllers.current.get(id)?.abort();
      const gone = itemsRef.current.find((i) => i.id === id);

      if (gone) release(gone);
      commit(itemsRef.current.filter((i) => i.id !== id));
    },
    [commit],
  );

  const clear = useCallback(() => {
    for (const controller of controllers.current.values()) controller.abort();
    controllers.current.clear();
    itemsRef.current.forEach(release);
    builder.current = null;
    setStarted(false);
    commit([]);
  }, [commit]);

  /** Starts sending, and keeps sending whatever is added or retried afterwards. */
  const start = useCallback(
    (build: (file: File) => UploadAttachmentInput) => {
      builder.current = build;
      setStarted(true);
      pump();
    },
    [pump],
  );

  const retry = useCallback(
    (id?: string) => {
      commit(
        itemsRef.current.map((item) =>
          (id ? item.id === id : true) && (item.status === 'error' || item.status === 'cancelled')
            ? { ...item, status: 'queued', progress: 0, error: null }
            : item,
        ),
      );
      pump();
    },
    [commit, pump],
  );

  const cancel = useCallback(() => {
    for (const controller of controllers.current.values()) controller.abort();
    commit(
      itemsRef.current.map((item) =>
        item.status === 'queued' ? { ...item, status: 'cancelled' } : item,
      ),
    );
  }, [commit]);

  useEffect(
    () => () => {
      for (const controller of controllers.current.values()) controller.abort();
      itemsRef.current.forEach(release);
    },
    [],
  );

  const count = (status: QueueStatus) => items.filter((i) => i.status === status).length;
  const running = started && count('uploading') + count('queued') > 0;

  return {
    items,
    add,
    remove,
    clear,
    start,
    retry,
    cancel,
    running,
    done: count('done'),
    failed: count('error'),
    cancelled: count('cancelled'),
    started,
  };
}
