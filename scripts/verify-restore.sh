#!/usr/bin/env bash
#
# Rehearses the restore, end to end, and checks the result.
#
#   ./scripts/verify-restore.sh                  # back up the live database, restore, compare
#   ./scripts/verify-restore.sh <existing-dump>  # rehearse from a dump you already hold
#
# Backs up, restores into a throwaway database, compares every table's row
# count against the source, and drops the throwaway. Exits non-zero on any
# mismatch.
#
# This exists because "we have backups" and "we can restore" are different
# claims, and only the second one matters. A dump that pg_restore can list may
# still fail to load — a missing extension, a role that does not exist, an
# ordering the schema will not accept. The only way to know is to load it, and
# the only way to keep knowing is to load it on a schedule.
#
# Run it after any change to the schema, and periodically in a real deployment.
# A restore rehearsed once a year is a restore nobody has rehearsed.

set -euo pipefail

# shellcheck source=scripts/db-common.sh
source "$(dirname "${BASH_SOURCE[0]}")/db-common.sh"

require_tool psql
require_server

here="$(dirname "${BASH_SOURCE[0]}")"
scratch="${VERIFY_SCRATCH_DB:-${PGDATABASE}_restorecheck}"

assert_safe_identifier "$scratch"

[[ "$scratch" != "$PGDATABASE" ]] \
    || die "The scratch database must not be the source database."

# Where the archive is unpacked for the check, and thrown away afterwards.
files_scratch="$(mktemp -d)"

drop_scratch_db() {
    psql -q -c "DROP DATABASE IF EXISTS \"${scratch}\" WITH (FORCE)" postgres 2>/dev/null || true
}

cleanup() {
    # Always, including on failure. A rehearsal that leaves debris behind
    # stops being run.
    drop_scratch_db
    rm -rf -- "$files_scratch"
}
trap cleanup EXIT

dump="${1:-}"

if [[ -z "$dump" ]]; then
    log "Taking a fresh backup to rehearse from"
    dump="$("${here}/backup.sh")"
else
    [[ -f "$dump" ]] || die "No such dump: ${dump}"
    log "Rehearsing from ${dump}"
fi

archive="$(files_archive_for "$dump")"

drop_scratch_db

if [[ -f "$archive" ]]; then
    "${here}/restore.sh" "$dump" "$scratch" --files "$files_scratch"
else
    "${here}/restore.sh" "$dump" "$scratch"
fi

# ---- compare -------------------------------------------------------------
#
# Row counts per table, from both databases, sorted. Not a checksum of the
# data: the goal is to catch a restore that silently dropped a table or loaded
# it empty, which is what actually goes wrong. A byte-level comparison would
# also flag the harmless differences a dump legitimately introduces.

counts_query="
SELECT relname, n_live_tup
FROM pg_stat_user_tables
ORDER BY relname;
"

log "Comparing row counts"

# ANALYZE first: n_live_tup is an estimate maintained by the statistics
# collector, and a freshly restored database has not been analysed, so every
# table would read as zero and the comparison would be meaningless.
psql -q -c "ANALYZE" "$PGDATABASE"
psql -q -c "ANALYZE" "$scratch"

source_counts="$(psql -tA -F'|' -c "$counts_query" "$PGDATABASE")"
restored_counts="$(psql -tA -F'|' -c "$counts_query" "$scratch")"

if [[ "$source_counts" != "$restored_counts" ]]; then
    log "Row counts differ between ${PGDATABASE} and the restored copy:"
    diff <(echo "$source_counts") <(echo "$restored_counts") >&2 || true
    die "Restore verification FAILED."
fi

tables="$(echo "$source_counts" | grep -c . || true)"
rows="$(echo "$source_counts" | awk -F'|' '{ total += $2 } END { print total + 0 }')"

# A dump of an empty database restores perfectly and proves nothing, so say so
# rather than reporting a pass.
if [[ "$tables" -eq 0 ]]; then
    die "The source database has no tables — there is nothing to verify."
fi

# ---- the documents ------------------------------------------------------
#
# The check the row counts cannot make. `attachments` restores perfectly from
# the dump alone — same rows, same count, comparison passes — and every one of
# them points at a file that is not there. The only way to catch it is to take
# the storage keys out of the restored database and look for them.

attachment_count="$(psql -tAc \
    'SELECT count(*) FROM attachments WHERE "IsDeleted" = false' "$scratch")"

if [[ "$attachment_count" -eq 0 ]]; then
    log "No attachments in the backup; nothing to cross-check."
elif [[ ! -f "$archive" ]]; then
    log "The restored database records ${attachment_count} attachment(s)."
    die "No attachment archive beside this dump — the restore would give a list of documents that are not there. Set ATTACHMENT_DIR when backing up."
else
    log "Checking ${attachment_count} attachment(s) against the archive"

    missing=0
    checked=0

    while IFS= read -r key; do
        [[ -n "$key" ]] || continue

        checked=$((checked + 1))

        if [[ ! -f "${files_scratch}/${key}" ]]; then
            missing=$((missing + 1))
            [[ "$missing" -le 10 ]] && log "  missing: ${key}"
        fi
    done < <(psql -tAc 'SELECT "StorageKey" FROM attachments WHERE "IsDeleted" = false' "$scratch")

    if [[ "$missing" -gt 0 ]]; then
        die "${missing} of ${checked} attachment(s) have no file in the archive."
    fi

    log "All ${checked} attachment(s) have their file."
fi

log "Restore verification PASSED: ${tables} table(s), ${rows} row(s) matched."
