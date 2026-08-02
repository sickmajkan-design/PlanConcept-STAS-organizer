import { LanguageOutlined } from '@mui/icons-material';
import { IconButton, Menu, MenuItem, Tooltip } from '@mui/material';
import { useState } from 'react';

import { useI18n } from '../i18n/useI18n';
import { locales, localeNames } from '../i18n/types';

/**
 * Language picker for the app bar.
 *
 * Deliberately reachable without signing in as well, since someone who cannot
 * read the sign-in screen cannot get past it to change the setting.
 */
export function LanguageSwitcher() {
  const { locale, setLocale, t } = useI18n();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  return (
    <>
      <Tooltip title={t('common.language')}>
        <IconButton
          color="inherit"
          aria-label={t('common.language')}
          onClick={(event) => setAnchor(event.currentTarget)}
        >
          <LanguageOutlined />
        </IconButton>
      </Tooltip>

      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        {locales.map((value) => (
          <MenuItem
            key={value}
            selected={value === locale}
            onClick={() => {
              setLocale(value);
              setAnchor(null);
            }}
          >
            {localeNames[value]}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
