# Dokumentation und Konventionen — Bestand

## Betroffene Dokumentationsstellen (Release-/Verteilungsthemen)

| Datei | Stelle | Inhalt |
|---|---|---|
| `docs/help/ci-cd/workflows.md` | Z. 14, 17 (Workflow-Tabelle „Die elf Workflows"), 44 (Bausteine), 54, 56-57 (Skripte), 81 (Abweichung „Release-Erstellung per gh CLI") | Beschreibt `release.yml` (Version, Reparatur, `release-win-x64.zip` + `update.json`), `staging-ci.yml` (Pre-Release mit Windows-ZIP), `build-and-package`, `resolve-release-version.mjs`, `package-windows.ps1`, `validate-workflows.py` |
| `docs/help/ci-cd/README.md` | Z. 21-23 (Ablauf Schritt 2: GitHub-Pre-Release), 26 (Schritt 4: finales Release), 38-44 (Tabelle „Release-Artefakte": `release-win-x64.zip`, `update.json`, iOS-`.ipa`) | Kernstelle für die Verteilungsbeschreibung |
| `docs/help/ci-cd/einrichtung.md` | Z. 110-113 (Prüf-Checkliste: „Pre-Release `v0.1.0-rc.1` mit `release-win-x64.zip` und `update.json`", „Erster Release auf `main`: `v0.1.0`"), Secrets-Tabelle Z. 58-70, Variablen Z. 77-94 | Einrichtungs-Checkliste |
| `docs/help/ci-cd/versionierung.md` | Z. 7 („Die Release-Notes erzeugt GitHub (`--generate-notes`)"), 20-21 (manueller Tag-Weg), 25-28 (RC-Version „in `update.json`"), 30-33 (Merge-Commit-Pflicht für Tag-Erreichbarkeit) | Tag-Abhängigkeit der Versionierung beschrieben |
| `docs/help/ci-cd/lokaler-pruefung.md` | Z. 23, 37 (`-Package` ↔ „Build and package") | Lokaler Prüflauf |
| `docs/help/ci-cd/ios-deployment.md` | komplett | Lokales iOS-Deployment/TestFlight via `iOS-Deployment.ps1`; kein GitHub-Release-Bezug |
| `docs/help/Preisdaten/api-schluessel.md` | Z. 36-48 (Tabelle „Enthält der Schlüssel das Artefakt?" — Windows-Paket Nein, iOS-`.ipa` Ja), **Z. 54-56 (bereits vorhandener Sicherheitshinweis: Build-Schlüssel ist aus der Assembly lesbar; für iOS unvermeidbar; eigener, jederzeit rotierbarer Schlüssel empfohlen)** | Wichtigste vorhandene Risikodokumentation — Ausgangspunkt für das Threat-Model |
| `README.md` | Z. 111-115 (Docs-Verzeichnis inkl. `docs/adr/`, `docs/help/ci-cd/` „Pipeline, Versionierung, Releases"), Z. 122-127 („API-Schlüssel und Testmodus") | Keine Download-/Release-Links; nur Verweise |
| `docs/RELEASE_NOTES.md` | gesamte Datei | Zweisprachig (EN/DE), Abschnitte „Important Notes Before Update"/„What's New" — redaktionell gepflegte Notes **zusätzlich** zu den generierten GitHub-Notes |
| `changes.log` | fortlaufend | Format `## YYYY-MM-DD — Titel (Issue #N)`, Bullet-Liste der Änderungen; Z. 356 dokumentiert die bisherige Release-Form |
| `pages/` | `pages/README.md`, `index.html:79` | Statische Präsentation; verlinkt nur „Quellcode auf GitHub" — **keine Download-/Release-Links** → unberührt |

## ADR-Konventionen (`docs/adr/`)

- **Nummerierung/Name:** `NNNN-kebab-case-titel.md`, fortlaufend; letzter ADR `0022-android-backup.md` → nächste Nummer **0023**. Kein Index/TOC — ADRs werden aus `README.md:111` und Kontext-Docs verlinkt.
- **Format** (aus `0015`, `0020`, `0022`): H1 `# ADR NNNN: Titel`; `## Status` („Angenommen (JJJJ-MM-TT)", ggf. mit Teil-A/B-Gliederung); `## Kontext` (Issue-/Befund-Bezug, Datenkarte); `## Entscheidungen` (Stichpunkte mit Fett-Begriffen und Datei-/Zeilenverweisen); ggf. weitere Abschnitte (z. B. „Teil B: Befunde"); `## Folgen`.
- **Thematisch benachbarte ADRs:** `0015-ios-delivery-and-privacy.md` (iOS-Auslieferung, Privacy), `0020-build-und-actions-governance.md` (Build-/Actions-Governance — enthält die `.ipa`-Verbots- und Schlüsselregeln der Validierung).

## Konventionen für Aufgaben-Dokumentation (`docs/features/task/`)

Pro Aufgabe ein Verzeichnis `docs/features/task/<branch-suffix>/` mit `requirement.md`, `inventory.md` + `inventory/` (Detaildocs + `test-results/`), `plan.md`, `test-results.md`, `review*.md` (Vorbild: `issue-58-…-automatisierung-wiederkehrende/`). Ältere Aufgaben liegen teils als Einzeldatei `<…>-tasks.md` vor.

## Recherche-Fakten zum Entscheidungskontext

- Issue #64 (Befund aus Review #62) nennt drei Optionen: benutzergebundener Schlüssel, Backend/Proxy, **oder bewusste Risikoentscheidung mit dokumentierter Rotation und Missbrauchsbeobachtung**. Die Owner-Entscheidung (`issue.md:37`): „Wir entfernen … die Ablage der releases aus GitHub und laden die App nur noch in den App Store."
- Akzeptanzkriterium: Threat-Model + Rotationsweg + Betreiberhinweise dokumentiert — Teile davon existieren schon in `api-schluessel.md:54-56` (Extrahierbarkeit, Rotation); ein Threat-Model und Betreiberhinweise zur Missbrauchsbeobachtung fehlen noch.
