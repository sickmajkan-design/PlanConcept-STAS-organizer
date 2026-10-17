import { useState } from 'react';
import type { UseMutationResult } from '@tanstack/react-query';

import { toApiError, type ApiError } from '../api/apiError';
import { useT } from '../i18n/useI18n';
import { scheduleUndoableDelete } from './undoQueue';
import { usePendingDeleteIds } from './usePendingDeleteIds';

/**
 * Holds the row awaiting confirmation, then — once confirmed — queues the
 * actual delete a few seconds out instead of running it immediately, so
 * confirming is not the last unrecoverable moment.
 *
 * The confirm dialog closes right away; the row itself stays visible in the
 * list until the deferred delete actually runs and the list refetches, and
 * the global undo snackbar (`UndoSnackbarHost`, mounted once in `AppLayout`)
 * is what lets the operator cancel it before that happens. Every caller
 * keeps working unmodified: `confirm` still just gets called from a
 * `ConfirmDialog`'s `onConfirm`, and `error` still surfaces a failed delete
 * whenever the deferred mutation eventually runs and rejects.
 *
 * With `immediate`, nothing is asked: the bin queues the delete straight away and the undo
 * snackbar is the whole safety net. For things that are easy to make again (a notice, a
 * task, a saved report), where "Are you sure?" costs more than the mistake it prevents.
 * The row is hidden at once through `hiddenIds`.
 */
export function useDeleteWithConfirm<T extends { id: string }>(
  mutation: UseMutationResult<void, unknown, string, unknown>,
  options: { immediate?: boolean } = {},
) {
  const t = useT();
  const [pending, setPending] = useState<T | null>(null);
  const hiddenIds = usePendingDeleteIds();

  const queue = (id: string) =>
    scheduleUndoableDelete(t('common.deletedUndoMessage'), () => mutation.mutateAsync(id), id);

  const confirm = async () => {
    if (!pending) return;

    const id = pending.id;
    setPending(null);

    queue(id);
  };

  const request = options.immediate
    ? (row: T | null) => {
        if (row) queue(row.id);
      }
    : setPending;

  // Converted here rather than in every page: the raw mutation error is
  // `unknown`, which cannot be rendered and infects the surrounding JSX type.
  const error: ApiError | null = mutation.isError ? toApiError(mutation.error) : null;

  return {
    pending,
    request,
    hiddenIds,
    cancel: () => setPending(null),
    confirm,
    isDeleting: mutation.isPending,
    error,
  };
}
