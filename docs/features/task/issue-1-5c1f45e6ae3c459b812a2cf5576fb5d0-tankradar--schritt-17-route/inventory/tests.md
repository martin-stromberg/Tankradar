# Tests (Bestand)

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-20, 08:31:45 bis 08:32:28 (+02:00, MESZ); siehe `test-results/start.txt` und `test-results/end.txt`.
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-17-route`, `7adcb504b93c36e1fae022c0a46f0f291525d668`.
- Uncommittete Änderungen im getesteten Stand: keine Änderungen an Code oder Tests; nur das untracked Verzeichnis `docs/features/` (Dokumentation, dieses Schritts und älterer Schritte). `git status --short` zeigte vor dem Lauf nur `?? docs/features/`.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 Pro 10.0.26300, .NET SDK 10.0.402, Konfiguration `Release`, Zielframework `net10.0-windows10.0.19041.0`; Bash-Shell. Umgebungsvariablen `IncludeAndroidTarget`, `IncludeIosTarget`, `IncludeMacCatalystTarget` = `false` (Windows-Job-Simulation wie `scripts/local-ci.ps1`); `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY`, `TANKRADAR_WINDOW` und `TANKATLAS_TEST_WINDOW` per `env -u` entfernt (in der Shell war `FUEL_PRICE_API_KEY` gesetzt; der Wert wurde nicht gelesen und taucht in keinem Nachweis auf).
- Ermittelte Testsuiten und Quellen der Testbefehle: `Tankradar.Tests.Unit`, `Tankradar.Tests.Integration`, `Tankradar.Tests.E2E` (FlaUI) aus `scripts/local-ci.ps1` (Zeilen 161 bis 190: `dotnet test src/Tankradar.Tests.<Suite> --configuration Release`), `.github/workflows/pr-staging-ci.yml`/`staging-ci.yml` und `.githooks/pre-push`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --results-directory <test-results> --logger "trx;LogFileName=Unit.trx"` (ohne Filter; baut vorher mit) | Repository-Root | 0 | 1502 | 0 | 0 | [Log](test-results/Unit.log), [TRX](test-results/Unit.trx), [Exit](test-results/Unit.exit) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --results-directory <test-results> --logger "trx;LogFileName=Integration.trx"` (ohne Filter) | Repository-Root | 0 | 254 | 0 | 0 | [Log](test-results/Integration.log), [TRX](test-results/Integration.trx), [Exit](test-results/Integration.exit) |
| E2E | nicht ausgeführt | - | unbekannt | unbekannt | unbekannt | unbekannt | siehe Testlücken |

Bauwarnungen: keine im Unit-Log (Zählung "warning" = 0).

### Nachgewiesene bestehende Testfehler

Keine. Im Unit- und im Integrationslauf ist kein Test fehlgeschlagen. Über die E2E-Suite wird keine Aussage getroffen.

### Testlücken und Ausführungsprobleme

- **E2E-Suite (`Tankradar.Tests.E2E`, 124 Tests laut Abnahme 16) wurde in der Bestandsaufnahme nicht ausgeführt.** Grund: Der Anwender arbeitet parallel am Rechner; ein E2E-Vollauf dauert rund 11 bis 12 Minuten und startet die App (auch Off-Screen) und wurde laut Auftrag als unnötiger Volllauf vermieden. Nachweis aus der Vorstufe (keine eigene Messung, anderer Commit `481b0fa`, Basis dieses Branches `7adcb50` enthält darüber hinaus nur Dokumentation): `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/acceptance-schritt-16.md`: `scripts/local-ci.ps1` Exit 0, Unit 1502/1502, Integration 254/254, E2E 124/124, `SearchE2ETests_LargeResult` 4,4 s im Gesamtlauf, 5/5 in Einzelwiederholungen. Ein Fehler in der E2E-Suite darf deshalb nicht als "preexisting" bezeichnet werden, ohne einen eigenen Ausgangslauf.
- Weitere Prüfungen aus `.githooks/pre-push` (Format, XML-Doku, Übersetzung, Enum-Abdeckung, Platzhalter) wurden hier nicht gesondert ausgeführt.
- iOS-Compile und Paketierung nicht ausgeführt (Windows-Umgebung ohne gekoppelten Mac).
- Keine Build-, Setup- oder Infrastrukturfehler; Workloads waren vorhanden, die `Include*Target`-Variablen wurden gesetzt, damit der Build nicht an fehlenden Android-/macOS-Workloads scheitern kann.
- Der CI-Befund zu `SearchE2ETests_LargeResult` (Lauf `39287091864`, Versuch 1) ist im Code des aktuellen Stands bereits adressiert; ein Nachweis im CI steht laut Abnahme aus.

## Testklassen (relevant für die Anforderung)

### Unit (`src/Tankradar.Tests.Unit/Unit/`)
- `NominatimGeocodingServiceTests_Resolve`, `_Failures`, `_Throttle` (Basis `NominatimGeocodingServiceTestBase`) - Auflösung, Fehlerabbildung, Mindestabstand von 1 s zwischen Anfragen.
- `GeocodingOptionsTests_Validation` - HTTPS-Pflicht, Loopback nur im Testmodus, Mindestabstand.
- `AddressInput`-Prüfungen in `SearchTextsTests_Address` und `MapViewModelTests_Address`.
- `TankerkoenigClientTests_Parsing`, `_Prices`, `_Retry`, `_EndpointNotConfigured` (Basis `TankerkoenigClientTestBase`) - Parsing, Wiederholung mit Wartezeit, Fehlerarten.
- `PriceApiOptionsTests_Validation`, `RequestThrottleTests_Interval`.
- `FuelPriceServiceTests_Search`, `_Offline`, `_ManyStations`, `_StorageFailure`, `_InvalidateCaches` (Basis `FuelPriceServiceTestBase`).
- `ApiKeyProviderTests_Resolution`, `ApiKeyStoreSelectorTests_TestMode`.
- `LocationServiceSelectorTests_TestMode`, `LocationServiceTests_Permission`.
- `SearchRadiusTests_Validation`, `SettingsEnumTests_Persistence`.
- `StationResultBuilderTests_PriceLines`; `MapViewModelTests_Search`, `_Address`, `_Chips`, `_Concurrency`, `_Failures`, `_FilterSort`, `_MapView`, `_Offline`, `_OpenStation`, `_Privacy`, `_Settings` (Basis `MapViewModelTestBase`).
- `MapMarkerBuilderTests_Markers`, `MapMarkerSelectionTests_Limit`, `MapViewportTests_Math`, `MapPointerTrackerTests_Mouse`, `PriceLevelClassifierTests_Levels`.
- `E2EInputPolicyTests_NoFocus` - Quelltextprüfung des E2E-Projekts auf Fokus-, Vordergrund-, Tastatur- und Mausaufrufe (zwei `Fact`s).
- `PlatformManifestTests_Location`, `ViewModelTests_PageTitles`.

### Integration (`src/Tankradar.Tests.Integration/Integration/`)
- `GeocodingMockServerTests_Flow`, `AddressSearchMockServerTests_Flow`, `AddressSearchPrivacyTests_Persistence`.
- `SearchMockServerTests_Flow`, `_Offline`, `_OwnerName`, `_RealFormat` (Basis `Support/SearchMockServerTestBase`), `SearchPrivacyTests_Logging`, `SearchPrivacyTests_Persistence`.
- `PriceServiceMockServerTests_Flow`, `PriceRefreshMockServerTests_Flow`, `PriceRepositoryTests_*`.
- `TileSourceMockServerTests_Flow`, `LoopbackHttpListenerTests_Retry`.
- `ApiKeyBuildTests_Sources`, `ApiKeyBuildTests_Log` (Basis `Support/ApiKeyBuildRunner`) - MSBuild-Lauf mit Schlüsselquellen und Log-Prüfung.
- Migrationen: `DatabaseInitializerTests_FirstStart`, `_Upgrade`, `MigrationsTests_ModelSync`, `*MigrationTests_Upgrade` (Vehicles, Tankbook, Favorites, NearbySetting, PriceCache, StationCustomName).

### E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI/`, nicht ausgeführt)
- `SearchE2ETestBase` (Basis für Suche; erzeugt `MockTankerkoenigServer`, `MockNominatimServer`, `MockTileServer`; `CreateEnvironment` setzt `TANKRADAR_PRICE_API_URL`, `TANKRADAR_PRICE_API_KEY`, optional `TANKATLAS_TEST_LOCATION`, `TANKRADAR_GEOCODING_URL`, `TANKRADAR_TILE_URL`) mit Helfern `OpenSearch`, `SetRadius`, `SelectChip`, `Submit`, `WaitForStationNames`, `WaitForStatusMessage`, `ReadTexts`, `FindAllOrdered`, `TextsByStation`, `StationNamesOf`, `SelectSort`, `SelectFilter`, `SetGpsUsage`, `SelectAddressMode`, `SelectLocationMode`, `SetAddress` (nutzt `SetFieldValue("Search.Address.Input", ...)`), `ReadAddress`, `WaitForResolvedPlace`.
- Suchtests: `SearchE2ETests_Basic`, `_Address`, `_AddressErrors`, `_Errors`, `_FilterSort`, `_FuelOrder`, `_GpsNever`, `_LargeResult`, `_Offline`, `_Radius`.
- Karte: `MapE2ETestBase`, `MapE2ETests_Markers`, `_Navigation`, `_View`; Detail: `DetailE2ETests_Open`; Start: `HomeE2ETests_*`.
- `SearchE2ETests_LargeResult` (`LargeResult_ShowsOnePageAndSortChangeStaysFast`): 300 zusätzliche Mock-Stationen, `PageSize` 25; liest die Restzahl aus dem Namen der Schaltfläche `Search.ShowMore` ("Weitere anzeigen (N weitere)"), `PollTimeout` 5 s, `PhaseDeadline` 90 s, `MaxSortChange` 10 s, `MaxSortChangeWithTimeouts` 30 s, `InvokeTimeout` 60 s; privat `WaitForRemaining`, `ReadRemaining`, `InvokeWithLongTimeout`, `PhaseResult`. Stand der Hinweise aus Abnahme 16: Namensreihenfolge nach dem Sortierwechsel wird nicht geprüft; Schlusszählung (25 Einträge) ist über die Schaltflächenzahl nur indirekt belegt; "50 Einträge nach Weitere anzeigen" prüft kein Test; die 10-s-Grenze bezieht sich auf die Gesamtzeit, bei UIA-Timeouts gilt 30 s.
- `TankbookEntriesE2ETests_Receipt` (Beleg-Test; liest den Namen über `Properties.Name.ValueOrDefault`; `ElementNotAvailableException` zwischen Suche und Lesen führt zum sofortigen Abbruch des Wartens laut Abnahme 16).
- `DiagnosticsCaptureE2ETests`, `NavigationE2ETests`.

