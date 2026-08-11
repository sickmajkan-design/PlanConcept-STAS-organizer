import { config } from '../config';

/**
 * Sends a client-side failure to the API, which logs it beside everything else.
 *
 * The panel had two ways to fail and no way to say so. A render error hit the
 * ErrorBoundary and printed to a console nobody has open; an unhandled promise
 * rejection printed to the same console and nothing else. Both are invisible
 * to whoever is meant to notice the panel is broken — which is not the person
 * looking at it, because they are on a site and their problem is that the
 * screen is blank.
 *
 * Deliberately not a third-party error service: the stack traces and screen
 * names here describe employees and their movements, so where that data goes
 * is the operator's decision, not a library's default. The API already has
 * structured logs, correlation ids and an OTLP exporter, and it is on their
 * host.
 *
 * Everything about this is fire-and-forget. A failure report that throws, or
 * retries, or blocks a render, has made the outage worse than the fault it was
 * describing.
 */

const ENDPOINT = '/api/v1/client-errors';

/** Longer than any useful trace, shorter than what the API will refuse. */
const MAX_STACK = 8_000;
const MAX_MESSAGE = 1_500;

/**
 * Reports sent per page load.
 *
 * A render loop can throw hundreds of times a second, and after the first
 * couple they are all the same stack. The server has its own limit; this stops
 * the browser spending the outage talking about it.
 */
const MAX_PER_SESSION = 10;

let sent = 0;

/**
 * True while a report is in flight.
 *
 * If posting the report itself fails — the API is what is broken — the failure
 * would be caught by the very handlers that call this, and each attempt would
 * generate another. That is a loop that ends in a hung tab.
 */
let reporting = false;

/** The last thing reported, so a repeated identical fault is only sent once. */
let previous = '';

function truncate(value: string, limit: number): string {
  return value.length <= limit ? value : `${value.slice(0, limit)}\n… truncated`;
}

function describe(error: unknown): { message: string; kind?: string; stack?: string } {
  if (error instanceof Error) {
    return {
      message: truncate(error.message || error.name || 'Error', MAX_MESSAGE),
      kind: error.name,
      stack: error.stack ? truncate(error.stack, MAX_STACK) : undefined,
    };
  }

  // Anything can be thrown in JavaScript, including nothing at all.
  if (error === undefined || error === null) {
    return { message: 'Unknown error (nothing was thrown)' };
  }

  return { message: truncate(String(error), MAX_MESSAGE) };
}

export interface ReportContext {
  /** Component stack, route, or anything else worth knowing. */
  detail?: string;
}

export async function reportClientError(
  error: unknown,
  context: ReportContext = {},
): Promise<void> {
  if (reporting || sent >= MAX_PER_SESSION) {
    return;
  }

  const described = describe(error);
  const fingerprint = `${described.kind}:${described.message}`;

  if (fingerprint === previous) {
    return;
  }

  previous = fingerprint;
  sent += 1;
  reporting = true;

  try {
    const stack = [described.stack, context.detail].filter(Boolean).join('\n\n');

    await fetch(`${config.apiBaseUrl}${ENDPOINT}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      // No credentials. The report says nothing that needs an account, and an
      // endpoint that works signed out is the one that can report a broken
      // sign-in screen.
      body: JSON.stringify({
        app: 'admin',
        message: described.message,
        kind: described.kind,
        stack: stack.length > 0 ? truncate(stack, MAX_STACK) : undefined,
        route: window.location?.pathname,
        platform: navigator?.userAgent?.slice(0, 200),
      }),
      // Lets the request outlive the page, which matters because the fault
      // that broke the panel is often the one the operator reloads away from.
      keepalive: true,
    });
  } catch {
    // Swallowed on purpose, and this is the one catch in the app that should
    // stay empty: there is nowhere left to report a failure to report.
  } finally {
    reporting = false;
  }
}

/**
 * Catches the failures no ErrorBoundary ever sees.
 *
 * A boundary catches errors thrown while rendering. It does not catch one from
 * an event handler, a `setTimeout`, or a rejected promise nobody awaited —
 * and those are most of them in a data-driven panel.
 */
export function installGlobalErrorReporting(): () => void {
  const onError = (event: ErrorEvent) => {
    void reportClientError(event.error ?? event.message, {
      detail: `${event.filename}:${event.lineno}:${event.colno}`,
    });
  };

  const onRejection = (event: PromiseRejectionEvent) => {
    void reportClientError(event.reason, { detail: 'unhandled promise rejection' });
  };

  window.addEventListener('error', onError);
  window.addEventListener('unhandledrejection', onRejection);

  return () => {
    window.removeEventListener('error', onError);
    window.removeEventListener('unhandledrejection', onRejection);
  };
}

/** Test seam: forgets what has been sent and how much. */
export function resetClientErrorReporting(): void {
  sent = 0;
  previous = '';
  reporting = false;
}
