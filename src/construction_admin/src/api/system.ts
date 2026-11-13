import { request } from './client';

/** How full the server's disk is, and how much of it the uploaded documents take. Super Admin only. */
export interface StorageStatus {
  /** False when the disk could not be read; the disk numbers are then zero. */
  known: boolean;
  totalBytes: number;
  freeBytes: number;
  usedBytes: number;
  freePercent: number;
  /** 0 fine, 1 low, 2 close to out. */
  level: 0 | 1 | 2;
  documentsBytes: number;
  documentsCount: number;
}

export type StorageCleanupKind = 'OrphanFiles' | 'DeletedRecordDocuments' | 'DeletionTraces';

export interface StorageCleanupCategory {
  kind: StorageCleanupKind;
  count: number;
  bytes: number;
  examples: string[];
  keptForRetention: number;
}

export interface StorageCleanupScan {
  categories: StorageCleanupCategory[];
  orphanGraceHours: number;
}

export interface StorageCleanupResult {
  kind: StorageCleanupKind;
  removedFiles: number;
  removedRows: number;
  freedBytes: number;
  keptForRetention: number;
}

export const systemApi = {
  storage: () => request<StorageStatus>({ method: 'GET', url: '/api/v1/system/storage' }),
  cleanupScan: () => request<StorageCleanupScan>({ method: 'GET', url: '/api/v1/system/storage/cleanup' }),
  cleanupRun: (kind: StorageCleanupKind) =>
    request<StorageCleanupResult>({ method: 'POST', url: '/api/v1/system/storage/cleanup', data: { kind } }),
};
