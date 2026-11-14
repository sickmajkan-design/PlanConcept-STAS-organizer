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

export interface Outage {
  startedAtUtc: string;
  endedAtUtc: string;
  seconds: number;
}

/** How long the application has been up, by the heartbeats it writes itself. */
export interface UptimeStatus {
  known: boolean;
  sinceUtc: string | null;
  windowDays: number;
  /** 0 to 100. */
  availabilityPercent: number;
  downSeconds: number;
  /** Newest first. */
  outages: Outage[];
}

export interface SecurityCheck {
  key: 'sshPassword' | 'sshRoot' | 'fail2ban' | 'firewall' | 'updates' | 'autoUpdates' | 'reboot' | 'ports';
  status: 'ok' | 'warn' | 'info';
  value: string;
  extra: string;
}

export interface Container {
  name: string;
  image: string;
  /** Docker's word: running, exited, restarting, paused, created, dead. */
  state: string;
  status: string;
}

/** What a script on the host wrote about Docker; the API never talks to Docker itself. */
export interface DockerStatus {
  known: boolean;
  updatedAtUtc: string | null;
  containers: Container[];
  images: { total: number; active: number; size: string; reclaimable: string } | null;
  lastPrune: string | null;
  /** The HTTPS certificate the proxy serves; null until the host script has read it. */
  certificate: { notAfterUtc: string; issuer: string } | null;
  /** Newest first, one per commit, at most five; the first is running. */
  releases: { seenAtUtc: string; commit: string; migrations: number }[];
  /** Basic hardening checks of the host; empty until the host script has written them. */
  security: SecurityCheck[];
}

/** The whole picture of the server, in one request, for the Super Admin's status page. */
export interface ServerStatus {
  atUtc: string;
  backups: BackupStatus;
  uptime: UptimeStatus;
  docker: DockerStatus;
  host: HostStatus;
  application: ApplicationStatus;
  database: DatabaseStatus;
  services: ServicesStatus;
  storage: StorageStatus;
}

/** One hour of the load charts. Percentages; load is per core and can pass 100. */
export interface MetricPoint {
  atUtc: string;
  loadPercent: number;
  memoryPercent: number;
  diskPercent: number;
}

export interface ServerMetrics {
  known: boolean;
  /** Oldest first. */
  points: MetricPoint[];
  diskGrowthBytesPerDay: number | null;
  diskDaysLeft: number | null;
}

/** One kind of unexpected server error over the last seven days. Counts only, never a message. */
export interface ServerErrorGroup {
  exceptionType: string;
  location: string;
  route: string;
  today: number;
  total: number;
  lastSeenAtUtc: string;
  firstVersion: string;
}

export type ServerAlertKey =
  | 'disk'
  | 'outage'
  | 'backupMissing'
  | 'backupOld'
  | 'scannerDown'
  | 'outboxAbandoned'
  | 'certificate'
  | 'restoreNotChecked';

/** Where one kind of server trouble is announced. Push is the in-app notice and the phone push together. */
export interface ServerAlertSetting {
  key: ServerAlertKey;
  emailEnabled: boolean;
  pushEnabled: boolean;
  lastSentAtUtc: string | null;
}

export const systemApi = {
  metrics: () => request<ServerMetrics>({ method: 'GET', url: '/api/v1/system/metrics' }),
  errors: () => request<ServerErrorGroup[]>({ method: 'GET', url: '/api/v1/system/errors' }),
  alerts: () => request<ServerAlertSetting[]>({ method: 'GET', url: '/api/v1/system/alerts' }),
  setAlert: (key: ServerAlertKey, channels: { emailEnabled: boolean; pushEnabled: boolean }) =>
    request<void>({ method: 'PUT', url: `/api/v1/system/alerts/${key}`, data: channels }),
  testAlert: () => request<void>({ method: 'POST', url: '/api/v1/system/alerts/test' }),
  storage: () => request<StorageStatus>({ method: 'GET', url: '/api/v1/system/storage' }),
  status: () => request<ServerStatus>({ method: 'GET', url: '/api/v1/system/status' }),
  cleanupScan: () => request<StorageCleanupScan>({ method: 'GET', url: '/api/v1/system/storage/cleanup' }),
  cleanupRun: (kind: StorageCleanupKind) =>
    request<StorageCleanupResult>({ method: 'POST', url: '/api/v1/system/storage/cleanup', data: { kind } }),
};
