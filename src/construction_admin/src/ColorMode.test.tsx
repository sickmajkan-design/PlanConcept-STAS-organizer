/**
 * @vitest-environment jsdom
 */
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { ColorModeProvider } from './ColorMode';
import { ColorModeToggle } from './components/ColorModeToggle';
import { I18nProvider } from './i18n/I18nProvider';

function renderToggle() {
  return render(
    <I18nProvider>
      <ColorModeProvider>
        <ColorModeToggle />
      </ColorModeProvider>
    </I18nProvider>,
  );
}

describe('the look of the panel', () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem('construction.locale', 'en');
    document.documentElement.style.colorScheme = '';
  });

  it('follows the device until the person chooses, then remembers the choice', () => {
    renderToggle();

    const button = () => screen.getByRole('button', { name: /Appearance/ });

    expect(button().getAttribute('aria-label')).toContain('follow the device');
    expect(window.localStorage.getItem('ui.colorMode')).toBeNull();

    fireEvent.click(button());
    expect(button().getAttribute('aria-label')).toContain('light');
    expect(window.localStorage.getItem('ui.colorMode')).toBe('light');
    expect(document.documentElement.style.colorScheme).toBe('light');

    fireEvent.click(button());
    expect(button().getAttribute('aria-label')).toContain('dark');
    expect(window.localStorage.getItem('ui.colorMode')).toBe('dark');
    expect(document.documentElement.style.colorScheme).toBe('dark');
    expect(document.querySelector('meta[name="theme-color"]')?.getAttribute('content')).toBe('#1c1815');

    // The third tap goes back to following the device, and forgets the fixed choice.
    fireEvent.click(button());
    expect(button().getAttribute('aria-label')).toContain('follow the device');
    expect(window.localStorage.getItem('ui.colorMode')).toBeNull();
  });

  it('starts in the choice remembered from the last visit', () => {
    window.localStorage.setItem('ui.colorMode', 'dark');

    renderToggle();

    expect(document.documentElement.style.colorScheme).toBe('dark');
    expect(screen.getByRole('button', { name: /Appearance/ }).getAttribute('aria-label')).toContain('dark');
  });
});
