import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

import { useT } from '../i18n/useI18n';

export interface PreviewField {
  label: string;
  /** Left out of the preview when empty, so a record shows what it has and nothing else. */
  value: ReactNode;
}

/**
 * What opens when someone double-clicks a row: everything that arrived, in one place, with the
 * same buttons the row has. The lists keep their table; this is the "open it and read it" step.
 */
export function RecordPreviewDialog({
  open,
  title,
  heading,
  status,
  fields,
  children,
  actions,
  onClose,
}: {
  open: boolean;
  title: string;
  /** The line under the title: who or what the record is about. */
  heading?: ReactNode;
  status?: ReactNode;
  fields: PreviewField[];
  /** Longer content under the fields (an item list, attachments). */
  children?: ReactNode;
  actions?: ReactNode;
  onClose: () => void;
}) {
  const t = useT();
  const shown = fields.filter((f) => f.value !== null && f.value !== undefined && f.value !== '' && f.value !== false);

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {(heading || status) && (
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 1 }}>
              <Typography variant="h6" sx={{ overflowWrap: 'anywhere' }}>
                {heading}
              </Typography>
              {status}
            </Stack>
          )}
          {shown.map((field) => (
            <Box key={field.label}>
              <Typography variant="caption" color="text.secondary">
                {field.label}
              </Typography>
              <Typography component="div" sx={{ overflowWrap: 'anywhere', whiteSpace: 'pre-wrap' }}>
                {field.value}
              </Typography>
            </Box>
          ))}
          {children}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ flexWrap: 'wrap', gap: 1 }}>
        <Button onClick={onClose}>{t('common.close')}</Button>
        {actions}
      </DialogActions>
    </Dialog>
  );
}
