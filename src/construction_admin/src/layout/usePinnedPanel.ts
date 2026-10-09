import { useCallback, useEffect, useState } from 'react';

import { useAuth } from '../auth/useAuth';
import { readScoped, storageScope, writeScoped } from '../hooks/userScopedStorage';

const PINNED_PANEL_KEY = 'nav.panelPinned';

/**
 * Whether this account keeps the menu's flyout panel open next to the rail instead of letting it
 * fold away. Off by default: the rail alone gives the pages the whole screen. Only honoured on
 * wide screens, where there is room for both.
 */
export function usePinnedPanel() {
  const { user } = useAuth();
  const scope = storageScope(user);
  const [pinned, setPinned] = useState<boolean>(() => readScoped<boolean>(scope, PINNED_PANEL_KEY, false));

  // A different account on the same mounted layout must not inherit the previous one's choice.
  useEffect(() => {
    setPinned(readScoped<boolean>(scope, PINNED_PANEL_KEY, false));
  }, [scope]);

  const togglePinned = useCallback(() => {
    setPinned((prev) => {
      writeScoped(scope, PINNED_PANEL_KEY, !prev);
      return !prev;
    });
  }, [scope]);

  return { pinned, togglePinned };
}
