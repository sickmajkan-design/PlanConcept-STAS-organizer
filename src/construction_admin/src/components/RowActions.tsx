import { Stack } from '@mui/material';
import type { ReactNode } from 'react';

/**
 * Wraps the action buttons in a grid row's last cell.
 *
 * Its only job is to stop the click reaching the row.
 *
 * Every list here gives the row an `onRowClick` that navigates to the detail
 * page, and the action buttons sit inside a cell of that same row. A click on
 * one of them bubbles, so pressing **Delete** opened the confirmation dialog
 * and navigated away in the same tick — the dialog was mounted and unmounted
 * before it could be seen, and nothing was ever deleted. The View and Edit
 * buttons hid it: they navigate to roughly where the row click was going
 * anyway, so only Delete looked broken, and only to somebody who tried it.
 *
 * Found by the first screen test written against a list page, which is the
 * argument for having them.
 */
export function RowActions({ children }: { children: ReactNode }) {
  return (
    <Stack
      direction="row"
      spacing={0.5}
      // On the container rather than on each button: a button added later
      // inherits the behaviour instead of needing to remember it.
      onClick={(event) => event.stopPropagation()}
    >
      {children}
    </Stack>
  );
}
