# Tests

Bestehende Tests und Hilfsmethoden rund um Suche, Lebenszyklus und Sitzungszustand der Karte sowie die „In der Nähe“-Suche.

## Testklassen (Unit, `src/Tankradar.Tests.Unit/Unit/`)

### `MapViewModelTests_Search` (Datei `MapViewModelTests_Search.cs`, Basis `MapViewModelTestBase`)
- `Appearing_DoesNotQueryLocationOrPrices` — **sichert das bisherige Verhalten ab**: Beim Erscheinen werden weder Standort noch Preisdienst abgefragt; Vorbelegung `RadiusKm` = 5, kein `ShowEmptyState`. Dieser Test kollidiert mit einer Startsuche in `OnAppearing`.
- `InvalidRadius_CallsNeitherLocationNorPrices` — Ungültiger Radius ruft nichts ab und zeigt `SearchTexts.RadiusInvalid`.
- `Success_SendsRadiusPositionAndAllSelectedFuelTypes` — Anfrage enthält Radius, Testposition und die gewählten Sorten; `Location.Calls` = `[GpsUsage.WhileInUse]`.
- `ValidRadius_IsSentUnchangedAndNeverAbove25` — Grenzwerte 1/5/25 werden unverändert gesendet.
- `GpsNever_PassesSettingAndSkipsPriceService` — `GpsUsage.Never` wird an den Standortdienst weitergereicht; keine Preisabfrage, Meldung `LocationDisabledBySetting`.
- `LocationProblems_ShowMatchingHintAndEmptyList` — Status `DisabledBySetting`/`PermissionDenied`/`Unavailable` → passende `SearchTexts`-Meldung.
- `NewSearch_ReplacesPreviousResultAndStatus` — Neue Suche ersetzt Ergebnis und Hinweis vollständig.
- `NoStations_WithoutFailure_ShowsEmptyState` — Leeres Ergebnis ohne Fehler zeigt `ShowEmptyState`.
- `SearchCommand_StartsSearch` — `SearchCommand` startet die Suche (`LastSearchTask` abwartbar).

### `MapViewModelTests_Settings` (Datei `MapViewModelTests_Settings.cs`)
- `DefaultSort_IsPreselectedFromSettings` — Standardsortierung aus den Einstellungen vorgewählt.
- `SessionSort_SurvivesReappearingUntilDefaultChanges` — In der Sitzung gewählte Sortierung bleibt über wiederholtes `OnAppearing` bestehen, bis sich der Standard ändert.
- `ResultViewMap_StillPreparesListStations` — Standardansicht „Karte“ startet eingeklappt; die Liste ist dennoch aufbereitet (kein neuer Abruf beim Ausklappen).
- `SettingsLoadFailure_BlocksSearchWithMessage` / `SettingsLoadSuccessAfterFailure_ClearsMessage` — Ladefehler blockiert die Suche (fail secure), späterer Erfolg räumt die Meldung.

### `MapViewModelTests_FilterSort` (Datei `MapViewModelTests_FilterSort.cs`)
- `Filter_SurvivesReappearing` — Gewählter Spritsortenfilter bleibt über erneutes Erscheinen erhalten.
- `Filter_FallsBackToAllWhenFuelIsDeselected` — Filter fällt auf „Alle“ zurück, wenn die Sorte abgewählt wurde.
- Weitere Tests prüfen Filter/Sortierung ohne erneuten Standort- oder Preisabruf (`Assert.Single(Location.Calls)`, `Assert.Single(Prices.Queries)`).

### `MapViewModelTests_Panel` (Datei `MapViewModelTests_Panel.cs`)
- `SessionPanelState_SurvivesReappearingUntilDefaultChanges` — Auf-/Zuklapptzustand der Trefferliste bleibt über das Erscheinen hinweg sitzungsbezogen.

### `MapViewModelTests_Concurrency` (Datei `MapViewModelTests_Concurrency.cs`)
- `OnDisappearing_CancelsRunningSearch` — Verlassen bricht die laufende Standortabfrage ab; keine Preisabfrage, keine Meldung, kein Ergebnis (Fake-`Gate` an `Location.Gate`).
- `SupersededSearch_DoesNotOverwriteNewerResult` — Eine abgelöste Suche überschreibt das neuere Ergebnis nicht.

### `MapViewModelTests_Offline` (Datei `MapViewModelTests_Offline.cs`)
- `OnDisappearing_UnsubscribesFromConnectionChanges` / `Reappearing_SubscribesAgain` — Event-Anmeldung genau einmal je sichtbarer Phase; erneutes Erscheinen meldet wieder an.

