#!/usr/bin/env bash
#
# Brings the deployment stack up and checks it is actually deployable.
#
# A compose file that parses is not a compose file that works. This starts the
# real thing — Postgres, the API, the admin panel and the proxy — and then asks
# it the questions a first deployment would fail on, none of which YAML can
# answer:
#
#   * Does TLS terminate at all, and does plain HTTP redirect to it?
#   * Does the API answer through the proxy, on the same origin as the panel?
#   * Does the panel's runtime config carry this installation's API address, or
#     the build's?
#   * Does a sign-in through the proxy return a refresh cookie that is both
#     HttpOnly and Secure? A `Secure` cookie over a broken TLS setup is
#     silently dropped, and the symptom is an operator signed out on every
#     reload with nothing in any log.
#   * Are the database and the API unreachable from outside?
#
# DOMAIN is `localhost` here, which makes Caddy issue its own internally
# trusted certificate instead of asking Let's Encrypt — so this runs on a
# laptop or a CI runner with no public name and no rate limit to burn. The rest
# of the stack is exactly what a site runs.
#
# Usage:
#   scripts/smoke-deploy.sh                 build the images and test them
#   scripts/smoke-deploy.sh --no-build      test images already present
#
# Leaves nothing behind: the stack and its volumes are removed on exit,
# including on failure.

set -euo pipefail

cd "$(dirname "$0")/.."

RED=$'\033[0;31m'
GREEN=$'\033[0;32m'
RESET=$'\033[0m'

COMPOSE_FILE=deploy/docker-compose.prod.yml
ENV_FILE=deploy/.env
PROJECT=construction-smoke

build=1
[[ "${1:-}" == '--no-build' ]] && build=0

failures=0

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

compose() {
    docker compose -p "$PROJECT" -f "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"
}

cleanup() {
    local status=$?

    echo
    echo '--- tearing the stack down'
    compose down --volumes --remove-orphans >/dev/null 2>&1 || true
    rm -f "$ENV_FILE"

    exit "$status"
}

# --- an environment that exists only for this run ---------------------------
#
# Real secrets, generated rather than fixed: a smoke test that runs on a
# published password teaches the habit of having one.
password=$(openssl rand -hex 24)
admin_password="Smoke-$(openssl rand -hex 8)!"

# High ports, not 80 and 443. A CI runner or a developer's laptop may well
# have something on those already, and the first run of this script found
# exactly that: the proxy could not bind and the stack never came up. The
# production defaults are unchanged; this run just does not assume the machine
# is empty.
http_port=${SMOKE_HTTP_PORT:-8080}
https_port=${SMOKE_HTTPS_PORT:-8443}
origin="https://localhost:${https_port}"

# A subnet unlikely to be taken. The first run of this in CI died on
# "Address already in use" while setting up container networking, with the
# default range colliding with something already on the runner — so the test
# picks its own rather than assuming the machine is empty here either.
subnet=${SMOKE_SUBNET:-10.83.0.0/16}

cat > "$ENV_FILE" <<EOF
DOMAIN=localhost
INTERNAL_SUBNET=${subnet}
HTTP_PORT=${http_port}
HTTPS_PORT=${https_port}
PUBLIC_ORIGIN=${origin}
TLS_EMAIL=smoke@example.invalid
POSTGRES_PASSWORD=${password}
JWT_SECRET_KEY=$(openssl rand -base64 48 | tr -d '\n')
SUPERADMIN_EMAIL=smoke@construction.local
SUPERADMIN_PASSWORD=${admin_password}
API_IMAGE=construction/api:smoke
ADMIN_IMAGE=construction/admin:smoke
SMTP_ALLOW_UNCONFIGURED=true
EOF

trap cleanup EXIT

if (( build )); then
    echo '--- building the images'
    docker build -f src/Construction.API/Dockerfile -t construction/api:smoke .
    docker build -f src/construction_admin/Dockerfile -t construction/admin:smoke src/construction_admin
fi

echo
echo '--- pulling the base images'

# Retried, because Docker Hub is somebody else's server and it fails. The
# third run of this in CI died on a plain HTTP 500 from the registry, with
# nothing wrong on this side at all. A check that goes red on another
# company's bad minute is a check people learn to re-run without reading, and
# then they re-run the real failures without reading too.
pulled=0
for attempt in 1 2 3; do
    if compose pull --quiet; then
        pulled=1
        break
    fi

    echo "registry pull failed (attempt ${attempt}); retrying" >&2
    sleep $((attempt * 10))