## Hilfsmethoden und Testinfrastruktur

### `src/TestSupport/`
- `LoopbackHttpListener.Start(out port, maxAttempts)` - startet `HttpListener` auf 127.0.0.1 mit Portwahl und Wiederholung bei Portkollision (zweite Überladung mit eigener Portwahl).
- `MockNominatimServer` - eigener Listener; feste Orte (`Berlin`, `10115`, `Oranienburg`, `Eberswalde`; `QueryUnknown` = "Nirgendwoburg" ohne Treffer), `BaseUrl`, `ResponseDelay`, `Requests`, `RequestTimes`, `LastQuery`, `LastUserAgent`, `LastRawRequest`, `EnqueueStatuses`, `EnqueueBody`; Antwort im Nominatim-`jsonv2`-Format.
- `MockTankerkoenigServer` - `list.php`/`detail.php`/`prices.php` für feste Stationen (`StationAlpha` bis `StationJetSide`), `AcceptedKey = "mock-key-for-tests"`, Radius bis 25 km (Entfernung per Haversine), `AddGeneratedStations(count)`, `AddOwnerNamedStation`, `AddSameBrandStations`, `SetPrices`, `EnqueueStatuses`, Zähler `ListRequests`, `DetailRequests`, `PricesRequests`, `TotalRequests`, `LastRequest`, `LastListRadius`, `LastListLatitude`, `LastListLongitude`, `LastPricesIds`, `ResponseDelay`.
- `MockTileServer` - Kachelantworten.
- `TestDataPaths` - Namen der Testmodus-Variablen: `TANKATLAS_TEST_DATA_PATH`, `TANKRADAR_PRICE_API_URL`, `TANKRADAR_GEOCODING_URL`, `TANKRADAR_TILE_URL`, `TANKRADAR_PRICE_API_KEY`, `TANKATLAS_TEST_LOCATION`, `TANKATLAS_TEST_RECEIPT_PHOTO`, `TANKATLAS_TEST_BACKUP_IMPORT`, `TANKATLAS_TEST_WINDOW` (`offscreen`/`foreground`). Eine Variable für eine Routing-Adresse gibt es nicht; ein Mock für OpenRouteService existiert nicht.
- `TransientWait.Until(condition, timeout, out lastTimeout, poll, sleep, elapsed)`, `TransientRetry` - Wiederholung bei UIA-Zeitüberschreitungen; `RecordingLogger`; `E2EStartupPolicy`; `TestDataPaths`; Sicherungshelfer `BackupTestArchive`, `BackupTestTampering`.

