# TestSupport (geteilte Testinfrastruktur)

`src/TestSupport` ist **kein eigenes Projekt**, sondern ein Verzeichnis gemeinsam genutzter
Quelldateien (Namespace `Tankradar.TestSupport`), die die Testprojekte per
`<Compile Include="..\TestSupport\<Datei>" Link="...">` einbinden:

- `Tankradar.Tests.Unit` linkt: `TransientRetry`, `TransientWait`, `E2EStartupPolicy`,
  `BackupTestArchive`, `BackupEncryptedTestArchive`, `RecordingLogger`,
  `LoopbackHttpListener`, `MockOpenRouteServiceServer`, `PolylineEncoder`
- `Tankradar.Tests.Integration` linkt: `MockTankerkoenigServer`, `MockNominatimServer`,
  `MockOpenRouteServiceServer`, `PolylineEncoder`, `MockTileServer`, `LoopbackHttpListener`,
  `BackupTestArchive`, `BackupEncryptedTestArchive`, `BackupTestTampering`, `RecordingLogger`
- `Tankradar.Tests.E2E` linkt: `BackupTestArchive`, `BackupTestTampering`, `TestDataPaths`,
  `TransientRetry`, `TransientWait`, `E2EStartupPolicy`, `MockTankerkoenigServer`,
  `MockNominatimServer`, `MockOpenRouteServiceServer`, `PolylineEncoder`, `MockTileServer`,
  `LoopbackHttpListener`

Konsequenz: Eine neue Datei in `src/TestSupport` wird **nicht** automatisch kompiliert —
sie muss in jeder gewünschten `.csproj` explizit gelinkt werden.

## `TransientRetry`
Datei: `src/TestSupport/TransientRetry.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `UiaTimeoutHResult` | public const | HRESULT 0x80131505 (COR_E_TIMEOUT) der UI-Automation |
| `Win32TimeoutErrorCode` | public const | Win32-Fehler 1460 (ERROR_TIMEOUT), als HRESULT 0x800705B4 |
| `DefaultMaxAttempts` | public const | 3 Versuche |
| `IsTransient(Exception)` | public static | Erkennt `TimeoutException`, `COMException` 0x80131505/0x800705B4, `Win32Exception` 1460/0x800705B4 (auch verschachtelt) |
| `Run<T>(action, description, maxAttempts, pause, sleep, backoffFactor, maxPause, totalLimit, elapsed)` | public static | Wiederholt nur transiente UIA-Timeouts; Backoff, Pausen-Obergrenze, Gesamtzeitlimit; wirft `InvalidOperationException` mit Versuchszahl/Wartezeit |
| `Run(Action, description, maxAttempts, pause, sleep)` | public static | Variante ohne Ergebnis |

Verwendet von: `E2ETestBase` (Automation-Aufbau, Hauptfenster-Abfrage, Titelsuche,
Reitersuche), `SettingsE2ETestBase.FindByAutomationId`, `SearchE2ETestBase.SelectChip`
u. a. — also ausschließlich E2E-/UIA-Kontext. Testbar über injizierte `sleep`/`elapsed`-Parameter
(Tests: `TransientRetryTests_Behavior` im Unit-Projekt).

## `TransientWait`
Datei: `src/TestSupport/TransientWait.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Until(condition, timeout, out lastTimeout, poll, sleep, elapsed)` | public static | Pollt Bedingung bis Frist; UIA-Timeout und `ElementNotAvailableException` zählen als „noch nicht erfüllt" |
| `Until(condition, timeout, out lastTimeout, out timeoutTime, poll, sleep, elapsed)` | public static | Wie oben, liefert zusätzlich die in transienten Fehlern verbrachte Zeit (`timeoutTime`) — Basis für „Messzeit abzüglich UIA-Wartezeit" |
| `IsTransient(Exception)` | public static | `TransientRetry.IsTransient` oder `FlaUI.Core.Exceptions.ElementNotAvailableException` (verschachtelt) |

Standard-Poll-Intervall: 100 ms. Verwendet von `E2ETestBase.WaitForPageTitle`/`SkipSetup`,
`SettingsE2ETestBase.WaitUntil` u. a. Tests: `TransientWaitTests`,
`TransientWaitTests_ElementNotAvailable` (Unit-Projekt).

## `E2EStartupPolicy`
Datei: `src/TestSupport/E2EStartupPolicy.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `UiaTimeoutEnvironmentVariable` | public const | `TANKRADAR_E2E_UIA_TIMEOUT_SECONDS` |
| `LocalUiaTimeoutSeconds` | public const | 15 s (lokal) |
| `CiUiaTimeoutSeconds` | public const | 60 s (CI, `GITHUB_ACTIONS`/`CI` = true) |
| `ResolveUiaTimeout(getEnvironmentVariable)` | public static | Env-Override (1–600 s) > CI-Default 60 s > lokaler Default 15 s |
| `StartWithRestart<T>(start, cleanup, maxStarts, description, sleep)` | public static | Führt App-Start + UIA-Verbindung aus; bei transientem Timeout `cleanup` + Neustart, max. `maxStarts` (E2ETestBase: `MaxStarts = 2`) |
| `WaitUntilReady(process, timeout)` | public static | Wartet bis Hauptfenster-Handle + `WaitForInputIdle`, höchstens `timeout`; Überschreiten kein Fehler |

Tests: `E2EStartupPolicyTests_Backoff` (Unit-Projekt).

