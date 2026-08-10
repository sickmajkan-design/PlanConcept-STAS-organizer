#!/usr/bin/env bash
#
# Rehearses the two things a schema change asks of you at the worst moment.
#
# Every migration in this repository has been applied forward, continuously, by
# the integration suite. Not one had ever been rolled back. That is a normal
# place to end up and a bad one to discover: the moment you need `Down` is the
# moment a deployment has gone wrong, under time pressure, on the only copy of
# the data that matters — and a `Down` that has never run is as likely to throw
# as to work.
#
# Two rehearsals, because they fail differently:
#
#   1. Round trip.  Seed data at HEAD, roll back one migration, roll forward
#      again, and compare. This is the actual emergency — you go back one step,
#      not to nothing — and what it proves is that the step is survivable with
#      data in the tables.
#
#   2. Full descent.  From HEAD all the way down to an empty database and back
#      up. This proves every `Down` in the history runs at all. It says nothing
#      about data, because rolling back far enough legitimately destroys some:
#      a migration that dropped a column cannot invent it again.
#
# Usage:
#   scripts/rehearse-migrations.sh                    against localhost:5432
#   PGHOST=… PGUSER=… scripts/rehearse-migrations.sh  against another server
#
# Uses a scratch database of its own and drops it on exit, including on
# failure. It never touches an existing one.

set -euo pipefail

cd "$(dirname "$0")/.."

RED=$'\033[0;31m'
GREEN=$'\033[0;32m'
RESET=$'\033[0m'

PGHOST=${PGHOST:-127.0.0.1}
PGPORT=${PGPORT:-5432}
PGUSER=${PGUSER:-postgres}
PGPASSWORD=${PGPASSWORD:-postgres}
export PGHOST PGPORT PGUSER PGPASSWORD

DATABASE=${DATABASE:-construction_rehearsal_$$}
CONNECTION="Host=${PGHOST};Port=${PGPORT};Database=${DATABASE};Username=${PGUSER};Password=${PGPASSWORD}"

export DOTNET_ROLL_FORWARD=Major
export ConnectionStrings__DefaultConnection="$CONNECTION"
PATH="$PATH:$HOME/.dotnet/tools"

failures=0

note() { printf '\n--- %s\n' "$1"; }

check() {
    local what=$1
    shift

    if "$@"; then
        printf '%s  ok%s  %s\n' "$GREEN" "$RESET" "$what"
    else
        printf '%sFAIL%s  %s\n' "$RED" "$RESET" "$what"
        failures=$((failures + 1))
    fi
}

cleanup() {
    local status=$?
    psql -q -d postgres -c "DROP DATABASE IF EXISTS \"$DATABASE\" WITH (FORCE)" >/dev/null 2>&1 || true
    exit "$status"
}

trap cleanup EXIT

# `--configuration Release` as well as `--no-build`: without it the tool looks
# for a Debug build that a release pipeline never produced, and reports a
# missing assembly rather than a missing flag.
ef() {
    dotnet ef "$@" \
        --project src/Construction.Infrastructure \
        --no-build --configuration Release
}

# The tool is a global one and a fresh runner has no global tools. Installed
# here rather than in the workflow so the script is the whole procedure — the
# point of a rehearsal is that somebody can run it on a laptop during an
# incident without first reading a YAML file.
ensure_ef_tool() {
    if dotnet ef --version >/dev/null 2>&1; then
        return
    fi

    echo "--- installing dotnet-ef"
    dotnet tool install --global dotnet-ef >/dev/null 2>&1 || true
    PATH="$PATH:$HOME/.dotnet/tools"
    export PATH

    dotnet ef --version >/dev/null 2>&1
}

sql() {
    psql -q -At -d "$DATABASE" -c "$1"
}

# --- the data the rehearsal is about ----------------------------------------
#
# Representative rather than exhaustive: an employee, the account behind them,
# a project, a shift, and GPS history spanning several months — the last
# because the newest migration partitions that table, and a rehearsal that
# seeded nothing into the table being changed would prove nothing about it.
seed() {
    psql -q -v ON_ERROR_STOP=1 -d "$DATABASE" >/dev/null <<'SQL'
INSERT INTO employees ("Id","EmployeeNumber","FirstName","LastName","Position",
                       "EmploymentDate","Status","IsDeleted","CreatedAt")
VALUES ('a0000000-0000-4000-8000-000000000001','R-001','Rehearsal','Employee',
        'Zidar','2024-01-01',0,false, now());

INSERT INTO projects ("Id","Name","Status","IsDeleted","CreatedAt")
VALUES ('b0000000-0000-4000-8000-000000000001','Rehearsal project',0,false, now());

INSERT INTO location_records ("EmployeeId","Latitude","Longitude","Accuracy",
                              "Timestamp","ReceivedAt")
SELECT 'a0000000-0000-4000-8000-000000000001',
       44.0 + random(), 18.0 + random(), 5.0, ts, ts
FROM generate_series(now() - interval '5 months', now(), interval '6 hours') AS ts;
SQL
}

# A fingerprint that survives a round trip. Sums as well as counts, so a
# migration that kept the right number of rows while mangling their contents
# is still caught.
fingerprint() {
    sql "
        SELECT (SELECT count(*) FROM employees)
            || '|' || (SELECT count(*) FROM projects)
            || '|' || (SELECT count(*) FROM location_records)
            || '|' || (SELECT coalesce(sum(\"Id\"), 0) FROM location_records)
            || '|' || (SELECT coalesce(round(sum(\"Latitude\" + \"Longitude\")::numeric, 6), 0)
                       FROM location_records)"
}

ensure_ef_tool

echo "--- building"
dotnet build src/Construction.Infrastructure --configuration Release --nologo >/dev/null

echo "--- scratch database ${DATABASE}"
psql -q -d postgres -c "CREATE DATABASE \"$DATABASE\"" >/dev/null

# ---------------------------------------------------------------------------
note 'rehearsal 1: one step back, with data in the tables'

ef database update >/dev/null
seed

before=$(fingerprint)
echo "    fingerprint before: $before"

# The migration before HEAD. Rolling "back one" means updating *to* it.
readarray -t migrations < <(ef migrations list --no-connect 2>/dev/null | grep -E '^[0-9]{14}_')

head_migration=${migrations[-1]}
previous_migration=${migrations[-2]}

echo "    HEAD is ${head_migration}"
echo "    rolling back to ${previous_migration}"

check "rolling back ${head_migration} runs" ef database update "$previous_migration"
check "rolling forward again runs" ef database update

after=$(fingerprint)
echo "    fingerprint after:  $after"

check 'the data came back unchanged' test "$before" = "$after"

# ---------------------------------------------------------------------------
note 'rehearsal 2: every Down in the history runs'

# No data assertions here on purpose. Going back far enough destroys some
# legitimately — DropEmployeePhotoUrl cannot invent the column's contents
# again — so what is under test is only whether each Down executes.
check 'the whole history rolls back to an empty database' ef database update 0
check 'and rolls forward again to HEAD' ef database update

check 'HEAD is applied at the end' bash -c '
    psql -q -At -d "$1" -c "SELECT count(*) FROM \"__EFMigrationsHistory\"" | grep -qv "^0$"' _ "$DATABASE"

echo
if (( failures )); then
    printf '%s%d rehearsal step(s) failed.%s\n' "$RED" "$failures" "$RESET"
    printf 'A rollback that does not work is one you will find out about during an incident.\n'
    exit 1
fi

printf '%sBoth rehearsals passed: the schema can go back, and forward again.%s\n' "$GREEN" "$RESET"