### E2E-Projekt
- `E2ETestBase` (`RunWithDiagnostics`, `NavigateToTab`, `RestartApplication`, Fenstermodus über `TANKATLAS_TEST_WINDOW`), `E2EDiagnostics`, `TankbookTestDatabase`.
- `SettingsE2ETestBase.SetFieldValue(automationId, text)` - setzt Texte per UIA-`Value`-Muster, prüft das Ergebnis und synchronisiert über UIA-Roundtrips ohne Fokuswechsel; `WaitUntil(condition, message[, timeout])`, `WaitForAutomationId`, `FindByAutomationId`.
- Unit-Support: `BaseTest`, `Unit/Support/*`.

### AutomationIds der Suche (`src/Tankradar.MAUI/Views/MapPage.xaml`)
`Search.Mode.{Key}`, `Search.Radius.{Key}`, `Search.View.{Key}`, `Search.Filter.{Key}`, `Search.Sort.{Key}`, `Search.Address.Input`, `Search.Address.Clear`, `Search.Attribution`, `Search.Submit`, `Search.StatusMessage`, `Search.ResolvedPlace`, `Search.SourceNote`, `Search.EmptyState`, `Search.OfflineBanner`, `Search.Map`, `Search.Map.Hint`, `Map.Legend`, `MapPage.Headline`, `Search.Station.Name`/`.Distance`/`.SubName`/`.Address`/`.Opening`/`.Open`, `Search.Fuel.{FuelType}`, `Search.Price.{FuelType}`, `Search.Age.{FuelType}`, `Search.Hint.Unconfirmed`, `Search.Hint.Automated`, `Search.ShowMore`.
