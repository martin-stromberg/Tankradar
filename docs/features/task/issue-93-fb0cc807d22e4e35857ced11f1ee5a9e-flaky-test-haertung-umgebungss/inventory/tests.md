# Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt:** 2026-11-22, 23:27–23:50 Uhr CET (22:27–22:50 UTC)
- **Branch / Commit:** `task/issue-93-fb0cc807d22e4e35857ced11f1ee5a9e-flaky-test-haertung-umgebungss`
  @ `f2192610882ea5d91017deb31fcbd88586c343c7` („Backmerge from main to staging (#95)"), Basisbranch `staging`
- **Uncommittete Änderungen im getesteten Stand:** ausschließlich das neue, unversionierte
  Verzeichnis `docs/features/task/issue-93-…/` (requirement.md, todo.md, diese Inventory-Dateien);
  keinerlei Änderungen an `src/`, `.github/`, `scripts/` oder Projektdateien.
- **Testumgebung:** Windows 11 (DESKTOP-CM8OBSG), .NET SDK **11.0.100** (`global.json`:
  `11.0.100`, rollForward `latestPatch`), MAUI-Workloads `maui-windows`/`ios`/`maccatalyst`/`android`
  installiert; Node.js (npm test), Python 3 + PyYAML, pwsh vorhanden; `actionlint` **nicht** im PATH.
- **Build vor den Testläufen:** `dotnet restore Tankradar.sln -p:Configuration=Release` +
  `dotnet build Tankradar.sln --configuration Release --no-restore` mit
  `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false`
  (Simulation der Windows-Jobs der CI) → erfolgreich, 0 Warnungen/0 Fehler.
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Job `build-and-test` in
  `.github/workflows/pr-staging-ci.yml`/`staging-ci.yml` (drei `dotnet test`-Schritte,
  TRX-Logger), `scripts/local-ci.ps1` (npm test, Workflow-Validierung, iOS-Skriptprüfung,
  Coverage) und `package.json` (`npm test` = `node --test scripts/*.test.mjs`).
- **Unterschiede zur CI-Umgebung (lokal):** UIA-Timeout 15 s statt 60 s
  (`E2EStartupPolicy.ResolveUiaTimeout` greift auf `GITHUB_ACTIONS`/`CI` zu — beide lokal
  ungesetzt); E2E-Fenstermodus `offscreen` (Default, `TANKRADAR_E2E_WINDOW` ungesetzt —
  die CI nutzt per Repo-Variable `foreground`); keine `DOTNET_Dbg*`-Variablen; angemeldete
  interaktive Windows-Sitzung.

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory <test-results> --logger "trx;LogFileName=test-results-unit.trx"` | Repo-Root | 0 | 2164 | 0 | 0 | [TRX](test-results/test-results-unit.trx), [Log](test-results/dotnet-test-unit.log) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory <test-results> --logger "trx;LogFileName=test-results-integration.trx"` | Repo-Root | 0 | 311 | 0 | 0 | [TRX](test-results/test-results-integration.trx), [Log](test-results/dotnet-test-integration.log) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --results-directory <test-results> --logger "trx;LogFileName=test-results-e2e.trx"` | Repo-Root | 0 | 184 | 0 | 0 | [TRX](test-results/test-results-e2e.trx), [Log-Tail](test-results/dotnet-test-e2e-tail.log) |
| Pipeline-Skripte (Node) | `npm test` | Repo-Root | 0 | 32 | 0 | 0 | [Log](test-results/npm-test.log) |
| Workflow-Validierung | `python scripts/validate-workflows.py` | Repo-Root | **1** | – | 3 Befunde (kein Test-Count) | – | [Log](test-results/validate-workflows.log) |
| Tests der Workflow-Validierung | `python scripts/test_validate_workflows.py` | Repo-Root | **1** | 13 | 1 | 0 | [Log](test-results/python-validate-workflows-tests.log) |
| iOS-Deployment-Skript | `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | Repo-Root | 0 | alle OK (Testanzahl nicht numeriert) | 0 | 0 | [Log](test-results/test-ios-deployment.log) |

Hinweise: TRX-Counters (maschinell geprüft): Unit `total=2164, executed=2164, passed=2164`;
Integration `total=311, executed=311, passed=311`; E2E `total=184, executed=184, passed=184`.
E2E-Gesamtdauer laut Testlauf: 18,5 min (seriell, `DisableTestParallelization`).
Im E2E-Log einmal die Konsolenmeldung `Application failed to exit` (FlaUI-`Close()`-Zeitüberschreitung;
Teardown beendet den Prozess anschließend per Prozessbaum-Kill — Test trotzdem bestanden).

## Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|------------------------|-------------------|----------------------------|-------------------|
| `ReleaseCommandTests.test_workflows_in_repository_are_clean` | `scripts/test_validate_workflows.py` (Z. 80–86) | `AssertionError: Lists differ: [] != [...]` — 2 Befunde: Schritt „Upload E2E crash dumps" lädt `e2e-dumps/` hoch („das lädt ein Verzeichnis hoch, das eine .ipa enthalten könnte") in `pr-staging-ci.yml` und `staging-ci.yml` (Job `build-and-test`). Ursache: `e2e-dumps` fehlt in `SAFE_UPLOAD_DIRS` (`scripts/validate-workflows.py` Z. 41). | `python scripts/test_validate_workflows.py` → [Log](test-results/python-validate-workflows-tests.log) |

Dieselbe Ursache lässt `python scripts/validate-workflows.py` mit Exit-Code 1 enden; der
direkte Lauf meldet **3** Befunde (die zwei `e2e-dumps`-Meldungen plus
„`staging-to-main-promotion.yml`: `workflow_run` referenziert nicht den Anzeigenamen von
`staging-ci.yml` ('Pre-Release')"). Der dritte Befund erscheint nur im Skriptlauf, weil die
Namensprüfung in `main()` steht und der Test nur `check_workflow`/`check_action` je Datei ruft.
Hintergrund des dritten Befunds: `staging-to-main-promotion.yml` triggert seit einer früheren
Umstellung auf `push`→`staging` (`"on": push`), der Validator prüft weiterhin
`workflow_run.workflows` (`scripts/validate-workflows.py` Z. 226–232).

**Beide Fehler sind preexisting** (unveränderter Basisstand `f219261`; der `e2e-dumps`-Befund
ist bereits im Issue-#58-Inventory dokumentiert). Kein Fehlschlag in den drei .NET-Testsuiten.

## Testlücken und Ausführungsprobleme

- **Kein Testfehlschlag in den .NET-Suiten**; alle 2659 ausgeführten Tests bestanden.
- **Nicht ausgeführte Prüfungen** (keine Tests, aber Teil der Pipeline): `dotnet format
  --verify-no-changes`, `dotnet build -p:TreatWarningsAsErrors=true` (als separater
  TreatWarningsAsErrors-Lauf; der Release-Build oben lief ohne das Flag),
  `check-vulnerabilities.mjs` (Sicherheitsprüfung, benötigt nuget.org), Coverage-Schwellwert
  (`check-coverage.mjs`, Cobertura-Reports liegen aber unter `test-results/<guid>/coverage.cobertura.xml`),
  iOS-Compile-Prüfung (macOS/iOS-Workload-lokal vorhanden, aber nicht ausgeführt — kein Test),
  `pre-push`-Hook-End-to-End-Lauf.
- **Übersprungene Tests:** keine (TRX `notExecuted=0` in allen drei Suiten).
- **`actionlint` nicht installiert:** `validate-workflows.py` führte nur die eingebauten
  Prüfungen aus (Hinweiszeile im Log).
- **Infrastruktur-Anmerkung:** E2E lief in einer interaktiven Sitzung im Off-Screen-Modus;
  in der CI läuft derselbe Schritt mit `TANKRADAR_E2E_WINDOW=foreground` und 60-s-UIA-Timeout.

## Testklassen (relevant für die Anforderung)

### `Tankradar.Tests.E2E` (alle FlaUI, `E2ETestBase`-Hierarchie)
- ~110 konkrete `*E2ETests_*`-Klassen — UI-Automation gegen die echte App; jede Klasse
  startet eine App-Instanz. Details: [test-infrastructure.md](test-infrastructure.md).
- `StartFailureCleanupE2ETests` — Meta-Test: `Dispose` nach abgebrochenem Start wirft nicht.
- `DiagnosticsCaptureE2ETests` — Meta-Test der Diagnoseerfassung (echte App).

### `Tankradar.Tests.Integration`
- `ApiKeyBuildTests_Log`, `ApiKeyBuildTests_Sources` — echte `dotnet build`-Kindprozesse (`ApiKeyBuildRunner`).
- `LoopbackHttpListenerTests_Retry` — Port-Kollisions- und Parallelstart-Verhalten.
- `*MockServerTests_*` (Search/Address/Area/AutoSearch/Geocoding/PriceRefresh/PriceService/RouteSearch/TileSource) — echte Loopback-`HttpListener` über `SearchMockServerTestBase`/`RouteSearchMockServerTestBase`.
- `BackupServiceTests_*` (14 Klassen, `[Collection("BackupRestoreSharedPools")]`) — echte SQLite-/Datei-Integration.
- `TestDataContextTests_Lifecycle`, `TestDatabaseTests_Pooling`, `MigrationsTests_*`, `*MigrationTests_*`, `*ServiceTests_RealDatabase`, `*PrivacyTests_*` — echte SQLite-Dateien in Temp-Verzeichnissen.

### `Tankradar.Tests.Unit`
- `TransientRetryTests_Behavior`, `TransientWaitTests(_ElementNotAvailable)`, `E2EStartupPolicyTests_Backoff` — Unit-Abdeckung der Retry-/Startup-Helfer (ohne OS-Abhängigkeit).
- `MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`, `OpenRouteServiceKeyCheckerTests` — echte Loopback-`HttpListener` **im Unit-Projekt**.
- `E2EInputPolicyTests_NoFocus` — Quelltext-Konventionstest (Muster für einen Meta-Test).
- `EnvironmentCollection`-Tests (`AppDataPathProviderTests_DataDirectoryResolution`, `TestModeVariableTests_Rename`) — serialisierte env-sensitive Tests.

## Hilfsmethoden (relevant für die Anforderung)

- `TransientRetry.Run` / `TransientRetry.IsTransient` — begrenzte UIA-Timeout-Wiederholung.
- `TransientWait.Until` (+ `timeoutTime`-Überladung) — Poll bis Frist, UIA-Timeout/`ElementNotAvailable` = „noch nicht erfüllt".
- `E2EStartupPolicy.ResolveUiaTimeout`/`StartWithRestart`/`WaitUntilReady` — UIA-Timeout-Auflösung (15 s lokal/60 s CI/Env-Override), max. 2 App-Starts, Bereitschaftswarten.
- `LoopbackHttpListener.Start` — freier Loopback-Port + 10× Retry bei `HttpListenerException`.
- `MockTankerkoenigServer`/`MockNominatimServer`/`MockOpenRouteServiceServer`/`MockTileServer` — Loopback-Mocks mit Anfrageprotokoll/Fehlerfolgen.
- `E2EDiagnostics.Capture`/`DumpTree`/`ResolveDirectory` — Diagnoseartefakte je Fehlschlag.
- `E2ETestBase.CloseApplicationAndWait`/`DiscardFailedStart` — `Kill(entireProcessTree: true)` bereits vorhanden.
- `TestDataContext`/`TestDatabase`/`TempDirectory`/`ApiKeyBuildRunner` — Temp-Verzeichnis-Lebenszyklen mit unterschiedlich harter Löschlogik (Details: [audit.md](audit.md)).
