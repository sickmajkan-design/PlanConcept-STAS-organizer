import { CloudDoneOutlined, WifiOffRounded } from '@mui/icons-material';
import { Alert, AlertTitle } from '@mui/material';
import { useEffect, useRef, useState } from 'react';

import { useOnlineStatus } from '../hooks/useOnlineStatus';
import { useT } from '../i18n/useI18n';

/** How long the "connection is back" message stays up, in milliseconds. */
const RESTORED_MS = 6_000;

/**
 * A bar that says the machine is offline, and briefly says when it is back.
 *
 * It earns its place because of how React Query behaves without a network. In
 * its default `networkMode: 'online'` a query that cannot run is *paused*, not
 * failed — the screen keeps showing a loading state indefinitely, and a
 * mutation sits in the same limbo with the save button spinning. There is no
 * error, so none of the screens' error states appear. Without this banner the
 * app looks broken and slow rather than disconnected, which on a site with one
 * bar of signal is the ordinary case rather than the exception.
 *
 * The restored message is not decoration: paused queries resume on reconnect
 * (`refetchOnReconnect` is on by default), so the moment it appears is also
 * the moment stale numbers on screen start becoming current, and it is worth
 * telling someone who was staring at a figure they were about to act on.
 */
export function OfflineBanner() {
  const online = useOnlineStatus();
  const t = useT();
  const [showRestored, setShowRestored] = useState(false);
  const wasOffline = useRef(false);

  useEffect(() => {
    if (!online) {
      wasOffline.current = true;
      setShowRestored(false);
      return;
    }

    // Only after an actual outage. Otherwise every load would open with a
    // "connection restored" for a connection that never went anywhere.
    if (!wasOffline.current) {
      return;
    }

    wasOffline.current = false;
    setShowRestored(true);

    const timer = window.setTimeout(() => setShowRestored(false), RESTORED_MS);

    return () => window.clearTimeout(timer);
  }, [online]);

  if (!online) {
    return (
      // `status` rather than MUI's default `alert`: a screen reader should
      // finish the sentence it is reading before announcing this, and an
      // assertive interruption on every flaky-signal blip is its own problem.
      <Alert severity="warning" icon={<WifiOffRounded />} role="status" sx={{ mb: 2 }}>
        <AlertTitle>{t('offline.title')}</AlertTitle>
        {t('offline.body')}
      </Alert>
    );
  }

  if (showRestored) {
    return (
      <Alert severity="success" icon={<CloudDoneOutlined />} role="status" sx={{ mb: 2 }}>
        {t('offline.restored')}
      </Alert>
    );
  }

  return null;
}
