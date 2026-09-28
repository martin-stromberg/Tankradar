# Tests: Git Hooks für Qualitätssicherung

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-28 23:16:23 UTC+02:00
- **Branch und Commit-ID:** `task/issue-1-5c1f45e6ae3c459b812a-2cf5576fb5d0-tankradar--schritt-2-git-hooks` (HEAD: 6d2f984)
- **Uncommittete Änderungen im getesteten Stand:** Keine (nur untracked docs/features/ und .claude/)
- **Testumgebung und Runtime-/SDK-Versionen:** 
  - .NET SDK 10.0.401
  - MAUI Workloads: android 36.1.69, ios 26.5.10318, maui-windows 10.0.20
  - Plattform: Windows 11 Pro 10.0.26200
  - xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.12)
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - Testbefehl aus README.md: `dotnet test Tankradar.sln`
  - Test-Projekte:
    - `src/Tankradar.Tests.Unit` (xUnit)
    - `src/Tankradar.Tests.Integration` (xUnit)
    - `src/Tankradar.Tests.E2E` (xUnit + FlaUI)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Baseline) | `dotnet test Tankradar.sln --verbosity normal` | `D:\Repositories\softwareschmiede\5c1f45e6-ae3c-459b-812a-2cf5576fb5d0` | 0 | 11 | 0 | 0 | [dotnet-test-baseline.log](test-results/dotnet-test-baseline.log) |

### Nachgewiesene bestehende Testfehler

Keine Testfehler nachgewiesen. Alle 11 Tests bestanden erfolgreich.

### Testlücken und Ausführungsprobleme

Keine Testausführungsprobleme oder Lücken festgestellt. Alle Test-Suites wurden vollständig ausgeführt:
- Unit Tests: 7 Tests, alle bestanden
- Integration Tests: 3 Tests, alle bestanden  
- E2E Tests: 1 Test, bestanden

**Android/iOS-Build-Hinweise:** Während des Testlaufs wurden Meldungen zu fehlenden Android-Application-Hosts (NETSDK1084 für `android-arm64` und `android-x64`) angezeigt. Dies ist erwartungsgemäß für eine Windows-Entwicklungsumgebung und blockiert die Tests nicht, da die Test-Projekte nur für `net10.0-windows10.0.19041.0` konfiguriert sind.

## Testklassen

### `Tankradar.Tests.Unit.Unit.AppDataPathProviderTests_DataDirectoryResolution`
Datei: `src/Tankradar.Tests.Unit/Unit/AppDataPathProviderTests_DataDirectoryResolution.cs`

- `GetDataDirectory_WithTestDataPathSet_ReturnsEnvironmentVariableValue` — Testet, dass die App das TEST_DATA_PATH-Verzeichnis verwendet, wenn gesetzt
- `GetDataDirectory_WithoutTestDataPathSet_ReturnsDefaultDirectoryFactoryResult` — Testet Fallback auf Standard-Datenverzeichnis
- `GetDataDirectory_WithWhitespaceOnlyTestDataPath_ReturnsDefaultDirectoryFactoryResult` — Testet Behandlung von Whitespace-only Pfaden

### `Tankradar.Tests.Unit.Unit.BaseViewModelTests_PropertyBinding`
Datei: `src/Tankradar.Tests.Unit/Unit/BaseViewModelTests_PropertyBinding.cs`

- `Constructor_StartsNotBusy` — Testet, dass ViewModel mit IsBusy=false initialisiert wird
- `Constructor_SetsTitle` — Testet Title-Initialisierung
- `SetTitle_SameValue_DoesNotRaisePropertyChanged` — Testet Property-Change-Notification für gleiche Werte
- `SetIsBusy_RaisesPropertyChangedForIsBusyAndIsNotBusy` — Testet Property-Change-Events für IsBusy und IsNotBusy

### `Tankradar.Tests.Integration.Integration.TestDataContextTests_Lifecycle`
Datei: `src/Tankradar.Tests.Integration/Integration/TestDataContextTests_Lifecycle.cs`

- `Constructor_CreatesIsolatedDataDirectory` — Testet Erstellung isolierter Test-Datenverzeichnisse
- `Constructor_CreatesUniqueDirectoryPerInstance` — Testet Eindeutigkeit von Test-Verzeichnissen
- `Cleanup_RemovesDataDirectory` — Testet Cleanup von Test-Datenverzeichnissen

### `Tankradar.Tests.E2E.E2E.FlaUI.NavigationE2ETests`
Datei: `src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`

- `AppStartsAndNavigatesThroughAllTabs` — FlaUI-E2E-Test: Startet Windows-App und navigiert durch alle Navigationstabs

## Hilfsmethoden

### `Tankradar.Tests.Unit.BaseTest`
Datei: `src/Tankradar.Tests.Unit/BaseTest.cs`

Zentrale Test-Basisklasse für Unit-Tests. Details folgen nach Code-Analyse.

### `TestSupport` Projekt
Datei: `src/TestSupport/`

Stellt gemeinsame Test-Hilfsmethoden und Infrastruktur zur Verfügung, wird von allen Test-Projekten genutzt.
