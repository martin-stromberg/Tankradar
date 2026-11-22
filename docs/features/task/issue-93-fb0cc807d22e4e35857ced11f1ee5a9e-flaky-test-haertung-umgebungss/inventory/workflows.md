# Workflows, CI-Skripte und lokale Prüfkette

## `.github/workflows/pr-staging-ci.yml` (Name: „PR CI for Staging")

Trigger: `pull_request` → `staging` (opened/synchronize/reopened).
Concurrency `pr-staging-<nr>`, `cancel-in-progress: true`. Permissions: `contents: read`, `checks: write`.

| Job | Runner | Timeout | Rolle |
|-----|--------|---------|-------|
| `detect-backmerge` | ubuntu-latest | – | Erkennt reine main→staging-Rückführungen (`scripts/detect-backmerge.mjs`); Output `is_backmerge` |
| `static-checks` | windows-latest | 30 min | npm test, iOS-Skriptprüfung (`test-ios-deployment.ps1`), `dotnet format --verify-no-changes --severity error`, Security-Scan (Composite Action), `dotnet build -p:TreatWarningsAsErrors=true` |
| `build-and-test` | windows-latest | 45 min | siehe unten |
| `ios-build` | xcode-27 | 60 min | Unsignierter iOS-Compile (Composite `package-ios`), **keine Tests** |
| `back-merge-skip` | ubuntu-latest | – | Platzhalter bei Backmerge |

### Job `build-and-test` — Schritte (Zeilen 106–224)

1. Checkout, `setup-dotnet` 11.0.1xx, `setup-node` 24, `dotnet workload restore Tankradar.sln`,
   `dotnet restore -p:Configuration=Release`, `dotnet build --configuration Release --no-build`-Vorbereitung.
