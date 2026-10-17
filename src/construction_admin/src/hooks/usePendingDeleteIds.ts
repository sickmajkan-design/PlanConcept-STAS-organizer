import { useEffect, useState } from 'react';

import { subscribeUndoQueue } from './undoQueue';

/**
 * The records whose delete is waiting out its undo window. A list hides these rows at once, so a click on
 * the bin visibly does something and cannot be clicked twice, while Undo still brings the row back.
 */
export function usePendingDeleteIds(): ReadonlySet<string> {
  const [ids, setIds] = useState<ReadonlySet<string>>(new Set());

  useEffect(
    () =>
      subscribeUndoQueue((items) => {
        setIds(new Set(items.flatMap((item) => (item.subjectId ? [item.subjectId] : []))));
      }),
    [],
  );

  return ids;
}
