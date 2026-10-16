/**
 * @vitest-environment jsdom
 */
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { attachmentsApi } from '../../api/attachments';
import { installFakeNetwork, renderScreen, signedIn } from '../../test/renderScreen';
import { useDocumentsAtCreate } from './DocumentsAtCreate';

function Form() {
  const docs = useDocumentsAtCreate('Employee', ['Contract', 'Visa', 'Other']);

  return (
    <div>
      {docs.section}
      <span data-testid="count">{docs.count}</span>
      <button onClick={() => void docs.send('new-employee')}>save</button>
    </div>
  );
}

describe('documents on a form that makes a record', () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    installFakeNetwork();
  });

  it('are held until the record exists, then filed against it', async () => {
    const upload = vi.spyOn(attachmentsApi, 'upload').mockResolvedValue({} as never);

    renderScreen(<Form />, { user: signedIn('Admin') });

    const input = await screen.findByTestId('file-input');
    fireEvent.change(input, { target: { files: [new File(['a'], 'ugovor.pdf', { type: 'application/pdf' })] } });

    expect(await screen.findByText('ugovor.pdf')).toBeDefined();
    expect(screen.getByTestId('count').textContent).toBe('1');
    // Nothing is sent while the record does not exist.
    expect(upload).not.toHaveBeenCalled();

    fireEvent.click(screen.getByText('save'));

    await waitFor(() => expect(upload).toHaveBeenCalledTimes(1));
    expect(upload.mock.calls[0]![0]).toMatchObject({ ownerType: 'Employee', ownerId: 'new-employee', category: 'Contract' });
  });

  it('can be taken out again before saving', async () => {
    renderScreen(<Form />, { user: signedIn('Admin') });

    fireEvent.change(await screen.findByTestId('file-input'), {
      target: { files: [new File(['a'], 'pogresna.pdf', { type: 'application/pdf' })] },
    });
    await screen.findByText('pogresna.pdf');

    fireEvent.click(screen.getByRole('button', { name: /pogresna\.pdf/ }));

    await waitFor(() => expect(screen.queryByText('pogresna.pdf')).toBeNull());
    expect(screen.getByTestId('count').textContent).toBe('0');
  });
});
