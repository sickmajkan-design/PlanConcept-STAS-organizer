/**
 * @vitest-environment jsdom
 */
import { ThemeProvider } from '@mui/material';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { I18nProvider } from '../../i18n/I18nProvider';
import { theme } from '../../theme';
import { FirstUseHint, HintLine } from './FirstUseHint';

function renderHint() {
  return render(
    <ThemeProvider theme={theme}>
      <I18nProvider>
        <FirstUseHint id="test" title="Kako dodati">
          <HintLine>Prevucite datoteke.</HintLine>
        </FirstUseHint>
      </I18nProvider>
    </ThemeProvider>,
  );
}

beforeEach(() => window.localStorage.clear());

describe('FirstUseHint', () => {
  it('shows the first time, and not again once it was understood', () => {
    const first = renderHint();
    expect(screen.getByText('Prevucite datoteke.')).toBeTruthy();

    fireEvent.click(screen.getByRole('button', { name: /Razumijem|Got it/ }));
    expect(screen.queryByText('Prevucite datoteke.')).toBeNull();

    first.unmount();
    renderHint();
    expect(screen.queryByText('Prevucite datoteke.')).toBeNull();
  });

  it('can be opened again from the small link', () => {
    window.localStorage.setItem('files.hint.test', 'done');
    renderHint();

    fireEvent.click(screen.getByRole('button', { name: /Kako ovo radi\?|How does this work\?/ }));

    expect(screen.getByText('Prevucite datoteke.')).toBeTruthy();
  });
});
