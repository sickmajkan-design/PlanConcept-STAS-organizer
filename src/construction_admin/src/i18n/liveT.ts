import { en, type MessageKey } from './en';
import type { TranslateValues } from './context';
import type { Message } from './types';

function interpolate(template: string, values: TranslateValues | undefined): string {
  if (!values) return template;
  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in values ? String(values[name]) : match,
  );
}

function englishFallback(key: MessageKey, values: TranslateValues | undefined): string {
  const message: Message = en[key];
  const text = typeof message === 'string' ? message : message.other;
  return interpolate(text, values);
}

/**
 * A `t()` reachable from outside React — `I18nProvider` calls
 * {@link setLiveTranslate} on every render, so this always reflects whatever
 * language is currently selected.
 *
 * Exists for the two kinds of code that need translated text but cannot call
 * a hook: `ApiError.message` (see `api/apiError.ts`) and Zod schema error
 * messages, which Zod evaluates lazily via an `error: () => …` callback at
 * validation time, not at schema-definition time — a callback, not a
 * component, so `useT()` is not available inside it either.
 *
 * A module-level mutable is not the usual React way to pass this down; the
 * alternative is threading `t` through every Zod schema and every place an
 * `ApiError` is read, which is a much larger and more invasive change for
 * the same result.
 */
let current: ((key: MessageKey, values?: TranslateValues) => string) | null = null;

export function setLiveTranslate(t: (key: MessageKey, values?: TranslateValues) => string): void {
  current = t;
}

/**
 * Falls back to the English dictionary directly when no provider has mounted
 * yet — a unit test that constructs an `ApiError` or runs a Zod schema with
 * no `<I18nProvider>` around it, mainly — so the text is still real English
 * rather than a raw key like `"apiError.unauthorized"`.
 */
export function liveT(key: MessageKey, values?: TranslateValues): string {
  return current ? current(key, values) : englishFallback(key, values);
}
