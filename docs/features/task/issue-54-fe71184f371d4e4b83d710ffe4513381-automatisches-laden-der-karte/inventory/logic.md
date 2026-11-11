# Logik

Betroffene Logikklassen der Anforderung „Automatisches Laden der Karte“.

## `MapViewModel`
Dateien: `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`, `MapViewModel.Panels.cs`, `MapViewModel.AreaReload.cs`, `MapViewModel.Route.cs` (partial)

Zentrale Logik der Karte. Relevante Felder für die Anforderung: `_lastResult` (letztes `StationSearchResult`), `_searchCancellation` (laufende Suche), `_searchMode` (Start: `SearchMode.CurrentLocation`), `_radiusKm` (Start: `SearchRadius.Default` = 5), `_mapMarkers`, `_origin`, `_allStations`/`_stations`/`_visibleCount`, `_settings`/`_settingsLoad`, `_subscribed`, `_routeResult`/`_routeSnapshot`/`_mapRoute`, `_isBottomSheetExpanded`, `_isSearchOverlayOpen`. Ein sitzungsbezogenes Flag „Startsuche bereits ausgelöst“ existiert **nicht**.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing()` | `public override` | Meldet `ConnectionChanged`/`IRouteSearchAccess.Changed` einmalig an (`_subscribed`), aktualisiert Offline-Zustand, setzt den Nachlade-Zeitgeber fort (`ResumeAreaReloadTimer`), startet `LastSettingsTask = LoadSettingsAsync()` und `LastRouteAccessTask = RefreshRouteAvailabilityAsync()`. Startet **keine** Suche. |
| `OnDisappearing()` | `public override` | Meldet die Events ab, ruft `CancelSearch()` und `StopAreaReloadTimer()` auf, leert `RouteProgressText` und setzt `IsBusy = false`. Verwirft **kein** Ergebnis (`_lastResult`, `_routeResult`, `MapMarkers`, `Origin`, Filter, Sortierung und Panelzustand bleiben erhalten). |
| `SearchAsync()` | `public async Task` | Bricht laufende Suche ab (`CancelSearch`), setzt `MapFitMode.FitToResult`, verzweigt bei `SearchMode.Route` zu `SearchRouteAsync`, prüft `SearchRadius.IsValid` und Adresseingabe, dann `RunGuardedSearchAsync` mit `RunSearchAsync(_radiusKm, token)`. Wird von `SearchCommand` (`LastSearchTask = SearchAsync()`) ausgelöst. |
| `RunGuardedSearchAsync(work, logFailure, onFinished)` | `private async Task` | Rahmen jeder Suche: neue `CancellationTokenSource`, `IsSearchOverlayOpen = false`, `IsBusy = true`; verwirft abgelöste Läufe (`IsCurrent`), protokolliert nur den Ausnahmetyp, bei Fehler `ClearResult()` + `SearchTexts.SearchFailed`. |
| `RunSearchAsync(radiusKm, token)` | `private async Task` | Einstellungen laden (`GetSettingsOrReportAsync`), Position je nach Modus (`ResolveCurrentLocationAsync`/`ResolveAddressAsync`), `StationSearchQuery` + `MapOrigin` bauen, `_priceService.SearchNearbyAsync`, `ApplyResult(result, origin, FitToResult)`. |
| `ResolveCurrentLocationAsync(settings, token)` | `private async Task<GeoPosition?>` | Ruft `_locationService.GetCurrentLocationAsync(settings.GpsUsage, token)`; bei Status ≠ `Available`: `ClearResult()` und `SearchTexts.GetLocationMessage(status)` als `StatusMessage`. |
| `ApplyResult(result, origin, fitMode)` | `private void` | Setzt `MapFitMode`, bei `FitToResult` `ResetAreaReload()`; übernimmt `_lastResult`, `Origin`, erhöht `MapResultVersion`, `ApplyFilterAndSort()`, Status-/Offline-Meldungen. |
| `ClearResult()` | `private void` | Verwirft Ergebnis vollständig: `ClearRouteState()`, `_lastResult = null`, Liste, `Origin`, `MapMarkers`, `MapResultVersion++`, `Stations`. Wird bei Moduswechsel, Fehlschlag und ungültiger Eingabe aufgerufen — **nicht** bei `OnDisappearing`/`OnAppearing`. |
| `ApplyFilterAndSort(bool resetPaging)` / `PublishVisible()` | `private` | Baut `_allStations` über `StationResultBuilder.Build` bzw. `BuildForRoute` und `MapMarkers` über `MapMarkerBuilder.Build` aus `_lastResult`/`_routeResult` neu auf (ohne Standort- oder API-Aufruf). |
| `LoadSettingsAsync()` / `LoadSettingsCoreAsync()` / `GetSettingsOrReportAsync(clearResult, token)` | `private` | Lädt `AppSettings` einmalig je Lauf (`_settingsLoad` dedupliziert), übernimmt Standardsortierung/-ansicht nur bei geändertem Standard (`_loadedDefaultSort`/`_loadedDefaultView`), baut `FuelFilterOptions` neu und wendet Filter bei vorhandenem `_lastResult` erneut an. |
| `ShowMore()` / `OpenStationAsync(station)` | `public`/`private` | Seitenweise Liste (`PageSize` = 25) bzw. Detailnavigation über `IStationNavigator`. |
| `OnViewportChanged(viewport)` / `OnViewportFitted(viewport)` | `public` (AreaReload) | Übernimmt den aktuellen bzw. eingepassten Ausschnitt für die Nachlade-Schaltfläche. |
| `ReloadVisibleArea()` / `RunAreaReloadAsync()` / `ReloadVisibleAreaAsync(token)` | `private` (AreaReload) | „Tankstellen in diesem Bereich laden“: Umkreisabfrage um die Ausschnittsmitte über `MapAreaQuery.From`, Sperre `AreaReloadMinInterval` (60 s), `ApplyResult(result, _origin, KeepViewport)` — die ursprüngliche `Origin`-Markierung bleibt. |
| `ChangeMode(mode)` | `private` | Moduswechsel: `CancelSearch`, `ResetAreaReload`, `ClearResult`, Statusmeldung leeren. |
| `SearchRouteAsync()` / `ExecuteRouteSearchAsync(cancellation)` / `ApplyRouteResult(result, snapshot)` | `private` (Route) | Routensuche mit `ResolveCurrentLocationAsync` als optionalem Start; `ApplyRouteResult` füllt `_routeResult`, `_lastResult`, `Origin`, `MapRoute`, `RouteSummary`. |
| `CancelSearch()` / `IsCurrent(cancellation)` | `private` | Bricht die laufende Suche ab, leert `RouteProgressText` und `IsAreaReloading`; nur der „aktuelle“ Lauf räumt auf. |
| `RefreshRouteAvailabilityAsync()` / `ApplyRouteAvailability(enabled)` | `private` (Route) | Blendet die Suchart „Route“ je nach `IRouteSearchAccess` ein/aus; fällt bei Abschalten im Routenmodus auf `CurrentLocation` zurück (mit `ClearResult`). |
| `InitializePanels()` / `InitializeAreaReload()` / `InitializeRouteOptions()` | `private` | Befehle und Auswahllisten: `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand`, `ToggleLegendCommand`, `ReloadVisibleAreaCommand`, Radius-/Modus-/Sortier-/Routenoptionen. |

Relevante Eigenschaften für die Anforderung: `LastSearchTask` (öffentliches Abwarten der Suche, auch für `ReloadVisibleArea`), `LastSettingsTask`, `LastRouteAccessTask`, `SearchCommand`, `MapFitMode`, `Origin`, `MapMarkers`, `MapResultVersion`, `ShowEmptyState`, `StatusMessage`, `IsSearchOverlayOpen`, `IsBottomSheetExpanded`.

Abonnierte Events: `IConnectionMonitor.ConnectionChanged` (in `OnAppearing`), `IRouteSearchAccess.Changed` (in `OnAppearing`), eigenes `PropertyChanged` (`OnOwnPropertyChanged`, `OnAreaReloadPropertyChanged`).
Publizierte Events: nur `PropertyChanged` (`BaseViewModel`).

## Seiten-Lebenszyklus: `TankradarContentPage`, `BaseViewModel`, `MapPage`
Dateien: `src/Tankradar.MAUI/Views/TankradarContentPage.cs`, `src/Tankradar.MAUI/ViewModels/BaseViewModel.cs`, `src/Tankradar.MAUI/Views/MapPage.xaml.cs`

| Methode | Klasse | Kurzbeschreibung |
|---------|--------|------------------|
| `OnAppearing()` | `TankradarContentPage` (protected override) | Ruft `(BindingContext as BaseViewModel)?.OnAppearing()` bei **jedem** Anzeigen auf — die von der Anforderung vermutete Weiterleitung ist bestätigt; `MapPage` muss nicht angefasst werden. |
| `OnDisappearing()` | `TankradarContentPage` (protected override) | Leitet analog an `BaseViewModel.OnDisappearing` weiter. |
| `OnAppearing()` / `OnDisappearing()` | `BaseViewModel` (`public virtual`) | Leere Basishooks; `MapViewModel` und `FavoritesViewModel` überschreiben sie. |
| `MapPage(viewModel, tileSource)` | `MapPage` (Konstruktor) | Setzt `BindingContext = viewModel`; `OnBackButtonPressed` schließt zuerst den Suchdialog; `OnMapViewportChanged`/`OnMapViewportFitted` reichen den Ausschnitt weiter. |

## `FavoritesViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/FavoritesViewModel.cs`

ViewModel der Startseite; hält die Position nur im privaten Feld `_position` (nur Arbeitsspeicher).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing()` | `public override` | Meldet `ConnectionChanged`/`ConnectionRestored` an, `LastTask = LoadAndRefreshAsync()` (lädt Gruppen und aktualisiert bei **jedem** Erscheinen erneut). |
| `OnDisappearing()` | `public override` | Meldet Events ab, `CancelRefresh()`, `SetBusy(false)` und **`_position = null`** — verwirft den Standort beim Verlassen. |
| `RefreshAsync(bool force)` | `public async Task` | Ermittelt ein `LocationResult` über `GetLocationAsync(settings.GpsUsage, token)`, aber nur wenn `GpsUsage.AllowsLocation()` und (`ids.Count > 0` oder `settings.ShowNearby`); danach `RefreshStationsAsync`, `Rebuild()` und `Nearby.LoadAsync(settings, location, token)` — das `LocationResult` wird an den Bereich weitergereicht, nicht an andere ViewModels. |
| `LoadAsync()` | `public async Task` | Lädt Gruppen und lokal bekannte Preise ohne Abruf (`GetKnownStationsAsync`). |
| `GetLocationAsync(gpsUsage, token)` | `private async Task<LocationResult>` | Ruft `_locationService.GetCurrentLocationAsync`; Ausnahmen (außer Abbruch) werden zu `LocationResult.Failure(Unavailable)`. |
| `HandleConnectionRestored()` | `private` | Nach Wiederverbindung `RefreshAsync(force: false)`, wenn zuletzt Offline-Stand oder veraltete Preise. |

