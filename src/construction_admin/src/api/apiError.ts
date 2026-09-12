import { AxiosError } from 'axios';

import type { MessageKey } from '../i18n/en';
import { liveT } from '../i18n/liveT';

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[] | string>;
};

/** Which generic message `describe()` renders when the server gave no text of its own. */
type ApiErrorKind =
  | 'timeout'
  | 'network'
  | 'server'
  | 'badRequest'
  | 'unauthorized'
  | 'forbidden'
  | 'notFound'
  | 'conflict'
  | 'unknown';

const KIND_TO_MESSAGE_KEY: Record<ApiErrorKind, MessageKey> = {
  timeout: 'apiError.timeout',
  network: 'apiError.network',
  server: 'apiError.server',
  badRequest: 'apiError.badRequest',
  unauthorized: 'apiError.unauthorized',
  forbidden: 'apiError.forbidden',
  notFound: 'apiError.notFound',
  conflict: 'apiError.conflict',
  unknown: 'apiError.unknown',
};

/**
 * Application-facing error. Translates the API's RFC 7807 problem-details
 * responses and transport failures into something a form or page can show.
 */
export class ApiError extends Error {
  readonly status?: number;

  /** Field name -> messages, populated for 400 validation responses. */
  readonly fieldErrors: Record<string, string[]>;

  /**
   * Set when this error's text is this class's own generic fallback rather
   * than real text the server sent (a `detail`/`title`, or a field-validation
   * message) — only then does `.message` render a translated string instead
   * of what was actually sent. Server-provided text is shown exactly as the
   * server sent it: translating that would mean localizing the whole API,
   * which is a separate, much larger piece of work than the client can do on
   * its own.
   */
  private readonly kind: ApiErrorKind | null;

  /** The English text passed to the constructor, kept for when there is no translator yet (e.g. a test). */
  private readonly fallbackMessage: string;

  constructor(
    message: string,
    status?: number,
    fieldErrors: Record<string, string[]> = {},
    kind: ApiErrorKind | null = null,
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
    this.kind = kind;
    this.fallbackMessage = message;

    // Shadows `Error`'s own instance property with a getter, so every
    // existing `error.message` read — and there are many — picks up
    // whichever language is current at the moment it is *read*, not the
    // moment this error was constructed.
    Object.defineProperty(this, 'message', {
      configurable: true,
      enumerable: false,
      get: () => (this.kind ? liveT(KIND_TO_MESSAGE_KEY[this.kind]) : this.fallbackMessage),
    });
  }

  get isValidationError(): boolean {
    return Object.keys(this.fieldErrors).length > 0;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  /**
   * Message for a field. The API reports names in PascalCase while forms use
   * camelCase, so the lookup is case-insensitive.
   */
  errorFor(field: string): string | undefined {
    const key = Object.keys(this.fieldErrors).find(
      (candidate) => candidate.toLowerCase() === field.toLowerCase(),
    );

    return key ? this.fieldErrors[key]?.[0] : undefined;
  }

  /**
   * Explicit equivalent of reading `.message` — prefer this in new code,
   * since it makes the translation dependency visible instead of relying on
   * the module-level translator described above.
   */
  describe(t: (key: MessageKey) => string): string {
    return this.kind ? t(KIND_TO_MESSAGE_KEY[this.kind]) : this.fallbackMessage;
  }
}

function parseFieldErrors(
  errors: ProblemDetails['errors'],
): Record<string, string[]> {
  if (!errors || typeof errors !== 'object') {
    return {};
  }

  return Object.fromEntries(
    Object.entries(errors).map(([key, value]) => [
      key,
      Array.isArray(value) ? value.map(String) : [String(value)],
    ]),
  );
}

function defaultMessageFor(status?: number): { message: string; kind: ApiErrorKind } {
  if (status === undefined) {
    return { message: 'Something went wrong. Please try again.', kind: 'unknown' };
  }

  if (status >= 500) {
    return {
      message: 'The server encountered an error. Please try again later.',
      kind: 'server',
    };
  }

  switch (status) {
    case 400:
      return {
        message: 'The request was rejected. Please check the entered data.',
        kind: 'badRequest',
      };
    case 401:
      return {
        message: 'Your session has expired. Please sign in again.',
        kind: 'unauthorized',
      };
    case 403:
      return {
        message: 'You do not have permission to perform this action.',
        kind: 'forbidden',
      };
    case 404:
      return { message: 'The requested item could not be found.', kind: 'notFound' };
    case 409:
      return { message: 'The action conflicts with the current data.', kind: 'conflict' };
    default:
      return { message: 'Something went wrong. Please try again.', kind: 'unknown' };
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof AxiosError) {
    if (error.code === AxiosError.ECONNABORTED || error.code === 'ETIMEDOUT') {
      return new ApiError(
        'The server took too long to respond. Please try again.',
        undefined,
        {},
        'timeout',
      );
    }

    if (!error.response) {
      return new ApiError(
        'No connection to the server. Check your network and try again.',
        undefined,
        {},
        'network',
      );
    }

    const status = error.response.status;
    const data = error.response.data as ProblemDetails | undefined;
    const fieldErrors = parseFieldErrors(data?.errors);
    const fallback = defaultMessageFor(status);

    const serverText = Object.values(fieldErrors)[0]?.[0] ?? data?.detail ?? data?.title;

    return new ApiError(
      serverText ?? fallback.message,
      status,
      fieldErrors,
      serverText ? null : fallback.kind,
    );
  }

  return new ApiError(error instanceof Error ? error.message : 'Something went wrong.');
}