## `LoopbackHttpListener`
Datei: `src/TestSupport/LoopbackHttpListener.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `DefaultMaxAttempts` | public const | 10 Startversuche |
| `Start(out port, maxAttempts)` | public static | Startet `HttpListener` auf freiem Loopback-Port (`TcpListener`-Probe auf Port 0) |
| `Start(selectPort, out port, maxAttempts)` | public static | Port per Callback; bei `HttpListenerException` neuer Port + Retry (Pause `20 ms × Versuch`) |

Verwendet von allen vier Mock-Servern. Tests: `LoopbackHttpListenerTests_Retry`
(Integration-Projekt).

## Mock-Server (Loopback-`HttpListener`)

| Klasse | Datei | Endpunkt-Mock |
|--------|-------|----------------|
| `MockTankerkoenigServer` | `src/TestSupport/MockTankerkoenigServer.cs` | Tankerkönig-API (`list.php`, `detail.php`, `prices.php`); feste Teststationen, Preis-Overrides, Status-Warteschlange, `ResponseDelay`, Anfragezähler/-protokoll |
| `MockNominatimServer` | `src/TestSupport/MockNominatimServer.cs` | Nominatim-Geokodierung; feste Ortsantworten (u. a. `QueryBerlin`, `QueryOranienburg`), Fehlerfolgen |
| `MockOpenRouteServiceServer` | `src/TestSupport/MockOpenRouteServiceServer.cs` | OpenRouteService (`v2/directions/...`); Schlüsselprüfung (`AcceptedKey`), feste/parameterbasierte Routen, Anfrageprotokoll (`LastAuthorization`, `LastRawRequest`) |
| `MockTileServer` | `src/TestSupport/MockTileServer.cs` | OSM-Kachelserver `/{z}/{x}/{y}.png`; 1×1-PNG, User-Agent-Prüfung, `ETag`/`Cache-Control`, Fehlerfolgen |

Gemeinsames Muster: Konstruktor startet `LoopbackHttpListener.Start` + `Task.Run(RunAsync)`;
`Dispose` (idempotent via `Interlocked`) bricht per `CancellationTokenSource` ab, schließt
den Listener und wartet die Schleife max. 2 s (`_loop.Wait(TimeSpan.FromSeconds(2))`,
`AggregateException` wird geschluckt).

## `TestDataPaths`
Datei: `src/TestSupport/TestDataPaths.cs`

Zentrale Konstanten der Testmodus-Umgebungsvariablen (nur in `Tankradar.Tests.E2E` gelinkt):

| Konstante | Wert |
|-----------|------|
| `TestDataPathEnvironmentVariable` | `TANKATLAS_TEST_DATA_PATH` |
| `PriceApiUrlEnvironmentVariable` | `TANKRADAR_PRICE_API_URL` |
| `GeocodingUrlEnvironmentVariable` | `TANKRADAR_GEOCODING_URL` |
| `TileUrlEnvironmentVariable` | `TANKRADAR_TILE_URL` |
| `PriceApiKeyEnvironmentVariable` | `TANKRADAR_PRICE_API_KEY` |
| `RoutingUrlEnvironmentVariable` | `TANKRADAR_ROUTING_URL` |
| `RoutingTestApiKeyEnvironmentVariable` | `TANKRADAR_ROUTING_TEST_API_KEY` |
| `RouteQueryIntervalEnvironmentVariable` | `TANKRADAR_ROUTE_QUERY_INTERVAL_MS` |
| `TestLocationEnvironmentVariable` | `TANKATLAS_TEST_LOCATION` |
| `TestReceiptPhotoEnvironmentVariable` | `TANKATLAS_TEST_RECEIPT_PHOTO` |
| `TestBackupImportEnvironmentVariable` | `TANKATLAS_TEST_BACKUP_IMPORT` |
| `TestWindowEnvironmentVariable` | `TANKATLAS_TEST_WINDOW` (`offscreen`/`foreground`) |

## Weitere Helfer

| Klasse | Datei | Zweck |
|--------|-------|-------|
| `RecordingLogger<T>` | `src/TestSupport/RecordingLogger.cs` | `ILogger<T>`, sammelt alle Einträge als Text (Privacy-/Fehler-Prüfungen) |
| `BackupTestArchive` | `src/TestSupport/BackupTestArchive.cs` | Erzeugt gültige Backup-ZIPs als Testdaten |
| `BackupEncryptedTestArchive` | `src/TestSupport/BackupEncryptedTestArchive.cs` | Erzeugt verschlüsselte Backup-Archive |
| `BackupTestTampering` | `src/TestSupport/BackupTestTampering.cs` | Manipuliert Backup-Archive (Manipulations-Tests) |
| `PolylineEncoder` | `src/TestSupport/PolylineEncoder.cs` | Kodiert Koordinaten als Polyline (Routing-Mock) |

## Nicht vorhanden (für die Anforderung relevant)

- Keine Trait-/Kategorien-Konstanten (`TestCategories`, `OsInterface`, `Flaky`)
- Kein `TempDirectoryCleanup`-Helfer (Retry/Backoff-Löschung)
- Kein Trait-Attribut (weder `TraitAttribute`-Ableitung noch Eigenbau)
- Keine Trait-Nutzung im gesamten Repository (grep `Trait`/`Category` in `src/`:
  nur unverbundene Treffer wie `NSPrivacyAccessedAPICategory*`, `entry.Category`)
