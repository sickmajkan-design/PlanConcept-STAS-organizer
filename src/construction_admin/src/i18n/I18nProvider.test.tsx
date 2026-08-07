/**
 * @vitest-environment jsdom
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import type { ReactNode } from 'react';

import { I18nProvider } from './I18nProvider';
import type { MessageKey } from './en';
import { useI18n, useT } from './useI18n';
import type { Locale } from './types';
import type { TranslateValues } from './context';

function Shows({ messageKey, values }: { messageKey: MessageKey; values?: TranslateValues }) {
  return <span data-testid="text">{useT()(messageKey, values)}</span>;
}

function LocaleSwitch() {
  const { locale, setLocale } = useI18n();

  return (
    <button type="button" onClick={() => setLocale(locale === 'sr' ? 'en' : 'sr')}>
      {locale}
    </button>
  );
}

const text = () => screen.getByTestId('text').textContent;

const shownLocale = () => screen.getByRole('button').textContent;

/**
 * jsdom's navigator reports `en-US`, so a test that says nothing about the
 * browser's languages is quietly testing the English branch. Every test that
 * cares states which browser it is pretending to be.
 */
function browserSpeaking(...languages: string[]) {
  Object.defineProperty(window.navigator, 'languages', {
    value: languages,
    configurable: true,
  });
}

/** Renders with the locale already chosen, which is what a returning operator has. */
function renderIn(locale: Locale, ui: ReactNode) {
  window.localStorage.setItem('construction.locale', locale);
  return render(<I18nProvider>{ui}</I18nProvider>);
}

beforeEach(() => {
  window.localStorage.clear();
  browserSpeaking('en-US');
  // The document is shared across the tests in this file; the provider sets
  // this on mount, so leaving the previous test's value would make the next
  // one pass for the wrong reason.
  document.documentElement.lang = '';
});

describe('plural selection', () => {
  it('picks all three Serbian forms', () => {
    // Serbian has one (1, 21, 31…), few (2–4, 22–24…) and other (5–20, 25…).
    // Getting this wrong prints "5 dan" — the kind of thing a native speaker
    // notices immediately and a test suite usually does not.
    const cases: [number, string][] = [
      [1, '1 dan'],
      [2, '2 dana'],
      [4, '4 dana'],
      [5, '5 dana'],
      [21, '21 dan'],
      [22, '22 dana'],
      [25, '25 dana'],
    ];

    for (const [count, expected] of cases) {
      const { unmount } = renderIn(
        'sr',
        <Shows messageKey="absences.days" values={{ count }} />,
      );

      expect(text()).toBe(expected);
      unmount();
    }
  });

  it('picks the English two', () => {
    const { unmount } = renderIn(
      'en',
      <Shows messageKey="absences.days" values={{ count: 1 }} />,
    );

    expect(text()).toBe('1 day');
    unmount();

    renderIn('en', <Shows messageKey="absences.days" values={{ count: 2 }} />);

    expect(text()).toBe('2 days');
  });

  it('falls back to `other` when no count is given', () => {
    renderIn('sr', <Shows messageKey="absences.days" />);

    expect(text()).toBe('{count} dana');
  });
});

describe('interpolation', () => {
  it('substitutes named values', () => {
    renderIn(
      'sr',
      <Shows messageKey="common.displayedRows" values={{ from: 1, to: 20, count: 57 }} />,
    );

    expect(text()).toBe('1–20 od 57');
  });

  it('leaves a placeholder alone rather than printing "undefined"', () => {
    // A missing value showing as `{to}` says "the translation wants something
    // here"; `undefined` says the data is broken. The first is easier to fix.
    renderIn('sr', <Shows messageKey="common.displayedRows" values={{ from: 1 }} />);

    expect(text()).toBe('1–{to} od {count}');
  });
});

describe('which language the app opens in', () => {
  it('follows the browser when it asks for one we have', () => {
    browserSpeaking('en-GB', 'sr');

    render(
      <I18nProvider>
        <LocaleSwitch />
      </I18nProvider>,
    );

    expect(shownLocale()).toBe('en');
  });

  it('serves Serbian to a Croatian or Bosnian browser', () => {
    // Serbian, Bosnian, Croatian and Montenegrin readers are all better served
    // by the Serbian text than by English.
    for (const tag of ['sr-Latn-RS', 'hr-HR', 'bs-BA', 'me']) {
      browserSpeaking(tag);

      const { unmount } = render(
        <I18nProvider>
          <LocaleSwitch />
        </I18nProvider>,
      );

      expect(shownLocale()).toBe('sr');
      unmount();
    }
  });

  it('falls back to Serbian for a browser asking for neither', () => {
    // The people using this are on sites in the region; English would be the
    // wrong guess far more often than the right one.
    browserSpeaking('de-DE', 'fr');

    render(
      <I18nProvider>
        <LocaleSwitch />
      </I18nProvider>,
    );

    expect(shownLocale()).toBe('sr');
  });

  it('lets a stored choice beat the browser', () => {
    browserSpeaking('en-US');

    renderIn('sr', <LocaleSwitch />);

    expect(shownLocale()).toBe('sr');
  });

  it('ignores a stored value that is not a locale we have', () => {
    window.localStorage.setItem('construction.locale', 'de');
    browserSpeaking('de-DE');

    render(
      <I18nProvider>
        <LocaleSwitch />
      </I18nProvider>,
    );

    expect(shownLocale()).toBe('sr');
  });
});

describe('switching language', () => {
  it('survives a reload once it has been chosen', async () => {
    browserSpeaking('de-DE');

    const { unmount } = render(
      <I18nProvider>
        <LocaleSwitch />
      </I18nProvider>,
    );

    expect(shownLocale()).toBe('sr');

    await userEvent.click(screen.getByRole('button'));
    expect(shownLocale()).toBe('en');

    unmount();

    render(
      <I18nProvider>
        <LocaleSwitch />
      </I18nProvider>,
    );

    expect(shownLocale()).toBe('en');
  });

  it('tells the browser which language the page is in', async () => {
    // Screen readers pronounce the page with this, and the browser's own
    // translate prompt reads it.
    renderIn('sr', <LocaleSwitch />);

    // Set on mount, not only on a change. `index.html` ships `lang="en"` and
    // most operators never touch the switcher, so waiting for one would leave
    // the page mislabelled for exactly the readers it was localised for.
    expect(document.documentElement.lang).toBe('sr-Latn');

    await userEvent.click(screen.getByRole('button'));

    expect(document.documentElement.lang).toBe('en');

    await userEvent.click(screen.getByRole('button'));

    // Latin script, stated: `sr` alone leaves a reader to guess between
    // Cyrillic and Latin, and this app is Latin.
    expect(document.documentElement.lang).toBe('sr-Latn');
  });
});

describe('useI18n outside a provider', () => {
  it('fails loudly rather than rendering English by accident', () => {
    // Without the throw, a component mounted outside the provider would get
    // `undefined` and crash somewhere else entirely.
    expect(() => render(<Shows messageKey="common.save" />)).toThrow(
      /useI18n must be used inside/,
    );
  });
});
