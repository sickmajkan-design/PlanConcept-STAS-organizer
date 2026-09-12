import { AutoAwesomeRounded } from '@mui/icons-material';
import { Fab, Tooltip } from '@mui/material';
import { useState } from 'react';

import { useAssistantStatus } from '../../features/assistant/useAssistant';
import { useT } from '../../i18n/useI18n';
import { AssistantPanel } from './AssistantPanel';

/**
 * The button that opens the assistant, and nothing at all when there is no
 * assistant to open.
 *
 * An installation without an API key is the normal case, not a fault — so it
 * shows no button rather than one that answers 503. The status call is asked
 * once per session and never retried: a panel that is missing is a smaller
 * problem than a panel that fails when pressed.
 */
export function AssistantLauncher() {
  const t = useT();
  const { data } = useAssistantStatus();
  const [open, setOpen] = useState(false);

  if (!data?.enabled) {
    return null;
  }

  return (
    <>
      <Tooltip title={t('assistant.title')}>
        <Fab
          color="primary"
          size="medium"
          aria-label={t('assistant.title')}
          onClick={() => setOpen(true)}
          sx={{ position: 'fixed', right: 24, bottom: 24, zIndex: (theme) => theme.zIndex.drawer - 1 }}
        >
          <AutoAwesomeRounded />
        </Fab>
      </Tooltip>

      <AssistantPanel open={open} onClose={() => setOpen(false)} />
    </>
  );
}
