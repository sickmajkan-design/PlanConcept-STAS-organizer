#!/bin/sh
#
# Run once by the postgres image, on the first start of an empty data volume.
#
# Creates the role the API connects as when API_DB_USER and API_DB_PASSWORD are
# set, and does nothing when they are not — so a deployment that has not opted
# in keeps running the API as `postgres`, exactly as before. The reasoning, and
# what to do for a database that already exists, is in
# scripts/create-app-role.sh.

# A subshell, because the image *sources* an init script that is not marked
# executable, and a bind mount from Windows never is. `set -u` in the entrypoint's
# own shell would then break whatever it runs after this.
(
    set -eu

    if [ -n "${APP_DB_USER:-}" ] && [ -n "${APP_DB_PASSWORD:-}" ]; then
        PGUSER=postgres PGDATABASE="${POSTGRES_DB:-construction}" \
            sh /scripts/create-app-role.sh
    else
        echo "10-app-role: API_DB_USER / API_DB_PASSWORD not both set; the API will connect as postgres."
    fi
)
