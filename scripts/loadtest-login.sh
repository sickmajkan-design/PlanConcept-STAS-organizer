#!/usr/bin/env bash
#
# What happens to sign-in when a whole shift arrives at once.
#
# Verifying a password is deliberately expensive — PBKDF2-HMAC-SHA256 at 100,000
# iterations — and that cost is paid by the server, once per attempt, on a core
# that can do nothing else meanwhile. It is the only endpoint in this API whose
# cost is set on purpose rather than by the work it does, so it is the only one
# where "how many at once" has an answer worth knowing before a site of sixty
# people finds it out at seven in the morning.
#
# This measures two different ceilings, because they are nowhere near each other:
#
#   1. CPU.       How many sign-ins a second the process can actually complete.
#   2. The limit. How many the rate limiter allows from one address per minute.
#
# The second is reached first, by a wide margin, and only for people who share
# an address — a site office behind one router, or a mobile carrier putting
# hundreds of subscribers behind one CGNAT address. That is what `--one-address`
# measures.
#
# Usage:
#   scripts/loadtest-login.sh                          against localhost:5199
#   scripts/loadtest-login.sh --url https://host       against a deployment
#   scripts/loadtest-login.sh --total 400 --concurrency 200
#   scripts/loadtest-login.sh --one-address            find the per-IP ceiling
#
# Every client is given its own loopback source address (127.x.x.x) unless
# --one-address is passed, so the rate limiter sees separate callers and the
# measurement is of the hashing rather than of the limiter. That is a fair model
# of a crew on mobile data, where each phone has its own address; it is not a
# fair model of a crew on one Wi-Fi, which is exactly why --one-address exists.
#
# Needs an account it can sign in as. It never creates one: this is pointed at
# real deployments, and a load test that leaves accounts behind is a load test
# nobody runs twice.

set -euo pipefail

URL=${URL:-http://127.0.0.1:5199}
EMAIL=${EMAIL:-admin@construction.local}
PASSWORD=${PASSWORD:-Admin123!}
TOTAL=100
CONCURRENCY=25
ONE_ADDRESS=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --url) URL=$2; shift 2 ;;
        --email) EMAIL=$2; shift 2 ;;
        --password) PASSWORD=$2; shift 2 ;;
        --total) TOTAL=$2; shift 2 ;;
        --concurrency) CONCURRENCY=$2; shift 2 ;;
        --one-address) ONE_ADDRESS=1; shift ;;
        -h|--help) sed -n '2,40p' "$0"; exit 0 ;;
        *) echo "unknown argument: $1" >&2; exit 2 ;;
    esac
done

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

export URL EMAIL PASSWORD ONE_ADDRESS

# One sign-in. Prints "<status> <seconds>".
attempt() {
    local n=$1
    local source=()

    if (( ! ONE_ADDRESS )); then
        # 127.0.0.0/8 is entirely local, so every address in it is usable
        # without configuring anything. Two octets give plenty of clients.
        source=(--interface "127.0.$(( (n / 200) + 1 )).$(( (n % 200) + 2 ))")
    fi

    curl -sk "${source[@]}" -o /dev/null -w '%{http_code} %{time_total}\n' --max-time 120 \
        -H 'Content-Type: application/json' \
        -d "{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}" \
        "${URL}/api/v1/auth/login"
}
export -f attempt

echo "--- ${TOTAL} sign-ins, ${CONCURRENCY} at a time, against ${URL}"
if (( ONE_ADDRESS )); then
    echo "    all from one address — this measures the rate limiter, not the CPU"
else
    echo "    each from its own address — this measures the CPU, not the limiter"
fi

# An endpoint that does nothing, sampled throughout. Sign-in saturating every
# core is only half a problem; the half that matters to everyone not signing in
# is whether the rest of the API goes with it.
(
    for _ in $(seq 1 400); do
        curl -sk -o /dev/null -w '%{time_total}\n' --max-time 30 "${URL}/health/live" || true
        sleep 0.1
    done > "$work/health"
) &
probe=$!

started=$(date +%s.%N)
seq 1 "$TOTAL" | xargs -P "$CONCURRENCY" -I{} bash -c 'attempt {}' > "$work/latencies" 2>/dev/null || true
finished=$(date +%s.%N)

kill "$probe" 2>/dev/null || true
wait "$probe" 2>/dev/null || true

python3 - "$work/latencies" "$work/health" "$started" "$finished" <<'PY'
import sys

rows = [line.split() for line in open(sys.argv[1]) if line.strip()]
health = sorted(float(line) for line in open(sys.argv[2]) if line.strip())
wall = float(sys.argv[4]) - float(sys.argv[3])

statuses = {}
for code, _ in rows:
    statuses[code] = statuses.get(code, 0) + 1

accepted = sorted(float(t) for code, t in rows if code == '200')


def pct(values, q):
    if not values:
        return float('nan')
    return values[min(int(len(values) * q), len(values) - 1)] * 1000


print()
print(f"  wall clock            {wall:.1f} s")
print(f"  completed             {len(rows)}  ->  {len(rows) / wall:.1f} per second")
print(f"  statuses              {statuses}")

if accepted:
    print(f"  accepted sign-in      p50 {pct(accepted, .5):.0f} ms"
          f"   p95 {pct(accepted, .95):.0f} ms"
          f"   p99 {pct(accepted, .99):.0f} ms"
          f"   max {accepted[-1] * 1000:.0f} ms")

if health:
    print(f"  /health/live meanwhile p50 {pct(health, .5):.0f} ms"
          f"   max {health[-1] * 1000:.0f} ms   ({len(health)} samples)")

rejected = statuses.get('429', 0)
if rejected:
    print()
    print(f"  {rejected} of {len(rows)} were refused with 429 by the rate limiter.")
    print("  Everyone behind one shared address shares one allowance, and the")
    print("  refusal reads as a wrong password to the person who typed the right one.")
PY