### `MapViewModelTests_AreaReloadTimer` (Datei `MapViewModelTests_AreaReloadTimer.cs`)
- `OnDisappearing_StopsTimer` / `OnAppearing_DuringBlock_RestartsTimer` / `OnAppearing_AfterBlock_ClearsHint` — Nachlade-Zeitgeber wird beim Verlassen angehalten und beim Erscheinen fortgesetzt.

### `MapViewModelTests_Route` / `MapViewModelTests_RouteAvailability` (Dateien `MapViewModelTests_Route.cs`, `MapViewModelTests_RouteAvailability.cs`; Basis `MapViewModelRouteTestBase`)
- `OnDisappearing_CancelsRunningRouteSearch` / `OnDisappearing_DuringRouteSearch_ClearsProgressAndReleasesSearch` — Abbruch der Routensuche beim Verlassen.
- `SwitchChangedWhilePageIsHidden_TakesEffectOnAppearing` / `ChangedEvent_IsSubscribedOnceAndReleasedOnDisappearing` — Routen-Verfügbarkeit wird beim Erscheinen neu abgeglichen.

### `ViewModelTests_PageTitles` (Datei `ViewModelTests_PageTitles.cs`)
- `MapViewModel_Constructor_SetsTitle` — Titel „Karte“; der Klassenkommentar verlangt, dass `OnAppearing` „ohne Seiteneffekte aufrufbar“ ist (für `MapViewModel` wird nur der Konstruktor geprüft).

### `NearbySectionViewModelTests_Section` (Datei `NearbySectionViewModelTests_Section.cs`)
- `Hidden_WhenSettingOff` / `Hidden_WhenGpsNever` / `Visible_WhenEnabledAndAllowed` — Sichtbarkeit nur bei `ShowNearby` und erlaubter Standortnutzung.
- `Load_BuildsEntriesSortedByDistanceMaxTen` / `Filter_ByFuelType_ChangesListWithoutNewRequest` — Aufbau und lokaler Filter ohne neue Abfrage.
- `NoLocation_ShowsLocationMessage` / `Offline_ShowsLastKnownEntries` / `NoResults_ShowsHint` — Meldungen und Offline-Stand.
- `Disabled_DoesNotRequestLocation` / `Enabled_RequestsLocationOnceEvenWithoutFavorites` — Standortabfrage nur bei Bedarf (über `FavoritesViewModel.RefreshAsync`-Aufrufmuster).

### `FavoritesViewModelTests_Home` / `FavoritesViewModelTests_Threading` (Basis `FavoritesViewModelTestBase`)
- `Disappearing_CancelsAndDropsPosition` — Verlassen bricht den Abruf ab und verwirft `_position`.
- `Disappearing_DuringLocation_KeepsNoPositionAndSkipsRequest` / `Disappearing_DuringLocation_WithoutStations_SkipsNearby` — Standortabfrage während des Verschwindens (`Location.OnCall`).

### `FuelPriceServiceTests_Search` (Datei `FuelPriceServiceTests_Search.cs`, Basis `FuelPriceServiceTestBase`)
- `IsSearchCached_ReflectsSearchCacheAndLifetime` — Cache-Antwort erst nach erfolgreichem Abruf, nur für denselben Schlüssel (Sorten egal, anderer Radius nicht), nur innerhalb von 5 Minuten (`Clock.Advance`), `ArgumentNullException` bei `null`.
- Weitere Tests zu Abruf, Speicherung, Sortenfilterung, Offline-Fallback.

### `LocationServiceTests_Permission` / `LocationServiceSelectorTests_TestMode`
- Berechtigungs- und Statusverhalten des `MauiLocationService`; Auswahl `TestLocationService` im Testmodus (`TANKATLAS_TEST_LOCATION`).

## Hilfsmethoden und Testdoubles (Unit)

