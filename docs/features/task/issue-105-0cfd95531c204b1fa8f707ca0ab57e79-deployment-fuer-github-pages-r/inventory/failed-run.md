# Fehlgeschlagener Lauf und Laufhistorie

Fakten zum Fehlschlag des Workflows „GitHub Pages", abgerufen per `gh`-CLI
(Account `softwarewerkstatt`, Token-Scope `workflow` vorhanden).
Vollständiges Job-Log des Fehllaufs:
[pages-run-45541447133-failed-job.log](test-results/pages-run-45541447133-failed-job.log).

## Fehllauf `45541447133` (2026-11-25)

| Feld | Wert |
|------|------|
| Run-ID | `45541447133` (`run_number: 7`, `run_attempt: 1`) |
| Event | `push` auf `main` |
| Head | `9af51e7` „Automated promotion from staging to main (#101)" |
| Erstellt | 2026-11-25T03:39:11Z |
| Ergebnis | `failure` — einziger Job `deploy` (Job-ID `138960689039`) rot |
| Runner | `ubuntu-26.04` (Image 20261116.202.1), Runner-Version `2.338.2`, Region `northcentralus` |

### Schritt-Ergebnisse (per `gh run view --json jobs`)

| Schritt | Conclusion | Zeitraum (UTC) |
|---------|-----------|----------------|
| Set up job | success | 03:39:16–03:39:18 |
| Checkout (`actions/checkout@v7`) | success | 03:39:18–03:39:19 |
| Setup Pages (`actions/configure-pages@v6`) | success | 03:39:19–03:39:20 |
| Upload Pages artifact (`actions/upload-pages-artifact@v5`) | success | 03:39:20–03:39:22 |
| **Deploy to GitHub Pages (`actions/deploy-pages@v5`)** | **failure** | 03:39:22–03:39:22 |
| Post Checkout / Complete job | success | 03:39:22 |

### Zeitlinie aus dem Job-Log (UTC)

| Zeit | Ereignis |
|------|----------|
| 03:39:20.66 | Start `actions/upload-pages-artifact@v5` |
| 03:39:21.54 | „Finished uploading artifact content to blob storage", SHA256 `a8dc414d…3e82` |
| **03:39:21.99** | **„Artifact github-pages successfully finalized. Artifact ID 14989192493"** (371 130 Bytes) |
| 03:39:22.00 | Start `actions/deploy-pages@v5` (Inputs: `artifact_name: github-pages`, `timeout: 600000`, `error_count: 10`, `reporting_interval: 5000`, `preview: false`) |
| 03:39:22.34 | „Fetching artifact metadata for \"github-pages\" in this workflow run" |
| **03:39:22.46** | **„Found 0 artifact(s)"** — ~0,48 s nach der Finalisierung |
| 03:39:22.63 | `##[error]`: „No artifacts named \"github-pages\" were found for this workflow run. Ensure artifacts are uploaded with actions/upload-artifact@v4 or later." (`getArtifactMetadata` in `api-client.js:85` → `deployment.js:74` → `index.js:30`) |

Zusätzlich im Log: `(node:2446) [DEP0040] DeprecationWarning: The
\`punycode\` module is deprecated` — bekannte Warnung von `deploy-pages@v5`
(Upstream-Issue `actions/deploy-pages#413`).

### Artifact-Stand

Das hochgeladene Artifact existiert unverändert im Run:

```json
{"id": 14989192493, "name": "github-pages", "size_in_bytes": 371130,
 "created_at": "2026-11-25T03:39:21Z", "expired": false}
```

(`gh api repos/martin-stromberg/Tankradar/actions/runs/45541447133/artifacts`)

### Aufgelöste Action-Versionen im Fehllauf (Download-Log)

| Action | Aufgelöster SHA |
|--------|-----------------|
| `actions/checkout@v7` | `3d3c42e5aac5ba805825da76410c181273ba90b1` |
| `actions/configure-pages@v6` | `45bfe0192ca1faeb007ade9deae92b16b8254a0d` |
| `actions/upload-pages-artifact@v5` | `fc324d3547104276b827a68afc52ff2a11cc49c9` |
| `actions/deploy-pages@v5` | `368f82528645a54fb793d4d04e342629a3f51346` (= Tag `v5.0.1`) |
| intern: `actions/upload-artifact` | `bbbca2ddaa5d8feaa63e36b76fdaad77386f024f` |

## Vergleich: letzter Erfolgslauf `44993397486` (2026-11-22)

Gleiche Ein-Job-Struktur, einziger fachlicher Unterschied:
`configure-pages@v5` statt `v6`.

| Zeit (UTC) | Ereignis |
|------------|----------|
| 06:22:12.24 | „Artifact github-pages successfully finalized. Artifact ID 14742696354" |
| 06:22:12.55 | „Fetching artifact metadata for \"github-pages\" in this workflow run" (~0,31 s später) |
| 06:22:12.68 | **„Found 1 artifact(s)"** |
| 06:22:19.20 | „Reported success!" |

Fakt: Der zeitliche Abstand zwischen Finalisierung und Metadata-Lookup war
im Erfolgslauf (~0,31 s) und im Fehllauf (~0,48 s) gleichartig klein —
der Unterschied Erfolg/Fehlschlag liegt damit nicht in einem offensichtlich
größeren Abstand, sondern im nicht-deterministischen Zeitpunkt, ab dem der
run-scoped Artifact-Lookup das finalisierte Artifact sieht.

## Laufhistorie des Workflows (alle 7 Runs, `gh run list --workflow pages.yml`)

| Run-ID | Datum (UTC) | Event / Branch | Conclusion | Bemerkung |
|--------|-------------|----------------|-----------|-----------|
| `45541447133` | 2026-11-25 | push / main (`9af51e7`, Promotion #101) | failure | nur Schritt `deploy-pages` rot; Artifact vorhanden |
| `44993397486` | 2026-11-22 | push / main (`aacdb22`, Promotion #92) | success | letzter erfolgreicher Lauf |
| `42077295989` | 2026-11-05 | push / main (`62c17a0`, PR #39) | success | |
| `41915587236` | 2026-11-04 | workflow_dispatch / staging (`a16e5ae`) | success | |
| `41906058228` | 2026-11-04 | workflow_dispatch / staging (`a27f280`) | success | |
| `41905868752` | 2026-11-04 | workflow_dispatch / staging (`a27f280`) | failure | Einrichtungsphase (Issue #34); Log nicht mehr abrufbar |
| `41905399075` | 2026-11-04 | push / staging (`a27f280`, PR #36) | failure | Einrichtungsphase (Issue #34); Log nicht mehr abrufbar |

Die beiden Fehlschläge vom 2026-11-04 stammen aus der Ersteinrichtung
(`pages.yml` triggerte damals noch auf `staging`, Commit `c92832e`); ihre
Logs sind über die API nicht mehr verfügbar (Aufbewahrungsfrist). Es ist
der erste Fehlschlag auf `main` seit der Inbetriebnahme.
