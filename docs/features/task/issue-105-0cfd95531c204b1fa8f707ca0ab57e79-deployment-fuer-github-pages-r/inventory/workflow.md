# Workflow `pages.yml` und Workflow-Kontext

Bestand der Datei `.github/workflows/pages.yml` (47 Zeilen) sowie der sie
umgebenden CI/CD-Struktur. Stand: Commit `00ec4e4` (Branch
`task/issue-105-...`, identisch mit `origin/staging`).

## Aufbau von `.github/workflows/pages.yml`

| Element | Wert | Zeilen |
|---------|------|--------|
| `name` | `GitHub Pages` | 1 |
| Trigger `push` | Branches `main`, Path-Filter `pages/**` und `.github/workflows/pages.yml` | 7–13 |
| Trigger `workflow_dispatch` | manueller Start | 14 |
| `permissions` | `contents: read`, `pages: write`, `id-token: write`, `actions: read` | 16–20 |
| `concurrency` | Gruppe `github-pages`, `cancel-in-progress: false` | 22–24 |
| Jobs | genau ein Job `deploy` | 26–47 |
| `deploy.runs-on` | `ubuntu-latest` | 28 |
| `deploy.timeout-minutes` | `10` | 29 |
| `deploy.environment` | `name: github-pages`, `url: ${{ steps.deployment.outputs.page_url }}` | 30–32 |

### Schritte des Jobs `deploy`

| # | Name | Action | Bemerkung |
|---|------|--------|-----------|
| 1 | `Checkout` | `actions/checkout@v7` | Zeile 34–35 |
| 2 | `Setup Pages` | `actions/configure-pages@v6` | Zeile 37–38; seit Dependabot-Bump `630b915` (vorher `v5`) |
| 3 | `Upload Pages artifact` | `actions/upload-pages-artifact@v5` | Zeile 40–43, `with: path: pages`; erzeugt das Artifact `github-pages` (impliziter Standardname der Action) |
| 4 | `Deploy to GitHub Pages` | `actions/deploy-pages@v5` | Zeile 45–47, `id: deployment` (liefert `outputs.page_url` für die Environment-URL); fehlgeschlagener Schritt |

Upload und Deploy laufen **im selben Job und unmittelbar aufeinander** — es
existiert kein `needs`-, `sleep`- oder Retry-Schritt dazwischen.

## Dateihistorie von `pages.yml`

| Commit | Datum | Inhalt |
|--------|-------|--------|
| `4186202` | 2026-11-04 | `feat: GitHub-Pages-Präsentationsseite der App` — Erstanlage (Issue #34) |
| `c92832e` | 2026-11-04 | `feat: Pages-Deploy zunächst auf staging begutachten` — Push-Trigger auf `staging` |
| `f5037a6` | 2026-11-04 | `feat: Pages-Deploy nach Sichtprüfung auf main umstellen` — Push-Trigger auf `main` |
| `630b915` | 2026-11-24 | `chore(deps): bump actions/configure-pages` — einzige Zeile: `configure-pages@v5` → `@v6` (Dependabot PR #102, Merge-Commit `00ec4e4`) |

`630b915` ist die **einzige Änderung an `pages.yml`** zwischen dem letzten
Erfolgslauf `44993397486` (2026-11-22, `aacdb22`) und dem Fehllauf
`45541447133` (2026-11-25, `9af51e7`). Die weiteren Commits im Bereich
`aacdb22..9af51e7` betreffen ausschließlich Anwendungscode/Plan-Dokumente
(`33d1b82`, `0637207`, `9bb937d` — Issue #65) und Merge-Commits (`0fae8df`,
`3a5c15e`, `f219261`, `a85552f`, `6df420d`, `181004c`, `13f0d36`, `c791b37`).

## Verwandte Workflows und Konfiguration (unverändert, laut Anforderung nicht im Scope)

- `.github/workflows/verify-pr-source.yml` — lehnt PRs nach `main` ab, deren
  `head_ref` nicht `staging` ist.
- `.github/workflows/staging-to-main-promotion.yml` — legt bei Push auf
  `staging` einen Draft-PR `staging` → `main` an (Label
  `automated-promotion`); löst keinen Pages-Lauf aus.
- `.github/workflows/pr-staging-ci.yml` / `staging-ci.yml` — PR- bzw.
  Push-CI für `staging`; enthalten keinen Bezug zu `pages.yml`.
- `.github/dependabot.yml` — Ökosystem `github-actions`, `target-branch:
  staging`, wöchentlich dienstags; die Gruppe `actions` bündelt `patch`,
  `minor` **und `major`**, weshalb der Major-Bump `configure-pages` 5 → 6
  über PR #102 hereinkam.
- Workflow-Bestand gesamt: **11 Dateien** unter `.github/workflows/`
  (`pages.yml`, `pr-staging-ci.yml`, `release.yml`,
  `review-architecture-security.yml`, `review-compliance.yml`,
  `review-nuget-packages.yml`, `security-scan.yml`, `staging-ci.yml`,
  `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`,
  `verify-pr-source.yml`).

## Dokumentationsstellen mit Bezug zu `pages.yml`

- `pages/README.md` — beschreibt Inhalt, Screenshot-Erzeugung und die
  Einmal-Einrichtung „Settings → Pages → Source: GitHub Actions"; erwähnt
  den einzelnen Deploy-Ablauf ohne Job-Details.
- `docs/help/ci-cd/workflows.md` Zeile 19 — Tabelle enthält `pages.yml`
  mit der Beschreibung „Push → `main` (nur `pages/` und die
  Workflow-Datei), manuell". Eine Zwei-Job-Umstellung müsste hier ggf.
  nachgezogen werden (die Tabellenzeile beschreibt nur Trigger, nicht das
  Job-Layout).
- `issue.md` — enthält die Original-Anforderung und den
  Original-Fehlertext des Fehllaufs.

## Nicht betroffene Bereiche

- Kein C#-Code, keine App-Komponenten — die Anforderung betrifft
  ausschließlich die CI/CD-Definition.
- `pages/` (`index.html`, `styles.css`, `app.js`, `assets/`) — Inhalt
  fachlich unverändert; `assets/` enthält `bg-scene.svg`, `appicon.svg`
  und `screenshots/*.png`.
