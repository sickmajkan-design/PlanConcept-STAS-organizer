import { DownloadOutlined, ExpandMoreOutlined } from '@mui/icons-material';
import {
  Button,
  ButtonGroup,
  CircularProgress,
  ListItemText,
  Menu,
  MenuItem,
  Snackbar,
} from '@mui/material';
import { useRef, useState } from 'react';

import type { ExportLanguage } from '../api/exports';
import { useI18n, useT } from '../i18n/useI18n';

/**
 * Downloads a spreadsheet, with a menu for the language of its headings.
 *
 * The language is offered rather than taken from the interface, because the
 * person exporting is often not the person reading. The office runs the app in
 * Serbian and sends the file to an accountant or a foreign client; defaulting
 * to the interface language and hiding the choice would make that a copy-paste
 * job.
 */
export function ExportButton({
  onExport,
  disabled,
}: {
  onExport: (language: ExportLanguage) => Promise<void>;
  disabled?: boolean;
}) {
  const t = useT();
  const { locale } = useI18n();
  const anchorRef = useRef<HTMLDivElement | null>(null);

  const [menuOpen, setMenuOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  const run = async (language: ExportLanguage) => {
    setMenuOpen(false);
    setBusy(true);

    try {
      await onExport(language);
    } catch {
      // The download either happens or it does not; there is no partial file
      // to explain, so a single message is the whole story.
      setFailed(true);
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <ButtonGroup ref={anchorRef} variant="outlined" size="small" disabled={disabled || busy}>
        {/* The plain click uses the language already on screen, which is the
            common case; the caret is there for the other one. */}
        <Button
          startIcon={
            busy ? <CircularProgress size={16} /> : <DownloadOutlined />
          }
          onClick={() => void run(locale)}
        >
          {busy ? t('exports.working') : t('exports.download')}
        </Button>
        <Button
          size="small"
          onClick={() => setMenuOpen(true)}
          aria-label={t('exports.language')}
        >
          <ExpandMoreOutlined fontSize="small" />
        </Button>
      </ButtonGroup>

      <Menu
        open={menuOpen}
        anchorEl={anchorRef.current}
        onClose={() => setMenuOpen(false)}
      >
        <MenuItem onClick={() => void run('sr')}>
          <ListItemText
            primary={`${t('exports.language')}: ${t('exports.languageSr')}`}
          />
        </MenuItem>
        <MenuItem onClick={() => void run('en')}>
          <ListItemText
            primary={`${t('exports.language')}: ${t('exports.languageEn')}`}
          />
        </MenuItem>
      </Menu>

      <Snackbar
        open={failed}
        autoHideDuration={6000}
        onClose={() => setFailed(false)}
        message={t('exports.failed')}
      />
    </>
  );
}
