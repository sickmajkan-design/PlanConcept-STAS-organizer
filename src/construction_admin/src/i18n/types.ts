export const locales = ['sr', 'en'] as const;

export type Locale = (typeof locales)[number];

/** Shown in the language switcher, in the language itself. */
export const localeNames: Record<Locale, string> = {
  sr: 'Srpski',
  en: 'English',
};

/**
 * A message is either a plain string or, when a number decides its form, one
 * entry per plural category.
 *
 * Serbian needs three: `one` (1, 21, 31…), `few` (2–4, 22–24…) and `other`
 * (5–20, 25…). English needs two, so `few` is optional. The categories are the
 * CLDR ones that `Intl.PluralRules` returns, rather than a scheme of our own.
 */
export type Message = string | { one: string; few?: string; other: string };

export type Messages = Record<string, Message>;
