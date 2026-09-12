import { useQuery } from '@tanstack/react-query';

import {
  ledgersApi,
  type LedgerListQuery,
  type SetLedgerCellColorInput,
  type SetLedgerCellInput,
} from '../../api/ledgers';
import type {
  CreateLedgerInput,
  LedgerColumnInput,
  LedgerRowInput,
  LedgerSectionInput,
  LedgerSummaryBoxInput,
  PromoteLedgerRowToAccommodationRateInput,
  PromoteLedgerRowToGeneralExpenseInput,
  UpdateLedgerInput,
} from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const ledgerKeys = createResourceKeys<LedgerListQuery>('ledgers');

/** A section's rows/cells live in their own cache entry, fetched only once that section opens. */
const sectionRowsKey = (ledgerId: string, sectionId: string) =>
  [...ledgerKeys.all, 'sectionRows', ledgerId, sectionId] as const;

/** Matches every loaded section's rows for one ledger — used to invalidate them together. */
const sectionRowsPrefix = (ledgerId: string) => [...ledgerKeys.all, 'sectionRows', ledgerId] as const;

/** Renaming the ledger or reordering sections only touches the shell. */
const shellCache = (ledgerId: string) => [ledgerKeys.detail(ledgerId), ledgerKeys.all];

/** Adding/removing a row changes the shell's row counts *and* the section's own row list. */
const rowStructureCache = (ledgerId: string) => [
  ledgerKeys.detail(ledgerId),
  sectionRowsPrefix(ledgerId),
  summaryKey(ledgerId),
  ledgerKeys.all,
];

/** Editing a row's label or a cell's value never changes row counts — only the section's own rows. */
const rowContentCache = (ledgerId: string) => [
  sectionRowsPrefix(ledgerId),
  summaryKey(ledgerId),
];

/** The summary panel's boxes are live sums of cells, so any write that changes those needs to refresh it too. */
const summaryKey = (ledgerId: string) => [...ledgerKeys.all, 'summary', ledgerId] as const;

/** A column's shape (name/type/order) is shared by every section, so its cells need refreshing too. */
const columnCache = (ledgerId: string) => [
  ledgerKeys.detail(ledgerId),
  sectionRowsPrefix(ledgerId),
  summaryKey(ledgerId),
  ledgerKeys.all,
];

export function useLedgersQuery(query: LedgerListQuery) {
  return useResourceList(ledgerKeys, ledgersApi.list, query);
}

export function useLedgerQuery(id: string | undefined) {
  return useResourceDetail(ledgerKeys, ledgersApi.get, id);
}

/** One section's rows and cells — disabled until the section is actually expanded. */
export function useLedgerSectionRowsQuery(
  ledgerId: string,
  sectionId: string,
  enabled: boolean,
) {
  return useQuery({
    queryKey: sectionRowsKey(ledgerId, sectionId),
    queryFn: () => ledgersApi.rows.getForSection(ledgerId, sectionId),
    enabled: enabled && !!ledgerId && !!sectionId,
  });
}

export function useCreateLedger() {
  return useResourceMutation(
    (input: CreateLedgerInput) => ledgersApi.create(input),
    [ledgerKeys.all],
  );
}

export function useUpdateLedger(id: string) {
  return useResourceMutation(
    (input: UpdateLedgerInput) => ledgersApi.update(id, input),
    shellCache(id),
  );
}

export function useDeleteLedger() {
  return useResourceMutation((id: string) => ledgersApi.remove(id), [ledgerKeys.all]);
}

export function useAddLedgerColumn(ledgerId: string) {
  return useResourceMutation(
    (input: LedgerColumnInput) => ledgersApi.columns.add(ledgerId, input),
    columnCache(ledgerId),
  );
}

export function useUpdateLedgerColumn(ledgerId: string) {
  return useResourceMutation(
    (variables: { columnId: string; input: LedgerColumnInput }) =>
      ledgersApi.columns.update(variables.columnId, variables.input),
    columnCache(ledgerId),
  );
}

export function useDeleteLedgerColumn(ledgerId: string) {
  return useResourceMutation(
    (columnId: string) => ledgersApi.columns.remove(columnId),
    columnCache(ledgerId),
  );
}

export function useReorderLedgerColumns(ledgerId: string) {
  return useResourceMutation(
    (orderedColumnIds: string[]) => ledgersApi.columns.reorder(ledgerId, orderedColumnIds),
    columnCache(ledgerId),
  );
}

export function useAddLedgerSection(ledgerId: string) {
  return useResourceMutation(
    (input: LedgerSectionInput) => ledgersApi.sections.add(ledgerId, input),
    shellCache(ledgerId),
  );
}

export function useUpdateLedgerSection(ledgerId: string) {
  return useResourceMutation(
    (variables: { sectionId: string; input: LedgerSectionInput }) =>
      ledgersApi.sections.update(variables.sectionId, variables.input),
    shellCache(ledgerId),
  );
}

export function useDeleteLedgerSection(ledgerId: string) {
  return useResourceMutation(
    (sectionId: string) => ledgersApi.sections.remove(sectionId),
    shellCache(ledgerId),
  );
}

