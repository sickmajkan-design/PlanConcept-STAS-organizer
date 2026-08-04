import { apiClient } from './client';
import { listParams } from './resource';

export type ExportLanguage = 'sr' | 'en';

export interface ExportQuery {
  from: string;
  to: string;
  /** Headings in this language. Serbian when unset. */
  language?: ExportLanguage;
}

export interface TimeEntryExportQuery extends ExportQuery {
  employeeId?: string;
  projectId?: string;
  approvedOnly?: boolean;
}

/**
 * Fetches a spreadsheet and hands it to the browser as a download.
 *
 * The endpoints need a bearer token, so a plain `<a href>` cannot reach them —
 * the browser would send that request without the token and get a 401. Going
 * through the authenticated client and wrapping the bytes in a blob URL is the
 * only way a private export downloads at all.
 *
 * The file name comes from the server rather than being guessed here, so the
 * period in the name always matches the period in the file.
 */
async function download(url: string, query: object): Promise<void> {
  const response = await apiClient.request<Blob>({
    method: 'GET',
    url,
    params: listParams(query),
    responseType: 'blob',
  });

  const objectUrl = URL.createObjectURL(response.data);

  try {
    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = fileNameFrom(response.headers['content-disposition']) ?? 'export.xlsx';
    link.click();
  } finally {
    // Revoked immediately: the click has already handed the blob to the
    // download manager, and holding it would keep the whole file in memory
    // for as long as the tab is open.
    URL.revokeObjectURL(objectUrl);
  }
}

/**
 * Reads the file name out of `Content-Disposition`.
 *
 * Prefers `filename*`, which is the RFC 5987 form and the only one that
 * survives a non-ASCII name. Our names are ASCII by construction, so the plain
 * `filename` is the usual hit — but reading the encoded one first costs
 * nothing and stops this being wrong the day that changes.
 */
function fileNameFrom(header: unknown): string | null {
  if (typeof header !== 'string') return null;

  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (encoded) return decodeURIComponent(encoded[1]);

  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain ? plain[1] : null;
}

export const exportsApi = {
  timeEntries: (query: TimeEntryExportQuery) =>
    download('/api/exports/time-entries', query),

  projectCosts: (query: ExportQuery & { projectId?: string }) =>
    download('/api/exports/project-costs', query),

  vehicleCosts: (query: ExportQuery & { vehicleId?: string }) =>
    download('/api/exports/vehicle-costs', query),

  materialMovements: (query: ExportQuery & { materialId?: string; projectId?: string }) =>
    download('/api/exports/material-movements', query),
};
