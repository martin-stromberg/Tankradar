# Bestandsaufnahme

Aufbauend auf Schritt 6 (Umkreissuche):

- `MapViewModel` (Services: Settings, `ILocationService`, `IFuelPriceService`, `IConnectionMonitor`, `TimeProvider`): `SearchAsync` -> `RunSearchAsync` ermittelt Standort, ruft `SearchNearbyAsync(StationSearchQuery)`, bereitet Liste per `StationResultBuilder` (Filter/Sortierung/Paging) auf. Chips: `ChoiceOptionBase`/`ChoiceOptionViewModel<T>`/`RadiusOptionViewModel`; `SearchRadius` (1–25, Stufen 1/2/5/10/15/25).
- `MapPage.xaml`: Karten (Radius, Suchen, Filter, Sortierung), AutomationIds `Search.*`.
- `SearchTexts`: zentrale deutsche Texte.
- `RequestThrottle`/`IDelay`/`TaskDelay` (Services/Pricing/Throttling.cs): wiederverwendbare Drosselung.
- `PriceApiOptions.FromEnvironment`: Testmodus über `TANKATLAS_TEST_DATA_PATH`, Endpunkt-Override nur dort, Fail-Secure ohne Endpunkt.
- `TankerkoenigClient`: Vorlage für HTTP-Zugriff (kein Redirect, Typ-only-Logging).
- `TestSupport`: `MockTankerkoenigServer` (HttpListener Loopback), `TestDataPaths`, `TransientRetry`.
- Tests: `MapViewModelTestBase` (Fakes), `SearchMockServerTestBase` (Integration), `SearchE2ETestBase` (FlaUI, off-screen).
- Docs: `docs/help/Suche/*`, `docs/help/index.md`, README Abschnitt Suche; ADR 0002 (Designabweichungen).
- MauiProgram: DI-Registrierung; `HttpClient` (UA Tankatlas/0.1) nur für Tankerkönig.

Lücken: kein Geokodierungsdienst, kein Adressmodus, keine Eingabevalidierung, keine OSM-Quellenangabe.
