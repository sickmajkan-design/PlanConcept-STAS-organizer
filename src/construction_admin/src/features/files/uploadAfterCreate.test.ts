/**
 * @vitest-environment jsdom
 */
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { attachmentsApi } from '../../api/attachments';
import { rememberUnsent, takeUnsent, uploadAfterCreate, type PendingFile } from './uploadAfterCreate';

const pending = (name: string, category: PendingFile['category'] = 'Other'): PendingFile => ({
  id: name,
  file: new File(['x'], name),
  category,
});

describe('files picked while a record is being made', () => {
  beforeEach(() => {
    window.sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('are filed against the new record one by one, each with its own type', async () => {
    const upload = vi.spyOn(attachmentsApi, 'upload').mockResolvedValue({} as never);

    const failed = await uploadAfterCreate('Employee', 'e1', [pending('ugovor.pdf', 'Contract'), pending('viza.pdf', 'Visa')]);

    expect(failed).toEqual([]);
    expect(upload).toHaveBeenCalledTimes(2);
    expect(upload.mock.calls[0]![0]).toMatchObject({ ownerType: 'Employee', ownerId: 'e1', category: 'Contract' });
    expect(upload.mock.calls[1]![0]).toMatchObject({ ownerId: 'e1', category: 'Visa' });
  });

  it('never throws: a file that fails is named, and the others still go', async () => {
    vi.spyOn(attachmentsApi, 'upload')
      .mockRejectedValueOnce(new Error('too big'))
      .mockResolvedValueOnce({} as never);

    const failed = await uploadAfterCreate('Vehicle', 'v1', [pending('velika.pdf'), pending('mala.pdf')]);

    expect(failed).toEqual(['velika.pdf']);
  });

  it('leaves what failed for the next page, once', () => {
    rememberUnsent('e1', ['velika.pdf', 'treca.pdf']);

    expect(takeUnsent('e1')).toEqual(['velika.pdf', 'treca.pdf']);
    // Read once: reloading the page must not nag again.
    expect(takeUnsent('e1')).toEqual([]);
    expect(takeUnsent('someone-else')).toEqual([]);
  });

  it('says nothing when everything went', () => {
    rememberUnsent('e1', ['x.pdf']);
    rememberUnsent('e1', []);

    expect(takeUnsent('e1')).toEqual([]);
  });
});

describe('a send that is told to slow down', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.useFakeTimers();
  });

  it('waits as long as the server said and tries the same file again', async () => {
    const { ApiError } = await import('../../api/apiError');
    const { uploadPatiently } = await import('./uploadAfterCreate');
    const upload = vi
      .spyOn(attachmentsApi, 'upload')
      .mockRejectedValueOnce(new ApiError('Too many', 429, {}, null, 30))
      .mockResolvedValueOnce({} as never);

    const done = uploadPatiently({ ownerType: 'Employee', ownerId: 'e1', category: 'Other', file: new File(['x'], 'a.pdf') });

    await vi.advanceTimersByTimeAsync(10_000);
    expect(upload).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(21_000);
    await done;
    expect(upload).toHaveBeenCalledTimes(2);

    vi.useRealTimers();
  });

  it('does not wait for any other refusal', async () => {
    const { ApiError } = await import('../../api/apiError');
    const { uploadPatiently } = await import('./uploadAfterCreate');
    const upload = vi.spyOn(attachmentsApi, 'upload').mockRejectedValue(new ApiError('Too big', 413));

    await expect(
      uploadPatiently({ ownerType: 'Employee', ownerId: 'e1', category: 'Other', file: new File(['x'], 'a.pdf') }),
    ).rejects.toBeDefined();
    expect(upload).toHaveBeenCalledTimes(1);

    vi.useRealTimers();
  });
});
