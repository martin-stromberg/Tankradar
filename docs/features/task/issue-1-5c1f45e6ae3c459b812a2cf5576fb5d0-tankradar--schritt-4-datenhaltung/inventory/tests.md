## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-04 19:54 +0200 (Unit-Lauf; Integration und E2E unmittelbar danach)
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-4-datenhaltung`, `7ea6bf5f87b4c720a2a3e2d0e4955e812f706460`
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Verzeichnis `docs/features/` (Planungsdokumente); kein Code geändert
- Testumgebung: Windows 11 Pro, .NET SDK 10.0.401, Zielframework net10.0-windows10.0.19041.0, xUnit 2.9.3, FlaUI 5.0.0
- Ermittelte Testsuiten und Quellen: `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (Projektdateien, `scripts/local-ci.ps1`, `docs/help/ci-cd`); E2E nutzt die bereits gebaute Debug-App `src/Tankradar.MAUI/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Tankradar.MAUI.exe`

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit | `dotnet test src/Tankradar.Tests.Unit --logger "trx;LogFileName=unit.trx"` | Repository-Root | 0 | 16 | 0 | 0 | [Log](test-results/unit.log), [TRX](test-results/unit.trx) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --logger "trx;LogFileName=Integration.trx"` | Repository-Root | 0 | 3 | 0 | 0 | [Log](test-results/Integration.log), [TRX](test-results/Integration.trx) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --logger "trx;LogFileName=E2E.trx"` | Repository-Root | 0 | 4 | 0 | 0 | [Log](test-results/E2E.log), [TRX](test-results/E2E.trx) |

### Nachgewiesene bestehende Testfehler

Keine. Alle 23 Tests der drei Suiten liefen erfolgreich.

### Testlücken und Ausführungsprobleme

Keine Build-, Setup- oder Infrastrukturfehler. Nicht ausgeführt: JavaScript-/Python-Tests unter `scripts/` und `.githooks/` (nicht Teil dieser Anforderung). Für die Anforderung fehlen Tests für Datenhaltung, Einstellungen und Optionen-Seite vollständig.

## Testklassen

### `AppConfigurationTests_BundleId` (Unit)
- `AppDisplayName_IsTankatlas_WhileBundleIdKeepsTechnicalName`, `Constructor_WithoutEnvironmentVariable_UsesDefaultBundleId`, `Constructor_WithEnvironmentVariable_UsesEnvironmentValue`, `LoadFromSettingsFileAsync_WithEnvironmentVariable_KeepsEnvironmentValue`

### `AppDataPathProviderTests_DataDirectoryResolution` (Unit)
- Drei Tests zu `GetDataDirectory` (mit `TEST_DATA_PATH`, ohne, nur Leerzeichen)

### `BaseViewModelTests_PropertyBinding` (Unit)
- `Constructor_SetsTitle`, `Constructor_StartsNotBusy`, `SetIsBusy_RaisesPropertyChangedForIsBusyAndIsNotBusy`, `SetTitle_SameValue_DoesNotRaisePropertyChanged`

### `ViewModelTests_PageTitles` (Unit)
- `Constructor_SetsExpectedTitle` (Theory; instanziiert ViewModels per `Activator.CreateInstance` mit parameterlosem Konstruktor, auch `SettingsViewModel` — bricht, sobald dieser Parameter erhält), `OnAppearing_DoesNotChangeState`

### `TestDataContextTests_Lifecycle` (Integration)
- `Constructor_CreatesIsolatedDataDirectory`, `Cleanup_RemovesDataDirectory`, `Constructor_CreatesUniqueDirectoryPerInstance`

### `NavigationE2ETests` (E2E)
- `AppStartsAndNavigatesThroughAllTabs` (nutzt `SettingsPage.Headline`), `MainWindowShowsDisplayNameAsTitle`

### `DiagnosticsCaptureE2ETests` (E2E)
- `FailingTestBodyProducesScreenshotUiTreeAndErrorFile`, `PassingTestBodyProducesNoDiagnostics`

## Hilfsmethoden

### `BaseTest` (Unit)
- `Dispose(bool)` — einheitliche Aufräumlogik.

### `TestDataContext` (Integration)
- `Initialize`, `Cleanup`, `Dispose`, Eigenschaft `DataDirectory` — GUID-Unterverzeichnis unter `TEST_DATA_PATH` bzw. `%LocalAppData%/Tankradar.Tests`.

### `E2ETestBase` (E2E)
- Konstruktor startet die App mit `TEST_DATA_PATH` auf ein Temp-Verzeichnis; `RunWithDiagnostics`; Eigenschaften `Automation`, `Application`, `MainWindow` (nur lesbar); `Dispose` schließt die App und löscht das Verzeichnis. Kein Neustart bei gleichem Datenverzeichnis möglich.

### `TestDataPaths` (`src/TestSupport`)
- Konstante `TestDataPathEnvironmentVariable` = `TEST_DATA_PATH`.