export function useReorderLedgerSections(ledgerId: string) {
  return useResourceMutation(
    (orderedSectionIds: string[]) => ledgersApi.sections.reorder(ledgerId, orderedSectionIds),
    shellCache(ledgerId),
  );
}

export function useAddLedgerRow(ledgerId: string) {
  return useResourceMutation(
    (variables: { sectionId: string; input: LedgerRowInput }) =>
      ledgersApi.rows.add(variables.sectionId, variables.input),
    rowStructureCache(ledgerId),
  );
}

export function useUpdateLedgerRow(ledgerId: string) {
  return useResourceMutation(
    (variables: { rowId: string; input: LedgerRowInput }) =>
      ledgersApi.rows.update(variables.rowId, variables.input),
    rowContentCache(ledgerId),
  );
}

export function useDeleteLedgerRow(ledgerId: string) {
  return useResourceMutation(
    (rowId: string) => ledgersApi.rows.remove(rowId),
    rowStructureCache(ledgerId),
  );
}

export function useReorderLedgerRows(ledgerId: string) {
  return useResourceMutation(
    (variables: { sectionId: string; orderedRowIds: string[] }) =>
      ledgersApi.rows.reorder(variables.sectionId, variables.orderedRowIds),
    rowContentCache(ledgerId),
  );
}

/** A promote also changes the "promoted this month" overview, on top of the row itself. */
const promotionCache = (ledgerId: string) => [
  ...rowContentCache(ledgerId),
  [...ledgerKeys.all, 'promotions', ledgerId],
];

export function usePromoteLedgerRowToGeneralExpense(ledgerId: string) {
  return useResourceMutation(
    (variables: { rowId: string; input: PromoteLedgerRowToGeneralExpenseInput }) =>
      ledgersApi.rows.promoteToGeneralExpense(variables.rowId, variables.input),
    promotionCache(ledgerId),
  );
}

export function usePromoteLedgerRowToAccommodationRate(ledgerId: string) {
  return useResourceMutation(
    (variables: { rowId: string; input: PromoteLedgerRowToAccommodationRateInput }) =>
      ledgersApi.rows.promoteToAccommodationRate(variables.rowId, variables.input),
    promotionCache(ledgerId),
  );
}

export function useSetLedgerCell(ledgerId: string) {
  return useResourceMutation(
    (input: SetLedgerCellInput) => ledgersApi.setCell(input),
    rowContentCache(ledgerId),
  );
}

export function useSetLedgerRowColor(ledgerId: string) {
  return useResourceMutation(
    (variables: { rowId: string; color: string | null }) =>
      ledgersApi.setRowColor(variables.rowId, variables.color),
    rowContentCache(ledgerId),
  );
}

export function useSetLedgerCellColor(ledgerId: string) {
  return useResourceMutation(
    (input: SetLedgerCellColorInput) => ledgersApi.setCellColor(input),
    rowContentCache(ledgerId),
  );
}

/** Every row with no Employee/Vehicle/Tool/Material link — one call, not one per section. */
export function useLedgerUnlinkedRowsQuery(ledgerId: string, enabled: boolean) {
  return useQuery({
    queryKey: [...ledgerKeys.all, 'unlinkedRows', ledgerId] as const,
    queryFn: () => ledgersApi.unlinkedRows(ledgerId),
    enabled: enabled && !!ledgerId,
  });
}

/** Every row in this ledger already pushed through to a real expense/rate. */
export function useLedgerPromotionsQuery(ledgerId: string) {
  return useQuery({
    queryKey: [...ledgerKeys.all, 'promotions', ledgerId] as const,
    queryFn: () => ledgersApi.promotions(ledgerId),
    enabled: !!ledgerId,
  });
}

/** The month-summary panel — every box's live value and the net total. */
export function useLedgerSummaryQuery(ledgerId: string) {
  return useQuery({
    queryKey: summaryKey(ledgerId),
    queryFn: () => ledgersApi.summary.get(ledgerId),
    enabled: !!ledgerId,
  });
}

export function useAddLedgerSummaryBox(ledgerId: string) {
  return useResourceMutation(
    (input: LedgerSummaryBoxInput) => ledgersApi.summary.addBox(ledgerId, input),
    [summaryKey(ledgerId)],
  );
}

export function useUpdateLedgerSummaryBox(ledgerId: string) {
  return useResourceMutation(
    (variables: { boxId: string; input: LedgerSummaryBoxInput }) =>
      ledgersApi.summary.updateBox(variables.boxId, variables.input),
    [summaryKey(ledgerId)],
  );
}

export function useDeleteLedgerSummaryBox(ledgerId: string) {
  return useResourceMutation(
    (boxId: string) => ledgersApi.summary.removeBox(boxId),
    [summaryKey(ledgerId)],
  );
}

export function useReorderLedgerSummaryBoxes(ledgerId: string) {
  return useResourceMutation(
    (orderedBoxIds: string[]) => ledgersApi.summary.reorderBoxes(ledgerId, orderedBoxIds),
    [summaryKey(ledgerId)],
  );
}
