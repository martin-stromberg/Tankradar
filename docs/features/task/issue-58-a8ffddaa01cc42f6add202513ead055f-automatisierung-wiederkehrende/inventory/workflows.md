# Workflows und gh-CLI-Muster

Bestand unter `.github/workflows/`: acht Dateien. Keine erzeugt GitHub-Issues; `gh issue` wird im gesamten Repository nirgends aufgerufen (geprüft per Suche in `.github/`).

## Workflow-Übersicht

| Datei | `name:` | Trigger | Runner | Issue-/PR-Bezug |
|---|---|---|---|---|
| `verify-pr-source.yml` | Verify PR Source | `pull_request` → `main` | ubuntu-latest | lehnt fremde PRs ab |
| `pr-staging-ci.yml` | PR CI for Staging | `pull_request` → `staging` | windows-latest, macos-latest | u. a. Artefakt `e2e-dumps-pr` |
| `staging-ci.yml` | Pre-Release | `push` → `staging` | windows-latest, ubuntu-latest, macos-latest | `gh release create` (Pre-Release) |
| `staging-to-main-promotion.yml` | Staging to Main Promotion | `workflow_run` auf „Pre-Release" | ubuntu-latest | Label `automated-promotion`, `gh pr create` |
| `sync-staging-with-main.yml` | Backmerge Main to Staging | `push` → `main` | ubuntu-latest | Label `automated-backmerge`, `gh pr create` |
| `release.yml` | Release | `push` → `main`, Tag `v*.*.*` | ubuntu/windows/macos | `gh release create`/`upload` |
| `security-scan.yml` | Security Scan | `schedule` Cron `0 4 * * 1` + `workflow_dispatch` | windows-latest | kein Issue-Mechanismus |
| `pages.yml` | GitHub Pages | `push` → `main` (pfadbeschränkt) + `workflow_dispatch` | ubuntu-latest | – |

## Für die Anforderung relevante Muster

### `security-scan.yml` — einziger Schedule-Workflow

Datei: `.github/workflows/security-scan.yml`

- `on: schedule: - cron: '0 4 * * 1'` (Montag 04:00 UTC) plus `workflow_dispatch` (Zeilen 6–9) — die Konvention „zeitgesteuert + manuell auslösbar", die die neuen Workflows übernehmen sollen.
- `permissions: contents: read` (Zeilen 11–12); `timeout-minutes: 30`; `defaults: run: shell: bash`.
- Führt über `.github/actions/security-scan` (`action.yml`) `dotnet list package --vulnerable` aus und lässt den Schritt bei Funden fehlschlagen — reine Schwachstellensuche, **kein** Issue-Mechanismus. Laut Anforderung unverändert zu lassen.
- Kommentar im Kopf (Zeilen 3–5) begründet den Trigger-Verzicht auf `pull_request`.

### `staging-to-main-promotion.yml` — Label-Idempotenz und Dedup-Muster

Datei: `.github/workflows/staging-to-main-promotion.yml`

- `permissions:` `contents: read`, `pull-requests: write`, **`issues: write`** (Zeilen 14–17) — `issues: write` ist bereits etabliert.
- Leichtgewichtiger Job `promote` auf `ubuntu-latest`, `timeout-minutes: 15`, `concurrency`-Gruppe `staging-to-main-promotion` mit `cancel-in-progress: false` (Zeilen 19–27).
- **Label idempotent anlegen** (Zeilen 48–52):
  `gh label create automated-promotion --color "0E8A16" --description "..." --force` mit `GH_TOKEN: ${{ github.token }}`.
- **Duplikatprüfung (für PRs, Zeilen 54–67):**
  `existing="$(gh pr list --base main --head staging --state open --json number)"`, dann nur bei `jq length -eq 0` ein `gh pr create ... --label "automated-promotion"`. Dasselbe Muster steht in `sync-staging-with-main.yml` (Zeilen 33–49, Label `automated-backmerge`, Farbe `1D76DB`).
- Kopf-Kommentar (Zeilen 3–7) warnt, dass der `workflow_run`-Trigger den **Anzeigenamen** referenziert und erst auf `main` wirkt — dokumentierte Plattformeinschränkung, die auch für `schedule`-Trigger gilt.
- Alle `run`-Schritte verwenden Bash-Syntax ohne explizite `shell:`-Angabe (Ubuntu-Default).

### `release.yml` — `GH_TOKEN`-Konvention

- `GH_TOKEN: ${{ github.token }}` als `env:` auf Step-Ebene (Zeilen 50, 95, 112) — dieselbe Bereitstellung wie für die geplanten `gh issue`-Aufrufe nötig.

### `pages.yml`

- `workflow_dispatch` neben pfadbeschränktem `push` (Zeilen 8–14); `concurrency`-Gruppe `github-pages`. Wird in der Dokumentationstabelle „Die sieben Workflows" (`docs/help/ci-cd/workflows.md`) **nicht** geführt.

## Vorhandene Labels (aus Workflows und Einrichtungsdoku)

| Label | Farbe | Verwendung |
|---|---|---|
| `automated-promotion` | `0E8A16` | Promotion-PR `staging` → `main` |
| `automated-backmerge` | `1D76DB` | Backmerge-PR `main` → `staging` |

Beide werden von den Workflows beim ersten Lauf selbst angelegt (`gh label create ... --force`); `docs/help/ci-cd/einrichtung.md` Abschnitt „3. Labels" beschreibt sie mit manuellem Anlege-Befehl. Für die drei neuen Prüftypen existiert noch **kein** Label und kein Issue-basierter Dedup-Mechanismus.
