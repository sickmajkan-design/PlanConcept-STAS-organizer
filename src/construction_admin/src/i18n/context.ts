import { createContext } from 'react';

import type { MessageKey } from './en';
import type { Locale } from './types';

/**
 * Values interpolated into a message. `count` additionally selects the plural
 * form, which is why it is typed rather than left to `unknown`.
 */
export type TranslateValues = Record<string, string | number> & { count?: number };

export type Translate = (key: MessageKey, values?: TranslateValues) => string;

export interface I18nContextValue {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: Translate;
}

export const I18nContext = createContext<I18nContextValue | undefined>(undefined);
