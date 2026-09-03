import { request } from './client';
import { listParams } from './resource';
import type {
  CreateLedgerInput,
  LedgerColumn,
  LedgerColumnInput,
  LedgerDetail,
  LedgerRow,
  LedgerRowInput,
  LedgerSection,
  LedgerSectionInput,
  LedgerSummary,
  ListQuery,
  PagedList,
  UpdateLedgerInput,
} from './types';

export type LedgerListQuery = ListQuery;

export interface SetLedgerCellInput {
  rowId: string;
  columnId: string;
  value: string | null;
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
  },

  setCell: (input: SetLedgerCellInput) =>
    request<void>({ method: 'PUT', url: '/api/v1/ledgers/cells', data: input }),
};
