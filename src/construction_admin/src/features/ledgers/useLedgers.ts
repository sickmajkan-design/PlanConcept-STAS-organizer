import { ledgersApi, type LedgerListQuery, type SetLedgerCellInput } from '../../api/ledgers';
import type {
  CreateLedgerInput,
  LedgerColumnInput,
  LedgerRowInput,
  LedgerSectionInput,
  UpdateLedgerInput,
} from '../../api/types';
import {
  createResourceKeys,
  useResourceDetail,
  useResourceList,
  useResourceMutation,
} from '../resourceQueries';

export const ledgerKeys = createResourceKeys<LedgerListQuery>('ledgers');

/** Every write below touches one ledger's nested shape, so they all just invalidate its one detail query. */
const ledgerDetailCache = (ledgerId: string) => [ledgerKeys.detail(ledgerId), ledgerKeys.all];

export function useLedgersQuery(query: LedgerListQuery) {
  return useResourceList(ledgerKeys, ledgersApi.list, query);
}

export function useLedgerQuery(id: string | undefined) {
  return useResourceDetail(ledgerKeys, ledgersApi.get, id);
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
    ledgerDetailCache(id),
  );
}

export function useDeleteLedger() {
  return useResourceMutation((id: string) => ledgersApi.remove(id), [ledgerKeys.all]);
}

export function useAddLedgerColumn(ledgerId: string) {
  return useResourceMutation(
    (input: LedgerColumnInput) => ledgersApi.columns.add(ledgerId, input),
    ledgerDetailCache(ledgerId),
  );
}

export function useUpdateLedgerColumn(ledgerId: string) {
  return useResourceMutation(
    (variables: { columnId: string; input: LedgerColumnInput }) =>
      ledgersApi.columns.update(variables.columnId, variables.input),
    ledgerDetailCache(ledgerId),
  );
}

export function useDeleteLedgerColumn(ledgerId: string) {
  return useResourceMutation(
    (columnId: string) => ledgersApi.columns.remove(columnId),
    ledgerDetailCache(ledgerId),
  );
}

export function useReorderLedgerColumns(ledgerId: string) {
  return useResourceMutation(
    (orderedColumnIds: string[]) => ledgersApi.columns.reorder(ledgerId, orderedColumnIds),
    ledgerDetailCache(ledgerId),
  );
}

export function useAddLedgerSection(ledgerId: string) {
  return useResourceMutation(
    (input: LedgerSectionInput) => ledgersApi.sections.add(ledgerId, input),
    ledgerDetailCache(ledgerId),
  );
}

export function useUpdateLedgerSection(ledgerId: string) {
  return useResourceMutation(
    (variables: { sectionId: string; input: LedgerSectionInput }) =>
      ledgersApi.sections.update(variables.sectionId, variables.input),
    ledgerDetailCache(ledgerId),
  );
}

export function useDeleteLedgerSection(ledgerId: string) {
  return useResourceMutation(
    (sectionId: string) => ledgersApi.sections.remove(sectionId),
    ledgerDetailCache(ledgerId),
  );
}

export function useReorderLedgerSections(ledgerId: string) {
  return useResourceMutation(
    (orderedSectionIds: string[]) => ledgersApi.sections.reorder(ledgerId, orderedSectionIds),
    ledgerDetailCache(ledgerId),
  );
}

export function useAddLedgerRow(ledgerId: string) {
  return useResourceMutation(
    (variables: { sectionId: string; input: LedgerRowInput }) =>
      ledgersApi.rows.add(variables.sectionId, variables.input),
    ledgerDetailCache(ledgerId),
  );
}

export function useUpdateLedgerRow(ledgerId: string) {
  return useResourceMutation(
    (variables: { rowId: string; input: LedgerRowInput }) =>
      ledgersApi.rows.update(variables.rowId, variables.input),
    ledgerDetailCache(ledgerId),
  );
}

export function useDeleteLedgerRow(ledgerId: string) {
  return useResourceMutation(
    (rowId: string) => ledgersApi.rows.remove(rowId),
    ledgerDetailCache(ledgerId),
  );
}

export function useReorderLedgerRows(ledgerId: string) {
  return useResourceMutation(
    (variables: { sectionId: string; orderedRowIds: string[] }) =>
      ledgersApi.rows.reorder(variables.sectionId, variables.orderedRowIds),
    ledgerDetailCache(ledgerId),
  );
}

export function useSetLedgerCell(ledgerId: string) {
  return useResourceMutation(
    (input: SetLedgerCellInput) => ledgersApi.setCell(input),
    ledgerDetailCache(ledgerId),
  );
}
