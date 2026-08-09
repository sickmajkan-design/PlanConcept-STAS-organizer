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
require_tool psql
require_tool sha256sum
require_tool tar
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

write_checksum "$dump"

size="$(du -h "$dump" | cut -f1)"
log "Wrote ${dump} (${size}) and its checksum"

# ---- the other half of the backup ----------------------------------------
#
# Attachments are rows in the database and bytes on a disk, and only one of
# those is in the dump. A restore of the dump alone produces a complete,
# correct, browsable list of documents, every one of which 404s — which is
# worse than an obvious failure, because it looks like it worked.

archive="$(files_archive_for "$dump")"

if [[ -n "$ATTACHMENT_DIR" ]]; then
    [[ -d "$ATTACHMENT_DIR" ]] \
        || die "ATTACHMENT_DIR is set to '${ATTACHMENT_DIR}', which is not a directory."

    log "Archiving attachment files from ${ATTACHMENT_DIR}"

    # Relative to the storage root, so the archive can be unpacked into a
    # different root on a different host — which is what a real recovery is.
    tar --create --gzip --file="$archive" --directory="$ATTACHMENT_DIR" .

    write_checksum "$archive"

    files="$(tar --list --file="$archive" | grep -cv '/$' || true)"
    archive_size="$(du -h "$archive" | cut -f1)"

    log "Wrote ${archive} (${archive_size}, ${files} file(s)) and its checksum"
else
    # Ask the database rather than assume. A deployment on object storage has
    # attachments and correctly has no ATTACHMENT_DIR; a deployment on local
    # disk that forgot to set it has the same configuration and a hole in its
    # backup. The only thing that tells them apart is whether anybody meant it,
    # so this states the fact and leaves the judgement to a person.
    attachments="$(psql -tAc \
        'SELECT count(*) FROM attachments WHERE "IsDeleted" = false' \
        "$PGDATABASE" 2>/dev/null || echo "?")"

    if [[ "$attachments" != "0" && "$attachments" != "?" ]]; then
        log "WARNING: ${attachments} attachment(s) recorded, but ATTACHMENT_DIR is unset."
        log "WARNING: this dump restores a list of documents whose files are not in it."
        log "WARNING: set ATTACHMENT_DIR, or confirm the bucket has its own backup."
    fi
fi

# ---- off the machine -----------------------------------------------------
#
# The step that makes this a backup rather than a copy. A volume beside the
# database survives a dropped table; it does not survive the host, and the
# host is what backups are for.
#
# Only when configured, and then it is not optional: a failure here fails the
# backup, because "the dump was written" and "the dump is somewhere else" are
# different claims and only the second one survives a fire.
if [[ -n "${OFFSITE_ENDPOINT:-}" || -n "${OFFSITE_COMMAND:-}" ]]; then
    artefacts=("$dump" "${dump}.sha256")

    if [[ -f "$archive" ]]; then
        artefacts+=("$archive" "${archive}.sha256")
    fi

    "$(dirname "${BASH_SOURCE[0]}")/offsite.sh" push "${artefacts[@]}"
else
    log "NOTE: no off-site copy configured; this backup exists only on this host."
fi

# Prune after a successful write, never before: a failed backup must not also
# be the thing that removes the last good one.
if [[ "$BACKUP_RETENTION_DAYS" -gt 0 ]]; then
    pruned=0
    kept=0

    while IFS= read -r -d '' old; do
        # Never delete the last copy of something that never left the host.
        #
        # This is the failure the retention window quietly creates: uploads
        # start failing on the 1st, nobody is watching, and on the 15th the
        # sweep removes the local copies of everything that was never sent.
        # The backup directory then contains a fortnight of nothing.
        if [[ -n "${OFFSITE_ENDPOINT:-}${OFFSITE_COMMAND:-}" \
            && ! -f "${old}.offsite" \
            && "${BACKUP_PRUNE_UNVERIFIED:-0}" != "1" ]]; then
            kept=$((kept + 1))
            continue
        fi

        # The archive goes with its dump. Pruning one and keeping the other
        # leaves a backup set that cannot restore and looks like it can.
        old_archive="$(files_archive_for "$old")"

        rm -f -- "$old" "${old}.sha256" "${old}.offsite" \
            "$old_archive" "${old_archive}.sha256" "${old_archive}.offsite"
        pruned=$((pruned + 1))
    done < <(find "$BACKUP_DIR" -maxdepth 1 -name "${PGDATABASE}-*.dump" \
        -type f -mtime "+${BACKUP_RETENTION_DAYS}" -print0)

    [[ "$pruned" -gt 0 ]] && log "Pruned ${pruned} dump(s) older than ${BACKUP_RETENTION_DAYS} days"

    if [[ "$kept" -gt 0 ]]; then
        log "WARNING: kept ${kept} expired dump(s) that have no confirmed off-site copy."
        log "WARNING: check why the uploads are failing; set BACKUP_PRUNE_UNVERIFIED=1 to prune anyway."
    fi
fi

echo "$dump"
