#!/usr/bin/env bash
#
# Refuses a commit that carries a credential.
#
# This is deliberately narrow. A scanner that flags every occurrence of the
# word "password" would fire on the CI service's `postgres/postgres`, on the
# browser suite's seeded account, and on half the test fixtures — and a check
# that cries wolf is a check somebody silences. So it looks for two things
# instead: files that must never be committed at all, and the *shapes* real
# credentials have.
#
# It also protects one invariant specific to this repository: `appsettings.json`
# ships with every secret empty, so that a deployment which forgets to set an
# environment variable fails to start rather than starting on a known key. A
# value creeping back into that file is the exact regression that would undo it,
# and it would look entirely innocent in a diff.
#
# Usage:
#   scripts/check-secrets.sh                 scan the tracked files
#   scripts/check-secrets.sh --range A..B    scan the patches in a commit range
#   scripts/check-secrets.sh --stdin         scan piped text
#   scripts/check-secrets.sh --self-test     prove the patterns still catch things
#
# `--range` exists for the case the tree scan cannot see: a credential that was
# committed and then deleted a commit later. It is gone from the working tree
# and still in the history, which is the only place it needs to be to have
# leaked. It does the git plumbing itself rather than leaving it to a caller,
# so that the exclusion below is applied to both scans from one place — the
# first version put the pipeline in the workflow, the exclusion was missing
# from it, and the commit that added this file tripped all seven of its own
# rules.
#
# The self-test exists because the failure mode of a scanner is silence. A
# regex that stops matching goes on reporting success for ever, and nobody
# notices until the thing it was watching for has been in the history for six
# months. CI runs the self-test first, so a broken pattern fails the build.

set -euo pipefail

cd "$(dirname "$0")/.."

RED=$'\033[0;31m'
GREEN=$'\033[0;32m'
YELLOW=$'\033[0;33m'
RESET=$'\033[0m'

# --- what must never be committed, by name ---------------------------------
#
# Each is already in a .gitignore. This is the second lock: an ignore rule is
# advice that `git add -f` overrules, and one of these landing in the history
# means rotating a key rather than reverting a commit.
FORBIDDEN_PATHS=(
  '(^|/)key\.properties$'
  '\.(jks|keystore|p12|pfx)$'
  '(^|/)google-services\.json$'
  '(^|/)GoogleService-Info\.plist$'
  '(^|/)\.env$'
  '(^|/)\.env\.(local|production|prod)$'
  '(^|/)secrets\.json$'
  '(^|/)id_(rsa|dsa|ecdsa|ed25519)$'
)

# --- what must never appear in a tracked file, by shape --------------------
#
# Three tab-separated fields: a name that reads as an instruction when it
# fails, the pattern, and an exception pattern (may be empty). The exception
# exists for one honest case — a vendor's published example credential — and
# not as a general escape hatch: it applies to the matching *line*, everywhere
# in the tree, so it has to be narrow enough to be true everywhere.
#
# ERE, not PCRE: `grep -E` has no lookahead, which is why "AKIA but not the
# example one" is a second field rather than a cleverer regex.
# This file is the one place in the repository that necessarily contains every
# pattern it looks for, so both scans skip it.
#
# That is a hole, and a small one worth naming: a real credential hidden inside
# this file would not be caught. It is short, it is reviewed, and GitHub's own
# secret scanning — recommended in SECURITY.md — has no such exemption.
SELF_PATH='scripts/check-secrets.sh'

CONTENT_RULES=(
  # A leaked service-account or Firebase key always carries one of these, so
  # this single rule covers that whole family. The tempting alternatives —
  # matching `"type": "service_account"` or `"private_key_id"` — match the
  # shape of the file rather than the secret inside it, and fire on the
  # example in PROVISIONING.md and on the unit-test fixtures, neither of which
  # is a credential. They were tried and removed.
  $'private key block\t-----BEGIN [A-Z ]*PRIVATE KEY-----\t'

  # AWS publishes example key ids and uses them throughout its own
  # documentation; they end in EXAMPLE, and a real key never does.
  $'AWS access key id\tAKIA[0-9A-Z]{16}\tAKIA[0-9A-Z]{9}EXAMPLE'

  $'Google API key\tAIza[0-9A-Za-z_-]{35}\t'
  $'GitHub personal access token\t(ghp|gho|ghu|ghs|ghr)_[0-9A-Za-z]{36}\t'
  $'GitHub fine-grained token\tgithub_pat_[0-9A-Za-z_]{22,}\t'
  $'Slack token\txox[baprs]-[0-9A-Za-z-]{10,}\t'
  $'JWT with a payload\teyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.\t'
)

