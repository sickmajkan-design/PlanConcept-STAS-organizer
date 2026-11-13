#!/usr/bin/env bash
# Frees the disk of what Docker no longer needs, after a deploy or on a schedule.
#
# Every deploy pulls a new `api` and `admin` image and leaves the previous one behind with no tag (388 of them, 16.8 GB, on
# the test server by 2026-11-13). Those are the only thing this removes, together with build cache older than a week.
# It never touches a running container's image, a tagged image, or a volume, so the documents, the database and the
# backups are out of its reach.
#
# Usage:  scripts/prune-docker.sh            (once)
#         /etc/cron.d/construction-prune     (every night, see docs/SECURITY.md)
set -euo pipefail

free_bytes() { df --output=avail -B1 / | tail -1 | tr -d ' '; }

before=$(free_bytes)

docker image prune -f > /dev/null
docker builder prune -f --filter "until=168h" > /dev/null 2>&1 || true

after=$(free_bytes)

printf '%s freed %s MB, %s MB free\n' \
  "$(date -u +%Y-%m-%dT%H:%MZ)" \
  "$(( (after - before) / 1024 / 1024 ))" \
  "$(( after / 1024 / 1024 ))"
