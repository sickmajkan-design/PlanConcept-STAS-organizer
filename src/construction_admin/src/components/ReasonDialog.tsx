import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  TextField,
} from '@mui/material';
import { useEffect, useState, type ReactNode } from 'react';

import { toApiError } from '../api/apiError';
import { useT } from '../i18n/useI18n';

/**
 * "Send it back, and say why" - the one dialog behind every rejection
 * (time entries, absences, vehicle costs). The API refuses a rejection with no
 * reason, so the button stays off until one is typed.
 *
 * `onSubmit` may return a promise: if it rejects, the dialog stays open and the
 * reason is shown under the field, so a failed rejection is never silent.
 */
export function ReasonDialog({
  open,
  title,
  hint,
  extra,
  label,
  submitLabel,
  onSubmit,
  onClose,
}: {
  open: boolean;
  title: string;
  hint: string;
  /** Context shown between the hint and the field, e.g. a leave balance. */
  extra?: ReactNode;
  label: string;
  submitLabel: string;
  onSubmit: (note: string) => void | Promise<unknown>;
  onClose: () => void;
}) {
  const t = useT();
  const [note, setNote] = useState('');
  const [pending, setPending] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);

  // Reopened for another row: start clean.
  useEffect(() => {
    if (!open) {
      setNote('');
      setFailure(null);
    }
  }, [open]);

  const submit = async () => {
    setFailure(null);
    setPending(true);

    try {
      await onSubmit(note.trim());
    } catch (err) {
      setFailure(toApiError(err).message);
    } finally {
      setPending(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: extra ? 0.5 : 2 }}>{hint}</DialogContentText>
        {extra}
        <TextField
          autoFocus
          fullWidth
          multiline
          minRows={2}
          label={label}
          value={note}
          onChange={(event) => setNote(event.target.value)}
          error={!!failure}
          helperText={failure ?? undefined}
          sx={{ mt: extra ? 2 : 0 }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={pending}>
          {t('common.cancel')}
        </Button>
        <Button
          variant="contained"
          color="warning"
          disabled={!note.trim()}
          loading={pending}
          onClick={() => void submit()}
        >
          {submitLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
