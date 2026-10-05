## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt: 2026-10-05, ca. 18:10 bis 18:20 (lokale Systemzeit, Windows 11)
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-6-umkreissuche`, `fbaf56985d58daab48dd770f5fb3b941208f47f4`
- Uncommittete Änderungen: nur das unversionierte Verzeichnis `docs/features/` (keine Code- oder Testdateien)
- Testumgebung: Windows 11 Pro 10.0.26200, .NET SDK 10.0.401, Konfiguration Release, Zielframework `net10.0-windows10.0.19041.0`; E2E mit FlaUI gegen die lokal gebaute Windows-App
- Quellen der Befehle: `README.md` (Zeile 112), `.github/workflows/pr-staging-ci.yml` (Zeilen 142 bis 162), `scripts/local-ci.ps1`

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build | `dotnet build Tankradar.sln --configuration Release` | Repo-Root | 0 | – | – | – | [Log](test-results/build.log) (0 Warnungen, 0 Fehler) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build` | Repo-Root | 0 | 204 | 0 | 0 | [Log](test-results/Unit.log) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build` | Repo-Root | 0 | 33 | 0 | 0 | [Log](test-results/Integration.log) |
| E2E | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build` | Repo-Root | 0 | 12 | 0 | 0 | [Log](test-results/E2E.log) |

### Nachgewiesene bestehende Testfehler

Keine: Alle Läufe waren vollständig erfolgreich.

### Testlücken und Ausführungsprobleme

- Keine nicht ausgeführten oder übersprungenen Tests. Der iOS-Build ist nicht Teil des Laufs (kein macOS).
- Testlücken bezogen auf die Anforderung: keine Tests für Standortdienst, Berechtigung nach `GpsUsage`, Ergebnisaufbereitung (Filter, Sortierung), `MapViewModel`-Logik, Suchoberfläche, UI-Radiusprüfung und Test-Standort; kein Test, der belegt, dass ein anderer Variablenname als der aktuelle den Testmodus nicht aktiviert; der Mock-Server wertet den Radius nicht aus.

## Testklassen

Unit (`src/Tankradar.Tests.Unit/Unit/`): `FuelPriceServiceTests_Search` (Suche, Filter, Cache, Radiusvalidierung), `FuelPriceServiceTests_Offline`, `FuelPriceServiceTests_Detail`, `FuelPriceServiceTests_StorageFailure`, `PriceFreshnessTests_Age`, `StationHintsTests_Derivation`, `ConnectionMonitorTests_Transitions`, `NetworkAccessTests_Reachability`, `PriceApiOptionsTests_Validation`, `ApiKeyStoreSelectorTests_TestMode`, `AppDataPathProviderTests_DataDirectoryResolution`, `AppSettingsTests_Defaults`/`_Validation`, `SettingsViewModelTests_*`, `ViewModelTests_PageTitles`, `DataSourceViewModelTests_Check`, `TankerkoenigClientTests_*`.

Integration (`src/Tankradar.Tests.Integration/Integration/`): `PriceServiceMockServerTests_Flow`, `PriceRepositoryTests_Persistence`, `PriceCacheMigrationTests_Upgrade`, `SettingsServiceTests_RestartPersistence`, `DatabaseInitializerTests_*`, `MigrationsTests_ModelSync`, `TestDataContextTests_Lifecycle`.

E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI/`): `NavigationE2ETests`, `SettingsE2ETests_Defaults`/`_FuelTypes`/`_Persistence`, `PriceServiceE2ETests_Reachable`/`_Unreachable`, `DiagnosticsCaptureE2ETests`.

## Hilfsmethoden

### `E2ETestBase` (`src/Tankradar.Tests.E2E/E2ETestBase.cs`)
- Konstruktor mit `additionalEnvironment`; startet die App mit `TestDataPaths.TestDataPathEnvironmentVariable` auf ein temporäres Verzeichnis; `NavigateToTab`, `RestartApplication`, `RunWithDiagnostics`.

### `SettingsE2ETestBase`, `PriceServiceE2ETestBase`
- `OpenSettings`, `WaitForAutomationId`, `WaitUntil`; `PriceServiceE2ETestBase` übergibt `TANKRADAR_PRICE_API_URL` und `TANKRADAR_PRICE_API_KEY`, bietet `MockRequestCount`, `RunServiceCheck`.

### `MockTankerkoenigServer` (`src/TestSupport/MockTankerkoenigServer.cs`)
- `StationAlpha` (Berlin, `e5` 1,859, `e10` 1,799, `diesel` 1,699, durchgehend offen) und `StationBeta` (ohne Diesel, feste Öffnungszeiten); `list.php` liefert immer beide mit festen `dist`-Werten (0,1 und 1,4 km); `EnqueueStatuses`, `ResponseDelay`, `ListRequests`, `DetailRequests`, `LastRequest`, `AcceptedKey`.

### Unit-Test-Helfer
- `FuelPriceServiceTestBase`, `StubTankerkoenigClient`, `FakeConnectionMonitor`, `ManualTimeProvider`, `InMemoryDbContextFactory`, `NoOpDatabaseInitializer` (Unit/Support); `TestDataPaths` (`src/TestSupport/TestDataPaths.cs`) mit den Umgebungsvariablen-Konstanten.