2. **`Test unit (blocking)`** (Z. 144–148): `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults --logger "trx;LogFileName=test-results-unit.trx"` — **kein `--filter`**.
3. **`Test integration (blocking)`** (Z. 150–154): analog `src/Tankradar.Tests.Integration`, `test-results-integration.trx`.
4. ReportGenerator 5.5.11 → `coverage-report/`; `node scripts/check-coverage.mjs coverage-report/Summary.txt $COVERAGE_THRESHOLD` (`COVERAGE_THRESHOLD: '70'` Job-Env).
5. `Prepare E2E crash dump directory` (`mkdir -p e2e-dumps`, Issue #22).
6. **`Test E2E with FlaUI (blocking)`** (Z. 169–186): `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --results-directory TestResults --logger "trx;LogFileName=test-results-e2e.trx"` — **kein Coverage-Collector, kein Filter**. Schritt-Env: `TANKRADAR_E2E_WINDOW` (Repo-Variable, Default `offscreen`), `DOTNET_DbgEnableMiniDump=1`, `DOTNET_DbgMiniDumpType=2`, `DOTNET_DbgMiniDumpName=.../e2e-dumps/tankradar-dump-%p.dmp`. Kommentar (Z. 170–177): „bewusste Abweichung von der CI-Vorlage" — E2E blockierend statt best-effort.
7. Artefakte (`if: always()`): `coverage-report-pr`, `test-results-pr` (`TestResults/*.trx`, 14 d), `e2e-diagnostics-pr` (`e2e-diagnostics/`, 14 d), `e2e-dumps-pr` (`e2e-dumps/`, 7 d).

Job-Env der Tests: `IncludeAndroidTarget=false`, `IncludeIosTarget=false`,
`IncludeMacCatalystTarget=false` (nur Windows-Ziel).

## `.github/workflows/staging-ci.yml` (Name: „Pre-Release")

Trigger: `push` → `staging`. Job `build-and-test` (Z. 99–216) ist **identisch** zu
`pr-staging-ci.yml` bis auf Artefaktnamen (`*-staging` statt `*-pr`). Danach:
`version` (`needs: [detect-backmerge, static-checks, build-and-test]`, Conventional-Commits-Versionierung),
`prerelease` (Windows-Paket + GitHub-Pre-Release), `ios-prerelease` (signiert/unsigniert).
**Wichtig für Lane-Trennung:** `version` hängt an `build-and-test` — ein neuer OS-Job darf
nicht in diese `needs`-Kette eintreten.

## `.github/workflows/release.yml` (Name: „Release")

Trigger: `push` → `main` oder Tag `v*.*.*`. Schritt **`Release gate - regular tests`**
(Z. 75–81, nur wenn `released == 'true' && release_action == 'create'`):

```bash
dotnet test src/Tankradar.Tests.Unit --configuration Release
dotnet test src/Tankradar.Tests.Integration --configuration Release
```

- Läuft **ohne** `--no-build` (Release-Build inkl. Tests; Kommentar: hartkodierte
  Debug-Pfade in Testhelfern würden hier auffallen, Vorlage 11.6).
- **Kein** E2E-Aufruf, **kein** Coverage, **kein** TRX-Logger, **kein** Filter.
- Fällt bei einer `OsInterface`-Markierung von Integrationstests fachlich in den Scope der
  Filterfrage (im Issue nur pr-/staging-CI genannt).

## `.github/workflows/verify-pr-source.yml`, `pages.yml`, Review-Workflows

`verify-pr-source.yml` (PR → main): Quellprüfung, keine Tests. `pages.yml`: Pages-Deploy, keine Tests.
Die drei `review-*.yml` nutzen die Composite Action `.github/actions/create-review-issue`
(Label idempotent per `gh label create --force`, Dedup per `gh issue list --state open --label`,
Issue per `gh issue create`, Titel + `date -u`-Datumsteil, Body beginnt mit `@softwarewerkstatt`;
benötigt `issues: write` + Checkout). Referenzmuster für ein künftiges Quarantäne-Issue.

## `scripts/local-ci.ps1` (258 Zeilen)

Lokale Spiegelung der Pipeline (parametrisierbar: `-SkipE2E`, `-E2EForeground`,
`-SkipSecurityScan`, `-Package`, `-PackageVersion`, `-CoverageThreshold`).

- `Invoke-Step` (Z. 64–90) mit optionalem `-BestEffort`-Flag — **wird derzeit nirgends
  genutzt**; alle Schritte sind blockierend oder übersprungen.
- Setzt Windows-Job-Simulation (`Include*Target=false`/`IncludeWindowsTarget=true`),
  stellt Env am Ende wieder her (`finally`).
- Testschritte (Z. 164–170): `dotnet test src/Tankradar.Tests.{Unit,Integration} --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults --logger "trx;LogFileName=test-results-<p>.trx"` — **kein Filter**.
- E2E-Schritt (Z. 186–199): `dotnet test src/Tankradar.Tests.E2E --no-build`, TRX
  `test-results-e2e.trx`; setzt `TANKRADAR_E2E_WINDOW=foreground` nur bei `-E2EForeground`.
- Weiter: `npm test`, `validate-workflows.py` + `test_validate_workflows.py`,
  `test-ios-deployment.ps1` (2×, einmal mit Windows-Job-Env), Format-Check,
  `check-vulnerabilities.mjs`, Warnungen-als-Fehler-Build, Coverage-Schwelle (70),
  optional Paket, iOS-Compile-Prüfung.

## `coverlet.runsettings` (Repo-Root)

XPlat-Code-Coverage, Format `cobertura`, `Include=[Tankradar.MAUI]*`, Ausschlüsse:
`obj/`, `*.g.cs`, `*.g.i.cs`, `Platforms/`, `*.xaml.cs`, `MauiProgram.cs`,
`*AutoInitializer.cs`, `Views/TankradarContentPage.cs`, `Views/Charts/`; `SkipAutoProps`.
Gilt für Unit- und Integrationsschritte (E2E ohne Collector).

## `scripts/validate-workflows.py` (+ `test_validate_workflows.py`)

Prüft alle `.github/workflows/*.yml` und `.github/actions/*/action.yml`: gültiges YAML,
Trigger/Jobs/`runs-on`/`steps|uses`, `needs`-Referenzen, Existenz lokaler Actions und
aufgerufener `scripts/`-Dateien, `workflow_run`-Namensgleichheit `staging-to-main-promotion.yml`
↔ `staging-ci.yml`-Anzeigename, `.ipa`-Upload-Verbot (`SAFE_UPLOAD_DIRS =
{coverage-report, TestResults, e2e-diagnostics}`, `SAFE_FILE_EXTENSIONS`), Schlüssel-Politik
(kein Tankerkönig-Schlüssel in Windows-Paketschritten). Optional `actionlint` im PATH.

**Ausgangszustand: schlägt mit Exit-Code 1 fehl (preexisting, 3 Befunde):**

1. `pr-staging-ci.yml` `build-and-test` Schritt „Upload E2E crash dumps": `e2e-dumps/` fehlt in `SAFE_UPLOAD_DIRS` (bekannter Alt-Befund, bereits im Issue-#58-Inventory dokumentiert).
2. `staging-ci.yml` `build-and-test` Schritt „Upload E2E crash dumps": derselbe Befund.
3. `staging-to-main-promotion.yml`: `workflow_run` referenziert nicht den Anzeigenamen von `staging-ci.yml` (`'Pre-Release'`) — der Workflow triggert inzwischen auf `push`→`staging` (`"on": push`, Z. 13–15), der Validator prüft aber weiter `workflow_run.workflows` (Z. 226–232). Ebenfalls preexisting auf dem Basisstand.

Folge: `test_validate_workflows.py::ReleaseCommandTests.test_workflows_in_repository_are_clean`
schlägt fehl (1 von 14 Tests). **Relevant für die Umsetzung:** Die Anforderung verlangt
„`validate-workflows.py` muss die geänderten Workflows weiterhin bestehen" — der aktuelle
Stand ist bereits rot; neue Befunde dürfen nicht hinzukommen (Umgang mit dem Altbestand ist
eine Umsetzungsfrage).

## `.githooks/test-execution-check.py` (pre-push)

`dotnet build` + `dotnet test --no-build` für alle Testprojekte mit „tests" im Namen,
**ohne** E2E (Default; `PRE_PUSH_E2E=1` testet die ganze Solution inkl. E2E).
Gesamt-Timeout 10 min (`TEST_TIMEOUT_SECONDS`), `HOOK_SKIP_TESTS=1` Notfall-Aus.
Kein Filter — ein `Category=OsInterface`-Filter würde hier fachlich ebenfalls greifen,
sofern Integrationstests markiert werden (Unit/Integration laufen weiter im Hook).

## Paket-/Versionsskripte (nicht testrelevant)

`check-coverage.mjs` (Schwellwert), `check-vulnerabilities.mjs`, `detect-backmerge.mjs`,
`determine-next-version.mjs`, `resolve-release-version.mjs`, `versioning.mjs`,
`package-windows.ps1`, `create-review-version.ps1`, `iOS-Deployment.ps1` +
`test-ios-deployment.ps1` (Syntax-/Hilfe-/Abbruchprüfung), `test_validate_workflows.py`.

## Nicht vorhandene Skripte (für die Anforderung relevant)

- `scripts/Retry-FlakyTests.ps1` — nicht vorhanden
- `scripts/Analyze-FlakyTestResults.ps1` — nicht vorhanden
- Kein TRX-Auswertungs-/Retry-/Statistik-Mechanismus; TRX-Dateien werden nur als Artefakte hochgeladen.
