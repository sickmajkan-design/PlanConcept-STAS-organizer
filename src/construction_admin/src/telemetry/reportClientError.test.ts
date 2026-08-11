/**
 * @vitest-environment jsdom
 *
 * The reporter reads `window.location` and `navigator`, and half of what it
 * does is listening for browser events. There is nothing to test about it
 * outside a document.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import {
  installGlobalErrorReporting,
  reportClientError,
  resetClientErrorReporting,
} from './reportClientError';

/**
 * The reporter's job is to be the least important thing in the process.
 *
 * Everything asserted here is about it staying out of the way: never throwing,
 * never looping, and never turning one broken screen into a stream of
 * identical reports. A crash reporter that makes the outage worse is worse
 * than no crash reporter, because it also looks like it is helping.
 */
describe('reportClientError', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    resetClientErrorReporting();
    fetchMock = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends the message, the kind and the stack', async () => {
    await reportClientError(new TypeError('x is not a function'));

    expect(fetchMock).toHaveBeenCalledTimes(1);

    const [url, init] = fetchMock.mock.calls[0];
    const body = JSON.parse(init.body);

    expect(String(url)).toContain('/api/v1/client-errors');
    expect(init.method).toBe('POST');
    expect(body.app).toBe('admin');
    expect(body.kind).toBe('TypeError');
    expect(body.message).toBe('x is not a function');
    expect(body.stack).toBeTruthy();
  });

  /**
   * A render loop throws the same error every frame. After the first, each
   * report costs the browser a request and tells the log nothing new.
   */
  it('sends the same fault once, not once per occurrence', async () => {
    const error = new Error('the grid is broken');

    await reportClientError(error);
    await reportClientError(new Error('the grid is broken'));
    await reportClientError(error);

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('stops after ten reports in one session', async () => {
    for (let index = 0; index < 25; index += 1) {
      // Distinct messages, so the de-duplication above is not what stops it.
      await reportClientError(new Error(`failure number ${index}`));
    }

    expect(fetchMock).toHaveBeenCalledTimes(10);
  });

  /**
   * The failure that would otherwise be a hang: the API is what is broken, so
   * posting the report fails, and the handler that catches that calls this
   * again.
   */
  it('never throws when the report itself fails', async () => {
    fetchMock.mockRejectedValue(new Error('network is down'));

    await expect(reportClientError(new Error('anything'))).resolves.toBeUndefined();
  });

  it('anything can be thrown in JavaScript, including nothing', async () => {
    await reportClientError(undefined);

    const body = JSON.parse(fetchMock.mock.calls[0][1].body);

    expect(body.message).toContain('Unknown error');
  });

  it('truncates a runaway stack rather than posting it', async () => {
    const error = new Error('recursed');
    error.stack = 'x'.repeat(50_000);

    await reportClientError(error);

    const body = JSON.parse(fetchMock.mock.calls[0][1].body);

    // Comfortably under the API's own 10,000 limit, which would otherwise
    // reject the report and lose it entirely.
    expect(body.stack.length).toBeLessThan(10_000);
    expect(body.stack).toContain('truncated');
  });

  /**
   * An ErrorBoundary catches errors thrown while rendering. It does not catch
   * one from an event handler, a timeout, or a rejected promise — and in a
   * data-driven panel those are most of them.
   */
  it('reports the errors no boundary would ever see', async () => {
    const uninstall = installGlobalErrorReporting();

    window.dispatchEvent(
      new ErrorEvent('error', { error: new Error('from a click handler') }),
    );

    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));

    expect(JSON.parse(fetchMock.mock.calls[0][1].body).message).toBe('from a click handler');

    uninstall();
  });
});