Abonnierte Events: `IConnectionMonitor.ConnectionChanged`, `IConnectionMonitor.ConnectionRestored`, Favoritenänderungen über `FavoritesChangeSubscription.Attach`.

## `NearbySectionViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/NearbySectionViewModel.cs`

Bereich „In der Nähe“; ermittelt den Standort **nicht** selbst, sondern erhält das `LocationResult` der Startseite.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(settings, location, cancellationToken)` | `public async Task` | Blendet den Bereich bei `!ShowNearby` oder `!GpsUsage.AllowsLocation()` aus (`Hide`); ohne `location.Position` Standortmeldung (`SearchTexts.GetLocationMessage`); sonst `StationSearchQuery(position.Latitude, position.Longitude, SearchRadius.Default, selected)` → `_priceService.SearchNearbyAsync` — **dieselbe Abfrageform** (5 km) wie eine Karten-Startsuche. |
| `ApplyFilter()` | `private` | Filtert `_stations` lokal über `StationResultBuilder.Build` (Sortierung `ResultSortOrder.Distance`, `MaxItems` = 10) — kein neuer Abruf. |
| `Hide()` | `private` | Setzt Sichtbarkeit, Liste und `_source` zurück. |

Relevante Eigenschaften: `IsVisible`, `Entries`, `FilterOptions`, `StatusMessage`, `IsOfflineResult` (`_source == OfflineFallback`), `LastOpenTask`.

## `FuelPriceService` (Such-Zwischenspeicher)
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs` (implementiert `IFuelPriceService`, `IPriceCacheInvalidator`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchNearbyAsync(query, cancellationToken)` | `public async Task` | `Validate(query)`; Cache-Schlüssel `CacheKey` = `Round(lat,3)|Round(lng,3)|RadiusKm` (invariant, ohne Sorten); Treffer jünger als `_options.CacheLifetime` → `PriceDataSource.Cache` aus `IPriceRepository.FindNearbyAsync`, sonst API-Abruf `_client.SearchAsync`, Speichern (`TrySaveAsync`), `_searchCache[key]` stempeln, `Live`-Ergebnis; offline/Api-Fehler → `SearchFallbackAsync` mit `OfflineFallback`. |
| `IsSearchCached(query)` | `public bool` | Liefert, ob `SearchNearbyAsync` ohne Abruf bedient würde (Schlüssel vorhanden und jünger als `CacheLifetime`); wirft `ArgumentNullException` bei `null`. |
| `PruneSearchCache(now)` | `private` | Entfernt abgelaufene Einträge (aufgerufen aus `SearchNearbyAsync`, `RefreshStationsAsync`, `GetKnownStationsAsync`). |
| `InvalidateCaches()` | `public` (`IPriceCacheInvalidator`) | Leert `_searchCache` und `_refreshed`. |

Konstanten/Felder: `_searchCache` (`ConcurrentDictionary<string, DateTime>`), `CacheLifetime` aus `PriceApiOptions` = 5 Minuten (`src/Tankradar.MAUI/Services/Pricing/PriceApiOptions.cs` Zeile 66). Konsequenz: „In der Nähe“ und eine identische Karten-Startsuche teilen sich den API-Abruf über den Cache — die Standortermittlung jedoch nicht.

## Standortdienste
Dateien: `src/Tankradar.MAUI/Services/Location/MauiLocationService.cs`, `TestLocationService.cs`, `LocationServiceSelector.cs`

| Klasse / Methode | Kurzbeschreibung |
|------------------|------------------|
| `MauiLocationService.GetCurrentLocationAsync(gpsUsage, token)` | `!gpsUsage.AllowsLocation()` → `DisabledBySetting` **ohne** Berechtigungs- oder Positionsabfrage; sonst `Permissions.LocationWhenInUse` prüfen/anfordern und `Geolocation.GetLocationAsync` (Accuracy Medium, 10 s Timeout); Fehler → `LocationResult.Failure` (`PermissionDenied`/`Unavailable`), nie Ausnahme außer Abbruch. Protokolliert nur Status/Typen, nie Koordinaten. |
| `TestLocationService` | Liefert im Testmodus einen festen `GeoPosition?` aus `TANKATLAS_TEST_LOCATION` (oder `Unavailable`); respektiert ebenfalls `AllowsLocation` → `DisabledBySetting`. |
| `LocationServiceSelector.Create(getEnvironmentVariable, createPlatformService)` | Wählt im Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) ausschließlich `TestLocationService`; `TryParseLocation` parst `breite,länge` invariant über `GeoPosition.TryCreate`. Registriert wird `ILocationService` als **Singleton** (`MauiProgram.cs` Zeile 96–98). |

## Aufbereitung: `StationResultBuilder`, `MapMarkerBuilder`
Dateien: `src/Tankradar.MAUI/Services/Search/StationResultBuilder.cs`, `src/Tankradar.MAUI/Services/Map/MapMarkerBuilder.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `StationResultBuilder.Build(stations, fuelTypes, filter, sortOrder, nowUtc)` | Filtert auf gewählte Sorten, erzeugt `StationListItem`s (Preiszeilen, Alter, Hinweise, Entfernung), sortiert nach `ResultSortOrder` (Fallback Preis). |
| `StationResultBuilder.BuildForRoute(routeStations, …)` | Wie `Build`, zusätzlich Streckenposition („ab Start“) und Abstand zur Route. |
| `StationResultBuilder.ResolveFuelType(fuelTypes, filter)` | Maßgebliche Sorte für Karte/Sortierung: gewählter Filter, sonst erste gewählte Sorte. |
| `MapMarkerBuilder.Build(stations, fuelType)` | Erzeugt `MapMarker` nur für Stationen mit gültiger Position (`HasPosition`), mit Preisniveau aus `PriceLevelClassifier` und Bedienhilfen-Text. |

## DI-Registrierung und Shell
Dateien: `src/Tankradar.MAUI/MauiProgram.cs`, `src/Tankradar.MAUI/AppShell.xaml`

- `builder.Services.AddTransient<MapViewModel>()` (Zeile 102) und `AddTransient<MapPage>()` (Zeile 130); `ILocationService` (Zeile 96) und `IFuelPriceService` (Zeile 307) sind **Singletons** — der Such-Cache des `FuelPriceService` ist damit sitzungsweit.
- `AppShell.xaml` (Zeilen 99–123): `TabBar` mit vier `ShellContent`; die Karte ist `Route="map"` mit `ContentTemplate="{DataTemplate views:MapPage}"`. Die `ShellContent`-Instanz bleibt sitzungslang bestehen — damit auch die einmal erzeugte `MapPage`/`MapViewModel`-Instanz, obwohl sie `Transient` registriert ist.
