#!/usr/bin/env bash
#
# Shared connection handling for the backup scripts.
#
# Sourced, never executed. The three scripts here all need the same answer to
# "which database, as whom", and getting that from one place means a fix to it
# is a fix everywhere rather than in two of three.

set -euo pipefail

# Connection settings, in the same shape libpq already understands, so
# PGPASSWORD / PGSERVICE / ~/.pgpass keep working and nobody has to learn a
# second convention.
: "${PGHOST:=localhost}"
: "${PGPORT:=5432}"
: "${PGUSER:=postgres}"
: "${PGDATABASE:=construction}"

export PGHOST PGPORT PGUSER PGDATABASE

# Where dumps go. A bind mount or a volume in a real deployment; the point is
# that it is not the same disk as the database, which is the failure this
# whole exercise is about.
: "${BACKUP_DIR:=./backups}"

# How long a dump is kept before the sweep removes it.
: "${BACKUP_RETENTION_DAYS:=14}"

log() {
    # To stderr, so a script whose stdout is a file path stays pipeable.
    printf '%s  %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*" >&2
}

die() {
    log "ERROR: $*"
    exit 1
}

require_tool() {
    command -v "$1" >/dev/null 2>&1 || die "$1 is not on PATH."
}

require_server() {
    require_tool pg_isready
    pg_isready -q || die "No PostgreSQL server answering at ${PGHOST}:${PGPORT}."
}

# Refuses a database name that is not a plain identifier.
#
# These names reach psql inside a CREATE DATABASE, which cannot be
# parameterised. Everything else in this system goes through EF Core with
# bound parameters; this is the one place a name is interpolated into SQL, so
# it is checked rather than trusted.
assert_safe_identifier() {
    local name="$1"
    [[ "$name" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] \
        || die "'$name' is not a valid database name (letters, digits and underscore only)."
}
