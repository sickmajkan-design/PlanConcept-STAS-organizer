/**
 * @vitest-environment jsdom
 */
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiError } from '../api/apiError';
import { renderScreen } from '../test/renderScreen';
import { ReasonDialog } from './ReasonDialog';

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
});

function renderDialog(onSubmit: (note: string) => void | Promise<unknown>) {
  return renderScreen(
    <ReasonDialog
      open
      title="Send back"
      hint="Say what to fix."
      label="Reason"
      submitLabel="Send back"
      onSubmit={onSubmit}
      onClose={() => {}}
    />,
  );
}

const submitButton = () => screen.getAllByRole('button', { name: 'Send back' }).pop() as HTMLButtonElement;

describe('ReasonDialog', () => {
  it('will not submit without a reason', () => {
    renderDialog(vi.fn());

    expect(submitButton().disabled).toBe(true);
  });

  it('submits the trimmed reason', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    renderDialog(onSubmit);

    fireEvent.change(screen.getByLabelText('Reason'), { target: { value: '  wrong amount  ' } });
    fireEvent.click(submitButton());

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('wrong amount'));
  });

  it('shows why a submit failed and keeps the reason', async () => {
    renderDialog(() => Promise.reject(new ApiError('This cost was changed by someone else just now.')));

    fireEvent.change(screen.getByLabelText('Reason'), { target: { value: 'wrong amount' } });
    fireEvent.click(submitButton());

    expect(await screen.findByText('This cost was changed by someone else just now.')).toBeTruthy();
    expect((screen.getByLabelText('Reason') as HTMLTextAreaElement).value).toBe('wrong amount');
  });
});
