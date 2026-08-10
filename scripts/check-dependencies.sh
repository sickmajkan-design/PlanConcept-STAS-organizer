#!/usr/bin/env bash
#
# Fails when a dependency has a published vulnerability.
#
# Both halves of this need care, for opposite reasons.
#
# `dotnet list package --vulnerable` exits 0 whether or not it found anything —
# it reports, it does not judge — so a naive `run:` step in a workflow passes
# for ever while printing the advisory nobody reads. The JSON output is parsed
# here instead of grepping the human text, which is localised and has changed
# wording between SDK versions.
#
# `npm audit` does exit non-zero, but on *every* severity by default, which on
# a front-end tree means a red build for a ReDoS in a build-time formatter. It
# is pinned to high and above: the point is to stop shipping a known
# exploitable dependency, not to make a ritual of it.
#
# Dart is the gap, and an honest one: pub has no vulnerability database and no
# audit command, so nothing here checks the Flutter tree. `flutter pub outdated`
# reports age, which is not the same question.

set -euo pipefail

cd "$(dirname "$0")/.."

RED=$'\033[0;31m'
GREEN=$'\033[0;32m'
YELLOW=$'\033[0;33m'
RESET=$'\033[0m'

fail=0

check_dotnet() {
  printf '== NuGet ==\n'

  local report
  report=$(mktemp)

  # --include-transitive matters more than the direct list: the advisories that
  # actually land are in packages nobody chose.
  dotnet list package --vulnerable --include-transitive --format json > "$report"

  if python3 - "$report" <<'PY'
import json
import sys

with open(sys.argv[1], encoding='utf-8') as handle:
    report = json.load(handle)

found = []

for project in report.get('projects', []):
    name = project['path'].rsplit('/', 1)[-1]

    # A project with nothing vulnerable carries only `path`. The `frameworks`
    # key appears exactly when there is something to report, which is a more
    # durable signal than any sentence in the human-readable output.
    for framework in project.get('frameworks') or []:
        for kind in ('topLevelPackages', 'transitivePackages'):
            for package in framework.get(kind) or []:
                severities = ', '.join(
                    advisory.get('severity', '?')
                    for advisory in package.get('vulnerabilities') or []
                )
                found.append(
                    f"  {name}: {package['id']} {package.get('resolvedVersion', '')}"
                    f" [{severities}]"
                )

if found:
    print('\n'.join(found))
    sys.exit(1)

sys.exit(0)
PY
  then
    printf '%sNo vulnerable NuGet packages.%s\n' "$GREEN" "$RESET"
  else
    printf '%sVulnerable NuGet packages:%s\n' "$RED" "$RESET"
    fail=1
  fi

  rm -f "$report"
}

check_npm() {
  printf '\n== npm (admin panel) ==\n'

  if (cd src/construction_admin && npm audit --audit-level=high); then
    printf '%sNo npm advisories at high or above.%s\n' "$GREEN" "$RESET"
  else
    printf '%snpm advisories at high or above — run `npm audit fix` in src/construction_admin.%s\n' \
      "$RED" "$RESET"
    fail=1
  fi
}

note_dart() {
  printf '\n== pub (mobile app) ==\n'
  printf '%sNot checked: pub has no vulnerability database and no audit command.%s\n' \
    "$YELLOW" "$RESET"
  printf 'Dependabot watches the version ranges (see .github/dependabot.yml);\n'
  printf 'nothing watches for advisories, because there is nothing to watch with.\n'
}

check_dotnet
check_npm
note_dart

if (( fail )); then
  exit 1
fi
