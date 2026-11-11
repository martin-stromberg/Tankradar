# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### `MapViewModel` (`src/Tankradar.MAUI/ViewModels/MapViewModel.cs`)

- [x] Feld `_autoSearchDone` (`bool`, privat) — vorhanden (Zeile 53)
- [x] Methode `MaybeStartAutoSearch()` (privat, `void`) — vorhanden (Zeilen 512–520): Guard `_autoSearchDone \|\| _lastResult is not null \|\| _searchMode != SearchMode.CurrentLocation`, Start über `LastSearchTask = AutoSearchOnceAsync()`
- [x] Methode `AutoSearchOnceAsync()` (privat, `Task`) — vorhanden (Zeilen 522–540): `GetSettingsOrReportAsync(clearResult: false, …)` zuerst; bei `settings is null` Auswertung `_autoSearchDone = _lastResult is not null \|\| HasStatusMessage`; bei `!settings.GpsUsage.AllowsLocation()` stilles Ende ohne Flag; sonst `await SearchAsync()` und danach `_autoSearchDone = _lastResult is not null \|\| HasStatusMessage`
- [x] `OnAppearing()` — `MaybeStartAutoSearch()` am Ende aufgerufen (Zeile 451); Methodenkommentar angepasst („stößt beim ersten Anzeigen die Startsuche am aktuellen Standort an"); alle bisherigen Schritte unverändert
- [x] `SearchAsync`, `SearchCommand`, `LastSearchTask`, `ClearResult`, `ApplyResult`, `MapFitMode`, `OnDisappearing` — unverändert

### `MapViewModelTestBase` (`src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`)

- [x] Methode `AppearAndAutoSearchAsync()` — vorhanden (Zeilen 110–116): `OnAppearing()` plus Abwarten von `LastSettingsTask`, `LastRouteAccessTask` und `LastSearchTask`
- [x] `AppearAsync()` — unverändert, kein `LastSearchTask`-Abwarten (kein Deadlock bei Gate-Tests)

### Neue Unit-Tests `MapViewModelTests_AutoLoad` (`src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AutoLoad.cs`)

- [x] `FirstAppearing_SearchesAtCurrentLocationWithDefaultRadius` — vorhanden
- [x] `Reappearing_KeepsResultWithoutNewQueries` — vorhanden
- [x] `Reappearing_AfterEmptyResult_DoesNotRepeat` — vorhanden
- [x] `Reappearing_AfterFailureMessage_DoesNotRepeat` — vorhanden (Theory mit `failAtLocation` true/false: `PermissionDenied` und `SearchException`)
- [x] `DisappearingDuringAutoSearch_ReappearingRetries` — vorhanden (`Location.Gate`, Abbruch ohne Ausgang, erneuter Start beim nächsten Erscheinen)
- [x] `GpsNever_SkipsAutoSearchSilently` — vorhanden (keine Calls/Queries, keine Meldung, kein Leerzustand)
- [x] `GpsNever_EnabledLater_AutoSearchRunsOnReappear` — vorhanden
- [x] `AppearingInAddressMode_DoesNotAutoSearch` — vorhanden

### Angepasste Unit-Tests

- [x] `MapViewModelTests_Search.Appearing_DoesNotQueryLocationOrPrices` — umgewidmet zu `Appearing_StartsSearchAtCurrentLocation` (Positive-Abdeckung)
- [x] `MapViewModelTests_Search.InvalidRadius_CallsNeitherLocationNorPrices` — Zähler relativ zur Startsuche
- [x] Aufrufzähler in `MapViewModelTests_Search`, `_Address` (Zähler-Reset in `SelectAddressModeAsync`), `_AreaReload`, `_AreaReloadBlock`, `_Chips`, `_FilterSort`, `_MapView`, `_Panel`, `_Route`, `_RouteAvailability`, `_Settings`, `_Sort`, `_SearchOverlay` angepasst; `MapViewModelTests_RouteValidation` benötigte keine Änderung (Zähler-Reset über `PrepareRouteAsync` in `MapViewModelRouteTestBase`)
- [x] `MapViewModelTests_Concurrency.OnDisappearing_CancelsRunningSearch` und `SupersededSearch_DoesNotOverwriteNewerResult` — an die Startsuche angepasst (manuelle Suche bleibt gegatet; die Startsuche als „laufende Suche" deckt `DisappearingDuringAutoSearch_ReappearingRetries` ab)
- [x] Übrige `MapViewModelTests_*` (`_Offline`, `_AreaReloadTimer`, `_Failures`, `_OpenStation`, `_Privacy`, übrige `_Route*`) — geprüft, ohne Änderungsbedarf; Unit-Gesamtlauf 2091/2091 grün laut `test-results.md`

### Integrationstest (`src/Tankradar.Tests.Integration/Integration/AutoSearchMockServerTests_Cache.cs`)

- [x] `AutoSearchMockServerTests_Cache` (neu, auf `SearchMockServerTestBase`): `AutoSearch_UsesSearchCache_AfterIdenticalQuery` — identische 5-km-Abfrage vorab (`IsSearchCached` = true, `ListRequests` = 1), dann `OnAppearing` + `LastSearchTask` abwarten: `ListRequests` bleibt 1, Ergebnis befüllt
- [x] Bestehende Integrationstests angepasst (`LastSearchTask`-Abwarten nach `OnAppearing`, relative Zähler, Adressmodus-Vorwahl in `RouteSearchMockServerTestBase`/`AddressSearchMockServerTests_Flow` zur Unterdrückung der Startsuche)

### E2E-Tests (`src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_AutoLoad.cs`)

- [x] Erstmaliges Laden — `OpeningMap_LoadsNearbyStationsWithoutSubmit` (Plannname `FirstOpen_LoadsStationsAtTestLocation`; Alpha/Beta, `ListRequests` == 1, `LastListRadius` == 5)
- [x] Tab-Wechsel — `TabSwitch_PreservesResultWithoutNewRequest`
- [x] Detail-Rückkehr — `DetailReturn_PreservesResultWithoutNewRequest` (Plannname `ReturnFromDetail_PreservesResult`)
- [x] `GpsUsage.Never` — `GpsNever_SkipsAutoSearchSilently` (Plannname `GpsNever_FirstOpen_SkipsAutoSearch`; `TotalRequests` == 0, keine Meldung, kein Leerzustand)
- [x] Leeres Gebiet — `EmptyArea_ShowsEmptyStateWithoutRepeatedRequest` (Plannname `FirstOpen_WithoutStations_ShowsEmptyState`)
- [x] `SearchE2ETests_Basic.OpeningSearch_DoesNotQueryBeforeSubmit` — entfernt
- [x] Request-Zähler-Anpassungen: `SearchE2ETestBase.WaitForAutoSearch()` neu, `Submit()` wartet Schaltflächen-Freigabe, `RouteE2ETestBase.OpenRouteSearch(expectAutoSearch)` mit `ListRequestsBeforeRoute`/`TotalRequestsBeforeRoute`, `NavigationE2ETests` erbt `SearchE2ETestBase`; relative Zähler in `MapE2ETests_SearchOverlay`, `MapE2ETests_View`, `MapE2ETests_Layout`, `RouteE2ETests_*`, `SearchE2ETests_*`; `MapE2ETests_AreaReload`/`Markers`/`Panel` und `SearchE2ETests_GpsNever` ohne Änderungsbedarf — E2E-Lauf grün laut `test-results.md`

### Verifikation

- [x] Build sowie Unit-, Integrations- und E2E-Läufe dokumentiert in `test-results.md` (Build 0 Fehler; Unit 2091/2091; Integration 298/298; E2E vollständig grün)

## Hinweise

- **Abweichende E2E-Testnamen:** Vier der fünf geplanten `MapE2ETests_AutoLoad`-Tests tragen andere Namen als im Plan (siehe oben); die Akzeptanzkriterien sind jeweils vollständig abgedeckt.
- **`CancellationToken.None`:** `AutoSearchOnceAsync` ruft `GetSettingsOrReportAsync` mit `CancellationToken.None` statt eines Such-Abbruchtokens (Plan: „token"). Funktional unkritisch — die Deduplizierung läuft über `_settingsLoad`, und der eigentliche Abbruchpfad (`CancelSearch` in `SearchAsync`) ist unverändert wirksam; ein Abbruch während des Einstellungsladens verzögert lediglich den frühen Ausstieg.
- **Cache-Nachweis im Integrationstest:** Der Plan sah eine Assertion `Source == PriceDataSource.Cache` vor; implementiert ist der Nachweis indirekt über `Server.ListRequests == 1` (kein zweiter API-Abruf nach identischer Abfrage). Das Cache-Kriterium ist damit belegt, die Quellangabe am Ergebnis wird nicht direkt geprüft.
- **Tab-Wechsel-Szenario:** `TabSwitch_PreservesResultWithoutNewRequest` wechselt Karte → „Tankbuch" → Karte statt Karte → „Favoriten" → Karte; das Kriterium „Ergebnis bleibt beim Hin- und Hernavigieren erhalten" ist erfüllt.
