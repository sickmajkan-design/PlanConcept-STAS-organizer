import { ExpandLessOutlined, ExpandMoreOutlined, HelpOutlineOutlined } from '@mui/icons-material';
import { Box, Collapse, IconButton, Paper, Stack, Typography } from '@mui/material';
import { useState, type ReactNode } from 'react';

import { useT } from '../../i18n/useI18n';

function read(id: string): boolean {
  try {
    return window.localStorage.getItem(`book.howItWorks.${id}`) !== 'closed';
  } catch {
    return true;
  }
}

function write(id: string, open: boolean) {
  try {
    window.localStorage.setItem(`book.howItWorks.${id}`, open ? 'open' : 'closed');
  } catch {
    // Remembering is a convenience; it never blocks the page.
  }
}

/**
 * The "How this works" box every screen of the book carries, as the mockup has it: a few plain sentences about
 * what the screen is and how it is counted. Open the first time; once closed it stays closed on that screen.
 */
export function HowItWorks({ id, children }: { id: string; children: ReactNode }) {
  const t = useT();
  const [open, setOpen] = useState(() => read(id));

  const toggle = () => {
    setOpen((value) => {
      write(id, !value);
      return !value;
    });
  };

  return (
    <Paper variant="outlined" sx={{ bgcolor: 'action.hover', borderStyle: 'dashed' }}>
      <Stack
        direction="row"
        onClick={toggle}
        sx={{ alignItems: 'center', justifyContent: 'space-between', px: 2, py: 1, cursor: 'pointer' }}
      >
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <HelpOutlineOutlined fontSize="small" color="action" />
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            {t('book.howItWorks')}
          </Typography>
        </Stack>
        <IconButton size="small" aria-label={t('book.howItWorks')} aria-expanded={open}>
          {open ? <ExpandLessOutlined fontSize="small" /> : <ExpandMoreOutlined fontSize="small" />}
        </IconButton>
      </Stack>
      <Collapse in={open} unmountOnExit>
        <Box sx={{ px: 2, pb: 2 }}>
          <Stack spacing={1}>{children}</Stack>
        </Box>
      </Collapse>
    </Paper>
  );
}

/** One sentence of a "How this works" box. */
export function HowLine({ children }: { children: ReactNode }) {
  return (
    <Typography variant="body2" color="text.secondary">
      {children}
    </Typography>
  );
}
