import { HelpOutlineOutlined } from '@mui/icons-material';
import { Alert, Button, Stack, Typography } from '@mui/material';
import { useState, type ReactNode } from 'react';

import { useT } from '../../i18n/useI18n';

const key = (id: string) => `files.hint.${id}`;

function dismissed(id: string): boolean {
  try {
    return window.localStorage.getItem(key(id)) === 'done';
  } catch {
    return false;
  }
}

function remember(id: string) {
  try {
    window.localStorage.setItem(key(id), 'done');
  } catch {
    // Remembering is a convenience; the hint can simply show again.
  }
}

/**
 * A short explanation shown the first time somebody meets a feature, and never again once they have said
 * they understood. It stays one click away ("How does this work?") so it can be read again later.
 */
export function FirstUseHint({ id, title, children }: { id: string; title: string; children: ReactNode }) {
  const t = useT();
  const [open, setOpen] = useState(() => !dismissed(id));

  if (!open) {
    return (
      <Button
        size="small"
        startIcon={<HelpOutlineOutlined fontSize="small" />}
        onClick={() => setOpen(true)}
        sx={{ alignSelf: 'flex-start' }}
      >
        {t('files.hint.show')}
      </Button>
    );
  }

  return (
    <Alert
      severity="info"
      icon={<HelpOutlineOutlined fontSize="inherit" />}
      action={
        <Button
          color="inherit"
          size="small"
          onClick={() => {
            remember(id);
            setOpen(false);
          }}
        >
          {t('files.hint.gotIt')}
        </Button>
      }
    >
      <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 0.5 }}>
        {title}
      </Typography>
      <Stack component="ul" spacing={0.5} sx={{ m: 0, pl: 2.5 }}>
        {children}
      </Stack>
    </Alert>
  );
}

/** One point of a hint. */
export function HintLine({ children }: { children: ReactNode }) {
  return (
    <Typography component="li" variant="body2">
      {children}
    </Typography>
  );
}
