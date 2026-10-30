← [Zurück zur Übersicht](index.md)

# Umkreissuche — Technischer Ablauf

## Übersicht

`MapViewModel` (Bereich „Karte“, `MapPage`) prüft den Radius, holt den Standort über `ILocationService`, ruft `IFuelPriceService.SearchNearbyAsync` auf und bereitet das Ergebnis mit `StationResultBuilder` zu `StationListItem`-Zeilen auf. Alle Texte stehen in `SearchTexts`.

| Komponente | Aufgabe |
|------------|---------|
| `MapViewModel` | Suchablauf, `RadiusKm`, `RadiusOptions` (Chips 1/2/5/10/15/25 km, setzen `RadiusKm`), `SearchCommand`, `Stations` (nur die sichtbaren Seiten à `PageSize` 25), `TotalStationCount`, `ShowMoreCommand`, `FuelFilterOptions`, `SortOptions`, `StatusMessage`, `IsOffline`, `SourceNote` |
| `IChoiceOption` | gemeinsame Sicht der Chips (`Label`, `AutomationKey`, `IsSelected`, `SelectCommand`, `SelectionHint`); Implementierungen `RadiusOptionViewModel`, `FuelFilterOptionViewModel`, `ChoiceOptionViewModel<T>` |
| `SearchRadius` | `Min` 1, `Max` 25, `Default` 5, `Steps` 1/2/5/10/15/25; `IsValid` prüft den Bereich; `TryParse` akzeptiert nur Ziffern (nach `Trim`) im Bereich 1–25 |
| `ILocationService` | liefert `LocationResult` (`LocationStatus`: `Available`, `PermissionDenied`, `DisabledBySetting`, `Unavailable`; `GeoPosition`) |
| `MauiLocationService` | Berechtigung `Permissions.LocationWhenInUse`, `Geolocation` (Genauigkeit Medium, Zeitlimit 10 s) |
| `TestLocationService` | fester Standort im Testmodus |
| `LocationServiceSelector` | wählt die Implementierung (`MauiProgram`, Singleton) und parst `TANKATLAS_TEST_LOCATION` |
| `GpsUsageExtensions.AllowsLocation` | erlaubt nur `Always` und `WhileInUse` (Fail Secure) |
| `StationResultBuilder` | Filter, Preiszeilen, Hinweise, Sortierung (rein, ohne Zustand); Hinweise aus Öffnungszeiten nur aus Detailangaben bis 24 Stunden Alter (`DetailFreshness`) |
| `OpenStationCommand` / `IStationNavigator` | öffnet die [Detailansicht](../Tankstellendetails/index.md) (Schaltfläche „Details“ und Antippen der Karte) |
| `SearchMode` | Suchart `CurrentLocation` / `Address`; `MapViewModel.ModeOptions` (Chips, AutomationId `Search.Mode.<Wert>`), `IsAddressMode`, `AddressText`, `ClearAddressCommand`, `ResolvedPlace`, `AttributionText` |
| `AddressInput` | `Validate` normalisiert (Leerraum; typografische Apostrophe und Striche der iOS-Tastatur wie U+2019, U+2013, U+2014 werden zu ' bzw. -) und prüft die Eingabe: 3 bis 120 Zeichen, Buchstaben, Ziffern, Leerzeichen und `. , - ' / ( ) & + # :`; Ergebnis `AddressInputError` (`None`, `Empty`, `TooShort`, `TooLong`, `InvalidCharacters`) |
| `IGeocodingService` / `NominatimGeocodingService` | `ResolveAsync` → `GeocodingResult` (`GeocodingStatus`: `Found`, `NotFound`, `InvalidInput`, `Unavailable`, `Rejected`, `InvalidResponse`, `EndpointNotConfigured`; `GeoPosition`, `PlaceName`) |
| `GeocodingOptions` | Basisadresse (HTTPS; HTTP nur Loopback im Testmodus), Zeitlimit 10 s, `MinRequestInterval` mindestens 1 s, Standard 1,1 s (`DefaultMinRequestInterval`, Sicherheitsabstand von 100 ms zur Nutzungsrichtlinie), `UserAgent`; `FromEnvironment` liest `TANKRADAR_GEOCODING_URL` nur im Testmodus |

## Ablauf `MapViewModel.SearchAsync`

1. Eine laufende Suche wird abgebrochen. Schlägt `SearchRadius.IsValid(RadiusKm)` fehl: `SearchTexts.RadiusInvalid`, kein Standort- und kein API-Aufruf.
2. Einstellungen werden bei Bedarf geladen (`ISettingsService`); Fehler: `SettingsTexts.LoadFailed`.
3. `GetCurrentLocationAsync(settings.GpsUsage)`: Bei `AllowsLocation() == false` kommt `DisabledBySetting` ohne Berechtigungsabfrage. Sonst Berechtigung prüfen/anfragen, dann Position. Status ungleich `Available`: Liste leeren, Hinweis aus `SearchTexts.GetLocationMessage`.
4. `StationSearchQuery(lat, lon, radiusKm, gewählte FuelTypes)` an `IFuelPriceService.SearchNearbyAsync` (Cache, Live, Offline-Rückfall, siehe [Preisdaten](../Preisdaten/ablauf-technisch.md)).
5. `ApplyResult`: `StationResultBuilder.Build`, Meldung aus `SearchTexts.GetFailureMessage(Failure, hasStations)`, `SourceNote` bei `PriceDataSource.OfflineFallback` mit Treffern, `IsOffline` bei fehlender Verbindung oder Rückfall.
6. Ausnahmen (außer Abbruch durch eine neuere Suche bzw. `OnDisappearing`): Liste leeren, `SearchTexts.SearchFailed`; protokolliert wird nur der Ausnahmetyp.

`OnAppearing` lädt nur Einstellungen und abonniert `IConnectionMonitor.ConnectionChanged`; `OnDisappearing` meldet ab und bricht die Suche ab. Filter-/Sortieränderungen rufen `ApplyFilterAndSort` auf (kein Standort-, kein API-Aufruf). Die Standardsortierung kommt aus `AppSettings.ResultSortOrder`.

```mermaid
flowchart TD
    A[Suchen] --> B{Radius 1-25?}
    B -- Nein --> X[RadiusInvalid]
    B -- Ja --> C{AllowsLocation?}
    C -- Nein --> Y[DisabledBySetting-Hinweis]
    C -- Ja --> D{Berechtigung und Position?}
    D -- Nein --> Z[PermissionDenied / Unavailable-Hinweis]
    D -- Ja --> E[SearchNearbyAsync]
    E --> F[StationResultBuilder.Build]
    F --> G[Liste, ggf. Offline-Hinweis]
```

## Ablauf der Adresssuche

Im Adressmodus ersetzt `ResolveAddressAsync` den Standortschritt (Schritt 3 oben); der übrige Ablauf (Preisdienst, Aufbereitung, Fehlermeldungen) ist identisch:

1. `SearchAsync`: Radius prüfen, danach `AddressInput.Validate` (Meldung aus `SearchTexts.GetAddressInputMessage`, kein Dienstaufruf).
2. Einstellungen laden; ohne Verbindung (`IConnectionMonitor.IsOnline == false`) `SearchTexts.AddressOffline`, keine Anfrage.
3. `IGeocodingService.ResolveAsync`: `NominatimGeocodingService` normalisiert die Eingabe erneut, wartet über eine eigene `RequestThrottle` (Mindestabstand 1,1 s auf monotoner Uhr, getrennt von der Drosselung des Preisdienstes) und sendet genau eine `GET`-Anfrage `search?format=jsonv2&limit=1&countrycodes=de&accept-language=de&q=<Eingabe>` mit `User-Agent` `Tankatlas/0.1 de.martinstromberg.tankradar`, ohne Weiterleitungen, ohne Wiederholung. Status 429/5xx und Netzwerkfehler → `Unavailable`, übrige 4xx/3xx → `Rejected`, leere Liste → `NotFound`, unbrauchbare Koordinaten → `InvalidResponse`.
4. Bei `Found` wird `ResolvedPlace` („Suche rund um: …“) gesetzt und `SearchNearbyAsync` mit der gefundenen Position aufgerufen; die Position lebt nur als lokale Variable. Sonst `SearchTexts.GetGeocodingMessage` (bei fehlender Verbindung „Offline“-Text).

```mermaid
flowchart TD
    A[Suchen im Adressmodus] --> B{Radius und Eingabe gültig?}
    B -- Nein --> X[Meldung, keine Anfrage]
    B -- Ja --> C{Online?}
    C -- Nein --> Y[AddressOffline]
    C -- Ja --> D[Nominatim: 1 Anfrage, höchstens 1 je s]
    D -- Fehler/leer --> Z[Meldung, keine Preisabfrage]
    D -- Treffer --> E[SearchNearbyAsync rund um den Treffer]
    E --> F[Liste wie bei der Standortsuche]
```

Datenschutz: Eingabe, Ortsname und Position werden weder gespeichert (`SearchPrivacyTests`-Muster in `AddressSearchPrivacyTests_Persistence`) noch protokolliert (nur Statuscodes bzw. Ausnahmetypen); `GeocodingResult.ToString()` liefert nur den Status.

## `StationResultBuilder`

- Berücksichtigt nur gewählte, definierte `FuelType`-Werte; ein Filter außerhalb davon gilt als „Alle“.
- Filter: nur Tankstellen mit Preis der Sorte. Preiszeilen in Einstellungsreihenfolge, je mit `PriceFreshness.FormatAge`/`IsStale`; `StationHints` liefert „Preis unbestätigt“ (nur über angezeigte Preise) und „Automatentankstelle“. Preise im Format „1,859 €“, Entfernung „1,4 km“ (`de-DE`).
- Sortierung: `Price` nach Preis der Filtersorte (sonst erste gewählte Sorte; fehlender Preis zuletzt), dann Entfernung, Name, Id; `Distance` und `Name` mit den jeweils anderen als Nachrang. Namensvergleich `de-DE`, ohne Groß-/Kleinschreibung.

## Datenschutz

Der Standort wird nur im Speicher für die Anfrage verwendet, nie gespeichert oder protokolliert (`MapViewModel` und `MauiLocationService` protokollieren keine Koordinaten; Tests `SearchPrivacyTests_*`). Plattform: iOS/MacCatalyst `NSLocationWhenInUseUsageDescription` („Ermittlung von Tankstellen in der Nähe“), iOS-`PrivacyInfo.xcprivacy` mit `NSPrivacyCollectedDataTypePreciseLocation` (nicht verknüpft, kein Tracking, Zweck App-Funktionalität), Windows `DeviceCapability` `location`.

## Testmodus und manuelle Abnahme unter Windows (ohne GPS)

Windows-Rechner haben meist keinen Standortdienst; ohne Testmodus meldet die Suche dann „Der Standort konnte nicht ermittelt werden…“ (kein stiller Ersatzstandort).

| Variable | Wirkung |
|----------|---------|
| `TANKATLAS_TEST_DATA_PATH` | aktiviert den Testmodus mit isoliertem Datenverzeichnis (der frühere Name `TEST_DATA_PATH` wirkt nicht mehr) |
| `TANKATLAS_TEST_LOCATION` | fester Standort `breite,länge` mit Dezimalpunkt, z. B. `52.5200,13.4050`; nur im Testmodus; fehlend oder ungültig: Standort „nicht ermittelbar“ |
| `TANKRADAR_PRICE_API_URL`, `TANKRADAR_PRICE_API_KEY` | Adresse (HTTP nur Loopback) und Schlüssel des Preisdienstes im Testmodus; ohne Adresse kein Abruf |
| `TANKRADAR_GEOCODING_URL` | Adresse (HTTP nur Loopback) des Ortssuchdienstes (Nominatim-Mock `MockNominatimServer`, `src/TestSupport`) im Testmodus; ohne Adresse keine Adressauflösung (kein Rückfall auf den produktiven Dienst). Der Mindestabstand von 1,1 s gilt auch im Testmodus |

Abnahme:

1. In der PowerShell-Sitzung setzen: `$env:TANKATLAS_TEST_DATA_PATH = "<leeres Testverzeichnis>"`, `$env:TANKATLAS_TEST_LOCATION = "52.5200,13.4050"`, `$env:TANKRADAR_PRICE_API_URL` und `$env:TANKRADAR_PRICE_API_KEY` auf einen erreichbaren Mock-Preisdienst (`MockTankerkoenigServer`, `src/TestSupport`).
2. App aus derselben Sitzung starten (`dotnet run --project src/Tankradar.MAUI -f net10.0-windows10.0.19041.0` oder `Tankradar.MAUI.exe`).
3. In **Optionen** Spritsorten wählen, dann **Karte** öffnen, im Suchdialog (Schaltfläche „Suche“) Radius setzen, **Suchen**: Panel-Liste, Filter und Sortierung prüfen.
4. Prüffälle: **Standort und GPS** = **Nie** (Hinweis, keine Abfrage); `TANKATLAS_TEST_LOCATION` entfernen oder ungültig setzen (Hinweis „Standort nicht ermittelt“); Radiusprüfung über Unit-/Integrationstests (die Oberfläche bietet nur gültige Stufen an); nicht erreichbare Preisdienst-Adresse (Offline-Hinweis).

Die automatisierten Tests nutzen dieselben Variablen: `E2ETestBase` / `SearchE2ETestBase` (FlaUI), `SearchMockServerTestBase` (Integration); Unit-Tests `MapViewModelTests_*`, `StationResultBuilderTests_*`, `LocationService*Tests`, `SearchRadiusTests_Validation`, `TestModeVariableTests_Rename`.

## Darstellung und Leistung

- Filter, Sortierung und Radius sind Chips (`Button` mit Auswahlzustand über `DataTrigger`; `SemanticProperties.Hint` = „Ausgewählt“, AutomationIds `Search.Radius.<km>`, `Search.Filter.<Sorte>`, `Search.Sort.<Wert>`). Ergebniskarten nutzen die Design-Tokens (16 px Rundung, Schattenebene 1, `price-hero` für Preise) und zeigen die Adresszeile (`StationListItem.AddressText`).
- Die Liste bleibt ein `BindableLayout` (nicht virtualisiert), stellt aber höchstens `MapViewModel.PageSize` (25) Karten dar; „Weitere anzeigen“ (`Search.ShowMore`) ergänzt je 25. Filter-, Sortier- und Suchwechsel setzen auf die erste Seite zurück. Abgesichert durch `MapViewModelTests_Chips`, `SearchMockServerTests_RealFormat` (300 Stationen) und `SearchE2ETests_LargeResult`.
- Die Umkreissuche der Quelle (`list.php`) liefert weder `openingTimes` noch `wholeDay`. `FuelPriceService` ergänzt Live-Ergebnisse um Öffnungszeiten aus früheren Detailabfragen (`IPriceRepository.GetKnownDetailsAsync`, `StationInfo.WithDetails`); ohne bekannte Details erscheint kein Hinweis „Automatentankstelle“. Der Mock-Server liefert das reale Format.
- Begründete Abweichungen vom Designentwurf: [ADR 0002](../../adr/0002-search-page-design-deviations.md).

## Kartenansicht (Schritt 9)

- **Vollfläche, Suchdialog und Panel (Issue #19):** `MapPage` legt `StationMapView` über die gesamte Seite; darüber liegen `MapSearchOverlay` (Sichtbarkeit `IsSearchOverlayOpen`, Befehle `OpenSearchOverlayCommand`/`CloseSearchOverlayCommand`; `RunGuardedSearchAsync` schließt den Dialog beim Suchstart), `StationBottomSheet` (unten, Kopfzeile mit `StationCountText`, Umschalten über `ToggleBottomSheetCommand`, `PanGestureRecognizer` mit Schwelle der halben Strecke, Höhe begrenzt auf 60 % der Seitenhöhe) und die Legende. Die Liste wird nur bei `IsBottomSheetExpanded` gezeigt; `ShowMoreVisible` = `HasMore && IsBottomSheetExpanded`.
- **Standardansicht:** `SelectView(ResultView)` setzt nur `IsBottomSheetExpanded` (`List` ausgeklappt, `Map` eingeklappt). Aufruf beim Laden der Einstellungen, nur wenn sich `AppSettings.ResultView` gegenüber dem zuletzt geladenen Wert ändert. `ViewOptions` und die Umschaltung existieren nicht mehr; `ResultView` bleibt als Einstellung erhalten.
- **Nachladen des Ausschnitts:** `StationMapView` löst `ViewportChanged` nur bei Nutzeraktionen aus (Ziehen, Wischen, Kneifen, Mausrad, Zoom-/Verschiebe-/Zurücksetzen-Schaltflächen); `ViewportFitted` meldet das Einpassen einer neuen Suche und setzt `_searchedViewport`. `OnViewportChanged` (nur Modus Standort/Adresse, Mitte oder Zoom verschieden) setzt `ShowAreaReloadButton` und `AreaReloadHint`; es wird nichts abgefragt. `ReloadVisibleAreaCommand` → `ReloadVisibleAreaAsync`: `MapAreaQuery.From(MapViewport.GetBounds(), Kraftstoffarten)` (Kreis um die Mitte, Radius = halbe Diagonale in ganzen km, aufgerundet, min. 1 km; `null` über `SearchRadius.Max` 25 km), genau ein `IFuelPriceService.SearchNearbyAsync`-Aufruf. Die Sperre `AreaReloadMinInterval` (60 s) prüft `IsAreaReloadBlocked` gegen `TimeProvider`; ein Timer aktualisiert die Restzeit im Hinweis. Ergebnis über `ApplyResult(..., MapFitMode.KeepViewport)`; `StationMapView.RefreshMarkers` führt dann kein `RequestFit` aus (`MapFitMode`).
- **Panel-Zustand:** Rein sitzungsbezogen (`_isBottomSheetExpanded`, nicht persistiert); Routenmodus blendet den Nachladen-Befehl aus.
- **Markierungen:** `MapMarkerBuilder.Build` erzeugt aus der gefilterten Gesamtmenge (`_allStations`) je Tankstelle mit gültiger Position einen `MapMarker` (Preis der Sorte aus `StationResultBuilder.ResolveFuelType`, Beschreibung, Preisniveau). Die Einstufung übernimmt `PriceLevelClassifier` (exakte Dezimalarithmetik, siehe [ADR 0005](../../adr/0005-map-view-decisions.md)).
- **Suchposition:** `RunSearchAsync` übergibt die ermittelte Position als `MapOrigin` an `ApplyResult`; sie liegt nur im Arbeitsspeicher (`MapViewModel.Origin`) und wird in `ClearResult` (Fehler, Moduswechsel) verworfen. `MapResultVersion` steigt bei jeder neuen Suche bzw. beim Verwerfen und löst das Einpassen der Karte aus (nicht bei Filter oder Sortierung).
- **Karte:** `StationMapView` (`Views/Controls`) zeichnet aus `MapViewport` (Web-Mercator, ganzzahliger Zoom 4–18, 256-px-Kacheln) sichtbare Kacheln als `Image` und Markierungen als `Button` (AutomationId `Map.Marker.<Stations-ID>`, Beschreibung = Name und Preis, Hinweis = Preisniveau). Nur Markierungen im Ausschnitt werden aufgebaut. Weitere AutomationIds: `Map.StationCount`, `Map.Zoom`, `Map.Attribution`, `Map.Origin`, `Map.ZoomIn`, `Map.ZoomOut`, `Map.Recenter`, `Map.Pan.North/South/East/West`, `Map.Legend`, `Map.ReloadArea`, `Map.ReloadArea.Hint`, `Search.Map`, `Search.Map.Hint`, `Search.Overlay.Open`, `Search.Overlay`, `Search.Panel`, `Search.Panel.Toggle`, `Search.Panel.Count`, `Search.Submit`.
- **Kacheln:** `ITileSource`/`HttpTileSource` mit `TileServerOptions`: HTTPS-Pflicht, Kennung `Tankatlas/0.1 de.martinstromberg.tankradar`, höchstens 2 parallele Abrufe, Speicher- und Datei-Cache (`<Datenverzeichnis>/tiles`, Rückfallfrist 7 Tage, 100 MB; Frische nach `TileCachePolicy`: Lebensdauer aus `Cache-Control`/`Expires` abzüglich der Angabe `Age` der Antwort, mindestens 0; eine `304`-Antwort ohne eigene Caching-Angaben übernimmt die in `TileMetadata.Lifetime` gespeicherte Lebensdauer, Altbestand ohne diese Angabe die Rückfallfrist), Wartefenster von 30 s nach Fehlern, Verzögerung von 150 ms vor dem Abruf. Tests: Umgebungsvariable `TANKRADAR_TILE_URL` (nur im Testmodus, Loopback-HTTP, `MockTileServer`); ohne Adresse im Testmodus werden keine Kacheln abgerufen.
- **Tests:** Unit (`PriceLevelClassifierTests_Levels`, `MapViewportTests_Math`, `MapMarkerBuilderTests_Markers`, `HttpTileSourceTests_Fetch`, `TileServerOptionsTests_Validation`, `MapViewModelTests_MapView`), Integration (`TileSourceMockServerTests_Flow`), E2E (`MapE2ETests_View`, `MapE2ETests_Markers`, `MapE2ETests_Navigation`; Bedienung nur über UI-Automation-Muster).

## Suche entlang einer Route (Schritt 17)

Entscheidungen und Begründungen: [ADR 0014](../../adr/0014-route-search.md).

| Komponente | Aufgabe |
|------------|---------|
| `MapViewModel` (`MapViewModel.Route.cs`) | Modus `SearchMode.Route`, Start (`RouteStartKind`), `StartInput`, `DestinationInput`, `RouteDistanceKm`, Validierung vor externen Aufrufen, Fortschritt, Ergebnis, `MapRoute` |
| `RouteSearchService` (`IRouteSearchService`) | Ablauf: Verfügbarkeit des Routingdienstes, Start/Ziel über `IGeocodingService`, Abstand < 200 m = `SamePlace`, Route über `IRoutingService`, `RouteSampler`, `RouteStationSearchService` |
| `OpenRouteServiceClient` (`IRoutingService`) | `POST v2/directions/driving-car`, Schlüssel im Kopf `Authorization`, 15 s Zeitlimit, 3 Versuche (1 s, 2 s), Drosselung 1,5 s, Fehlerabbildung auf `RoutingFailure` |
| `PolylineDecoder`, `RouteGeometry`, `RouteProfile` | Dekodierung (Genauigkeit 5), Haversine, Abstand/Streckenposition (`Locate`), `PointAt`, Vereinfachung (Douglas-Peucker) |
| `RouteSampler` | Abfragepunkte: Schritt = 2 · (25 − d) · 0,95, Radius ⌈s/2/0,95 + d⌉ (1 bis 25 km), höchstens `RouteSearchOptions.MaxQueries` (3) Abfragen, `MaxSearchLengthKm` je Abstand; zu lange Routen: `RouteSearchResult.Limit` |
| `RouteStationSearchService` | Umkreisabfragen strikt nacheinander (`IFuelPriceService.SearchNearbyAsync`), Deduplizierung, Filter nach Abstand, Sortierung nach Streckenposition; Abbruch bei Fehler; `RouteQueryPacer` reserviert je Abfrage einen Absendezeitpunkt im Abstand `MinQueryInterval` plus `QueryIntervalReserve` (10 s plus 1,25 s; auch suchübergreifend und bei gleichzeitigen Aufrufern, abbrechbar; per `IsSearchCached` erkannte Cache-Treffer warten nicht); alle Abfragen laufen weiter über die allgemeine Drosselung des Preisdienstes |
| `StationResultBuilder.BuildForRoute` | `DistanceKm` = Streckenposition, `RouteOffsetKm`; sonst dieselbe Filter-, Preis- und Sortierlogik |
| `MapRouteProjector`, `MapRouteDrawable`, `StationMapView.Route` | Projektion/Zuschnitt der Linie auf den Ausschnitt, Zeichnen unter den Markern, Ziel `Map.Destination`, Anpassung des Ausschnitts |

AutomationIds: `Search.Mode.Route`, `Search.Route.StartKind.CurrentLocation|Address`, `Search.Route.Start.Input|Clear`, `Search.Route.Destination.Input|Clear`,
`Search.RouteDistance.{1|2|5|10}`, `Search.Route.Summary`, `Search.Route.Progress`, `Search.Attribution.Routing`, `Search.Station.RouteOffset`, `Map.Destination`.

Testmodus: `TANKRADAR_ROUTING_URL` (Adresse des Mocks `MockOpenRouteServiceServer`; ohne Adresse wird die Routenberechnung verweigert) und
`TANKRADAR_ROUTING_TEST_API_KEY` (nur im Testmodus; belegt die In-Memory-Ablage von `RouteSearchAccess` mit Schlüssel und eingeschaltetem Schalter vor), `TANKRADAR_ROUTE_QUERY_INTERVAL_MS` (Abstand der Preisabfragen, Standard im Testmodus 0). Schlüssel und Schalter: `IRouteSearchAccess`/`RouteSearchAccess` (Credential Locker bzw. Keychain des Geräts, nicht in der Sicherung der App; iOS-Keychain-Einträge können in verschlüsselten Gerätesicherungen enthalten sein); Prüfung per `OpenRouteServiceKeyChecker`; Karte in den Optionen: `RouteSearchSettingsViewModel` (`Settings.Route.*`). `MapViewModel` zeigt `SearchMode.Route` nur bei `IsEnabledAsync()` und reagiert auf `Changed`. Tests rufen nie Produktivendpunkte. Datenschutz: Route, Orte und Positionen bleiben im Arbeitsspeicher
des `MapViewModel`; es gibt keine Migration.
