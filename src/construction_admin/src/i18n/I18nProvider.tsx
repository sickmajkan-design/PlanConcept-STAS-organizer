import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';

import { I18nContext, type I18nContextValue, type Translate, type TranslateValues } from './context';
import { en } from './en';
import { sr } from './sr';
import { locales, type Locale, type Message, type Messages } from './types';

const DICTIONARIES: Record<Locale, Messages> = { sr, en };

const STORAGE_KEY = 'construction.locale';


/**
 * The locale to start in: whatever was chosen last, otherwise the browser's
 * preference, otherwise Serbian.
 *
 * Serbian rather than English is the fallback because the people using this
 * are on sites in the region; an English default would be the wrong guess far
 * more often than the right one.
 */
function initialLocale(): Locale {
  if (typeof window === 'undefined') {
    return 'sr';
  }

  const stored = window.localStorage.getItem(STORAGE_KEY);

  if (stored && (locales as readonly string[]).includes(stored)) {
    return stored as Locale;
  }

  const preferred = window.navigator.languages ?? [window.navigator.language];

  for (const tag of preferred) {
    const base = tag.toLowerCase().split('-')[0];

    // Serbian, Bosnian, Croatian and Montenegrin readers are all served
    // better by the Serbian text than by English.
    if (base === 'sr' || base === 'bs' || base === 'hr' || base === 'me') {
      return 'sr';
    }

    if (base === 'en') {
      return 'en';
    }
  }

  return 'sr';
}

/** Picks the plural form for `count` using the locale's own CLDR rules. */
function resolvePlural(message: Message, locale: Locale, count: number | undefined): string {
  if (typeof message === 'string') {
    return message;
  }

  if (count === undefined) {
    return message.other;
  }

  const category = new Intl.PluralRules(locale === 'sr' ? 'sr-Latn' : 'en').select(count);

  if (category === 'one') return message.one;
  if (category === 'few') return message.few ?? message.other;

  return message.other;
}

function interpolate(template: string, values: TranslateValues | undefined): string {
  if (!values) {
    return template;
  }

  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in values ? String(values[name]) : match,
  );
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(initialLocale);

  // On the chosen locale rather than only on a change, because most operators
  // never change it: the app opens in Serbian and `index.html` says `lang="en"`,
  // so without this the page stays mislabelled for exactly the readers it was
  // localised for. Screen readers pronounce the text from this attribute, and
  // the browser's translate prompt reads it too.
  useEffect(() => {
    document.documentElement.lang = locale === 'sr' ? 'sr-Latn' : 'en';
  }, [locale]);

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next);
    window.localStorage.setItem(STORAGE_KEY, next);
  }, []);

  const t = useCallback<Translate>(
    (key, values) => {
      // Falling back to English rather than showing the raw key means a
      // missing translation degrades to readable text. The typed `sr`
      // dictionary makes that unreachable today, and this keeps it survivable
      // if a locale is ever added without being completed.
      const message = DICTIONARIES[locale][key] ?? en[key];

      if (message === undefined) {
        return key;
      }

      return interpolate(resolvePlural(message, locale, values?.count), values);
    },
    [locale],
  );

  const value = useMemo<I18nContextValue>(() => ({ locale, setLocale, t }), [locale, setLocale, t]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}
