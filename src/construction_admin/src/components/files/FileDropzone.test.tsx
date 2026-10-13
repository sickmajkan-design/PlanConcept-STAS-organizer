/**
 * @vitest-environment jsdom
 */
import { ThemeProvider } from '@mui/material';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { I18nProvider } from '../../i18n/I18nProvider';
import { theme } from '../../theme';
import { FileDropzone } from './FileDropzone';

const pdf = (name = 'a.pdf', size = 100) =>
  new File([new Uint8Array(size)], name, { type: 'application/pdf', lastModified: 1 });

function renderZone(props: Partial<Parameters<typeof FileDropzone>[0]> = {}) {
  const onFiles = vi.fn();
  const onReject = vi.fn();

  render(
    <ThemeProvider theme={theme}>
      <I18nProvider>
        <FileDropzone onFiles={onFiles} onReject={onReject} {...props} />
      </I18nProvider>
    </ThemeProvider>,
  );

  return { onFiles, onReject, zone: screen.getByTestId('dropzone') };
}

const drag = (files: File[], types: string[] = ['Files']) => ({ dataTransfer: { files, types, dropEffect: '' } });

describe('FileDropzone', () => {
  it('takes files dropped on it and hands back the accepted ones', () => {
    const { zone, onFiles, onReject } = renderZone();

    fireEvent.drop(zone, drag([pdf('a.pdf'), pdf('b.pdf', 50)]));

    expect(onFiles).toHaveBeenCalledOnce();
    expect(onFiles.mock.calls[0]![0].map((f: File) => f.name)).toEqual(['a.pdf', 'b.pdf']);
    expect(onReject).not.toHaveBeenCalled();
  });

  it('says which were turned away and why while still taking the rest', () => {
    const { zone, onFiles, onReject } = renderZone({ maxBytes: 1000 });
    const exe = new File(['x'], 'virus.exe', { lastModified: 1 });

    fireEvent.drop(zone, drag([pdf('ok.pdf'), pdf('big.pdf', 5000), exe]));

    expect(onFiles.mock.calls[0]![0].map((f: File) => f.name)).toEqual(['ok.pdf']);
    expect(onReject.mock.calls[0]![0].map((r: { file: File; reason: string }) => [r.file.name, r.reason])).toEqual([
      ['big.pdf', 'size'],
      ['virus.exe', 'type'],
    ]);
  });

  it('highlights while files are over it and does not flicker when the pointer crosses its children', () => {
    const { zone } = renderZone();

    fireEvent.dragEnter(zone, drag([]));
    fireEvent.dragEnter(zone, drag([])); // entering a child
    expect(zone.getAttribute('data-over')).toBe('true');

    fireEvent.dragLeave(zone, drag([])); // leaving that child
    expect(zone.getAttribute('data-over')).toBe('true');

    fireEvent.dragLeave(zone, drag([]));
    expect(zone.getAttribute('data-over')).toBeNull();
  });

  it('ignores a drag that is not carrying files', () => {
    const { zone, onFiles } = renderZone();

    fireEvent.dragEnter(zone, drag([], ['text/plain']));
    expect(zone.getAttribute('data-over')).toBeNull();

    fireEvent.drop(zone, drag([], ['text/plain']));
    expect(onFiles).not.toHaveBeenCalled();
  });

  it('takes only one file when it is a single-file zone and says the others were skipped', () => {
    const { zone, onFiles, onReject } = renderZone({ multiple: false });

    fireEvent.drop(zone, drag([pdf('a.pdf', 1), pdf('b.pdf', 2)]));

    expect(onFiles.mock.calls[0]![0].map((f: File) => f.name)).toEqual(['a.pdf']);
    expect(onReject.mock.calls[0]![0].map((r: { reason: string }) => r.reason)).toEqual(['count']);
  });

  it('takes files chosen through the file picker', () => {
    const { onFiles } = renderZone();

    fireEvent.change(screen.getByTestId('file-input'), { target: { files: [pdf('izabrano.pdf')] } });

    expect(onFiles.mock.calls[0]![0].map((f: File) => f.name)).toEqual(['izabrano.pdf']);
  });

  it('opens the chooser from the keyboard, so dragging is never the only way in', () => {
    const { zone } = renderZone();
    const click = vi.spyOn(screen.getByTestId('file-input'), 'click');

    fireEvent.keyDown(zone, { key: 'Enter' });
    fireEvent.keyDown(zone, { key: ' ' });

    expect(click).toHaveBeenCalledTimes(2);
    expect(zone.getAttribute('tabindex')).toBe('0');
    expect(zone.getAttribute('role')).toBe('button');
  });

  it('takes an image pasted into it', () => {
    const { zone, onFiles } = renderZone();
    const shot = new File([new Uint8Array(10)], 'slika.png', { type: 'image/png', lastModified: 1 });

    fireEvent.paste(zone, { clipboardData: { files: [shot] } });

    expect(onFiles.mock.calls[0]![0].map((f: File) => f.name)).toEqual(['slika.png']);
  });

  it('does nothing while disabled', () => {
    const { zone, onFiles } = renderZone({ disabled: true });

    fireEvent.drop(zone, drag([pdf()]));

    expect(onFiles).not.toHaveBeenCalled();
    expect(zone.getAttribute('aria-disabled')).toBe('true');
  });
});