done

if (( ! pulled )); then
    printf '%sCould not pull the base images after three attempts.%s\n' "$RED" "$RESET"
    printf 'This is the registry, not the stack. Try again later.\n'
    exit 1
fi

echo
echo '--- starting the stack'
compose up -d

echo
echo '--- waiting for the API to become ready through the proxy'

ready=0
for _ in $(seq 1 60); do
    if curl -ksf --max-time 5 ${origin}/health/ready >/dev/null 2>&1; then
        ready=1
        break
    fi
    sleep 2
done

if (( ! ready )); then
    printf '%sThe stack never became ready.%s Recent logs:\n' "$RED" "$RESET"
    compose logs --tail 60
    exit 1
fi

echo
echo '--- checks'

# The checks run in `bash -c` subshells, so the addresses have to be exported
# rather than merely set — a single-quoted subshell does not inherit a plain
# shell variable, and every check would quietly test the empty string.
export origin http_port https_port

# TLS, and the redirect onto it. Caddy issues its own certificate for
# `localhost`, so `-k` accepts it; that it is a certificate at all is the point.
check 'HTTPS answers' \
    bash -c 'curl -ksf --max-time 10 "$origin/health/live" >/dev/null'

check 'plain HTTP redirects to HTTPS' \
    bash -c '[[ "$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "http://localhost:$http_port/health/live")" =~ ^30[18]$ ]]'

check 'HSTS is set at the edge' \
    bash -c 'curl -ksI --max-time 10 "$origin/" | grep -qi "^strict-transport-security:"'

# The panel, and the file that tells it where its API is.
check 'the admin panel is served' \
    bash -c 'curl -ksf --max-time 10 "$origin/" | grep -q "<div id=\"root\">"'

check 'the SPA fallback serves a client-side route' \
    bash -c 'curl -ksf --max-time 10 "$origin/employees/new" | grep -q "<div id=\"root\">"'

# The whole reason config.js exists: this image was built without an API
# address and got one at start-up. If this check ever passes against a value
# baked at build time, the runtime layer has stopped doing anything.
check "the panel was given this installation's API address" \
    bash -c 'curl -ksf --max-time 10 "$origin/config.js" | grep -qF "apiBaseUrl: \"$origin\""'

check 'config.js is not cacheable' \
    bash -c 'curl -ksI --max-time 10 "$origin/config.js" | grep -qi "cache-control:.*no-store"'

# Sign-in through the proxy, which is where a broken TLS or cookie setup shows.
login=$(curl -ks --max-time 15 -D /tmp/smoke-headers.txt \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"smoke@construction.local\",\"password\":\"${admin_password}\"}" \
    "$origin/api/v1/auth/login")

check 'the seeded administrator can sign in through the proxy' \
    bash -c 'grep -q accessToken <<< "$1"' _ "$login"

# The linchpin. `Secure` follows `Request.IsHttps`, which behind a proxy is
# true only when the API trusts that proxy's address. Get Network__TrustedProxies
# wrong and the cookie arrives without `Secure`, the browser discards it, the
# operator is signed out on every reload — and nothing appears in any log.
check 'the refresh cookie is HttpOnly and Secure' \
    bash -c 'grep -i "^set-cookie:" /tmp/smoke-headers.txt | grep -qi httponly &&
             grep -i "^set-cookie:" /tmp/smoke-headers.txt | grep -qi secure'

# Nothing but the proxy should be reachable. These are the ports the
# development compose publishes and this one must not.
check 'Postgres is not published to the host' \
    bash -c '! timeout 3 bash -c "</dev/tcp/127.0.0.1/5432" 2>/dev/null'

check 'the API is not published to the host' \
    bash -c '! timeout 3 bash -c "</dev/tcp/127.0.0.1/8080" 2>/dev/null'

# The correlation id every log line and problem-details body carries. If the
# proxy strips it, an operator quoting an error has nothing to quote.
check 'a correlation id survives the proxy' \
    bash -c 'curl -ksI --max-time 10 "$origin/api/v1/auth/me" | grep -qi "^x-correlation-id:"'

rm -f /tmp/smoke-headers.txt

echo
if (( failures )); then
    printf '%s%d check(s) failed.%s\n' "$RED" "$failures" "$RESET"
    echo 'Logs from the stack:'
    compose logs --tail 80
    exit 1
fi

printf '%sThe deployment stack works.%s\n' "$GREEN" "$RESET"