fail=0

note_failure() {
  fail=1
  printf '%s%s%s\n' "$RED" "$1" "$RESET"
}

# Splits a rule into the globals `rule_name`, `rule_pattern`, `rule_except`.
parse_rule() {
  local rule=$1
  local rest

  rule_name=${rule%%$'\t'*}
  rest=${rule#*$'\t'}
  rule_pattern=${rest%%$'\t'*}
  rule_except=${rest#*$'\t'}
}

# Reports whether [line] trips [pattern] without being covered by [except].
# The one place matching happens, so the scan and the self-test cannot drift.
line_trips_rule() {
  local line=$1 pattern=$2 except=$3

  printf '%s\n' "$line" | grep -qE -- "$pattern" || return 1

  if [[ -n "$except" ]] && printf '%s\n' "$line" | grep -qE -- "$except"; then
    return 1
  fi

  return 0
}

# Tracked files only: build output, node_modules and the pub cache are not
# ours and are not committed, and scanning them would take minutes and find
# other people's test fixtures.
scan_paths() {
  local pattern matches

  for pattern in "${FORBIDDEN_PATHS[@]}"; do
    matches=$(git ls-files | grep -E "$pattern" || true)

    if [[ -n "$matches" ]]; then
      note_failure "Committed file that must never be committed (/$pattern/):"
      printf '  %s\n' $matches
    fi
  done
}

scan_content() {
  local rule rule_name rule_pattern rule_except hits filtered

  local -a files
  mapfile -t files < <(git ls-files | grep -vxF "$SELF_PATH")

  for rule in "${CONTENT_RULES[@]}"; do
    parse_rule "$rule"

    # -I skips binaries, -n gives the line so the report is actionable.
    hits=$(grep -InE -- "$rule_pattern" "${files[@]}" 2>/dev/null || true)

    if [[ -n "$rule_except" && -n "$hits" ]]; then
      filtered=$(printf '%s\n' "$hits" | grep -vE -- "$rule_except" || true)
    else
      filtered=$hits
    fi

    if [[ -n "$filtered" ]]; then
      note_failure "Looks like a $rule_name:"
      printf '  %s\n' "$filtered"
    fi
  done
}

# The one repository-specific invariant: production defaults stay empty.
#
# `appsettings.Development.json` is exempt on purpose — it carries obvious
# local values so a developer can run the thing, and it is never deployed.
scan_production_defaults() {
  local file='src/Construction.API/appsettings.json'
  local matches

  if [[ ! -f "$file" ]]; then
    printf '%sSkipped: %s not found%s\n' "$YELLOW" "$file" "$RESET"
    return
  fi

  matches=$(grep -nE '"(SecretKey|Password|ApiKey|ClientSecret)"[[:space:]]*:[[:space:]]*"[^"]+"' "$file" || true)

  if [[ -n "$matches" ]]; then
    note_failure "$file must ship with empty secrets — a deployment that forgets an environment variable has to fail loudly, not start on a value from the repository:"
    printf '  %s\n' "$matches"
  fi
}

self_test() {
  local failures=0
  local rule rule_name rule_pattern rule_except sample sample_name

  # One planted sample per rule. If a pattern breaks, its sample stops
  # matching and this names which one.
  local -a samples=(
    $'private key block\t-----BEGIN RSA PRIVATE KEY-----'
    $'AWS access key id\tAKIA2E0A8F3B244C9D71'
    $'Google API key\tAIzaSyA1234567890abcdefghijklmnopqrstuvw'
    $'GitHub personal access token\tghp_0123456789abcdefghijklmnopqrstuvwxyz'
    $'GitHub fine-grained token\tgithub_pat_01234567890123456789ab_0123456789'
    $'Slack token\txoxb-0123456789-abcdefghij'
    $'JWT with a payload\teyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NX0.signature'
  )

  if (( ${#samples[@]} != ${#CONTENT_RULES[@]} )); then
    printf '%sself-test: %d rules but %d samples — every rule needs one%s\n' \
      "$RED" "${#CONTENT_RULES[@]}" "${#samples[@]}" "$RESET"
    return 1
  fi

  for sample in "${samples[@]}"; do
    sample_name=${sample%%$'\t'*}
    rule_pattern=''

    for rule in "${CONTENT_RULES[@]}"; do
      parse_rule "$rule"
      [[ $rule_name == "$sample_name" ]] && break
      rule_pattern=''
    done

    if [[ -z "$rule_pattern" ]]; then
      printf '%sself-test: no rule named "%s"%s\n' "$RED" "$sample_name" "$RESET"
      failures=1
      continue
    fi

    if ! line_trips_rule "${sample#*$'\t'}" "$rule_pattern" "$rule_except"; then
      printf '%sself-test: the "%s" pattern no longer catches its own sample%s\n' \
        "$RED" "$sample_name" "$RESET"
      failures=1
    fi
  done

  # And the other direction, which matters more: strings this repository
  # legitimately contains must NOT match. A scanner that fires on these is a
  # scanner somebody turns off within a week. The last three are real lines
  # that earlier, looser rules flagged; they stay here so that widening a rule
  # back out fails the self-test instead of failing a build on a Tuesday.
  local -a innocent=(
    'POSTGRES_PASSWORD: postgres'
    'password: E2ePassword123!'
    'Password=postgres'
    'JwtSettings__SecretKey: ${JWT_SECRET_KEY:?set it}'
    'const password = "Gradnja123";'
    '"SecretKey": "",'
    'export AWS_ACCESS_KEY_ID="AKIAIOSFODNN7EXAMPLE"'
    'FIREBASE_CREDENTIALS_JSON={"type":"service_account",...}'
    '("Firebase:CredentialsJson", "{\"type\":\"service_account\"}");'
  )

  local line
  for line in "${innocent[@]}"; do
    for rule in "${CONTENT_RULES[@]}"; do
      parse_rule "$rule"

      if line_trips_rule "$line" "$rule_pattern" "$rule_except"; then
        printf '%sself-test: "%s" wrongly tripped the "%s" rule%s\n' \
          "$RED" "$line" "$rule_name" "$RESET"
        failures=1
      fi
    done
  done

  if (( failures )); then
    return 1
  fi

  printf '%sself-test: %d rules catch their samples; %d real lines from this repository trip none of them%s\n' \
    "$GREEN" "${#samples[@]}" "${#innocent[@]}" "$RESET"
}

# Scans arbitrary text — a diff, a patch, whatever is piped in — with the
# content rules. Paths and the appsettings invariant do not apply here; those
# are questions about the tree, not about a stream of lines.
scan_stdin() {
  local input rule rule_name rule_pattern rule_except hits filtered

  input=$(cat)

  for rule in "${CONTENT_RULES[@]}"; do
    parse_rule "$rule"

    hits=$(printf '%s\n' "$input" | grep -nE -- "$rule_pattern" || true)

    if [[ -n "$rule_except" && -n "$hits" ]]; then
      filtered=$(printf '%s\n' "$hits" | grep -vE -- "$rule_except" || true)
    else
      filtered=$hits
    fi

    if [[ -n "$filtered" ]]; then
      note_failure "Looks like a $rule_name, in the text scanned:"
      printf '  %s\n' "$filtered"
    fi
  done
}

# Scans the patches introduced by a commit range, with this file excluded — the
# same exclusion the tree scan uses, from the same variable, so the two cannot
# drift apart again.
scan_range() {
  local range=$1

  if [[ -z "$range" ]]; then
    printf 'Usage: %s --range <before>..<after>\n' "$0" >&2
    exit 2
  fi

  # Through a file rather than a pipe. `git log ... | scan_stdin` runs the
  # function in a subshell, where the `fail` flag it sets is discarded when the
  # subshell exits — the scan prints the finding and then reports success,
  # which is the worst way for a scanner to be wrong. Redirection keeps it in
  # this shell.
  local patch
  patch=$(mktemp)

  git log --format= --patch "$range" -- . ":(exclude)$SELF_PATH" > "$patch"
  scan_stdin < "$patch"
  rm -f "$patch"
}

case "${1:-}" in
  --self-test)
    self_test
    exit $?
    ;;
  --range)
    self_test
    scan_range "${2:-}"
    ;;
  --stdin)
    self_test
    scan_stdin
    ;;
  '')
    self_test
    scan_paths
    scan_content
    scan_production_defaults
    ;;
  *)
    printf 'Unknown option: %s\n' "$1" >&2
    exit 2
    ;;
esac

if (( fail )); then
  printf '\n%sSecret scan failed.%s If one of these is a false positive, narrow the\n' "$RED" "$RESET"
  printf 'rule in scripts/check-secrets.sh rather than adding an allowlist — an\n'
  printf 'allowlist grows until the scan means nothing.\n'
  exit 1
fi

printf '%sSecret scan clean.%s\n' "$GREEN" "$RESET"
