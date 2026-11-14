#!/usr/bin/env bash
# Writes what Docker is doing into a folder the API can read, so the Server status page can show the containers and the
# images without the API ever touching Docker (giving a web application the Docker socket is giving it the host).
#
# Runs on the host, from cron, every 5 minutes (see docs/SECURITY.md). Reads only: `docker ps`, `docker system df` and the
# last line of the nightly prune log. Takes a fraction of a second and writes three small files.
#
# The files land in the `backup-data` volume under host/, which the API mounts read-only as /backups.
set -euo pipefail

volume="${DOCKER_STATUS_VOLUME:-construction_backup-data}"
dir="$(docker volume inspect "$volume" -f '{{.Mountpoint}}')/host"
mkdir -p "$dir"

put() { # put <name>: stdin becomes the file, replaced in one step so the API never reads half of it
  cat > "$dir/$1.new" && mv -f "$dir/$1.new" "$dir/$1"
}

docker system df --format '{{json .}}' | put df.jsonl
tail -n 1 /var/log/construction-prune.log 2>/dev/null | put prune.txt || true
# The certificate the proxy is serving right now (end date and issuer), from the proxy itself rather than from its files.
domain="$(grep -E '^DOMAIN=' "${DEPLOY_ENV:-/opt/construction/deploy/.env}" 2>/dev/null | cut -d= -f2- || true)"
if [ -n "$domain" ]; then
  tls="$(echo | timeout 10 openssl s_client -connect 127.0.0.1:443 -servername "$domain" 2>/dev/null \
    | openssl x509 -noout -enddate -issuer 2>/dev/null || true)"
  [ -n "$tls" ] && printf '%s\n' "$tls" | put tls.txt
fi
# Last, because its modified time is the "updated at" the page shows.
docker ps -a --format '{{json .}}' | put containers.jsonl
