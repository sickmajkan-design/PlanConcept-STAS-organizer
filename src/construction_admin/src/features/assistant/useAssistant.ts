import { useMutation, useQuery } from '@tanstack/react-query';
import { useCallback, useState } from 'react';

import {
  assistantApi,
  type AssistantAnswer,
  type AssistantMessage,
} from '../../api/assistant';
import { toApiError } from '../../api/apiError';
import { useI18n } from '../../i18n/useI18n';

/** One line in the panel. Errors are turns too, so the thread reads in order. */
export interface AssistantTurn {
  id: string;
  role: 'user' | 'assistant' | 'error';
  text: string;
  toolsUsed?: string[];
  truncated?: boolean;
}

/**
 * How many earlier messages travel with each question.
 *
 * The server refuses more than twenty, so trimming here is not a second guard
 * — it is what stops a long afternoon's conversation being refused outright
 * once it crosses the line. The panel keeps showing the whole thread; only
 * what is sent is trimmed.
 */
export const HISTORY_SENT = 20;

let nextId = 0;

function turnId(): string {
  nextId += 1;

  return `turn-${nextId}`;
}

export function useAssistantStatus() {
  return useQuery({
    queryKey: ['assistant', 'status'],
    queryFn: assistantApi.status,

    // The answer changes only when the installation is reconfigured and
    // restarted, so asking once per session is enough.
    staleTime: Infinity,
    retry: false,
  });
}

export function useAssistant() {
  const { locale } = useI18n();
  const [turns, setTurns] = useState<AssistantTurn[]>([]);

  // The history travels as a mutation variable rather than being read from
  // state inside `mutationFn`. React Query re-reads the options it was last
  // rendered with when the mutation actually runs, and by then the question
  // being asked has already been appended to `turns` — so the assistant was
  // sent its own unanswered question as the last thing it had said.
  const mutation = useMutation({
    mutationFn: (input: { message: string; history: AssistantMessage[] }) =>
      assistantApi.ask({ ...input, locale }),
  });

  const { mutateAsync } = mutation;

  const ask = useCallback(
    async (message: string) => {
      const asked = message.trim();

      if (!asked) {
        return;
      }

      // Taken before the new turn is appended: this is what was said *before*
      // the question, which is exactly what the conversation is.
      const history = toHistory(turns);

      setTurns((current) => [
        ...current,
        { id: turnId(), role: 'user', text: asked },
      ]);

      try {
        const answer: AssistantAnswer = await mutateAsync({ message: asked, history });

        setTurns((current) => [
          ...current,
          {
            id: turnId(),
            role: 'assistant',
            text: answer.text,
            toolsUsed: answer.toolsUsed,
            truncated: answer.truncated,
          },
        ]);
      } catch (error) {
        // Kept in the thread rather than raised as a toast: the question it
        // belongs to is right above it, and a failed question is worth seeing
        // next to the ones that worked.
        setTurns((current) => [
          ...current,
          { id: turnId(), role: 'error', text: toApiError(error).message },
        ]);
      }
    },
    [mutateAsync, turns],
  );

  const clear = useCallback(() => setTurns([]), []);

  return { turns, ask, clear, isAsking: mutation.isPending };
}

/**
 * The thread as the API wants it: the questions and the answers that worked.
 *
 * Failed turns are left out deliberately. An error message is this panel's
 * text, not something the assistant said, and replaying it as its own words
 * would have it apologising for a network fault on the next question.
 */
function toHistory(turns: AssistantTurn[]): AssistantMessage[] {
  return turns
    .filter((turn) => turn.role !== 'error')
    .slice(-HISTORY_SENT)
    .map((turn) => ({
      role: turn.role === 'user' ? ('User' as const) : ('Assistant' as const),
      text: turn.text,
    }));
}
