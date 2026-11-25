# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-25, ca. 11:22–11:25 Uhr `+0100`
  (lokale Rechnerzeit).
- **Branch und Commit-ID:** `task/issue-105-0cfd95531c204b1fa8f707ca0ab57e79-deployment-fuer-github-pages-r`,
  HEAD `00ec4e4ce6da8b88ed42c7bdb0031ff726a59d74` (Dependabot-Merge PR #102;
  identisch mit `origin/staging`; `origin/main` einen Commit weiter,
  `9af51e7`, inhaltlich gleiche `pages.yml`).
- **Uncommittete Änderungen im getesteten Stand:** keine verfolgten
  Änderungen; einziges Untracked-Verzeichnis
  `docs/features/task/issue-105-…/` (die hier erstellte Dokumentation
  samt Nachweisen). Keine Stashes.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows-Arbeitsplatz, Git
  Bash; `.NET SDK 11.0.100` mit Workloads `android`, `ios`,
  `maccatalyst`, `maui-windows`; Node `v24.15.0`; Python `3.13.14`;
  `pwsh` vorhanden; `actionlint` nicht im PATH; `gh 2.92.0`
  (Account `softwarewerkstatt`).
- **Ermittelte Testsuiten und Quellen der Testbefehle:** die Befehle sind
  den CI-Workflows `.github/workflows/pr-staging-ci.yml` und
  `staging-ci.yml` sowie `scripts/local-ci.ps1` entnommen (dort
  dokumentierte lokale Entsprechung der Pipeline):
  `npm test` (package.json: `node --test "scripts/*.test.mjs"`),
  `python scripts/test_validate_workflows.py`,
  `python scripts/validate-workflows.py`,
  `pwsh -NoProfile -File scripts/test-ios-deployment.ps1`,
  `dotnet test` der Projekte `Tankradar.Tests.Unit`,
  `Tankradar.Tests.Integration`, `Tankradar.Tests.E2E` mit dem
  CI-Filter `Category!=OsInterface`. Zuvor `dotnet restore` +
  `dotnet build` (Release, `--no-restore`) mit denselben
  Zielframework-Ausblendungen wie die CI-Windows-Jobs
  (`IncludeAndroidTarget=false`, `IncludeIosTarget=false`,
  `IncludeMacCatalystTarget=false`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Pipeline-Skript-Tests (Node) | `npm test` (= `node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 32 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |
| Workflow-Validierungstests (Python) | `python scripts/test_validate_workflows.py` | Repo-Root | 0 | 14 | 0 | 0 | [python-test-validate-workflows.log](test-results/python-test-validate-workflows.log) |
| Workflow-Strukturprüfung | `python scripts/validate-workflows.py` | Repo-Root | 0 | — (Check, keine Testzählung; „OK: 11 Workflows und alle lokalen Actions … gültig") | — | — | [validate-workflows.log](test-results/validate-workflows.log) |
| iOS-Deployment-Skriptprüfung | `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | Repo-Root | 0 | — (Check-Skript; alle Prüfpunkte „OK", Endmeldung „iOS-Deployment-Pruefung erfolgreich") | — | — | [test-ios-deployment.log](test-results/test-ios-deployment.log) |
| .NET Build (Voraussetzung) | `dotnet build Tankradar.sln --configuration Release --no-restore` (mit `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false`) | Repo-Root | 0 | — (0 Warnungen, 0 Fehler, ~26 s) | — | — | Konsolenausgabe, kein Report |
| .NET Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --filter "Category!=OsInterface" --settings coverlet.runsettings --collect:"XPlat Code Coverage"` | Repo-Root | 0 | 2155 | 0 | 0 | [test-results-unit.trx](test-results/test-results-unit.trx) |
| .NET Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --filter "Category!=OsInterface" …` | Repo-Root | 0 | 199 | 0 | 0 | [test-results-integration.trx](test-results/test-results-integration.trx) |
| .NET E2E | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --filter "Category!=OsInterface"` (`TANKRADAR_E2E_WINDOW=offscreen`) | Repo-Root | 0 | 2 | 0 | 0 | [test-results-e2e.trx](test-results/test-results-e2e.trx) |

Gesamt: **2402 .NET-Tests + 32 Node-Tests + 14 Python-Tests ausgeführt,
alle erfolgreich**; beide Prüfskripte mit Exit-Code 0.

### Nachgewiesene bestehende Testfehler

Keine. Alle ausgeführten Läufe endeten mit Exit-Code 0 und ohne
Fehlschläge. (Der in einer früheren Bestandsaufnahme — Issue #58 —
dokumentierte `e2e-dumps`-Befund von `validate-workflows.py` ist in der
Zwischenzeit behoben: `e2e-dumps` steht inzwischen in `SAFE_UPLOAD_DIRS`,
`scripts/validate-workflows.py:41`.)

### Testlücken und Ausführungsprobleme

- **OS-Interface-Lane nicht ausgeführt** (`--filter "Category=OsInterface"`
  für alle drei .NET-Testprojekte): In der CI bewusst als best-effort-Job
  `os-interface-tests` mit `continue-on-error: true` geführt
  (`staging-ci.yml` Zeilen 228–346, Issue #93); enthält u. a.
  FlaUI-Desktop-Tests mit echter App-Prozess-Steuerung. Lokal wegen
  Aufwand und fehlender Blockierwirkung nicht gelaufen — der
  Ausgangszustand dieser Lane ist **unbekannt** und darf nicht als
  fehlerfrei gewertet werden.
- **Format- und Security-Scan-Schritte der CI nicht ausgeführt**
  (`dotnet format --verify-no-changes`, Composite-Action
  `security-scan`): statische Checks, keine Testsuiten; der Security-Scan
  benötigt nuget.org-Zugriff.
- **`actionlint` nicht verfügbar:** `validate-workflows.py` lief nur mit
  den eingebauten Prüfungen (Hinweis im Log); tiefergehende
  actionlint-Befunde sind damit nicht ausgeschlossen.
- **Kein automatisierbarer Test für das eigentliche Fehlerbild:** Der
  Race zwischen `upload-pages-artifact` und `deploy-pages` entsteht nur
  auf einem GitHub-Runner; kein vorhandener Test deckt das Verhalten von
  `pages.yml` ab. `validate-workflows.py` prüft Struktur (Trigger, Jobs,
  `needs`-Referenzen, Skript-/Action-Existenz, .ipa-Policy), nicht das
  Laufzeitverhalten. Die Validierung der Korrektur kann nur über den
  tatsächlichen Workflow-Lauf erfolgen.
- Die Logs der beiden Einrichtungs-Fehlläufe vom 2026-11-04 sind nicht
  mehr abrufbar (Aufbewahrungsfrist überschritten).

## Testklassen

Es existiert **keine Testklasse und kein Test**, der `pages.yml` oder das
Pages-Deployment fachlich abdeckt. Für den betroffenen Bereich (CI/CD)
relevant sind folgende bestehende Testartefakte:

### `scripts/test_validate_workflows.py` (unittest, 14 Tests)

- `UploadArtifactTests` — Prüfung der .ipa-/Upload-Pfad-Policy von
  `check_upload_artifact` (sichere Verzeichnisse, .ipa-Namen, Muster,
  Variablen, Pfadausbrüche).
- `ReleaseCommandTests` — Prüfung von `gh release`-Kommandozeilen;
  `test_workflows_in_repository_are_clean` wendet `check_workflow` auf
  alle 11 Workflow-Dateien an (struktureller Regressionstest für
  `pages.yml`-Änderungen).
- `WindowsPackageKeyPolicyTests` — Key-Policy für
  `build-and-package`-Action und `package-windows.ps1`.

### `scripts/*.test.mjs` (node:test, 32 Tests)

- `check-coverage.test.mjs`, `check-vulnerabilities.test.mjs`,
  `detect-backmerge.test.mjs`, `resolve-release-version.test.mjs`,
  `versioning.test.mjs` — decken die CI-Hilfsskripte ab; kein Bezug zu
  `pages.yml`.

### Hilfsmethoden

- `scripts/validate-workflows.py` — wiederverwendbare Prüffunktionen
  (`check_workflow`, `check_run_steps`, `check_key_policy` u. a.);
  `check_workflow` validiert bei einer `pages.yml`-Änderung u. a. die
  `needs`-Referenzen eines etwaigen Zwei-Job-Layouts automatisch mit.
