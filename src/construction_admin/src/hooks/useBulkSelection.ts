import { useState } from 'react';
import type { GridRowSelectionModel } from '@mui/x-data-grid';

const EMPTY_SELECTION: GridRowSelectionModel = { type: 'include', ids: new Set() };

/**
 * Wraps the grid's own selection model with the one thing every bulk-action
 * bar needs: a plain array of selected ids, plus a way to clear it after
 * acting on them.
 *
 * Only the `'include'` shape (the model MUI's grid produces for a bounded,
 * server-paged page of rows — every case this app has) is turned into ids;
 * `'exclude'` only arises from a "select all, then deselect a few" gesture
 * against an unbounded set, which nothing here offers, so it is treated as
 * no selection rather than guessed at.
 */
export function useBulkSelection() {
  const [model, setModel] = useState<GridRowSelectionModel>(EMPTY_SELECTION);

  const selectedIds = model.type === 'include' ? [...model.ids].map(String) : [];

  return {
    model,
    setModel,
    selectedIds,
    count: selectedIds.length,
    clear: () => setModel(EMPTY_SELECTION),
  };
}
