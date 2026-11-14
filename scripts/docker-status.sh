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
# Basic hardening of the host for the security card: key=value lines, read-only (nothing is changed here). The pending-update
# count runs `apt-get -s` (about 3 s of CPU), so it is refreshed once an hour and kept in updates.cache.
cache="$dir/updates.cache"
if [ ! -f "$cache" ] || [ -n "$(find "$cache" -mmin +60 2>/dev/null)" ]; then
  sim="$(apt-get -s upgrade 2>/dev/null | grep '^Inst' || true)"
  printf 'updates_total=%s\nupdates_security=%s\n' "$(printf '%s' "$sim" | grep -c . || true)" "$(printf '%s' "$sim" | grep -ci security || true)" > "$cache"
fi
{
  cat "$cache"
  sshd -T 2>/dev/null | awk '$1=="passwordauthentication"{print "ssh_password="$2} $1=="permitrootlogin"{print "ssh_root="$2}' || true
  if [ "$(systemctl is-active fail2ban 2>/dev/null || true)" = active ]; then echo fail2ban=active
  elif command -v fail2ban-client > /dev/null 2>&1; then echo fail2ban=inactive
  else echo fail2ban=missing; fi
  echo "auto_updates=$(systemctl is-enabled unattended-upgrades 2>/dev/null || echo missing)"
  if [ -f /var/run/reboot-required ]; then echo reboot_required=yes; else echo reboot_required=no; fi
  echo "ports=$(ss -tlnH 2>/dev/null | awk '{print $4}' | grep -vE '^(127\.|\[?::1\]?)' | sed 's/.*://' | sort -un | paste -sd, - || true)"
  if command -v ufw > /dev/null 2>&1; then echo "firewall=$(ufw status 2>/dev/null | head -1 | awk '{print tolower($2)}')"; else echo firewall=missing; fi
} | put security.txt
# One sample for the load charts: time | 1-minute load | cores | memory used % | disk used %. Seven days at one per five
# minutes is about 2000 lines; the file is cut to the last 2100.
metrics="$dir/metrics.log"
read -r load1 _ < /proc/loadavg
mem="$(awk '/^MemTotal:/{t=$2} /^MemAvailable:/{a=$2} END{if (t>0) printf "%d", (t-a)*100/t}' /proc/meminfo)"
disk="$(df --output=pcent / | tail -1 | tr -dc 0-9)"
printf '%s|%s|%s|%s|%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$load1" "$(nproc)" "${mem:-0}" "${disk:-0}" >> "$metrics"
tail -n 2100 "$metrics" > "$metrics.new" && mv -f "$metrics.new" "$metrics"
# Which release the API container runs, written down only when it changes (time | commit | database migrations applied),
# so the page can list the releases that were live and how many database changes each had. Kept to the last 50.
rev="$(docker inspect construction-api-1 --format '{{index .Config.Labels "org.opencontainers.image.revision"}}' 2>/dev/null || true)"
log="$dir/releases.log"
if [ -n "$rev" ] && [ "$(tail -n 1 "$log" 2>/dev/null | cut -d'|' -f2)" != "$rev" ]; then
  mig="$(docker exec construction-postgres-1 psql -U postgres -d construction -tAc 'select count(*) from "__EFMigrationsHistory"' 2>/dev/null | tr -d ' ' || true)"
  printf '%s|%s|%s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$rev" "${mig:-0}" >> "$log"
  tail -n 50 "$log" > "$log.new" && mv -f "$log.new" "$log"
fi
# Last, because its modified time is the "updated at" the page shows.
docker ps -a --format '{{json .}}' | put containers.jsonl
