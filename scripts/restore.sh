#!/usr/bin/env bash
#
# Restores a dump into a database.
#
#   ./scripts/restore.sh backups/construction-20260808T090000Z.dump construction_scratch
#   ./scripts/restore.sh <dump> construction --force
#
# The target database is created if it does not exist. If it does exist and
# holds any table, the restore stops unless --force is given.
#
# That guard is the whole reason this is a script rather than a line in a
# runbook. The realistic way to lose data during a restore is not a corrupt
# dump — it is restoring last night's copy over the database that was fine,
# while trying to fix a different one. Making the destructive case require an
# extra word means it cannot happen by pasting a command.

set -euo pipefail

# shellcheck source=scripts/db-common.sh
source "$(dirname "${BASH_SOURCE[0]}")/db-common.sh"

require_tool pg_restore
require_tool psql
require_tool sha256sum
require_server

dump="${1:-}"
target="${2:-}"
force="${3:-}"

[[ -n "$dump" && -n "$target" ]] \
    || die "Usage: $0 <dump-file> <target-database> [--force]"

[[ -f "$dump" ]] || die "No such dump: ${dump}"

assert_safe_identifier "$target"

# Checked before anything is touched. A dump that does not match its checksum
# is a dump that was truncated in transit, and finding that out halfway
# through a restore leaves a half-populated database that looks plausible.
if [[ -f "${dump}.sha256" ]]; then
    log "Verifying checksum"
    sha256sum --check --status "${dump}.sha256" \
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