### `MapViewModelTestBase` (Datei `MapViewModelTestBase.cs`)
- `AppearAsync()` — ruft `ViewModel.OnAppearing()` und wartet `LastSettingsTask` + `LastRouteAccessTask` ab.
- `AppearAndViewportAsync(mode, zoom, search)` — Erscheinen, Suchart wählen, optional suchen, Ausschnitt melden.
- `SearchAsync()` — führt `ViewModel.SearchAsync()` aus.
- `Result(source, failure, stations)` — erzeugt `StationSearchResult`.
- `RouteSuccess(startName, destinationName, stations)` — erfolgreiches `RouteSearchResult`.
- `TwoStations()` — zwei Teststationen („Nahe“, „Ferne“) mit allen Sorten.
- Fakes im Aufbau: `FakeSettingsService Settings`, `FakeLocationService Location`, `FakeGeocodingService Geocoding`, `FakeFuelPriceService Prices`, `FakeRouteSearchService RouteSearch`, `FakeRouteSearchAccess RouteAccess`, `FakeConnectionMonitor Connection`, `ManualTimeProvider Clock`, `FakeStationNavigator Navigator`, `RecordingLogger<MapViewModel> Logger`.

### `FakeLocationService` (Datei `Support/FakeLocationService.cs`)
- `Result` — geliefertes `LocationResult` (Standard: Berlin-Mitte `StationFactory.CenterLatitude/CenterLongitude`).
- `Calls` — `GpsUsage`-Werte aller Aufrufe (zählt Standortabfragen).
- `Gate` / `OnCall` — blockiert bzw. hakt in den Aufruf (laufende/abgebrochene Ortung).

### `FakeFuelPriceService` (Datei `Support/FakeFuelPriceService.cs`)
- `Result`, `Queries` — Suchergebnis und empfangene `StationSearchQuery`s (zählt Preisabfragen).
- `SearchGate`, `SearchException` — blockiert/wirft in `SearchNearbyAsync`.
- `IsSearchCached` — liefert fest `false` (steuerbar nur indirekt; `RouteDoubles` hat eine parametrierbare Variante).

### `FakeSettingsService` (Datei `Support/FakeSettingsService.cs`)
- `Stored`, `LoadException`, `LoadCount`, `Saved`, `SaveGate` — Einstellungen im Speicher samt Aufrufprotokoll; `Stored` mit `with`-Ausdruck veränderbar (z. B. `GpsUsage`, `ShowNearby`, `ResultSortOrder`).

### `StationFactory` (Datei `Support/StationFactory.cs`)
- `CenterLatitude` = 52.52, `CenterLongitude` = 13.405 (Berlin-Mitte als gemeinsame Testposition); `Create`/`CreateCustom` für `StationInfo`s mit Preisen.

### `FavoritesViewModelTestBase` (Datei `FavoritesViewModelTestBase.cs`)
- `CreateViewModel()` — baut `FavoritesViewModel` mit echtem `NearbySectionViewModel` und denselben Fakes (`Prices`, `Location`, `Connection`); `SeedPrices` versorgt beide mit Teststationen (`FavoritesFixture` auf In-Memory-SQLite).

## Integrationstests (`src/Tankradar.Tests.Integration/`)

### `SearchMockServerTestBase` (Datei `Integration/Support/SearchMockServerTestBase.cs`)
- `CreateService(baseUrl, key, minRequestInterval)` — **echter** `FuelPriceService` mit `TankerkoenigClient` gegen `MockTankerkoenigServer` und SQLite-Preis-Cache (`TestDatabase`); ermöglicht Cache-Nachweise über den echten Such-Cache.
- `CreateViewModel(service, geocoding, routeSearch)` — `MapViewModel` mit `SettingsService`, `TestLocationService` (feste Position `SearchLatitude`/`SearchLongitude` = 52.5123456/13.4123456), `SwitchableConnection`, `SwitchableTime`, `NullStationNavigator`.
- `Query(radiusKm)` — `StationSearchQuery` an der Suchposition mit `AllFuels`.
- `SwitchableConnection`/`SwitchableTime` — steuerbare Verbindung und Uhr; `LogText` sammelt alle Logzeilen.

### `MockTankerkoenigServer` (Datei `src/TestSupport/MockTankerkoenigServer.cs`)
- `ListRequests`, `DetailRequests`, `PricesRequests`, `TotalRequests` — Zähler je Endpunkt (Abruf-Nachweis).
- `LastListRadius`, `LastListLatitude`, `LastListLongitude`, `LastRequest` — Parameter der letzten Umkreisanfrage.
- `ListRequestTimes`, `EnqueueStatuses`, `ResponseDelay`, `AddGeneratedStations`, `AddStationAt`, `AddRouteStations`, `SetPrices` — Steuerung von Antworten und Testdaten.
- Stationskennungen `StationAlpha`/`StationBeta`/`StationGamma`/`StationDelta`/`StationEpsilon` u. a.; `AcceptedKey`.

## E2E-Tests (FlaUI, `src/Tankradar.Tests.E2E/E2E/FlaUI/`)

