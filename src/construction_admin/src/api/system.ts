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

export interface HostStatus {
  known: boolean;
  os: string;
  uptimeSeconds: number | null;
  cores: number;
  load1: number | null;
  load5: number | null;
  load15: number | null;
  memoryTotalBytes: number | null;
  memoryAvailableBytes: number | null;
  swapTotalBytes: number | null;
  swapFreeBytes: number | null;
}

export interface ApplicationStatus {
  startedAtUtc: string;
  uptimeSeconds: number;
  version: string;
  environment: string;
  runtime: string;
  processMemoryBytes: number;
  managedHeapBytes: number;
  threads: number;
}

export interface TableSize {
  name: string;
  bytes: number;
}

export interface DatabaseStatus {
  reachable: boolean;
  version: string | null;
  sizeBytes: number;
  connections: number;
  activeConnections: number;
  maxConnections: number;
  cacheHitPercent: number | null;
  largestTables: TableSize[];
}

export interface ServicesStatus {
  virusScannerEnabled: boolean;
  virusScannerReachable: boolean;
  fileStorage: 'disk' | 'objectStorage';
  outboxPending: number;
  outboxAbandoned: number;
  oldestPendingUtc: string | null;
}

export interface BackupItem {
  name: string;
  takenAtUtc: string;
  /** Dump plus documents archive. */
  sizeBytes: number;
  hasDocuments: boolean;
  /** A verified copy exists off the server. */
  offsite: boolean;
}

export interface BackupStatus {
  /** False when the backup folder is not visible to the API. */
  known: boolean;
  /** Newest first. */
  items: BackupItem[];
  restoreCheckedAtUtc: string | null;
}

/** The whole picture of the server, in one request, for the Super Admin's status page. */
export interface ServerStatus {
  atUtc: string;
  backups: BackupStatus;
  host: HostStatus;
  application: ApplicationStatus;
  database: DatabaseStatus;
  services: ServicesStatus;
  storage: StorageStatus;
}

export const systemApi = {
  storage: () => request<StorageStatus>({ method: 'GET', url: '/api/v1/system/storage' }),
  status: () => request<ServerStatus>({ method: 'GET', url: '/api/v1/system/status' }),
  cleanupScan: () => request<StorageCleanupScan>({ method: 'GET', url: '/api/v1/system/storage/cleanup' }),
  cleanupRun: (kind: StorageCleanupKind) =>
    request<StorageCleanupResult>({ method: 'POST', url: '/api/v1/system/storage/cleanup', data: { kind } }),
};
