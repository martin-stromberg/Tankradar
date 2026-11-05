#!/usr/bin/env sh
set -eu

if ! command -v git >/dev/null 2>&1; then
  echo "FEHLER: Git wurde nicht gefunden. Bitte Git installieren und sicherstellen, dass es im PATH liegt." >&2
  exit 1
fi

repo_root="$(git rev-parse --show-toplevel)"

git config --local core.hooksPath .githooks

chmod +x "$repo_root/.githooks/pre-commit" "$repo_root/.githooks/pre-push" "$repo_root/.githooks/install-hooks.sh" 2>/dev/null || true

if ! command -v python3 >/dev/null 2>&1; then
  echo "WARNUNG: 'python3' wurde nicht im PATH gefunden. Die Git-Hooks benoetigen Python 3.x." >&2
fi

echo
echo "Git-Hooks wurden fuer dieses Repository aktiviert (core.hooksPath=.githooks)."
echo "Details: docs/help/git-hooks/"
