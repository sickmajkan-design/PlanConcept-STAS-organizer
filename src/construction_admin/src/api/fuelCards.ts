import { request } from './client';
import { idempotencyHeaders } from './idempotency';
import { listParams } from './resource';
import type {
  FuelCard,
  FuelCardInput,
  FuelImportColumnMapping,
  FuelImportPreviewResult,
  FuelImportResult,
  ListQuery,
  PagedList,
} from './types';

export interface FuelCardListQuery extends ListQuery {
  vehicleId?: string;
}

/** Mirrors the API's FuelImportRules.MaxSizeBytes. */
export const MAX_FUEL_STATEMENT_BYTES = 10 * 1024 * 1024;

export const FUEL_STATEMENT_ACCEPTED_EXTENSIONS = '.xlsx,.csv';

function importForm(file: File, mapping: FuelImportColumnMapping, hasHeaderRow: boolean) {
  const form = new FormData();
  form.append('file', file);
  form.append('hasHeaderRow', String(hasHeaderRow));
  form.append('cardNumberColumn', String(mapping.cardNumberColumn));
  form.append('occurredOnColumn', String(mapping.occurredOnColumn));
  form.append('amountColumn', String(mapping.amountColumn));
  form.append('litresColumn', String(mapping.litresColumn));

  if (mapping.supplierColumn != null) {
    form.append('supplierColumn', String(mapping.supplierColumn));
  }

  if (mapping.noteColumn != null) {
    form.append('noteColumn', String(mapping.noteColumn));
  }

  if (mapping.odometerColumn != null) {
    form.append('odometerColumn', String(mapping.odometerColumn));
  }

  if (mapping.fuelProductTypeColumn != null) {
    form.append('fuelProductTypeColumn', String(mapping.fuelProductTypeColumn));
  }

  return form;
}

export const fuelCardsApi = {
  list: (query: FuelCardListQuery) =>
    request<PagedList<FuelCard>>({
      method: 'GET',
      url: '/api/v1/fuel-cards',
      params: listParams(query),
    }),

  add: (input: FuelCardInput, idempotencyKey?: string) =>
    request<FuelCard>({
      method: 'POST',
      url: '/api/v1/fuel-cards',
      data: input,
      headers: idempotencyHeaders(idempotencyKey),
    }),

  remove: (id: string) =>
    request<void>({ method: 'DELETE', url: `/api/v1/fuel-cards/${id}` }),

  import: {
    preview: (file: File, mapping: FuelImportColumnMapping, hasHeaderRow: boolean) =>
      request<FuelImportPreviewResult>({
        method: 'POST',
        url: '/api/v1/fuel-cards/import/preview',
        data: importForm(file, mapping, hasHeaderRow),
      }),

    commit: (file: File, mapping: FuelImportColumnMapping, hasHeaderRow: boolean) =>
      request<FuelImportResult>({
        method: 'POST',
        url: '/api/v1/fuel-cards/import',
        data: importForm(file, mapping, hasHeaderRow),
      }),
  },
};
