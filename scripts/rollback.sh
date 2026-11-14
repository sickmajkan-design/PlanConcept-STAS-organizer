#!/usr/bin/env bash
# Puts the application back on an earlier release, or forward to the newest one. Run on the server.
#
#   /opt/construction/scripts/rollback.sh <full commit hash>   # a release listed on the Server status page
#   /opt/construction/scripts/rollback.sh latest                # the newest release again
#
# What it does, in this order, and stops at the first thing that fails:
#   1. takes a fresh backup (database and documents), so a bad rollback can be undone;
#   2. pulls the `api` and `admin` images of that release from the registry (they are tagged `sha-<commit>`);
#   3. restarts only `api` and `admin` on them and waits until /health/ready answers.
# Nothing is changed before step 3, so a failed backup or pull leaves the running release alone.
#
# What it does NOT do: undo the database. The database keeps every change the newer release made. That is safe when those
# changes only added tables or columns (the status page says how many there are); it is not when one removed or renamed
# something the older release needs. A normal deploy (pull `latest`, `up -d`) puts the newest release back by itself.
set -euo pipefail

target="${1:-}"
if [ "$target" != latest ] && ! [[ "$target" =~ ^[0-9a-f]{40}$ ]]; then
  echo "Usage: $0 <40-character commit hash | latest>" >&2
  exit 1
fi
tag=latest
[ "$target" = latest ] || tag="sha-$target"

cd "${DEPLOY_ROOT:-/opt/construction}"
env_file=deploy/.env
compose=(docker compose -f deploy/docker-compose.prod.yml --env-file "$env_file")

base() { grep -E "^$1=" "$env_file" | cut -d= -f2- | sed 's/:[^:/]*$//'; } # the image name without its tag
api_image="$(base API_IMAGE):$tag"
admin_image="$(base ADMIN_IMAGE):$tag"
domain="$(grep -E '^DOMAIN=' "$env_file" | cut -d= -f2-)"

echo "1/3 Backup first..."
"${compose[@]}" exec -T backup bash /scripts/backup.sh > /dev/null

echo "2/3 Pulling $tag ..."
API_IMAGE="$api_image" ADMIN_IMAGE="$admin_image" "${compose[@]}" pull api admin > /dev/null

echo "3/3 Restarting api and admin on $tag ..."
API_IMAGE="$api_image" ADMIN_IMAGE="$admin_image" "${compose[@]}" up -d api admin > /dev/null

for _ in $(seq 1 45); do
  if curl -fsk --resolve "$domain:443:127.0.0.1" "https://$domain/health/ready" > /dev/null 2>&1; then
    echo "OK: the application answers on $tag."
    "$(dirname "${BASH_SOURCE[0]}")/docker-status.sh" || true # records the release right away
    exit 0
  fi
  sleep 2
done

echo "The application did not answer within 90 seconds. Look at: docker logs --tail 100 construction-api-1" >&2
exit 1
