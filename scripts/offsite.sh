#!/usr/bin/env bash
#
# Copies a backup off the machine it was taken on, and proves it arrived.
#
#   ./scripts/offsite.sh push backups/construction-20260809T151651Z.dump
#   ./scripts/offsite.sh pull construction-20260809T151651Z.dump /tmp/recovered.dump
#   ./scripts/offsite.sh status
#
# A dump on a volume beside the database survives a dropped table and a bad
# migration. It does not survive losing the host, and losing the host is the
# incident backups exist for. This is the step that makes them real.
#
# Two things here are not decoration:
#
#   * Every upload is **verified**, by comparing the checksum of what the
#     provider says it now holds against the checksum of what was sent. An
#     upload that returns 200 and stores nothing is a real failure mode, and a
#     backup nobody has read back is a hope.
#
#   * A verified upload writes a **receipt** next to the artefact. `backup.sh`
#     refuses to prune anything without one, so a fortnight of silently failing
#     uploads cannot end with the local copies deleted too.
#
# Transport: S3 (or anything that speaks it — MinIO, Backblaze B2, Wasabi,
# Cloudflare R2) over curl, signed with SigV4 by `s3-sigv4.sh`. No aws-cli or
# rclone needed, because the backup container is a postgres:16-alpine with no
# room for a Python install. Set OFFSITE_COMMAND to use your own uploader
# instead, and the built-in is skipped entirely.

set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# shellcheck source=scripts/db-common.sh
source "${here}/db-common.sh"
# shellcheck source=scripts/s3-sigv4.sh
source "${here}/s3-sigv4.sh"

# ---- configuration -------------------------------------------------------

# Endpoint of the storage service, e.g. https://s3.eu-central-1.amazonaws.com
# or http://minio:9000. Empty means off-site copying is not configured.
: "${OFFSITE_ENDPOINT:=}"
: "${OFFSITE_BUCKET:=}"
: "${OFFSITE_PREFIX:=construction/}"

# Your own uploader, if you have one. Called as `$OFFSITE_COMMAND <file>`.
# Anything non-zero fails the backup.
: "${OFFSITE_COMMAND:=}"

# Recipient public key for client-side encryption with `age`.
#
# Optional and strongly recommended. A dump of this database holds employee
# contact details, dates of birth and a minute-by-minute record of where
# people were; handing that to a storage provider in the clear makes them a
# processor of it. Encrypting first means they hold bytes.
: "${OFFSITE_AGE_RECIPIENT:=}"

# `etag` compares the checksum the provider reports; `download` reads the
# whole object back and hashes it. ETag is exact for a single-part upload —
# which is all this script does — and free. Download is the belt-and-braces
# check worth running on a schedule rather than nightly.
: "${OFFSITE_VERIFY:=etag}"

usage() {
    cat >&2 <<'USAGE'
Usage:
  offsite.sh push <file>...          upload and verify, then write a receipt
  offsite.sh pull <name> <dest>      download, verify, decrypt
  offsite.sh status [max-age-hours]  age of the newest verified copy
USAGE
    exit 2
}

configured() {
    [[ -n "$OFFSITE_COMMAND" ]] && return 0
    [[ -n "$OFFSITE_ENDPOINT" && -n "$OFFSITE_BUCKET" ]] && return 0
    return 1
}

# The name an artefact has off-site: its basename under the prefix, plus the
# encryption suffix when it is encrypted. Flat on purpose — a recovery starts
# by listing the bucket, and a directory tree is one more thing to be wrong
# about at the worst moment.
remote_key_for() {
    printf '%s%s' "$OFFSITE_PREFIX" "$(basename "$1")"
}

receipt_for() {
    printf '%s.offsite' "$1"
}

# ---- push ----------------------------------------------------------------

push_one() {
    local artefact="$1"

    [[ -f "$artefact" ]] || die "No such file: ${artefact}"

    local payload="$artefact"
    local cleanup_payload=""

    if [[ -n "$OFFSITE_AGE_RECIPIENT" ]]; then
        require_tool age

        payload="${artefact}.age"
        cleanup_payload="$payload"

        log "Encrypting $(basename "$artefact") for ${OFFSITE_AGE_RECIPIENT}"
        age --encrypt --recipient "$OFFSITE_AGE_RECIPIENT" --output "$payload" "$artefact"
    fi

    local key
    key="$(remote_key_for "$payload")"

    local local_sum
    local_sum="$(sha256sum "$payload" | cut -d' ' -f1)"

    if [[ -n "$OFFSITE_COMMAND" ]]; then
        log "Uploading ${key} with OFFSITE_COMMAND"
        "$OFFSITE_COMMAND" "$payload" \
            || die "OFFSITE_COMMAND failed for ${payload}"

        # Nothing to verify against: a custom uploader knows where it put the
        # file and this script does not. Its exit status is the only signal,
        # so the receipt records exactly that and no more.
        write_receipt "$artefact" "$key" "$local_sum" "command"
    else
        log "Uploading ${key} to ${OFFSITE_BUCKET}"
        s3_put "$OFFSITE_BUCKET" "$key" "$payload"

        verify_remote "$key" "$payload" "$local_sum"

        write_receipt "$artefact" "$key" "$local_sum" "$OFFSITE_VERIFY"
    fi

    log "Off-site copy of $(basename "$artefact") confirmed"

    [[ -n "$cleanup_payload" ]] && rm -f -- "$cleanup_payload"

    return 0
}

