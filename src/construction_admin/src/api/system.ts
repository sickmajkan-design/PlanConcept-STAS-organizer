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

export const systemApi = {
  storage: () => request<StorageStatus>({ method: 'GET', url: '/api/v1/system/storage' }),
};
