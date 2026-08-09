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

# Where the attachment files live, when file storage is the local disk.
#
# Empty means "not backed up", and that is a loud condition rather than a
# quiet default: a database dump on its own restores a complete list of
# documents none of which exist. `backup.sh` asks the database how many
# attachments it holds and refuses to be silent about the gap.
#
# A deployment on object storage sets this to nothing and relies on the
# bucket's own versioning instead — see docs/PROVISIONING.md §4.
: "${ATTACHMENT_DIR:=}"

# Writes <file>.sha256 recording the *basename*, never the path it happens to
# have today.
#
# Found by doing the drill rather than by reading the code: `sha256sum path >
# path.sha256` records whatever path it was given, so a checksum written at
# /mnt/backups on the machine that died is checked against /mnt/backups on the
# machine that replaced it — where the file is not. The first step of a real
# recovery failed, on a file that was perfectly intact.
write_checksum() {
    local file="$1"
    ( cd "$(dirname "$file")" && sha256sum "$(basename "$file")" > "$(basename "$file").sha256" )
}

# Verifies <file>.sha256 from inside the file's own directory, which is the
# other half of the same fix.
check_checksum() {
    local file="$1"
    ( cd "$(dirname "$file")" && sha256sum --check --status "$(basename "$file").sha256" )
}

# The archive that belongs to a dump. One stamp, two files: they are a set,
# and restoring one without the other is the failure this naming is meant to
# make obvious.
files_archive_for() {
    local dump="$1"
    printf '%s' "${dump%.dump}-files.tar.gz"
}

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
