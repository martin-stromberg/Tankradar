# Externe Vorgaben und Standards

Dokumentiert am: 2026-09-28

Die in der Anforderung referenzierten Repositories sind privat und nur über die authentifizierte
GitHub CLI (`gh api`) abrufbar. Die Originaldateien wurden unverändert unter
[`external-sources/`](external-sources/) abgelegt und sind die **verbindliche Quelle** — bei der
Planung und Umsetzung sind diese Originale zu lesen, nicht nur diese Übersicht.

## 1. CI-Workflows

- Quelle: `martin-stromberg/Pattern-Collection` → `CI-Workflows/instructions.md`
- Kopie: [`external-sources/ci-workflows-instructions.md`](external-sources/ci-workflows-instructions.md)
- Kernpunkte (siehe Original, Abschnitte 2–12):
  - Branch-Modell `staging` → `main`, automatisierte Pre-Releases auf `staging`, finale Releases auf `main`, semantische Versionierung.
  - Sieben Workflow-Dateien: `verify-pr-source.yml`, `pr-staging-ci.yml`, `staging-ci.yml`, `release.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `security-scan.yml`.
  - Composite Actions `build-and-package` und `security-scan`, `scripts/resolve-release-version.mjs`, `release.config.js`, `update.json`-Manifest.
  - Troubleshooting-Kapitel 11 und Checkliste in Kapitel 12.
- Projektspezifisch laut Anforderung: erste Testversionen ab 0.1; Actions können wegen Billing-Limits (privates Repo bis 1.0) scheitern — Tests/Prüfungen dann lokal.

## 2. Git-Hooks

- Quelle: `martin-stromberg/Pattern-Collection` → `Git-Hooks/readme.md` und `Git-Hooks/githooks/`
- Kopien: [`external-sources/git-hooks-readme.md`](external-sources/git-hooks-readme.md), [`external-sources/githooks/`](external-sources/githooks/)
- Installation über `core.hooksPath` (Skripte `install-hooks.sh` / `install-hooks.cmd`).
- Hooks: `pre-commit` (Warnungen/Prüfungen auf gestagte Dateien), `pre-push` (blockiert direkte Pushes auf `main`/`staging`, strikte Repo-weite Prüfungen).
- Prüfskripte: `csproj-xmldoc-check.py`, `no-notimplemented-check.py`, `enum-coverage-check.py`, `translation-check.py`, `razor-l10n-check.py`, `razor-usage-check.py` (die Razor-Skripte sind für eine MAUI/XAML-App voraussichtlich nicht einschlägig bzw. anzupassen).

## 3. iOS-Deployment

- Quelle: `martin-stromberg/VideoPlayer-App` (Branch `staging`) → `scripts/iOS-Deployment.ps1`
- Kopie: [`external-sources/iOS-Deployment.ps1`](external-sources/iOS-Deployment.ps1)
- Beispielskript für Build/Signierung/Upload der iOS-App über macOS; als Vorlage für das Tankradar-Deployment zu adaptieren.
