#!/bin/sh
#
# Creates the database role the API connects as, so it does not run as the
# `postgres` superuser.
#
# Why it matters: a superuser can read every database on the server, write files
# on the host (COPY ... TO PROGRAM runs shell commands), and load extensions. An
# SQL injection or a leaked connection string in an application running as one is
# a host compromise. As an ordinary role that owns only its own database, the
# same mistake is limited to that database.
#
# What the role is, and is not:
#   * NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS.
#   * It OWNS the application database and everything in it. That is deliberate
#     and not a shortcut: the API applies EF migrations and creates and drops
#     the monthly location_records partitions itself, so it needs DDL rights on
#     its own schema. Separating a migration role from a runtime role would mean
#     moving both of those out of the API, which is a larger change than this.
#   * btree_gist (the only extension the migrations create) is a "trusted"
#     extension, so the owner of the database may create it.
#
# Idempotent: safe to run on a fresh database, and on an existing one to switch
# it over. Run as a superuser, normally the `postgres` user:
#
#   # first start of a new deployment: run automatically by
#   # deploy/postgres-init/10-app-role.sh when API_DB_USER and API_DB_PASSWORD
#   # are set in .env
#
#   # an existing deployment (data volume already initialised):
#   #   1. put API_DB_USER=construction_app and API_DB_PASSWORD=<new> in .env
#   #   2. docker compose -f docker-compose.prod.yml up -d postgres   (picks up the mount)
#   #   3. docker compose -f docker-compose.prod.yml exec postgres sh /scripts/create-app-role.sh
#   #   4. docker compose -f docker-compose.prod.yml up -d api        (reconnects as the new role)
#
# Environment (the compose file maps API_DB_* onto these):
#   APP_DB_USER      required, a new role name — never `postgres`
#   APP_DB_PASSWORD  required
#   PGDATABASE       the application database, default construction
#   PGUSER, PGHOST...  how to reach the server as a superuser (libpq defaults)

set -eu

app_user=${APP_DB_USER:?set APP_DB_USER, for example construction_app}
app_password=${APP_DB_PASSWORD:?set APP_DB_PASSWORD}
database=${PGDATABASE:-construction}

# Refusing this is the point of the script: demoting or re-owning `postgres`
# would break the server, and "the API user is postgres" is the state being left.
case "$app_user" in
    postgres | "")
        echo "create-app-role: APP_DB_USER must be a new role name, not '$app_user'." >&2
        exit 1
        ;;
esac

# The password never appears on a command line (and so never in `ps`): it goes
# in as a psql variable and is quoted by the server-side format().
#
# \gexec runs each row of a result as a statement. Used throughout because DO
# blocks do not see psql variables.
psql -X -v ON_ERROR_STOP=1 \
    -v app_user="$app_user" -v app_password="$app_password" -v database="$database" \
    -d postgres <<'SQL'
SELECT format('CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', :'app_user')
 WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'app_user')
\gexec

-- Also resets the password, so re-running with a new one rotates it.
SELECT format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', :'app_user', :'app_password')
\gexec

SELECT format('ALTER DATABASE %I OWNER TO %I', :'database', :'app_user')
\gexec

-- Nobody else needs to connect to it.
SELECT format('REVOKE ALL ON DATABASE %I FROM PUBLIC', :'database')
\gexec
SQL

# Hand over anything an earlier run as a superuser already created. A no-op on a
# fresh database. Connected to the application database, because ownership is
# per database.
#
# Sequences that belong to a column (identity columns, serial) are skipped: they
# change owner with their table, and ALTER SEQUENCE on an identity sequence is an
# error.
psql -X -v ON_ERROR_STOP=1 -v app_user="$app_user" -d "$database" <<'SQL'
SELECT format('ALTER TABLE %I.%I OWNER TO %I', n.nspname, c.relname, :'app_user')
  FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
   AND pg_get_userbyid(c.relowner) <> :'app_user'
\gexec

SELECT format('ALTER SEQUENCE %I.%I OWNER TO %I', n.nspname, c.relname, :'app_user')
  FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
 WHERE n.nspname = 'public' AND c.relkind = 'S'
   AND pg_get_userbyid(c.relowner) <> :'app_user'
   AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.objid = c.oid AND d.deptype IN ('a', 'i'))
\gexec
SQL

echo "create-app-role: '$app_user' owns '$database' and is not a superuser."