### `SearchE2ETestBase` (Datei `SearchE2ETestBase.cs`, Basis `SettingsE2ETestBase`/`E2ETestBase`)
- `TestLocation` = `"52.5200,13.4050"` — fester Teststandort über `TANKATLAS_TEST_LOCATION`; `CreateEnvironment` verdrahtet Mock-Preisdienst, Nominatim, Kachel- und Routingserver.
- `OpenSearch()` — `NavigateToTab("Karte")` und Warten auf `Search.Overlay.Open`/`Search.Panel.Toggle` (Tab-Wechsel als Auslöser von `OnAppearing`).
- `OpenSearchOverlay()`/`Submit()`/`SetRadius`/`SelectAddressMode`/`SelectLocationMode`/`SetGpsUsage` — Bedienung des Suchdialogs und der Optionen.
- `WaitForStationNames`, `WaitForStatusMessage`, `ReadPanelCount`, `Exists`, `WaitUntil`, `RunWithDiagnostics` — Abwarten und Prüfen der Oberfläche; `Server` zählt API-Abrufe.

### `MapE2ETestBase` (Datei `MapE2ETestBase.cs`, Basis `SearchE2ETestBase`)
- `SearchAndShowMap()` — Sucht am Teststandort und wartet auf Alpha/Beta samt Kartenzähler „2 von 2 Stationen sichtbar“.
- `WaitForMapCount`/`ReadMapCount`/`ReadZoom`/`PressMapButton`/`FindMarkers`/`IsAreaReloadShown`/`PressAreaReload` — Kartenanzeigen und -bedienung per UI Automation.
- `AlphaMarker`/`BetaMarker` — AutomationIds `Map.Marker.<id>`.

### `SearchE2ETests_Basic` (Datei `SearchE2ETests_Basic.cs`)
- `OpeningSearch_DoesNotQueryBeforeSubmit` — **sichert das bisherige Verhalten ab**: nach `OpenSearch` ist `Server.TotalRequests == 0`, keine Treffer, kein Leerzustand. Kollidiert mit einer Startsuche.
- `Search_DefaultRadius_ShowsNearStationsWithPricesAgeAndHints` — Suche mit Vorbelegung 5 km; `Server.ListRequests == 1`, `LastListRadius == 5`.

### `SearchE2ETests_GpsNever` (Datei `SearchE2ETests_GpsNever.cs`)
- Tests zur Standortnutzung „Nie“ in der Suche (Meldung statt Abfrage; via `SetGpsUsage`).

### `MapE2ETests_Navigation` (Datei `MapE2ETests_Navigation.cs`)
- `Zoom_ChangesLevelWithinLimits`, `ZoomIn_HidesDistantMarkers_AndRecenterShowsAll` — Kartenbedienung.
- `PanelToggleButton_CollapsesAndExpandsWithoutGesture` — prüft u. a. `Server.ListRequests == 1` (Panelwechsel löst keine neue Abfrage aus).

### `HomeE2ETestBase` / `HomeE2ETests_Nearby` / `HomeE2ETests_NearbyGpsNever`
- `OpenHome()` — Tab „Favoriten“; `EnableNearby()` schaltet „In der Nähe“ ein; Tests zu Sichtbarkeit, Sortierung, Filter und Detailnavigation des Bereichs (identische Abfrageform wie die Startsuche).

### `NavigationE2ETests`
- Tab-Wechsel-Hilfsmuster (`NavigateToTab(tabTitle, pageAutomationIds)` in `E2ETestBase` Zeile 137) für Hin- und Hernavigation zwischen den vier Bereichen.

## Test-Infrastruktur allgemein

### `TestDataPaths` (Datei `src/TestSupport/TestDataPaths.cs`)
- `TANKATLAS_TEST_DATA_PATH` — schaltet den Testmodus frei (Mock-Endpunkte, `TestLocationService`, isolierte Datenbank).
- `TANKATLAS_TEST_LOCATION` — fester Teststandort `breite,länge` (invariant) für `TestLocationService`.
- `TANKRADAR_PRICE_API_URL`, `TANKRADAR_PRICE_API_KEY`, `TANKRADAR_GEOCODING_URL`, `TANKRADAR_TILE_URL`, `TANKRADAR_ROUTING_URL`, `TANKRADAR_ROUTING_TEST_API_KEY`, `TANKRADAR_ROUTE_QUERY_INTERVAL_MS`, `TANKATLAS_TEST_WINDOW`.
