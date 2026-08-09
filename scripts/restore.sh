#!/usr/bin/env bash
#
# Restores a dump into a database.
#
#   ./scripts/restore.sh backups/construction-20260808T090000Z.dump construction_scratch
#   ./scripts/restore.sh <dump> construction --force
#   ./scripts/restore.sh <dump> construction --force --files /var/lib/attachments
#
# The target database is created if it does not exist. If it does exist and
# holds any table, the restore stops unless --force is given.
#
# That guard is the whole reason this is a script rather than a line in a
# runbook. The realistic way to lose data during a restore is not a corrupt
# dump — it is restoring last night's copy over the database that was fine,
# while trying to fix a different one. Making the destructive case require an
# extra word means it cannot happen by pasting a command.
#
# --files unpacks the attachment archive that was taken with this dump into
# the given directory. The two halves belong together: the database holds the
# metadata, the disk holds the bytes, and a restore of one without the other
# gives a list of documents that are not there.

set -euo pipefail

# shellcheck source=scripts/db-common.sh
source "$(dirname "${BASH_SOURCE[0]}")/db-common.sh"

require_tool pg_restore
require_tool psql
require_tool sha256sum
require_server

dump="${1:-}"
target="${2:-}"
shift 2 2>/dev/null || true

force=""
files_target=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --force)
            force="--force"
            shift
            ;;
        --files)
            files_target="${2:-}"
            [[ -n "$files_target" ]] || die "--files needs a directory."
            shift 2
            ;;
        *)
            die "Unknown argument: $1"
            ;;
    esac
done

[[ -n "$dump" && -n "$target" ]] \
    || die "Usage: $0 <dump-file> <target-database> [--force] [--files <dir>]"

[[ -f "$dump" ]] || die "No such dump: ${dump}"

assert_safe_identifier "$target"

# Checked before anything is touched. A dump that does not match its checksum
# is a dump that was truncated in transit, and finding that out halfway
# through a restore leaves a half-populated database that looks plausible.
if [[ -f "${dump}.sha256" ]]; then
    log "Verifying checksum"
    check_checksum "$dump" \
        || die "Checksum mismatch for ${dump} — do not restore this file."
else
    log "WARNING: no ${dump}.sha256 alongside the dump; integrity not verified."
fi

exists="$(psql -tAc \
    "SELECT 1 FROM pg_database WHERE datname = '${target}'" postgres)"

if [[ "$exists" == "1" ]]; then
    tables="$(psql -tAc \
        "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" \
        "$target")"

    if [[ "$tables" -gt 0 && "$force" != "--force" ]]; then
        die "Database '${target}' already has ${tables} table(s). Re-run with --force to overwrite it."
    fi

    [[ "$tables" -gt 0 ]] && log "Overwriting ${tables} existing table(s) in '${target}' (--force given)"
else
    log "Creating database '${target}'"
    psql -q -c "CREATE DATABASE \"${target}\"" postgres
fi

log "Restoring ${dump} into ${target}"

# --clean --if-exists so a re-restore over an existing database replaces rather
# than collides. --single-transaction so a failure leaves the target as it was
# instead of half restored; --exit-on-error to make that actually happen,
# because pg_restore's default is to keep going and report at the end.
pg_restore \
    --dbname="$target" \
    --clean \
    --if-exists \
    --no-owner \
    --no-privileges \
    --single-transaction \
    --exit-on-error \
    "$dump"

log "Restored into '${target}'"

# ---- the file half -------------------------------------------------------

archive="$(files_archive_for "$dump")"

if [[ -n "$files_target" ]]; then
    require_tool tar

    [[ -f "$archive" ]] \
        || die "No attachment archive beside the dump: ${archive}"

    if [[ -f "${archive}.sha256" ]]; then
        log "Verifying the archive checksum"
        check_checksum "$archive" \
            || die "Checksum mismatch for ${archive} — do not unpack this file."
    else
        log "WARNING: no ${archive}.sha256; archive integrity not verified."
    fi

    mkdir -p "$files_target"

    log "Unpacking ${archive} into ${files_target}"
    tar --extract --gzip --file="$archive" --directory="$files_target"

    restored_files="$(find "$files_target" -type f | wc -l)"
    log "Unpacked ${restored_files} file(s)"
elif [[ -f "$archive" ]]; then
    # The archive was taken and is sitting right there unused. Saying so costs
    # one line and prevents the recovery that is declared finished while every
    # document 404s.
    log "NOTE: ${archive} exists but --files was not given; attachments were not restored."
fi
