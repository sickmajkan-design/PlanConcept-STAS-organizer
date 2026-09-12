import { Button, Snackbar } from '@mui/material';
import { useEffect, useState } from 'react';

import { subscribeUndoQueue, undoDelete, type PendingUndo } from '../hooks/undoQueue';
import { useT } from '../i18n/useI18n';

/**
 * One snackbar, mounted once, showing the oldest pending delete with an
 * Undo action. Multiple deletes queue rather than stack — a wall of toasts
 * for a multi-row delete would be worse than showing them one at a time.
 */
export function UndoSnackbarHost() {
  const t = useT();
  const [queue, setQueue] = useState<PendingUndo[]>([]);

  useEffect(() => subscribeUndoQueue(setQueue), []);

  const current = queue[0];

  return (
    <Snackbar
      open={!!current}
      message={current?.message}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      action={
        current && (
          <Button color="inherit" size="small" onClick={() => undoDelete(current.id)}>
            {t('common.undo')}
          </Button>
        )
      }
    />
  );
}
