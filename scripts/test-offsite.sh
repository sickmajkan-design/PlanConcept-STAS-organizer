#!/usr/bin/env bash
#
# Exercises the off-site copy against a local S3 that checks the signature.
#
#   ./scripts/test-offsite.sh
#
# The signing code in s3-sigv4.sh is the part of the backup path with no
# forgiving failure mode: one wrong newline in the canonical request and every
# provider answers 403 with no indication which part was wrong, and the first
# time anybody finds out is the night the uploads start failing.
#
# So this stands up a small server that implements the *verifying* half of
# SigV4 independently — in Python, with hashlib and hmac, from the same
# published algorithm rather than from the bash — and refuses anything whose
# signature it cannot reproduce. A round trip through it proves the two agree.
#
# What it does not prove: that AWS agrees with both of them. That needs an
# account and a bucket, and it is the one step of this that is genuinely
# somebody else's to run. See docs/PROVISIONING.md §4.
#
# Needs python3 and curl. Not part of the backup container — this is a test,
# run it on a machine that has them.

set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

command -v python3 >/dev/null || { echo "python3 is required" >&2; exit 2; }

work="$(mktemp -d)"
server_pid=""

cleanup() {
    [[ -n "$server_pid" ]] && kill "$server_pid" 2>/dev/null || true
    rm -rf -- "$work"
}
trap cleanup EXIT

port="${TEST_S3_PORT:-18099}"

export AWS_ACCESS_KEY_ID="AKIAIOSFODNN7EXAMPLE"
export AWS_SECRET_ACCESS_KEY="wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
export OFFSITE_ENDPOINT="http://127.0.0.1:${port}"
export OFFSITE_BUCKET="backups"
export OFFSITE_PREFIX="construction/"
export OFFSITE_REGION="eu-central-1"
export OFFSITE_PATH_STYLE=1
export BACKUP_DIR="${work}/backups"

mkdir -p "$BACKUP_DIR" "${work}/objects"

python3 "${here}/fake-s3.py" \
    --port "$port" \
    --root "${work}/objects" \
    --access-key "$AWS_ACCESS_KEY_ID" \
    --secret-key "$AWS_SECRET_ACCESS_KEY" \
    --region "$OFFSITE_REGION" &
server_pid=$!

# Wait for it rather than sleeping a guessed amount.
for _ in $(seq 1 50); do
    if curl --silent --output /dev/null "http://127.0.0.1:${port}/healthz"; then
        break
    fi
    sleep 0.1
done

pass=0
fail=0

check() {
    local name="$1" expected="$2" actual="$3"

    if [[ "$expected" == "$actual" ]]; then
        printf '  ok    %s\n' "$name"
        pass=$((pass + 1))
    else
        printf '  FAIL  %s\n        expected: %s\n        actual:   %s\n' \
            "$name" "$expected" "$actual"
        fail=$((fail + 1))
    fi
}

echo "SigV4 round trip against a signature-checking server"

# ---- a real artefact, of a size that is not a special case ---------------

artefact="${BACKUP_DIR}/construction-20260809T151651Z.dump"
head -c 300000 /dev/urandom > "$artefact"

original_sum="$(sha256sum "$artefact" | cut -d' ' -f1)"

# ---- push ---------------------------------------------------------------

if "${here}/offsite.sh" push "$artefact" >"${work}/push.log" 2>&1; then
    check "push exits zero" "0" "0"
else
    check "push exits zero" "0" "1"
    sed 's/^/        /' "${work}/push.log"
fi

check "the server stored the object" "yes" \
    "$([[ -f "${work}/objects/backups/construction/construction-20260809T151651Z.dump" ]] && echo yes || echo no)"

check "a receipt was written" "yes" \
    "$([[ -f "${artefact}.offsite" ]] && echo yes || echo no)"

check "the receipt records the remote key" \
    "remote_key=construction/construction-20260809T151651Z.dump" \
    "$(grep '^remote_key=' "${artefact}.offsite")"

# The ETag path is the default and the one that runs nightly, so it is the one
# under test above. `download` is the deeper check.
check "the upload was verified by ETag" "verified_by=etag" \
    "$(grep '^verified_by=' "${artefact}.offsite")"

# ---- pull ---------------------------------------------------------------

recovered="${work}/recovered.dump"

if "${here}/offsite.sh" pull "construction-20260809T151651Z.dump" "$recovered" \
    >"${work}/pull.log" 2>&1; then
    check "pull exits zero" "0" "0"
else
    check "pull exits zero" "0" "1"
    sed 's/^/        /' "${work}/pull.log"
fi

check "what came back is byte-for-byte what went up" "$original_sum" \
    "$(sha256sum "$recovered" 2>/dev/null | cut -d' ' -f1)"

# ---- full read-back verification ----------------------------------------

second="${BACKUP_DIR}/construction-20260809T160000Z.dump"
head -c 4096 /dev/urandom > "$second"

if OFFSITE_VERIFY=download "${here}/offsite.sh" push "$second" \
    >"${work}/download.log" 2>&1; then
    check "push with OFFSITE_VERIFY=download exits zero" "0" "0"
else
    check "push with OFFSITE_VERIFY=download exits zero" "0" "1"
    sed 's/^/        /' "${work}/download.log"
fi

# ---- the negative case --------------------------------------------------
#
# The check that gives the positive ones their meaning. If the server accepted
# anything, every test above would pass against a signer that emitted
# gibberish.

third="${BACKUP_DIR}/construction-20260809T170000Z.dump"
head -c 1024 /dev/urandom > "$third"

if AWS_SECRET_ACCESS_KEY="not-the-right-secret" \
    "${here}/offsite.sh" push "$third" >"${work}/badkey.log" 2>&1; then
    check "a wrong secret is refused" "refused" "accepted"
else
    check "a wrong secret is refused" "refused" "refused"
fi

check "and no receipt was written for it" "no" \
    "$([[ -f "${third}.offsite" ]] && echo yes || echo no)"

# ---- status -------------------------------------------------------------

if "${here}/offsite.sh" status 26 >"${work}/status.log" 2>&1; then
    check "status passes with a fresh copy" "0" "0"
else
    check "status passes with a fresh copy" "0" "1"
    sed 's/^/        /' "${work}/status.log"
fi

# Aged by hand rather than by waiting. The check is "how long since a copy
# last reached somewhere else", and the answer that matters is the one nobody
# is watching for at 4am.
touch -d '30 hours ago' "${BACKUP_DIR}"/*.offsite

if "${here}/offsite.sh" status 26 >/dev/null 2>&1; then
    check "status fails when the newest copy is 30h old" "fails" "passes"
else
    check "status fails when the newest copy is 30h old" "fails" "fails"
fi

rm -f "${BACKUP_DIR}"/*.offsite

if "${here}/offsite.sh" status >/dev/null 2>&1; then
    check "status fails when nothing was ever copied off" "fails" "passes"
else
    check "status fails when nothing was ever copied off" "fails" "fails"
fi

echo
printf '%d passed, %d failed\n' "$pass" "$fail"

[[ "$fail" -eq 0 ]]
