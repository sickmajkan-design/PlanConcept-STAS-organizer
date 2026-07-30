import { AxiosError } from 'axios';

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[] | string>;
};

/**
 * Application-facing error. Translates the API's RFC 7807 problem-details
 * responses and transport failures into something a form or page can show.
 */
export class ApiError extends Error {
  readonly status?: number;

  /** Field name -> messages, populated for 400 validation responses. */
  readonly fieldErrors: Record<string, string[]>;

  constructor(
    message: string,
    status?: number,
    fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
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

function defaultMessageFor(status?: number): string {
  if (status === undefined) {
    return 'Something went wrong. Please try again.';
  }

  if (status >= 500) {
    return 'The server encountered an error. Please try again later.';
  }

  switch (status) {
    case 400:
      return 'The request was rejected. Please check the entered data.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested item could not be found.';
    case 409:
      return 'The action conflicts with the current data.';
    default:
      return 'Something went wrong. Please try again.';
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof AxiosError) {
    if (error.code === AxiosError.ECONNABORTED || error.code === 'ETIMEDOUT') {
      return new ApiError('The server took too long to respond. Please try again.');
    }

    if (!error.response) {
      return new ApiError(
        'No connection to the server. Check your network and try again.',
      );
    }

    const status = error.response.status;
    const data = error.response.data as ProblemDetails | undefined;
    const fieldErrors = parseFieldErrors(data?.errors);

    const message =
      Object.values(fieldErrors)[0]?.[0] ??
      data?.detail ??
      data?.title ??
      defaultMessageFor(status);

    return new ApiError(message, status, fieldErrors);
  }

  return new ApiError(
    error instanceof Error ? error.message : 'Something went wrong.',
  );
}
