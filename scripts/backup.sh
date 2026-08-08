#!/usr/bin/env bash
#
# Takes one backup of the application database.
#
# Writes a custom-format dump plus a SHA-256 checksum, then prunes dumps older
# than BACKUP_RETENTION_DAYS. Prints the dump path on stdout so a caller can
# pipe it straight into an upload.
#
# Custom format (-Fc) rather than plain SQL on purpose: it is compressed, it
# can be restored selectively, and pg_restore can reorder to satisfy foreign
# keys. A plain-SQL dump of this schema restores only if the statements happen
# to be in dependency order.
#
#   ./scripts/backup.sh
#   BACKUP_DIR=/mnt/backups PGDATABASE=construction ./scripts/backup.sh
#
# Exits non-zero on any failure, including a dump that pg_restore cannot read
# back — a backup nobody has opened is a file, not a backup.

set -euo pipefail

# shellcheck source=scripts/db-common.sh
source "$(dirname "${BASH_SOURCE[0]}")/db-common.sh"

require_tool pg_dump
require_tool pg_restore
require_tool sha256sum
require_server

mkdir -p "$BACKUP_DIR"

stamp="$(date -u +%Y%m%dT%H%M%SZ)"
dump="${BACKUP_DIR}/${PGDATABASE}-${stamp}.dump"

log "Dumping ${PGDATABASE} from ${PGHOST}:${PGPORT} to ${dump}"

# --no-owner and --no-privileges: the restore target is very often a different
# cluster with different role names, and a dump that insists on roles nobody
# created fails at the moment it is needed most.
pg_dump \
    --format=custom \
    --no-owner \
    --no-privileges \
    --file="$dump" \
    "$PGDATABASE"

# Reading the table of contents back proves the file is a dump pg_restore
# understands, not a truncated write or an empty file from a full disk. It
# does not prove the data restores — verify-restore.sh does that — but it is
# cheap enough to run on every backup, and it catches the common failure.
pg_restore --list "$dump" >/dev/null \
    || die "The dump was written but pg_restore cannot read it: ${dump}"

sha256sum "$dump" > "${dump}.sha256"

size="$(du -h "$dump" | cut -f1)"
log "Wrote ${dump} (${size}) and its checksum"

# Prune after a successful write, never before: a failed backup must not also
# be the thing that removes the last good one.
if [[ "$BACKUP_RETENTION_DAYS" -gt 0 ]]; then
    pruned=0

    while IFS= read -r -d '' old; do
        rm -f -- "$old" "${old}.sha256"
        pruned=$((pruned + 1))
    done < <(find "$BACKUP_DIR" -maxdepth 1 -name "${PGDATABASE}-*.dump" \
        -type f -mtime "+${BACKUP_RETENTION_DAYS}" -print0)

    [[ "$pruned" -gt 0 ]] && log "Pruned ${pruned} dump(s) older than ${BACKUP_RETENTION_DAYS} days"
fi

echo "$dump"
