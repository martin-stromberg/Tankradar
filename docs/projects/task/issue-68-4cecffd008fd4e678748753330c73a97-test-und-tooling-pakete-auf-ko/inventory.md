# Bestandsaufnahme

Stand: Basisbranch `task/issue-68-...` == `origin/staging` (0 ahead/0 behind).

## Lösung und Projekte

- `Tankradar.sln` mit 5 Projekten:
  - `src/Tankradar.MAUI` — .NET-MAUI-App (net10.0-android/-ios/-maccatalyst/
    -windows10.0.19041.0; Ziele per `Include*Target`-Property ausblendbar;
    `MauiVersion` fest auf 10.0.130 gepinnt, ADR 0016).
  - `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`,
    `src/Tankradar.Tests.E2E` — jeweils `net10.0-windows10.0.19041.0`, xunit.
  - `src/PagesScreenshots` — Exe, referenziert E2E-Projekt.
- `src/TestSupport/` — geteilte Testhilfen, per `<Compile Include Link>` in die
  Testprojekte (und teils in die App) eingebunden; kein eigenes Projekt.

## Paketverwaltung (Ist)

- Keine zentrale Paketverwaltung: kein `Directory.Packages.props`, kein
  `Directory.Build.props`. Jede `.csproj` pinnt Versionen lokal.
- Test-Trio identisch gepinnt in allen 3 Testprojekten:
  `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3,
  `xunit.runner.visualstudio` 3.1.4, `coverlet.collector` 6.0.4.
- E2E zusätzlich: `FlaUI.Core`/`FlaUI.UIA3` 5.0.0, `Microsoft.Data.Sqlite` 10.0.15,
  `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14.
- App: `Microsoft.Maui.Controls` `$(MauiVersion)`=10.0.130,
  `Microsoft.EntityFrameworkCore.Sqlite`/`.Design` 10.0.15,
  `Microsoft.Extensions.Logging.Debug` 10.0.15,
  `SQLitePCLRaw.bundle_e_sqlite3` 2.1.14 (bewusster Pin gegen GHSA-2m69-gcr7-jv3q
  in transitiver 2.1.11 — Kommentar im csproj).
- `coverlet.runsettings` vorhanden.

## SDK- und Actions-Governance (Ist)

- Keine `global.json`. CI nutzt `actions/setup-dotnet@v6` mit
  `dotnet-version: '10.0.x'` in pr-staging-ci, staging-ci, release, security-scan.
- Externe Actions ausschließlich über Major-Tags: `actions/checkout@v7` (19×),
  `upload-artifact@v7` (8×), `setup-node@v7` (7×), `setup-dotnet@v6` (6×),
  `upload-pages-artifact@v5`, `deploy-pages@v5`, `configure-pages@v5`.
- Eigene Composite-Actions unter `.github/actions/` (security-scan, package-ios,
  create-review-issue, build-and-package).
- Kein Dependabot/Renovate (`.github/` enthält nur `actions/` und `workflows/`).
- Es existiert bereits `review-nuget-packages.yml` („Weekly NuGet Package Review",
  schedule) — prüfen, was dieser Workflow leistet, bevor Dependabot ergänzt wird.

## Workflows/CI

- `pr-staging-ci.yml`: PR-Gate für staging, überspringt CI bei reinem Backmerge
  (`scripts/detect-backmerge.mjs`).
- `staging-ci.yml`, `release.yml` (Release-Gate: reguläre Tests vor Packaging),
  `security-scan.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`
  (Backmerge-Automatisierung), `verify-pr-source.yml`, `pages.yml`,
  Review-Workflows. Windows-Runner blenden Apple-Ziele aus; iOS-Workload nur macOS.
- `scripts/local-ci.ps1` — lokale CI-Nachbildung.
- Workflow-Validierung: `scripts/validate-workflows.py`, `test_validate_workflows.py`.

## iOS/EF-Core-Status (Issue #71)

- `Tankradar.MAUI.csproj`: `<MtouchInterpreter>all</MtouchInterpreter>` für iOS
  (Release läuft ohne JIT; Kommentar: `-all`-Variante sprengte 60-Min-CI-Limit).
- `TrimmerRootAssembly`: Tankradar.MAUI, Microsoft.EntityFrameworkCore(.Relational,
  .Sqlite), Microsoft.Data.Sqlite.
- EF Core 10.0.15 mit Sqlite; Migrations unter `src/Tankradar.MAUI/Data/Migrations/`
  (z. B. AddFuelTypeSettings); `DatabaseInitializer`, `DatabaseFileProtector`.
- Kein Compiled Model vorhanden.

## App-Struktur relevant für Issue #70 (Diagnosepaket)

- MVVM: `ViewModels/SettingsViewModel.cs`, `Views/SettingsPage.xaml`; UI-Texte in
  `Resources/Texts/*Texts.cs` (keine resx).
- `Services/SettingsService.cs` + `ISettingsService`, `AppDataPathProvider`,
  `ApiKeys` (Tankerkönig etc.), Export-Services (`Services/Export/`), Backup-
  Services — ein Diagnosepaket kann sich an Export/Backup-Patterns orientieren.
- Logging: `Microsoft.Extensions.Logging.Debug` (nur Debug-Kontext); `ILogger`
  in vielen Services; kein produktiver Log-Puffer/Datei-Sink vorhanden.
- Datensparsames Logging ist bewusste Architektur (Issue #70-Befund).

## Konventionen

- ADRs: `docs/adr/NNNN-titel.md`, nächste freie Nummer 0020.
- Feature-/Projektartefakte: `docs/features/{branchname}/` bzw.
  `docs/projects/{branchname}/` (Branch-Slashes als Unterverzeichnisse).
- Doku: `docs/help/` (thematische Ordner), `docs/RELEASE_NOTES.md`.
- Commits: `feat:`/`plan:`/`merge:`-Präfixe, deutsch.
- Kommentare im Code überwiegend deutsch, mit Begründungen (siehe csproj-Kommentare).

## Einschränkungen dieser Umgebung

- Windows-Maschine: iOS-/MacCatalyst-Builds und reale iOS-Messungen für #71 nicht
  durchführbar → Risikoabwägung + ADR statt Messwerte, oder Messung auf Windows-
  Build als Indikator. MAUI-Workload-Verfügbarkeit lokal zu prüfen.
- E2E-Tests (FlaUI) benötigen Windows-Desktop-Session; lokale Ausführbarkeit zu prüfen,
  CI übernimmt den verbindlichen Lauf.
