import { request } from './client';

export type AssistantRole = 'User' | 'Assistant';

export interface AssistantMessage {
  role: AssistantRole;
  text: string;
}

export interface AskAssistantInput {
  message: string;
  history: AssistantMessage[];
  locale: string;
}

export interface AssistantAnswer {
  text: string;
  /** Which tools the answer was built from, so the reader can see it was looked up. */
  toolsUsed: string[];
  /** True when the assistant ran out of lookups before it finished. */
  truncated: boolean;
}

export interface AssistantStatus {
  enabled: boolean;
}

/**
 * The assistant answers after looking things up, which takes longer than any
 * other call this panel makes.
 *
 * `client.ts` times out at 20 seconds, which is right for a list endpoint and
 * would cut off a question that needed two or three lookups. This is set above
 * the server's own cut-off so the caller is told what happened by the API
 * rather than by a dead socket.
 */
const ASK_TIMEOUT_MS = 70_000;

export const assistantApi = {
  status: () =>
    request<AssistantStatus>({
      method: 'GET',
      url: '/api/v1/assistant/status',
    }),

  ask: (input: AskAssistantInput) =>
    request<AssistantAnswer>({
      method: 'POST',
      url: '/api/v1/assistant/chat',
      data: input,
      timeout: ASK_TIMEOUT_MS,
    }),
};
