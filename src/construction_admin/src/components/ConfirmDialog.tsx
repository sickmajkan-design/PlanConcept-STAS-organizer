import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { toApiError } from '../api/apiError';
import { useT } from '../i18n/useI18n';

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  destructive = false,
  loading = false,
  error,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  description: string;
  confirmLabel?: string;
  destructive?: boolean;
  loading?: boolean;
  /** Shown under the description when the last confirm attempt failed. For a
   * caller that tracks its own error; a handler that returns a promise gets
   * this for free (see `onConfirm`). */
  error?: string | null;
  /**
   * May return a promise. If it rejects, the dialog stays open and shows why,
   * and the button shows progress while it runs.
   *
   * This is the rule that keeps a failed confirm from being silent. The dialog
   * stays open either way, so a handler that awaited a mutation and threw —
   * "you may not review your own cost", a conflict, a dropped connection —
   * used to leave the operator pressing a button that appeared to do nothing.
   */
  onConfirm: () => void | Promise<unknown>;
  onCancel: () => void;
}) {
  const t = useT();
  const [pending, setPending] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);

  // A dialog reopened for another row must not still be carrying the last
  // row's error.
  useEffect(() => {
    if (!open) setFailure(null);
  }, [open]);

  const confirm = async () => {
    setFailure(null);
    setPending(true);

    try {
      await onConfirm();
    } catch (err) {
      setFailure(toApiError(err).message);
    } finally {
      setPending(false);
    }
  };

  const shownError = error ?? failure;

  return (
    <Dialog open={open} onClose={onCancel} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{description}</DialogContentText>
        {shownError && (
          <Typography variant="body2" color="error" role="alert" sx={{ mt: 1.5 }}>
            {shownError}
          </Typography>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onCancel} disabled={loading || pending}>
          {t('common.cancel')}
        </Button>
        <Button
          onClick={() => void confirm()}
          color={destructive ? 'error' : 'primary'}
          variant="contained"
          loading={loading || pending}
        >
          {confirmLabel ?? t('common.delete')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
