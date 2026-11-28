# Workflows — Bestand

Alle Dateien unter `.github/workflows/`. Elf Workflows; nur `release.yml` und `staging-ci.yml` erzeugen GitHub-Releases bzw. Release-Tags.

## `release.yml` — „Release"

Trigger (`release.yml:6-9`): `push` auf `main` **und** manuell gepushter Tag `v*.*.*` — beide Wege sind vollwertige Releases (Kommentar Z. 3-5).
Permissions (`release.yml:11-12`): `contents: write` auf Workflow-Ebene — heute für `gh release` **und** implizit für die Tag-Erzeugung durch GitHub nötig.
Concurrency (`release.yml:24-26`): Gruppe `release`, `cancel-in-progress: false`.

### Job `release` (windows-latest, `release.yml:15-143`)

Outputs: `released`, `version`, `tag`, `release_action` (aus Schritt `version`, Z. 30-34).
Env: `IncludeAndroidTarget/IncludeIosTarget/IncludeMacCatalystTarget=false` (nur Windows-Ziel; Z. 19-22).

| Schritt | Zeilen | Funktion |
|---|---|---|
| Checkout | 36-40 | `fetch-depth: 0`, `fetch-tags: true` (Tags nötig für Versionsermittlung) |
| Setup Node.js | 42-45 | Node 24 |
| Resolve release version | 47-51 | `node scripts/resolve-release-version.mjs` mit `GH_TOKEN`; liefert `released`, `version`, `tag`, `release_kind` (`automatic`\|`manual`), `release_action` (`create`\|`upload-existing`\|leer) |
| Check out release tag for asset repair | 53-63 | Nur bei `release_action == 'upload-existing'`: checkt den vorhandenen Tag detached aus und verifiziert die Commit-Identität (Reparaturpfad) |
| Setup .NET / Install MAUI workloads | 65-73 | Nur bei `released == 'true'`; SDK `11.0.1xx` |
| Release gate – regular tests (unit/integration) | 75-86 | Nur bei `release_action == 'create'`: `dotnet test` Unit + Integration, `Category!=OsInterface`, **blockierend**, TRX nach `TestResults/` |
| Release gate – OS interface tests | 88-99 | Nur bei `create`: beide Suiten `Category=OsInterface`, `continue-on-error: true` (best-effort, Issue #93) |
| Upload release-gate test results | 101-107 | Artefakt `test-results-release` (nur `create`, `if: always()`) |
| Build and package | 109-114 | Composite Action `./.github/actions/build-and-package` → `artifacts/release-win-x64.zip` + `artifacts/update.json` |
| **Create GitHub release** | **116-133** | Nur bei `create`: `gh release create "$RELEASE_TAG" artifacts/release-win-x64.zip artifacts/update.json --title … --generate-notes`; beim `automatic`-Kind zusätzlich `--target "$GITHUB_SHA" --latest`. **Erzeugt den Release-Tag implizit** (automatischer Weg) |
| **Upload asset to existing GitHub release** | **135-142** | Nur bei `upload-existing`: `gh release upload "$RELEASE_TAG" … --clobber` (Asset-Reparatur) |

### Job `ios` (xcode-27, `release.yml:146-174`)

- `needs: release`; läuft nur bei `released == 'true'` **und** `release_action == 'create'` — beim Reparaturpfad `upload-existing` läuft der iOS-Job **nicht**.
- Einziger Schritt `Build, sign and upload iOS` = `./.github/actions/package-ios` mit allen Signierungs- und App-Store-Connect-Secrets sowie `fuel-price-api-key: ${{ secrets.FUEL_PRICE_API_KEY }}` (Z. 174).
- Kommentar Z. 144-145: signiert + TestFlight-Upload bei `IOS_SIGNING_ENABLED='true'` und vollständigen Secrets, sonst unsignierte Compile-Prüfung **ohne Fehlschlag**.

## `staging-ci.yml` — „Pre-Release"

Trigger (`staging-ci.yml:5-8`): `push` auf `staging`. Anzeigename `Pre-Release` wird von `staging-to-main-promotion.yml` referenziert (Warnhinweis Z. 3-4; die Promotion triggert tatsächlich per `push`, die Prüfung in `validate-workflows.py` greift nur bei `workflow_run`).
Permissions (`staging-ci.yml:14-16`): `contents: read`, `checks: write` auf Workflow-Ebene.

| Job | Runner | Funktion |
|---|---|---|
| `detect-backmerge` | ubuntu-latest | Erkennt reine Back-Merges von `main` via `scripts/detect-backmerge.mjs`; überspringt dann alle Prüfungen |
| `static-checks` | windows-latest | Restore, `npm test` (Z. 80-81), `test-ios-deployment.ps1` (Z. 83-85), `dotnet format --verify-no-changes`, `security-scan`-Action, statische Analyse (`TreatWarningsAsErrors`) |
| `build-and-test` | windows-latest | Build + blockierende Tests `Category!=OsInterface` (Unit, Integration mit Coverage ≥ 70 %, E2E-Rest), Artefakte `coverage-report-staging`, `test-results-staging`, `e2e-*` |
| `os-interface-tests` | windows-latest | Best-effort-Lane (`continue-on-error` auf Job-Ebene), `Category=OsInterface`, Flaky-Retry + Analyse |
| `version` | ubuntu-latest | `node scripts/determine-next-version.mjs --rc` (Z. 371-377); Outputs `changed`, `version`, `rc_tag`, `rc_version`; `needs`: `detect-backmerge`, `static-checks`, `build-and-test` (nicht `os-interface-tests`) |
| `prerelease` | windows-latest | Nur bei `changed == 'true'`; **Job-Level `permissions: contents: write`** (Z. 390-391); `build-and-package` mit `rc_version`/`rc_tag`, dann **`gh release create "$RC_TAG" artifacts/release-win-x64.zip artifacts/update.json --target "$GITHUB_SHA" --title … --prerelease --generate-notes`** (Z. 405-414) — erzeugt den RC-Tag implizit |
| `ios-prerelease` | xcode-27 | Nur bei `changed == 'true'`; `package-ios` mit `rc_version` und allen Secrets inkl. `FUEL_PRICE_API_KEY` (Z. 418-446) |
| `back-merge-skip` | ubuntu-latest | Platzhalter bei Back-Merge |

Hinweis: Der `prerelease`-Job checkt **ohne** `fetch-tags` aus (Z. 396-398) — er braucht die Tags nicht, da `determine-next-version` bereits im `version`-Job lief.

## `pr-staging-ci.yml` — „PR CI for Staging"

Gleiche Check-Jobs wie `staging-ci.yml`, plus Job **`ios-build`** (xcode-27, Z. 357-373): ruft `package-ios` nur mit `xcode-version` auf — keine Signierungs-Secrets, kein `fuel-price-api-key` → unsignierte Compile-Prüfung, nie ein Upload. Erzeugt keine Releases/Assets.

## Übrige Workflows (kein Release-Bezug)

| Datei | Relevanz |
|---|---|
| `staging-to-main-promotion.yml` | Push → `staging`: erzeugt Entwurfs-PR `staging` → `main` (Label `automated-promotion`, Dedup via `gh pr list`). Kein `gh release`, kein Tagging. Permissions: `contents: read`, `pull-requests: write`, `issues: write`. |
| `sync-staging-with-main.yml` | Push → `main`: Backmerge-PR `main` → `staging`. Fachlich relevant nur über die Merge-Commit-Pflicht, damit Release-Tags von `staging` aus erreichbar bleiben (`docs/help/ci-cd/versionierung.md:30-33`). |
| `verify-pr-source.yml` | PR → `main` nur von `staging`. |
| `pages.yml` | GitHub-Pages-Deploy bei Push auf `main` (nur `pages/`). |
| `security-scan.yml` | Wöchentlicher Schwachstellenscan. |
| `review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml` | Zeitgesteuerte Issue-Erzeugung via `.github/actions/create-review-issue`. |

## Wege, die heute ein Release-Artefakt/Tag erzeugen

1. `release.yml` Job `release` → `gh release create` (stabiler Tag `vX.Y.Z` + 2 Assets) — automatisch bei Push auf `main` (Tag auf `$GITHUB_SHA`, `--latest`) oder bei manuellem Tag-Push (Tag existiert bereits).
2. `release.yml` Job `release` → `gh release upload --clobber` (Asset-Reparatur, kein neuer Tag).
3. `staging-ci.yml` Job `prerelease` → `gh release create --prerelease` (RC-Tag `vX.Y.Z-rc.N` + 2 Assets).

**Es existiert kein `git tag`-/`git push`-Schritt in irgendeinem Workflow** — die gesamte Tag-Erzeugung ist implizit über `gh release create`. Auf GitHub liegen aktuell 48 Releases (zuletzt `v0.10.0` stabil, `v0.11.0-rc.1` Pre-Release; Assets je `release-win-x64.zip` + `update.json`) und 50 lokale bzw. gepushte Git-Tags `v*` (12 stabile, `v0.1.0`–`v0.10.0`).
