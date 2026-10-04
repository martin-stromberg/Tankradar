# Umsetzungsplan (Schritt 5)

## Datenmodell (Migration `AddPriceCache`, per `dotnet ef`)
- `Stations` (Id string PK, Name, Brand, Street, HouseNumber, PostCode, Place, Latitude, Longitude, WholeDay?, OpeningTimesJson?, DetailsUpdatedUtc?, LastSeenUtc).
- `PriceEntries` (Id long PK, StationId FK, FuelTypeKey, Price decimal, RetrievedUtc; Index StationId+FuelTypeKey+RetrievedUtc). Nur Neuanlage, keine Änderung bestehender Tabellen.

## Komponenten (`src/Tankradar.MAUI/Services/Pricing`, Modelle in `Models/Pricing`)
- `PriceFreshness` (60-Min-Grenze, „vor X Min.“), `StationHints` (Automatentankstelle aus Öffnungszeiten, Preis unbestätigt).
- `PriceApiOptions` (Basis-URL, Zeitlimit, Wiederholungen, Mindestabstand, Cache-Dauer; HTTPS-Pflicht, http nur Loopback im Testmodus).
- `TankerkoenigClient` (Parsing, Zeitlimit, Wiederholung exponentiell, Drosselung über `IRequestThrottle`/`IDelay`).
- `IApiKeyStore` (Windows: Credential Locker, iOS und sonst: SecureStorage/Keychain), `ApiKeyProvider` (Build-Schlüssel aus Assembly-Metadaten -> sichere Ablage; Testmodus-Override).
- `PriceRepository` (EF: speichern mit Zeitstempel, letzte Preise, Umkreis aus Cache via Haversine, nichts von Nutzerposition gespeichert).
- `FuelPriceService` (Cache-Entscheidung, Live, Offline-Rückfall, Fehlerabbildung ohne Ausnahme zum Aufrufer, `CheckAvailabilityAsync`).
- `ConnectionMonitor` + `INetworkStatusSource` (MAUI-Connectivity), Ereignisse `ConnectionChanged`/`ConnectionRestored`.
- Build-Schlüssel: MSBuild-Eigenschaft `TankerkoenigApiKey` (Umgebungsvariable `TANKERKOENIG_API_KEY` oder gitignorierte `tankerkoenig.local.props`) -> `AssemblyMetadata`.

## Oberfläche
- Optionen: Karte „Datenquelle“ mit Quellenangabe, Schlüsselstatus, Schaltfläche „Verbindung zum Preisdienst prüfen“ und Statusmeldung (AutomationIds `Settings.Attribution`, `Settings.PriceService.Check`, `Settings.PriceService.Status`).

## Tests
- Unit: Freshness/Grenze, Hinweise, Optionen/HTTPS, Client (Fake-Handler: Parsing, Retry, Timeout, 4xx ohne Retry), Service (Cache, Offline, Fehler), ConnectionMonitor, ViewModel, UIA-Retry-Helfer.
- Integration: Repository (Zeitstempel, Historie, Umkreis), Migration auf bestehender DB mit Daten, Client+Service gegen echten lokalen Mock-Server (`MockTankerkoenigServer`, HttpListener).
- E2E: Quellenangabe sichtbar; Dienstprüfung gegen Mock erreichbar bzw. nicht erreichbar (Testkonfiguration über `TANKRADAR_PRICE_API_URL`, nur mit `TEST_DATA_PATH`).
- E2E-Robustheit: `TransientRetry` (TimeoutException/COMException 0x80131505, 3 Versuche, klare Meldung) für UIA3-Aufbau und `GetMainWindow`.

## Dokumentation
Hilfeseite `docs/help/Preisdaten/`, Schlüsselhinterlegung lokal und als GitHub-Secret (CI-Workflows reichen `TANKERKOENIG_API_KEY` durch), README, changes.log.

## Offene Punkte
(keine)