verify_remote() {
    local key="$1" payload="$2" local_sum="$3"

    if [[ "$OFFSITE_VERIFY" == "none" ]]; then
        log "WARNING: OFFSITE_VERIFY=none — the upload was not checked."
        return 0
    fi

    if [[ "$OFFSITE_VERIFY" == "etag" ]]; then
        local etag expected
        etag="$(s3_etag "$OFFSITE_BUCKET" "$key")"

        expected="$(md5sum "$payload" | cut -d' ' -f1)"

        # A single PUT stores the MD5 of the body as the ETag. Multipart
        # uploads and server-side encryption with KMS do not, and this script
        # does neither — but the provider might, so an ETag that is not a
        # plain MD5 is treated as "cannot verify this way" rather than as a
        # mismatch, and the check falls back rather than lying either way.
        if [[ ! "$etag" =~ ^[0-9a-f]{32}$ ]]; then
            log "The provider's ETag (${etag}) is not a plain MD5; reading the object back instead."
        elif [[ "$etag" == "$expected" ]]; then
            log "ETag matches; ${key} is stored intact."
            return 0
        else
            die "ETag mismatch for ${key}: provider says ${etag}, sent ${expected}."
        fi
    fi

    # Full read-back. The only check that does not take the provider's word
    # for anything.
    local scratch
    scratch="$(mktemp)"

    log "Reading ${key} back to verify"
    s3_get "$OFFSITE_BUCKET" "$key" "$scratch"

    local remote_sum
    remote_sum="$(sha256sum "$scratch" | cut -d' ' -f1)"

    rm -f -- "$scratch"

    [[ "$remote_sum" == "$local_sum" ]] \
        || die "Checksum mismatch for ${key}: stored ${remote_sum}, sent ${local_sum}."

    log "Read back and matched; ${key} is stored intact."
}

write_receipt() {
    local artefact="$1" key="$2" sum="$3" how="$4"

    cat > "$(receipt_for "$artefact")" <<RECEIPT
# Written by offsite.sh. backup.sh will not prune an artefact without one.
uploaded_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
remote_key=${key}
sha256=${sum}
verified_by=${how}
RECEIPT
}

# ---- pull ----------------------------------------------------------------

pull_one() {
    local name="$1" dest="$2"

    configured || die "Off-site storage is not configured."

    local key="${OFFSITE_PREFIX}${name}"

    log "Downloading ${key}"
    s3_get "$OFFSITE_BUCKET" "$key" "$dest"

    if [[ "$dest" == *.age ]]; then
        require_tool age

        [[ -n "${OFFSITE_AGE_IDENTITY:-}" ]] \
            || die "The object is encrypted; set OFFSITE_AGE_IDENTITY to the private key file."

        local plain="${dest%.age}"

        log "Decrypting to ${plain}"
        age --decrypt --identity "$OFFSITE_AGE_IDENTITY" --output "$plain" "$dest"

        rm -f -- "$dest"
        dest="$plain"
    fi

    log "Recovered ${dest}"
    echo "$dest"
}

# ---- status --------------------------------------------------------------

# Answers the question a monitoring check should be asking, which is not "did
# tonight's backup run" but "when did a copy last reach somewhere else".
show_status() {
    local max_age_hours="${1:-26}"

    local newest="" newest_epoch=0

    while IFS= read -r -d '' receipt; do
        local epoch
        epoch="$(stat -c %Y "$receipt")"

        if [[ "$epoch" -gt "$newest_epoch" ]]; then
            newest_epoch="$epoch"
            newest="$receipt"
        fi
    done < <(find "$BACKUP_DIR" -maxdepth 1 -name '*.offsite' -type f -print0 2>/dev/null)

    if [[ -z "$newest" ]]; then
        die "No verified off-site copy exists in ${BACKUP_DIR}."
    fi

    local age_hours=$(( ( $(date +%s) - newest_epoch ) / 3600 ))

    log "Newest verified off-site copy: $(basename "${newest%.offsite}"), ${age_hours}h old"

    [[ "$age_hours" -le "$max_age_hours" ]] \
        || die "The newest off-site copy is ${age_hours}h old (limit ${max_age_hours}h)."
}

# ---- entry ---------------------------------------------------------------

command="${1:-}"
shift || true

case "$command" in
    push)
        [[ $# -gt 0 ]] || usage

        configured || die \
            "Off-site storage is not configured. Set OFFSITE_ENDPOINT and OFFSITE_BUCKET, or OFFSITE_COMMAND."

        require_tool sha256sum

        if [[ -z "$OFFSITE_AGE_RECIPIENT" && -z "$OFFSITE_COMMAND" ]]; then
            log "WARNING: OFFSITE_AGE_RECIPIENT is unset — the provider will hold this backup in the clear."
            log "WARNING: it contains contact details, dates of birth and location history. See docs/PRIVACY.md."
        fi

        for artefact in "$@"; do
            push_one "$artefact"
        done
        ;;
    pull)
        [[ $# -eq 2 ]] || usage
        pull_one "$1" "$2"
        ;;
    status)
        show_status "${1:-26}"
        ;;
    *)
        usage
        ;;
esac
