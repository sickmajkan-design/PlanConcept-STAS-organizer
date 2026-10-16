import { CssBaseline, ThemeProvider } from '@mui/material';
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

import { buildTheme } from './theme';

/** What the person chose: follow the device, or fix it. */
export type ColorModeChoice = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'ui.colorMode';

/** The bar's colour in each mode, for the browser's own chrome on a phone. */
const BAR_COLOR = { light: '#ffffff', dark: '#1c1815' } as const;

interface ColorModeValue {
  choice: ColorModeChoice;
  /** What is actually shown. */
  mode: 'light' | 'dark';
  setChoice: (choice: ColorModeChoice) => void;
  /** system, then light, then dark, then system again. */
  cycle: () => void;
}

const ColorModeContext = createContext<ColorModeValue>({
  choice: 'system',
  mode: 'light',
  setChoice: () => undefined,
  cycle: () => undefined,
});

function readChoice(): ColorModeChoice {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);

    return stored === 'light' || stored === 'dark' ? stored : 'system';
  } catch {
    return 'system';
  }
}

function systemPrefersDark(): boolean {
  return typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/**
 * The theme of the whole panel, light or dark. It follows the device until the person chooses, remembers the
 * choice in this browser, and tells the browser which scheme the page is in so scrollbars, form controls and
 * the phone's address bar match.
 */
export function ColorModeProvider({ children }: { children: ReactNode }) {
  const [choice, setChoiceState] = useState<ColorModeChoice>(readChoice);
  const [systemDark, setSystemDark] = useState(systemPrefersDark);

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return;

    const query = window.matchMedia('(prefers-color-scheme: dark)');
    const onChange = (event: MediaQueryListEvent) => setSystemDark(event.matches);

    query.addEventListener('change', onChange);

    return () => query.removeEventListener('change', onChange);
  }, []);

  const mode: 'light' | 'dark' = choice === 'system' ? (systemDark ? 'dark' : 'light') : choice;
  const theme = useMemo(() => buildTheme(mode), [mode]);

  useEffect(() => {
    document.documentElement.style.colorScheme = mode;
    document.documentElement.dataset.theme = mode;

    // One tag, kept in step: the colour of the bar at the top, so the phone's chrome does not clash.
    let meta = document.querySelector<HTMLMetaElement>('meta[name="theme-color"]');

    if (!meta) {
      meta = document.createElement('meta');
      meta.name = 'theme-color';
      document.head.appendChild(meta);
    }

    meta.content = BAR_COLOR[mode];
  }, [mode]);

  const setChoice = useCallback((next: ColorModeChoice) => {
    setChoiceState(next);

    try {
      if (next === 'system') window.localStorage.removeItem(STORAGE_KEY);
      else window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Blocked storage: the choice holds until the page is closed.
    }
  }, []);

  const cycle = useCallback(() => {
    setChoice(choice === 'system' ? 'light' : choice === 'light' ? 'dark' : 'system');
  }, [choice, setChoice]);

  const value = useMemo(() => ({ choice, mode, setChoice, cycle }), [choice, mode, setChoice, cycle]);

  return (
    <ColorModeContext.Provider value={value}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </ColorModeContext.Provider>
  );
}

export function useColorMode(): ColorModeValue {
  return useContext(ColorModeContext);
}
