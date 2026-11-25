# Umgebung, Action-Versionen und Upstream-Stand

Ermittelt per `gh api` (Stand 2026-11-25).

## GitHub-Pages-Konfiguration des Repositories

`gh api repos/martin-stromberg/Tankradar/pages`:

```json
{"build_type": "workflow",
 "html_url": "https://martin-stromberg.github.io/Tankradar/",
 "source": {"branch": "main", "path": "/"},
 "status": null}
```

`build_type: workflow` bestätigt: Settings → Pages → Source steht auf
„GitHub Actions" (Voraussetzung laut `pages/README.md` — eingerichtet).

## Verfügbare Action-Releases (Upstream)

| Action | Verwendet in `pages.yml` | Neuester Release (Upstream) | Bemerkung |
|--------|--------------------------|-----------------------------|-----------|
| `actions/checkout` | `v7` | — | im Fehllauf SHA `3d3c42e…` |
| `actions/configure-pages` | `v6` | `v6.0.0` (2026-03-25) | `v6` ist aktuellste Major-Version |
| `actions/upload-pages-artifact` | `v5` | `v5.0.0` (2026-04-10) | `v5` ist aktuellste Major-Version |
| `actions/deploy-pages` | `v5` | **`v5.0.1` (2026-09-01)** | `v5` und `v5.0.1` zeigen auf denselben SHA `368f82528645a54fb793d4d04e342629a3f51346` — genau der SHA, den der Fehllauf geladen hat |

Fakt: `deploy-pages` läuft bereits auf dem neuesten verfügbaren Release
(`v5.0.1`); es existiert **kein neuerer Release**, auf den ein
Versionspin zeigen könnte.

## Einschlägige Upstream-Issues/PRs in `actions/deploy-pages` (alle offen)

| # | Typ | Titel | Bezug |
|---|-----|-------|-------|
| `#451` | Issue (offen, 5 Kommentare) | „Intermittent \"No artifacts named github-pages\" when deploy-pages runs right after upload-pages-artifact in the same job" | deckt das beobachtete Fehlerbild exakt ab |
| `#455` | PR (offen, unmerged) | „fix: retry artifact discovery after upload" | upstreamer Fix-Vorschlag mit Retry im Lookup |
| `#399` | PR (offen, unmerged) | „Add support for artifact_id input and improve artifact selection for …" | würde Lookup per `artifact_id` ermöglichen |
| `#338` | Issue (offen) | „Allow deploy by artifact_id" | Hintergrund: Re-Run erzeugt zweites `github-pages`-Artifact → „Multiple artifacts"-Fehler |
| `#413` | Issue (offen) | „Bundled punycode DEP0040 warning persists in v5" | die im Fehllauf-Log sichtbare `DEP0040`-Warnung ist bekannt und kosmetisch |
| `#433` | Issue (offen) | „Upgrade artifact client to remove punycode deprecation" | gleicher Komplex |

## Laufzeit-Umgebung im Fehllauf

- Runner-Image `ubuntu-26.04` (Version `20261116.202.1`), Runner `2.338.2`,
  Region `northcentralus`.
- GITHUB_TOKEN-Berechtigungen laut Log: `Actions: read`, `Contents: read`,
  `Metadata: read`, `Pages: write` — deckt sich mit dem
  `permissions`-Block der Datei; `id-token: write` ist gesetzt
  (OIDC für Pages-Deployment).
- Im Fehllauf wurde `configure-pages@v6` geladen (SHA `45bfe019…`); der
  Bump `v5 → v6` (Dependabot PR #102, `630b915`/`00ec4e4`) ist die einzige
  Dateiänderung seit dem letzten Erfolgslauf. Laut Dependabot-Committext
  enthält v6 Runtime-/Dependency-Upgrades; ein belegter Einfluss auf den
  Artifact-Lookup ist nicht dokumentiert.

## Lokale Werkzeug-/Authentifizierungs-Fakten

- `gh version 2.92.0`; authentifizierte Accounts: `softwarewerkstatt`
  (aktiv, per `GH_TOKEN`, u. a. Scope `workflow`, `repo`) und
  `martin-stromberg` (Keyring).
- `.NET SDK 11.0.100` (`global.json` fordert `11.0.100`, rollForward
  `latestPatch`); installierte Workloads: `android`, `ios`,
  `maccatalyst`, `maui-windows` (Version 11.0.100.1).
- Node `v24.15.0`, Python `3.13.14`, `pwsh` vorhanden; `actionlint`
  **nicht** im PATH (Fallback-Prüfungen von `validate-workflows.py`
  wurden verwendet).

## Git-Zustand des Arbeitsstandes

- Branch `task/issue-105-…`, HEAD `00ec4e4` (= Dependabot-Merge PR #102,
  identisch mit `origin/staging`).
- `origin/main` ist einen Commit weiter (`9af51e7`, Promotion #101) —
  der Fehllauf lief auf genau diesem Stand; `pages.yml` ist auf
  `main` und Branch inhaltlich identisch.
- Keine Stashes; einzige uncommittete Änderung: der untracked Ordner
  `docs/features/task/issue-105-…/` (diese Dokumentation).
