import { useState } from 'react';
import type { UseMutationResult } from '@tanstack/react-query';

import { toApiError, type ApiError } from '../api/apiError';

/**
 * Holds the row awaiting confirmation and runs the delete once confirmed.
 *
 * A failed delete deliberately leaves the dialog open: the mutation's own
 * error state is what the page shows, and closing the dialog would suggest
 * the row went away when it did not.
 */
export function useDeleteWithConfirm<T extends { id: string }>(
  mutation: UseMutationResult<void, unknown, string, unknown>,
) {
  const [pending, setPending] = useState<T | null>(null);

  const confirm = async () => {
    if (!pending) return;

    try {
      await mutation.mutateAsync(pending.id);
      setPending(null);
    } catch {
      // Surfaced by the caller through the mutation's error state.
    }
  };

  // Converted here rather than in every page: the raw mutation error is
  // `unknown`, which cannot be rendered and infects the surrounding JSX type.
  const error: ApiError | null = mutation.isError ? toApiError(mutation.error) : null;

  return {
    pending,
    request: setPending,
    cancel: () => setPending(null),
    confirm,
    isDeleting: mutation.isPending,
    error,
  };
}
