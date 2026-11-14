import type { ServerStatus } from '../../api/system';

export type AlertTone = 'warning' | 'error';

export interface ServerAlert {
  /** Also the i18n key suffix: `serverStatus.alert.<key>`. */
  key:
    | 'databaseDown'
    | 'scannerDown'
    | 'diskLow'
    | 'diskCritical'
    | 'memoryLow'
    | 'memoryCritical'
    | 'loadHigh'
    | 'loadCritical'
    | 'connectionsHigh'
    | 'connectionsCritical'
    | 'outboxAbandoned'
    | 'outboxStuck'
    | 'backupMissing'
    | 'backupOld'
    | 'backupNotOffsite'
    | 'restoreNotChecked'
    | 'containerDown'
    | 'dockerStale'
    | 'certificateExpiring'
    | 'securityChecks';
  tone: AlertTone;
}

const MINUTE = 60_000;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;

/**
 * What on the server needs a person, from the one reading the page shows. Kept as one function with no screen in it,
 * so the thresholds are visible in one place and can be tested: the page only colours what this says.
 */
export function serverAlerts(status: ServerStatus, now: number = Date.now()): ServerAlert[] {
  const alerts: ServerAlert[] = [];

  if (!status.database.reachable) alerts.push({ key: 'databaseDown', tone: 'error' });
  if (status.services.virusScannerEnabled && !status.services.virusScannerReachable) {
    alerts.push({ key: 'scannerDown', tone: 'error' });
  }

  if (status.storage.known && status.storage.level === 2) alerts.push({ key: 'diskCritical', tone: 'error' });
  else if (status.storage.known && status.storage.level === 1) alerts.push({ key: 'diskLow', tone: 'warning' });

  const { memoryTotalBytes: total, memoryAvailableBytes: available } = status.host;
  if (total && available !== null && available !== undefined) {
    const free = available / total;
    if (free < 0.07) alerts.push({ key: 'memoryCritical', tone: 'error' });
    else if (free < 0.15) alerts.push({ key: 'memoryLow', tone: 'warning' });
  }

  const { load5, cores } = status.host;
  if (load5 !== null && load5 !== undefined && cores > 0) {
    if (load5 > cores * 2) alerts.push({ key: 'loadCritical', tone: 'error' });
    else if (load5 > cores) alerts.push({ key: 'loadHigh', tone: 'warning' });
  }

  if (status.database.reachable && status.database.maxConnections > 0) {
    const used = status.database.connections / status.database.maxConnections;
    if (used > 0.9) alerts.push({ key: 'connectionsCritical', tone: 'error' });
    else if (used > 0.7) alerts.push({ key: 'connectionsHigh', tone: 'warning' });
  }

  if (status.services.outboxAbandoned > 0) alerts.push({ key: 'outboxAbandoned', tone: 'error' });

  const oldest = status.services.oldestPendingUtc ? Date.parse(status.services.oldestPendingUtc) : null;
  if (oldest !== null && now - oldest > 15 * MINUTE) alerts.push({ key: 'outboxStuck', tone: 'warning' });

  // Backups are nightly, so 26 h is "last night's did not happen". Nothing is said when the folder is not visible.
  const { known, items, restoreCheckedAtUtc } = status.backups;
  if (known) {
    const newest = items[0];
    if (!newest) alerts.push({ key: 'backupMissing', tone: 'error' });
    else {
      if (now - Date.parse(newest.takenAtUtc) > 26 * HOUR) alerts.push({ key: 'backupOld', tone: 'error' });
      if (!newest.offsite) alerts.push({ key: 'backupNotOffsite', tone: 'warning' });
    }
    // Rehearsed weekly; 10 days leaves room for one missed Sunday.
    if (!restoreCheckedAtUtc || now - Date.parse(restoreCheckedAtUtc) > 10 * DAY) {
      alerts.push({ key: 'restoreNotChecked', tone: 'warning' });
    }
  }

  // A container Docker says is not running, or a report the host has stopped refreshing (the cron job is gone).
  if (status.docker.known) {
    if (status.docker.containers.some((c) => c.state !== 'running')) alerts.push({ key: 'containerDown', tone: 'error' });
    const updated = status.docker.updatedAtUtc ? Date.parse(status.docker.updatedAtUtc) : null;
    if (updated !== null && now - updated > 20 * MINUTE) alerts.push({ key: 'dockerStale', tone: 'warning' });
  }

  // Under two weeks is "renew it" (Caddy normally does, a month ahead); under one week, or past, it is urgent.
  if (status.docker.certificate) {
    const left = Date.parse(status.docker.certificate.notAfterUtc) - now;
    if (left < 7 * DAY) alerts.push({ key: 'certificateExpiring', tone: 'error' });
    else if (left < 14 * DAY) alerts.push({ key: 'certificateExpiring', tone: 'warning' });
  }

  // Hardening gaps are never urgent on their own, so they only ever warn.
  if (status.docker.security.some((check) => check.status === 'warn')) alerts.push({ key: 'securityChecks', tone: 'warning' });

  return alerts;
}

/** The worst tone among the alerts, or null when all is well. */
export function worstTone(alerts: ServerAlert[]): AlertTone | null {
  if (alerts.some((alert) => alert.tone === 'error')) return 'error';
  return alerts.length > 0 ? 'warning' : null;
}
