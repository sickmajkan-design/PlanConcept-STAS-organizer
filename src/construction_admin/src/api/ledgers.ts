import { request } from './client';
import { listParams } from './resource';
import type {
  CreateLedgerInput,
  LedgerColumn,
  LedgerColumnInput,
  LedgerDetail,
  LedgerPromotion,
  LedgerRow,
  LedgerUnlinkedRow,
  LedgerRowInput,
  LedgerSection,
  LedgerSectionInput,
  LedgerSummary,
  LedgerSummaryBox,
  LedgerSummaryBoxInput,
  LedgerSummaryPanel,
  ListQuery,
  PagedList,
  PromoteLedgerRowToAccommodationRateInput,
  PromoteLedgerRowToGeneralExpenseInput,
  UpdateLedgerInput,
} from './types';

export type LedgerListQuery = ListQuery;

export interface SetLedgerCellInput {
  rowId: string;
  columnId: string;
  value: string | null;
}

export interface SetLedgerCellColorInput {
  rowId: string;
  columnId: string;
  color: string | null;
}

/**
 * Every route the SuperAdmin's free-form ledger needs — deliberately not
 * `createCrudApi`, since a ledger's shape (columns/sections/rows/cells) is
 * too nested for that shared helper.
 */
export const ledgersApi = {
  list: (query: LedgerListQuery) =>
    request<PagedList<LedgerSummary>>({
      method: 'GET',
      url: '/api/v1/ledgers',
      params: listParams(query),
    }),

  get: (id: string) =>
    request<LedgerDetail>({ method: 'GET', url: `/api/v1/ledgers/${id}` }),

  create: (input: CreateLedgerInput) =>
    request<LedgerDetail>({ method: 'POST', url: '/api/v1/ledgers', data: input }),

  update: (id: string, input: UpdateLedgerInput) =>
    request<LedgerDetail>({ method: 'PUT', url: `/api/v1/ledgers/${id}`, data: input }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/ledgers/${id}` }),

  columns: {
    add: (ledgerId: string, input: LedgerColumnInput) =>
      request<LedgerColumn>({
        method: 'POST',
        url: `/api/v1/ledgers/${ledgerId}/columns`,
        data: input,
      }),

    update: (columnId: string, input: LedgerColumnInput) =>
      request<LedgerColumn>({
        method: 'PUT',
        url: `/api/v1/ledgers/columns/${columnId}`,
        data: input,
      }),

    remove: (columnId: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/ledgers/columns/${columnId}` }),

    reorder: (ledgerId: string, orderedColumnIds: string[]) =>
      request<void>({
        method: 'PUT',
        url: `/api/v1/ledgers/${ledgerId}/columns/reorder`,
        data: { orderedColumnIds },
      }),
  },

  sections: {
    add: (ledgerId: string, input: LedgerSectionInput) =>
      request<LedgerSection>({
        method: 'POST',
        url: `/api/v1/ledgers/${ledgerId}/sections`,
        data: input,
      }),

    update: (sectionId: string, input: LedgerSectionInput) =>
      request<LedgerSection>({
        method: 'PUT',
        url: `/api/v1/ledgers/sections/${sectionId}`,
        data: input,
      }),

    remove: (sectionId: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/ledgers/sections/${sectionId}` }),

    reorder: (ledgerId: string, orderedSectionIds: string[]) =>
      request<void>({
        method: 'PUT',
        url: `/api/v1/ledgers/${ledgerId}/sections/reorder`,
        data: { orderedSectionIds },
      }),
  },

  rows: {
    /** One section's rows and cells — fetched only once that section is opened. */
    getForSection: (ledgerId: string, sectionId: string) =>
      request<LedgerSection>({
        method: 'GET',
        url: `/api/v1/ledgers/${ledgerId}/sections/${sectionId}/rows`,
      }),

    add: (sectionId: string, input: LedgerRowInput) =>
      request<LedgerRow>({
        method: 'POST',
        url: `/api/v1/ledgers/sections/${sectionId}/rows`,
        data: input,
      }),

    update: (rowId: string, input: LedgerRowInput) =>
      request<LedgerRow>({
        method: 'PUT',
        url: `/api/v1/ledgers/rows/${rowId}`,
        data: input,
      }),

    remove: (rowId: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/ledgers/rows/${rowId}` }),

    reorder: (sectionId: string, orderedRowIds: string[]) =>
      request<void>({
        method: 'PUT',
        url: `/api/v1/ledgers/sections/${sectionId}/rows/reorder`,
        data: { orderedRowIds },
      }),

    /** Pushes a manually-typed row through the real General Expense form — an explicit action, not a sync. */
    promoteToGeneralExpense: (rowId: string, input: PromoteLedgerRowToGeneralExpenseInput) =>
      request<LedgerRow>({
        method: 'POST',
        url: `/api/v1/ledgers/rows/${rowId}/promote/general-expense`,
        data: input,
      }),

    /** Pushes a manually-typed row through the real Accommodation-rate form. */
    promoteToAccommodationRate: (rowId: string, input: PromoteLedgerRowToAccommodationRateInput) =>
      request<LedgerRow>({
        method: 'POST',
        url: `/api/v1/ledgers/rows/${rowId}/promote/accommodation-rate`,
        data: input,
      }),
  },

  setCell: (input: SetLedgerCellInput) =>
    request<void>({ method: 'PUT', url: '/api/v1/ledgers/cells', data: input }),

  setRowColor: (rowId: string, color: string | null) =>
    request<void>({ method: 'PUT', url: `/api/v1/ledgers/rows/${rowId}/color`, data: { color } }),

  setCellColor: (input: SetLedgerCellColorInput) =>
    request<void>({ method: 'PUT', url: '/api/v1/ledgers/cells/color', data: input }),

  /** Every row with no Employee/Vehicle/Tool/Material link — one call across the whole ledger. */
  unlinkedRows: (ledgerId: string) =>
    request<LedgerUnlinkedRow[]>({
      method: 'GET',
      url: `/api/v1/ledgers/${ledgerId}/unlinked-rows`,
    }),

  /** Every row already pushed through to a real expense/rate this month. */
  promotions: (ledgerId: string) =>
    request<LedgerPromotion[]>({
      method: 'GET',
      url: `/api/v1/ledgers/${ledgerId}/promotions`,
    }),

  summary: {
    get: (ledgerId: string) =>
      request<LedgerSummaryPanel>({
        method: 'GET',
        url: `/api/v1/ledgers/${ledgerId}/summary`,
      }),

    addBox: (ledgerId: string, input: LedgerSummaryBoxInput) =>
      request<LedgerSummaryBox>({
        method: 'POST',
        url: `/api/v1/ledgers/${ledgerId}/summary-boxes`,
        data: input,
      }),

    updateBox: (boxId: string, input: LedgerSummaryBoxInput) =>
      request<LedgerSummaryBox>({
        method: 'PUT',
        url: `/api/v1/ledgers/summary-boxes/${boxId}`,
        data: input,
      }),

    removeBox: (boxId: string) =>
      request<void>({ method: 'DELETE', url: `/api/v1/ledgers/summary-boxes/${boxId}` }),

    reorderBoxes: (ledgerId: string, orderedBoxIds: string[]) =>
      request<void>({
        method: 'PUT',
        url: `/api/v1/ledgers/${ledgerId}/summary-boxes/reorder`,
        data: { orderedBoxIds },
      }),
  },
};
