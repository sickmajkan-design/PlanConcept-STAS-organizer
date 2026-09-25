import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Link as MuiLink,
  Stack,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { useAuth } from '../../auth/useAuth';
import { config } from '../../config';
import { useT } from '../../i18n/useI18n';
import { hasSeen, markSeen, sectionsFor } from './releaseNotes';

/** The item that names the address the phone app is downloaded from. */
const DOWNLOAD_ITEM = 'releaseNotes.setup.downloadLink';

/**
 * "What's new", shown once after signing in — so the people using the platform
 * know something was done, without having to be told separately.
 *
 * Shown to an account once per release, and only the parts that concern it: a
 * worker is not told about the finance screens they cannot open. Dismissing it
 * (by any route) is remembered for that account in this browser.
 */
export function ReleaseNotesDialog() {
  const t = useT();
  const { user } = useAuth();
  const [open, setOpen] = useState(false);

  const userId = user?.id;

  useEffect(() => {
    if (userId && !hasSeen(userId)) setOpen(true);
  }, [userId]);

  if (!user) return null;

  const sections = sectionsFor(user);

  const close = () => {
    markSeen(user.id);
    setOpen(false);
  };

  return (
    <Dialog open={open && sections.length > 0} onClose={close} fullWidth maxWidth="sm" scroll="paper">
      <DialogTitle>{t('releaseNotes.title')}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2.5}>
          <Typography variant="body2" color="text.secondary">
            {t('releaseNotes.intro')}
          </Typography>

          {sections.map((section) => (
            <Stack key={section.id} spacing={0.75}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                {t(section.titleKey)}
              </Typography>
              <Stack component="ul" spacing={0.75} sx={{ m: 0, pl: 2.5 }}>
                {section.itemKeys
                  // The download note is about an address this installation may not have.
                  .filter((key) => key !== DOWNLOAD_ITEM || config.appDownloadUrl)
                  .map((key) => (
                    <Typography key={key} component="li" variant="body2">
                      {t(key)}
                      {key === DOWNLOAD_ITEM && (
                        <>
                          {' '}
                          <MuiLink href={config.appDownloadUrl} sx={{ wordBreak: 'break-all' }}>
                            {config.appDownloadUrl}
                          </MuiLink>
                        </>
                      )}
                    </Typography>
                  ))}
              </Stack>
            </Stack>
          ))}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button variant="contained" onClick={close}>
          {t('releaseNotes.close')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
