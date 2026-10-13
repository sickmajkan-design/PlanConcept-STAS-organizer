/**
 * @vitest-environment jsdom
 */
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

import type { UploadAttachmentInput, UploadOptions } from '../../api/attachments';
import type { Attachment } from '../../api/types';
import { useUploadQueue } from './useUploadQueue';

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>{children}</QueryClientProvider>
);

const file = (name: string) => new File(['x'], name, { type: 'application/pdf', lastModified: 1 });
const build = (f: File): UploadAttachmentInput => ({
  ownerType: 'Employee',
  ownerId: 'e1',
  category: 'Other',
  file: f,
});

/** An uploader whose requests are settled by the test, so concurrency and failures can be staged. */
function stagedUploader() {
  const pending: { name: string; resolve: () => void; reject: (e: Error) => void; options?: UploadOptions }[] = [];
  const uploader = vi.fn(
    (input: UploadAttachmentInput, options?: UploadOptions) =>
      new Promise<Attachment>((resolve, reject) => {
        pending.push({
          name: input.file.name,
          options,
          resolve: () => resolve({} as Attachment),
          reject,
        });
        options?.signal?.addEventListener('abort', () => reject(new Error('aborted')));
      }),
  );

  return { uploader, pending };
}

describe('useUploadQueue', () => {
  it('sends at most three at once and starts the next as one finishes', async () => {
    const { uploader, pending } = stagedUploader();
    const { result } = renderHook(() => useUploadQueue({ uploader }), { wrapper });

    act(() => result.current.add(['a', 'b', 'c', 'd', 'e'].map((n) => file(`${n}.pdf`))));
    act(() => result.current.start(build));

    expect(pending.map((p) => p.name)).toEqual(['a.pdf', 'b.pdf', 'c.pdf']);
    expect(result.current.items.filter((i) => i.status === 'uploading')).toHaveLength(3);

    await act(async () => pending[0]!.resolve());

    await waitFor(() => expect(pending.map((p) => p.name)).toContain('d.pdf'));
    expect(result.current.done).toBe(1);
  });

  it('keeps going when one fails, marks it, and sends only that one again on retry', async () => {
    const { uploader, pending } = stagedUploader();
    const { result } = renderHook(() => useUploadQueue({ uploader }), { wrapper });

    act(() => result.current.add([file('a.pdf'), file('b.pdf')]));
    act(() => result.current.start(build));

    await act(async () => pending[0]!.reject(new Error('veza prekinuta')));
    await act(async () => pending[1]!.resolve());

    await waitFor(() => expect(result.current.running).toBe(false));
    expect(result.current.failed).toBe(1);
    expect(result.current.done).toBe(1);
    expect(result.current.items.find((i) => i.file.name === 'a.pdf')?.error).toBe('veza prekinuta');

    act(() => result.current.retry());

    expect(pending.map((p) => p.name)).toEqual(['a.pdf', 'b.pdf', 'a.pdf']);
    await act(async () => pending[2]!.resolve());
    await waitFor(() => expect(result.current.done).toBe(2));
    expect(result.current.failed).toBe(0);
  });

  it('reports progress per file', async () => {
    const { uploader, pending } = stagedUploader();
    const { result } = renderHook(() => useUploadQueue({ uploader }), { wrapper });

    act(() => result.current.add([file('a.pdf')]));
    act(() => result.current.start(build));
    act(() => pending[0]!.options?.onProgress?.(0.5));

    expect(result.current.items[0]!.progress).toBe(0.5);
  });

  it('cancelling aborts what is in flight, leaves finished files alone and marks the waiting ones', async () => {
    const { uploader, pending } = stagedUploader();
    const { result } = renderHook(() => useUploadQueue({ uploader, concurrency: 1 }), { wrapper });

    act(() => result.current.add([file('a.pdf'), file('b.pdf'), file('c.pdf')]));
    act(() => result.current.start(build));
    await act(async () => pending[0]!.resolve());
    await waitFor(() => expect(pending).toHaveLength(2));

    await act(async () => result.current.cancel());

    await waitFor(() => expect(result.current.running).toBe(false));
    const statuses = Object.fromEntries(result.current.items.map((i) => [i.file.name, i.status]));
    expect(statuses).toEqual({ 'a.pdf': 'done', 'b.pdf': 'cancelled', 'c.pdf': 'cancelled' });
    expect(pending).toHaveLength(2);
  });

  it('files added while it runs join the upload', async () => {
    const { uploader, pending } = stagedUploader();
    const { result } = renderHook(() => useUploadQueue({ uploader }), { wrapper });

    act(() => result.current.add([file('a.pdf')]));
    act(() => result.current.start(build));
    act(() => result.current.add([file('late.pdf')]));

    expect(pending.map((p) => p.name)).toEqual(['a.pdf', 'late.pdf']);
  });

  it('removing a row drops it from the queue', () => {
    const { uploader } = stagedUploader();
    const { result } = renderHook(() => useUploadQueue({ uploader }), { wrapper });

    act(() => result.current.add([file('a.pdf'), file('b.pdf')]));
    const id = result.current.items[0]!.id;
    act(() => result.current.remove(id));

    expect(result.current.items.map((i) => i.file.name)).toEqual(['b.pdf']);
  });
});
