import { DarkModeOutlined, LightModeOutlined, SettingsBrightnessOutlined } from '@mui/icons-material';
import { IconButton, Tooltip } from '@mui/material';

import { useColorMode } from '../ColorMode';
import { useT } from '../i18n/useI18n';

/**
 * One button for the look of the panel: follow the device, light, or dark. Each tap goes to the next, and the
 * tooltip says what is on now. Reachable before signing in, like the language switch beside it.
 */
export function ColorModeToggle() {
  const t = useT();
  const { choice, cycle } = useColorMode();

  const label =
    choice === 'system' ? t('theme.system') : choice === 'light' ? t('theme.light') : t('theme.dark');

  return (
    <Tooltip title={`${t('theme.title')}: ${label}`}>
      <IconButton color="inherit" aria-label={`${t('theme.title')}: ${label}`} onClick={cycle}>
        {choice === 'system' ? <SettingsBrightnessOutlined /> : choice === 'light' ? <LightModeOutlined /> : <DarkModeOutlined />}
      </IconButton>
    </Tooltip>
  );
}
