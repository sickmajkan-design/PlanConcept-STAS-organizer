/**
 * @vitest-environment jsdom
 */
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../test/renderScreen';
import { AttachmentList } from './AttachmentList';

let network: FakeNetwork;

const SCREEN_TIMEOUT = 40_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

const pdf = (name: string) => new File([new Uint8Array(200)], name, { type: 'application/pdf', lastModified: 1 });

function renderList(canUpload = true) {
  return renderScreen(
    <AttachmentList
      ownerType="Employee"
      ownerId="e1"
      categories={['Contract', 'Certificate', 'Other']}
      canUpload={canUpload}
    />,
    { user: signedIn('Admin') },
  );
}

const dropOn = (element: HTMLElement, files: File[]) =>
  fireEvent.drop(element, { dataTransfer: { files, types: ['Files'] } });

describe('documents on a record accept dropped files', () => {
  it('opens the upload dialog with the dropped files already in it and sends them', async () => {
    network.reply('/attachments', 200, []);
    renderList();

    const target = await screen.findByTestId('attachment-drop-target');
    dropOn(target, [pdf('viza.pdf'), pdf('ugovor.pdf')]);

    const dialog = await screen.findByRole('dialog');
    expect(await within(dialog).findByText('viza.pdf')).toBeTruthy();
    expect(within(dialog).getByText('ugovor.pdf')).toBeTruthy();

    await userEvent.click(within(dialog).getByRole('button', { name: /^(Otpremi|Upload)$/ }));

    await waitFor(() => {
      const posts = network.calls.filter((c) => c.method.toLowerCase() === 'post' && c.url.includes('/attachments'));
      expect(posts).toHaveLength(2);
    });
  }, SCREEN_TIMEOUT);

  it('turns away a type that is not allowed and says why, without opening nothing useful', async () => {
    network.reply('/attachments', 200, []);
    renderList();

    const target = await screen.findByTestId('attachment-drop-target');
    dropOn(target, [new File(['x'], 'program.exe', { lastModified: 1 })]);

    const dialog = await screen.findByRole('dialog');
    expect(await within(dialog).findByText(/program\.exe/)).toBeTruthy();
    expect(within(dialog).getByRole('button', { name: /^(Otpremi|Upload)$/ }).hasAttribute('disabled')).toBe(true);
  }, SCREEN_TIMEOUT);

  it('ignores a drop where the account may not add documents', async () => {
    network.reply('/attachments', 200, []);
    renderList(false);

    const target = await screen.findByTestId('attachment-drop-target');
    dropOn(target, [pdf('viza.pdf')]);

    expect(screen.queryByRole('dialog')).toBeNull();
  }, SCREEN_TIMEOUT);
});
