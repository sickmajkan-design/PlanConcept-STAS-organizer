import { Box } from '@mui/material';
import { useEffect, useState } from 'react';

import { COUNTRY_CODE_TO_LABEL } from '../data/countries';

type Flags = (typeof import('./countryFlags'))['FLAGS'];

let loaded: Flags | null = null;
let loading: Promise<Flags> | null = null;

/** The flags are fetched once, on first use, as a chunk of their own. */
function loadFlags(): Promise<Flags> {
  loading ??= import('./countryFlags').then((m) => (loaded = m.FLAGS));

  return loading;
}

/**
 * The flag of a country with its two-letter code, e.g. a Swiss flag and "CH". A real picture, because the flag emoji
 * show as two letters on Windows. The country's name is on hover and for a screen reader. A code the app does not know
 * shows as the letters alone, and no code shows nothing.
 */
export function CountryFlag({ code, showCode = true }: { code: string | null | undefined; showCode?: boolean }) {
  const [flags, setFlags] = useState<Flags | null>(loaded);

  useEffect(() => {
    if (!flags && code) void loadFlags().then(setFlags);
  }, [flags, code]);

  if (!code) return null;

  const upper = code.toUpperCase();
  const name = COUNTRY_CODE_TO_LABEL.get(upper) ?? upper;
  const Flag = flags?.[upper];

  return (
    <Box
      component="span"
      role="img"
      aria-label={name}
      title={name}
      sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, verticalAlign: 'middle', whiteSpace: 'nowrap' }}
    >
      {Flag && (
        <Box
          component={Flag}
          aria-hidden
          sx={{ width: 20, height: 14, borderRadius: '2px', boxShadow: '0 0 0 1px rgba(0,0,0,.2)', flex: 'none' }}
        />
      )}
      {showCode && (
        <Box component="span" sx={{ fontSize: '0.7rem', fontWeight: 600, letterSpacing: '0.03em', color: 'text.secondary' }}>
          {upper}
        </Box>
      )}
    </Box>
  );
}
