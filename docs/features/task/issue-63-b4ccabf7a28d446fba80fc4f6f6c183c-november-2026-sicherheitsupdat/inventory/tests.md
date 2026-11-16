# Tests — Lage und Ausführung

## Testprojekte

| Projekt | Dateien (`.cs`) | Framework/TFM | Besonderheiten |
|---|---|---|---|
| `src/Tankradar.Tests.Unit/` | ~350 | xUnit, `net10.0-windows10.0.19041.0` | `ProjectReference` → `Tankradar.MAUI`; In-Memory-SQLite (`InMemoryDbContextFactory`); `EnableMaui*Processing=false` gegen Resizetizer-Konflikte (csproj Z. 10–15) |
| `src/Tankradar.Tests.Integration/` | ~68 | xUnit, gleicher TFM | reale SQLite-Dateien (`Integration/Support/TestDatabase.cs`), Migrations-Upgrade-Tests, Mock-HTTP-Server aus `TestSupport` |
| `src/Tankradar.Tests.E2E/` | ~113 | xUnit + FlaUI 5.0 (`FlaUI.Core`/`FlaUI.UIA3`) | startet die **gebaute** App-Exe; isolierte Test-DB via `Microsoft.Data.Sqlite` (direkte Referenz, zu aktualisieren!) |

Gemeinsame Test-Hilfen in `src/TestSupport/` (kein eigenes Projekt; per
`<Compile Include Link>` verteilt — u. a. `MockTankerkoenigServer`,
`MockNominatimServer`, `MockOpenRouteServiceServer`, `MockTileServer`,
`LoopbackHttpListener`, `TransientRetry`, `E2EStartupPolicy`).

## Ausführung in der CI (PR-CI und Staging-CI identisch)

```bash
dotnet restore Tankradar.sln -p:Configuration=Release
dotnet build Tankradar.sln --configuration Release --no-restore

dotnet test src/Tankradar.Tests.Unit        --configuration Release --no-build \
  --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults \
  --logger "trx;LogFileName=test-results-unit.trx"

dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build \
  --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults \
  --logger "trx;LogFileName=test-results-integration.trx"

# Coverage-Gate: ReportGenerator 5.5.11 → scripts/check-coverage.mjs, Schwelle 70 %

dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build \
  --results-directory TestResults --logger "trx;LogFileName=test-results-e2e.trx"
```

Env für Windows-Jobs: `IncludeAndroidTarget=false IncludeIosTarget=false
IncludeMacCatalystTarget=false` (Restore/Build nur Windows-TFM).

## E2E-Besonderheiten (FlaUI/UI-Automation)

- Die Tests starten `Tankradar.MAUI.exe` aus
  `src/Tankradar.MAUI/bin/<Config>/net10.0-windows10.0.19041.0/win-x64/`
  (`E2ETestBase.cs` Z. 22) — **`dotnet build` der Solution ist Vorbedingung**;
  fehlt die Exe, meldet der Test einen entsprechenden Hinweis (Z. 588).
- App läuft im Testmodus **Off-Screen** (`TANKRADAR_E2E_WINDOW=offscreen`,
  Default in CI; Rückfall `foreground` via Repo-Variable bzw. lokal
  `local-ci.ps1 -E2EForeground`). Benötigt eine Windows-Desktop-Sitzung.
- Steuerung ausschließlich über UI-Automation-Muster (UIA3); bei UI-Automation-
  Timeouts begrenzte Wiederholung (`TransientRetry`).
- Fehlerdiagnose: Screenshots/UI-Baum unter `e2e-diagnostics/`, Crash-Dumps
  (`DOTNET_DbgEnableMiniDump=1`, Type 2) unter `e2e-dumps/` (Issue #22:
  0xC0000005 im WinRT-Finalizer) — beide werden in der CI als Artefakte hochgeladen.
- Kein Coverage-Collector im E2E-Lauf (bewusst: UI-Tests sollen Abdeckung nicht
  verwässern); E2E-Schritt steht **nach** der Coverage-Prüfung.
- Direkte SQLite-Zugriffe der E2E-Tests (`Microsoft.Data.Sqlite` 10.0.10 →
  10.0.15) dienen dem Befüllen/Zählen der isolierten Testdatenbank
  (`TankbookTestDatabase.cs`, `TestSupport/BackupTestTampering.cs`).

## Lokale Ausführung

- Komplett: `dotnet test Tankradar.sln` (inkl. E2E, README Z. 76) oder
  `scripts/local-ci.ps1` (voller CI-Spiegel; `-SkipE2E` für schnellen Lauf).
- Pre-Push-Hook: `dotnet build` + `dotnet test` für Unit + Integration
  (E2E nur mit `PRE_PUSH_E2E=1`), `.githooks/test-execution-check.py`.
- `coverlet.runsettings` (Repo-Wurzel): Coverage nur für `[Tankradar.MAUI]*`,
  Ausschlüsse: generierte Dateien, `Platforms/**`, `*.xaml.cs`, `MauiProgram.cs`,
  `*AutoInitializer.cs`, `Views/TankradarContentPage.cs`, `Views/Charts/**`.
