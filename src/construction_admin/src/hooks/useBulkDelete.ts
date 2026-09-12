import { useState } from 'react';
import type { UseMutationResult } from '@tanstack/react-query';

import { useT } from '../i18n/useI18n';
import { scheduleUndoableDelete } from './undoQueue';

/**
 * The bulk sibling of `useDeleteWithConfirm`: confirms once for the whole
 * selection, then queues every row as a single undoable action — one
 * "N deleted. Undo" rather than N separate toasts, and undo cancels the
 * whole batch together, not row by row.
 */
export function useBulkDelete(mutation: UseMutationResult<void, unknown, string, unknown>) {
  const t = useT();
  const [pendingIds, setPendingIds] = useState<string[] | null>(null);

  const confirm = async () => {
    if (!pendingIds || pendingIds.length === 0) return;

    const ids = pendingIds;
    setPendingIds(null);

    scheduleUndoableDelete(
      t('common.deletedCountUndoMessage', { count: ids.length }),
      async () => {
        await Promise.all(ids.map((id) => mutation.mutateAsync(id)));
      },
    );
  };

  return {
    pendingIds,
    request: setPendingIds,
    cancel: () => setPendingIds(null),
    confirm,
  };
}
