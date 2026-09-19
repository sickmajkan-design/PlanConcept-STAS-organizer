/**
 * @vitest-environment jsdom
 */
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiError } from '../api/apiError';
import { renderScreen } from '../test/renderScreen';
import { ConfirmDialog } from './ConfirmDialog';

/**
 * A confirm dialog stays open when what it confirms fails — and used to say
 * nothing. An approval the API refused ("you cannot review a cost you recorded
 * yourself") left the operator pressing a button that appeared to do nothing.
 */
beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
});

function renderDialog(onConfirm: () => void | Promise<unknown>, extra = {}) {
  return renderScreen(
    <ConfirmDialog
      open
      title="Approve this cost?"
      description="It will count as reviewed."
      confirmLabel="Approve"
      onConfirm={onConfirm}
      onCancel={() => {}}
      {...extra}
    />,
  );
}

describe('ConfirmDialog', () => {
  it('shows why a confirm failed, and stays open', async () => {
    renderDialog(() => Promise.reject(new ApiError('You cannot review a cost you recorded yourself.')));

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    expect((await screen.findByRole('alert')).textContent).toContain(
      'You cannot review a cost you recorded yourself.',
    );
    // Still there, and usable again — not stuck spinning.
    expect((screen.getByRole('button', { name: 'Approve' }) as HTMLButtonElement).disabled).toBe(false);
  });

  it('shows nothing when the confirm succeeds', async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    renderDialog(onConfirm);

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    await waitFor(() => expect(onConfirm).toHaveBeenCalledOnce());
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('still works with a handler that returns nothing', () => {
    const onConfirm = vi.fn();
    renderDialog(onConfirm);

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    expect(onConfirm).toHaveBeenCalledOnce();
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('shows an error the caller supplies itself', () => {
    renderDialog(() => {}, { error: 'Conflict with the current data.' });

    expect(screen.getByRole('alert').textContent).toContain('Conflict with the current data.');
  });

  it('clears the last failure when the next attempt starts', async () => {
    const onConfirm = vi
      .fn<() => Promise<unknown>>()
      .mockRejectedValueOnce(new ApiError('First attempt failed.'))
      .mockResolvedValueOnce(undefined);
    renderDialog(onConfirm);

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));
    expect((await screen.findByRole('alert')).textContent).toContain('First attempt failed.');

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    await waitFor(() => expect(screen.queryByRole('alert')).toBeNull());
  });
});
