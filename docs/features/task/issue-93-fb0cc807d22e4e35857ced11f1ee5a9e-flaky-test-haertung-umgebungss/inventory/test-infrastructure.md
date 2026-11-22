# Testinfrastruktur der drei Testprojekte

Alle drei Testprojekte zielen auf `net11.0-windows10.0.19041.0`, xUnit 2.9.3 +
`xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 17.14.2, `coverlet.collector` 6.0.4
(zentrale Versionen in `Directory.Packages.props`). **xUnit v2** — nicht v3 (Blaupause nutzt
`Xunit.v3`/`ITraitAttribute`; in v2 existiert stattdessen `Xunit.TraitAttribute`).

## `src/Tankradar.Tests.E2E` (FlaUI/UI-Automation)

- `AssemblyInfo.cs`: `[assembly: CollectionBehavior(DisableTestParallelization = true)]` —
  alle E2E-Tests laufen seriell (jeder Test startet eine eigene App-Instanz).
- FlaUI 5.0.0 (`FlaUI.Core`, `FlaUI.UIA3`), `Microsoft.Data.Sqlite` (direkter DB-Zugriff).
- ~111 Dateien unter `E2E/FlaUI/`, **184 ausgeführte Tests** (TRX-Counter des Ausgangslaufs);
  ~99 Dateien kapseln den Testkörper in `RunWithDiagnostics`.

### `E2ETestBase` (`src/Tankradar.Tests.E2E/E2ETestBase.cs`, 594 Zeilen)

| Element | Zeilen | Verhalten |
|---------|--------|-----------|
| Konstruktor | 54–63 | Testdatenverzeichnis `Path.GetTempPath()/Tankradar.Tests.E2E/<guid>` anlegen, `CreateAutomation()`, `LaunchApplication()` |
| `RunWithDiagnostics` | 114–126 | Testkörper ausführen; bei Fehler `E2EDiagnostics.Capture` (Screenshot, UIA-Baum, error.txt), dann rethrow |
| `NavigateToTab(tabTitle, ids)` | 137–187 | `WaitForTab` 30 s + bis zu 5 Versuche × 5 s Deadline-Poll (SelectionItem/Invoke, kein Mausklick) |
| `WaitForPageTitle` | 193–200 | `TransientWait.Until` 30 s |
| `IsShellTitle`/`FindTab`/`WaitForTab` | 208–237 | `TransientRetry.Run` um `FindFirstDescendant`; `WaitForTab` 100-ms-Poll bis Frist |
| `Dispose` | 283–310 | `CloseApplicationAndWait()` → `Automation.Dispose()` (null-sicher) → `Directory.Delete(_testDataDirectory, recursive: true)` **nur `IOException` gefangen, kein Retry** → `Cleanup()` |
| `CloseApplicationAndWait` | 343–381 | `Application.Close()` (Fehler nur `Console.Error`), `WaitForExit(15 s)`, sonst `process.Kill(entireProcessTree: true)` + `WaitForExit(10 s)`; `LogTeardown` |
| `RestartApplication` | 322–337 | `CloseApplicationAndWait` + `LaunchApplication` (gleiches Datenverzeichnis; optional neue Env) |
| `LaunchApplication` | 403–442 | `ProcessStartInfo` (App-EXE, `UseShellExecute=false`, Testmodus-Env) → `E2EStartupPolicy.StartWithRestart` (`MaxStarts=2`) → ggf. `SkipSetup` |
| `StartAndConnect` | 458–485 | `Application.Launch`, `WaitUntilReady` bis UIA-Timeout, `GetMainWindow` mit `windowTimeout = max(uiaTimeout, 30 s)` in `TransientRetry` (4 Versuche, Backoff 2→4→8 s, maxPause 15 s, `totalLimit = max(60 s, 2×uiaTimeout)`), erste Titelabfrage analog |
| `DiscardFailedStart` | 487–504 | `process.Kill(entireProcessTree: true)` + `WaitForExit(10 s)`, Automation neu |
| `SkipSetup` | 536–567 | `TransientWait.Until` 30 s auf `Setup.Skip`, 5 Invoke-Versuche, je 6 s auf Ausblenden |
| `ResolveWindowMode`/`ResolveAppPath` | 574–594 | `TANKRADAR_E2E_WINDOW` → `offscreen`/`foreground`; App-Pfad `Tankradar.MAUI\bin\<Config>\net11.0-windows10.0.19041.0\win-x64\Tankradar.MAUI.exe` |

**Deadline-Befund (für Schritt 4 der Anforderung):** `WaitForTab`/`WaitForPageTitle`/
`SkipSetup`-Warten 30 s und `SettingsE2ETestBase.DefaultTimeout` 20 s sind Host-Poll-Fristen;
die Kind-Frist (UIA `ConnectionTimeout`/`TransactionTimeout`) ist in der CI 60 s. Ein einzelner
UIA-Aufruf kann damit länger laufen als die Host-Frist — exakt das Referenzfehler-Muster
(Host-Deadline < Kind-Budget). Innerhalb der Frist werden UIA-Timeouts zwar als „noch nicht
erfüllt" gewertet (`TransientWait`), die Frist selbst bleibt aber 20–30 s < 60 s UIA-Timeout.

### Abgeleitete Basisklassen (alle `E2ETestBase`-Nachfahren starten App + UIA)

| Klasse | Zweck | Cleanup/Eigenheiten |
|--------|-------|---------------------|
| `SettingsE2ETestBase` | Optionen-/Einrichtungsbedienung | `DefaultTimeout = 20 s` (`WaitUntil` via `TransientWait`); `FindByAutomationId` mit `TransientRetry` (3×, 1 s) |
| `SearchE2ETestBase : SettingsE2ETestBase` | Suchtests; hält **4 Mock-Server** (`MockTankerkoenigServer`, `MockNominatimServer`, `MockTileServer`, `MockOpenRouteServiceServer`) | `Cleanup()` (Z. 554–562) dispost die Server null-sicher |
| `MapE2ETestBase`, `HomeE2ETestBase`, `RouteE2ETestBase`, `FavoritesE2ETestBase` | Bereichsspezifische Testdaten/Env | u. a. Mock-Server + `RestartApplication` mit neuer Env |
| `PriceServiceE2ETestBase : SettingsE2ETestBase` | Preisdienst-Karte | `Cleanup()` dispost `MockTankerkoenigServer`; `PriceServiceE2ETests_Unreachable` nutzt `TcpListener`-Probe auf Port 0 (freier Port → bewusst unerreichbar) |
| `TankbookE2ETestBase`, `BackupE2ETestBase`, `TankbookExportE2ETestBase` | Tankbuch/Backup/Export | `Cleanup()`-Overrides in mehreren Testklassen (Import/Photo/TamperedSchema/Encryption räumen Testdateien ab); `TankbookE2EDriver` UI-Treiber |
| `TankbookTestDatabase` (statisch) | Direkte SQLite-Zugriffe auf `tankatlas.db` der laufenden App | Verbindungen ohne Pool |
| `E2EDiagnostics` (statisch, `src/Tankradar.Tests.E2E/E2EDiagnostics.cs`) | `Capture`/`DumpTree`/`ResolveDirectory` | Schreibt `.png`/`.uitree.txt`/`.error.txt` nach `e2e-diagnostics/` (Env `TANKRADAR_E2E_DIAGNOSTICS_DIR`); Erfassungsfehler nie weiterwerfen; `PrintWindow`-API auch off-screen |
| `StartFailureCleanupE2ETests` | Meta-Test: `Dispose` nach abgebrochenem Start wirft nicht | nutzt `RuntimeHelpers.GetUninitializedObject` — **kein App-Start** |
| `DiagnosticsCaptureE2ETests : E2ETestBase` | Meta-Test: Diagnosedateien entstehen | `Directory.Delete` in `finally` **ohne Fehlerbehandlung** (Z. 40) |

### Sonstige `Directory.Delete`-/`Cleanup`-Stellen im E2E-Projekt

- `E2ETestBase.Dispose` (Z. 295–305): `Directory.Delete` rekursiv, fängt nur `IOException`, kein Retry/Backoff.
- `DiagnosticsCaptureE2ETests` (Z. 38–41): `Directory.Delete` in `finally` ohne jede Fehlerbehandlung — kann einen Assert- oder Startfehler überdecken.

## `src/Tankradar.Tests.Integration`

Kein `AssemblyInfo`-Parallelisierungsverbot (xUnit-Standard: Klassen parallel, Sammlungen seriell).

| Datei/Klasse | Rolle | OS-Bezug |
|--------------|-------|----------|
| `TestDataContext` (`TestDataContext.cs`) | Isoliertes Datenverzeichnis (`TANKATLAS_TEST_DATA_PATH` oder `LocalApplicationData/Tankradar.Tests/<guid>`) | **`Cleanup()` (Z. 37–43): `Directory.Delete` rekursiv ohne jede Fehlerbehandlung** — ein Löschfehler in `Dispose` kann den eigentlichen Assert überdecken |
| `Integration/Support/TestDatabase` | Echte SQLite-Datei im `TestDataContext`-Verzeichnis, `pooling: false` | `Dispose` → `TestDataContext.Dispose` (s. o.) |
| `Integration/Support/SearchMockServerTestBase` | Basis der Mock-Server-Tests: `MockTankerkoenigServer` + `MockNominatimServer` + `MockOpenRouteServiceServer` + `TestDatabase`; echte `FuelPriceService`/`NominatimGeocodingService`/`OpenRouteServiceClient` gegen Loopback | **3 echte Loopback-Portbindungen je Testklasse** |
| `Integration/Support/RouteSearchMockServerTestBase : SearchMockServerTestBase` | Routensuch-Setup | + `MockTileServer`-Nutzung in `TileSourceMockServerTests_Flow` |
| `Integration/Support/ApiKeyBuildRunner` | Baut `KeyProbe.csproj` via **`Process.Start("dotnet", "build", "-v:diag", "-bl:…")`** (Z. 74–124) | **Kindprozess mit eigenem 5-Minuten-Budget** (`WaitForExit(5 min)` → `Kill(entireProcessTree: true)` → `TimeoutException`); `MSBUILDDISABLENODEREUSE=1`; `Dispose` (Z. 160–174): `Directory.Delete` fängt `IOException`+`UnauthorizedAccessException`, **kein Retry** |
| `Integration/ApiKeyBuildTests_Log` (3 Facts) + `ApiKeyBuildTests_Sources` (3+ Facts) | Schlüsselquellen/-logging via echtem `dotnet build` | Je Test ein `dotnet build`-Kindprozess (~Sekunden bis Minuten auf Runnern) |
| `Integration/LoopbackHttpListenerTests_Retry` (3 Facts) | Port-Kollisions-/Parallelstarts-Tests | echte `HttpListener`-Bindungen |
| `Integration/Support/BackupTestEnvironment` + `BackupCollection` | Backup-Integrationsumgebung (`TestDatabase`-basiert) | `[CollectionDefinition("BackupRestoreSharedPools")]` serialisiert 14 Backup-Testklassen (SqliteConnection.ClearAllPools wirkt prozessweit) |
| `Integration/Support/RequestTimingHandler`, `NullStationNavigator` | HTTP-Timing-Messung, Null-Navigation | — |

Mock-Server-verwendende Integrationstestklassen (Audit-Kandidaten): `AddressSearchMockServerTests_*`,
`AreaSearchMockServerTests_*`, `AutoSearchMockServerTests_*`, `GeocodingMockServerTests_*`,
`PriceRefreshMockServerTests_*`, `PriceServiceMockServerTests_*`, `RouteSearchMockServerTests_*`,
`SearchMockServerTests_*`, `TileSourceMockServerTests_Flow` — alle binden echte Loopback-Ports
über `LoopbackHttpListener` (Retry 10× bereits vorhanden). `TileSourceMockServerTests_Flow`
enthält zudem `Directory.Delete` im Dispose-Pfad (Z. 30).

## `src/Tankradar.Tests.Unit`

- `BaseTest` (`BaseTest.cs`): IDisposable-Basis mit `Dispose(bool)`-Muster.
- `Unit/Support/TempDirectory`: `Directory.Delete` in `Dispose`, fängt nur `IOException`, kein Retry.
- `Unit/Support/EnvironmentCollection`: `[CollectionDefinition("TestDataPathEnvironment")]`
  serialisiert env-sensitive Tests (`AppDataPathProviderTests_DataDirectoryResolution`,
  `TestModeVariableTests_Rename`) — die einzige bestehende Mechanik gegen Umgebungs-Races.
- **Loopback-Nutzung im Unit-Projekt** (Audit-relevant, widerspricht der Annahme „kaum
  OS-Kandidaten"): `MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`,
  `OpenRouteServiceKeyCheckerTests` starten `MockOpenRouteServiceServer` (echte `HttpListener`-Portbindung),
  weil das Unit-csproj `LoopbackHttpListener`/`MockOpenRouteServiceServer` linkt.
- Echtes Dateisystem (Audit-Kandidaten, `Directory.Delete` im Dispose): `BackupCryptoTests` (Z. 23),
  `HttpTileSourceTests_{AgeAndRevalidation,Fetch,HttpCaching,NoStoreAndExpires}` (Z. 65/41/47/29),
  `ReceiptPhotoStoreTests_{Detection,Files}` (Z. 26/52).
- Meta-/Konventionstests als Muster für einen künftigen `Flaky`-Meta-Test:
  `E2EInputPolicyTests_NoFocus` (Regex-Quelltextprüfung des E2E-Projekts),
  `IosProjectFilesTests` (Projektdatei-Konventionen), `MigrationsTests`-Analogien.
- Weitere Quelltext-/Konventionstests: `TestModeVariableTests_Rename`.

## Umgebungsvariablen der Testarchitektur

| Variable | Wirkung | Verbraucher |
|----------|---------|-------------|
| `TANKRADAR_E2E_WINDOW` | `offscreen` (Default) / `foreground` | `E2ETestBase.ResolveWindowMode`; CI-Variable + `local-ci.ps1 -E2EForeground` |
| `TANKRADAR_E2E_UIA_TIMEOUT_SECONDS` | UIA-Timeout-Override 1–600 s | `E2EStartupPolicy.ResolveUiaTimeout` |
| `TANKRADAR_E2E_DIAGNOSTICS_DIR` | Diagnoseverzeichnis-Override | `E2EDiagnostics.ResolveDirectory` |
| `TANKATLAS_TEST_DATA_PATH` | Testmodus + isoliertes Datenverzeichnis der App | `E2ETestBase`, `TestDataContext` |
| `GITHUB_ACTIONS`/`CI` | UIA-Timeout 60 s statt 15 s | `E2EStartupPolicy` |
| `PRE_PUSH_E2E`, `HOOK_SKIP_TESTS`, `TEST_TIMEOUT_SECONDS` | pre-push-Steuerung | `.githooks/test-execution-check.py` |
| `DOTNET_Dbg*` | Crash-Dumps des App-Prozesses | CI-E2E-Schritt |
