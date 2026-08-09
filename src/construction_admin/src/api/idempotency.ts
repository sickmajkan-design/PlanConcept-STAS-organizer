/** The header the API reads. */
export const IDEMPOTENCY_HEADER = 'Idempotency-Key';

/**
 * A fresh key, naming one attempt at one action.
 *
 * `crypto.randomUUID` rather than a counter or a timestamp: two tabs, or two
 * people on the same screen, must not be able to produce the same key. The
 * dashes are dropped only to stay comfortably inside the API's length limit.
 */
export function newIdempotencyKey(): string {
  return crypto.randomUUID().replace(/-/g, '');
}

/**
 * Headers for a write that must not happen twice, or nothing when no key was
 * supplied.
 */
export function idempotencyHeaders(
  key: string | undefined,
): Record<string, string> | undefined {
  return key ? { [IDEMPOTENCY_HEADER]: key } : undefined;
}
