/**
 * @vitest-environment jsdom
 */
import { ThemeProvider } from '@mui/material';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { I18nProvider } from '../../i18n/I18nProvider';
import { theme } from '../../theme';
import { DropArea } from './DropArea';

function renderArea(onFile = vi.fn(), disabled = false) {
  render(
    <ThemeProvider theme={theme}>
      <I18nProvider>
        <DropArea onFile={onFile} disabled={disabled}>
          <button type="button">Izaberi</button>
        </DropArea>
      </I18nProvider>
    </ThemeProvider>,
  );

  return { onFile, area: screen.getByTestId('drop-area') };
}

const drag = (files: File[], types: string[] = ['Files']) => ({ dataTransfer: { files, types, dropEffect: '' } });
const sheet = (name: string) => new File(['x'], name, { lastModified: 1 });

describe('DropArea', () => {
  it('hands the first dropped file to the same handler the chooser uses', () => {
    const { area, onFile } = renderArea();

    fireEvent.drop(area, drag([sheet('izvod.xlsx'), sheet('drugi.xlsx')]));

    expect(onFile).toHaveBeenCalledOnce();
    expect(onFile.mock.calls[0]![0].name).toBe('izvod.xlsx');
  });

  it('marks the region while a file is over it and clears it on leaving', () => {
    const { area } = renderArea();

    fireEvent.dragEnter(area, drag([]));
    expect(area.getAttribute('data-over')).toBe('true');

    fireEvent.dragLeave(area, drag([]));
    expect(area.getAttribute('data-over')).toBeNull();
  });

  it('ignores a drag of something that is not a file', () => {
    const { area, onFile } = renderArea();

    fireEvent.dragEnter(area, drag([], ['text/plain']));
    fireEvent.drop(area, drag([], ['text/plain']));

    expect(area.getAttribute('data-over')).toBeNull();
    expect(onFile).not.toHaveBeenCalled();
  });

  it('does nothing while disabled', () => {
    const { area, onFile } = renderArea(vi.fn(), true);

    fireEvent.drop(area, drag([sheet('a.xlsx')]));

    expect(onFile).not.toHaveBeenCalled();
  });
});
