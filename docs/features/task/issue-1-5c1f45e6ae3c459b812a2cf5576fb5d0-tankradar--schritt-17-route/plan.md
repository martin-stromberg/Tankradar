# Umsetzungsplan: Schritt 17 - Tankstellen entlang einer Route

## Übersicht

Die Suche im Bereich "Karte" erhält einen dritten Modus "Route" (`SearchMode.Route`). Start (aktueller Standort oder Adresse) und Ziel werden über die bestehende Nominatim-Geokodierung aufgelöst, die Route (Profil Auto) kommt von OpenRouteService (neuer `OpenRouteServiceClient`, nur HTTPS, Zeitlimit, Wiederholung mit zunehmender Wartezeit, Schlüssel `ROUTING_API_KEY` nach dem Muster des Tankerkönig-Schlüssels). Entlang der dekodierten Polylinie werden wenige, deterministisch platzierte Tankerkönig-Umkreisabfragen (je höchstens 25 km) gestellt, zusammengeführt, nach Station-ID dedupliziert, auf den einstellbaren Abstand zur Route gefiltert und nach Streckenposition sortiert; Liste, Karte (mit Routenlinie), Filter, Preise und Detailansicht nutzen die vorhandenen Bausteine. Zusätzlich werden die Härtungen aus der Abnahme von Schritt 16 umgesetzt (E2E-Stabilität, Dokumentation, Sprachausgabe der Schaltfläche "Weitere anzeigen"). Betroffen: Suche (`MapViewModel`, `MapPage`, `StationMapView`), neue Dienste unter `Services/Routing/`, Schlüsselablage und Build (`MSBuild`, `ApiKeys`, `scripts/`, CI-Aktion), `src/TestSupport`, Tests und Dokumentation.

## Entscheidungen

Fachliche und technische Festlegungen (alle in ADR 0014 zu begründen):

| Thema | Entscheidung | Begründung |
|-------|--------------|------------|
| Abstandsstufen zur Route | 1, 2, 5, 10 km, Standard 2 km (`RouteDistance`, Chips wie bei `SearchRadius`) | Deckt Autobahn-Ausfahrten (1-2 km) bis Landstraßen-Umwege (10 km) ab; höhere Werte vergrößern Abfragezahl und Trefferlisten unverhältnismäßig. |
| Persistenz des Abstands | Nicht persistent, nur im `MapViewModel` (wie `SearchRadius`); keine Migration, kein neues Feld in `AppSettings` | Der Radius der Suche ist ebenfalls nicht persistent; Minimierung gespeicherter Daten; keine Schemaänderung, keine Auswirkung auf Sicherung/Export. |
| Abfragegeometrie | Pro Abfrage Radius R <= 25 km. Die Route der Länge L wird in n = ceil(L / Schritt) gleich lange Abschnitte geteilt, Schritt = 2 * (25 - d) * 0.95 (d = Abstand zur Route); der Mittelpunkt jeder Abfrage liegt auf der Streckenmitte des Abschnitts; Radius je Abfrage = Abschnittshälfte / 0.95 + d, aufgerundet auf 0,5 km, mindestens 1 km, höchstens 25 km. | Dreiecksungleichung: Eine Station im Abstand <= d von einem Routenpunkt im Abschnitt liegt höchstens (Abschnittshälfte + d) vom Mittelpunkt entfernt; damit ist das Abstandsband lückenlos überdeckt (Beweis und Rechenbeispiele im ADR; Sicherheitsfaktor 0.95 gegen Rundung). Gleichmäßige Teilung vermeidet einen kleinen Restabschnitt. |
| Obergrenze der Abfragen und Routenlänge | Höchstens 15 Tankerkönig-Abfragen und 400 km Routenlänge je Suche (Konstanten in `RouteSearchOptions`); bei Überschreitung Abbruch mit Meldung, keine stille Kürzung | Bei d = 10 km ergibt 400 km genau 15 Abfragen (400 / 28,5). Die Grenze hält die Last pro Suche klein; bei Standard 2 km genügen für 400 km neun Abfragen. Überlange Routen erzeugen eine verständliche Meldung statt unvollständiger Ergebnisse. |
| Takt der Tankerkönig-Abfragen | Strikt nacheinander (nie parallel), Mindestabstand durch den vorhandenen `RequestThrottle` der Preis-API (>= 1 s, nicht verkürzbar); keine automatische Wiederholung der Gesamtsuche; identische Abfragepunkte sind deterministisch und werden vom Such-Cache (5 min) bedient; keine Hintergrund- oder Wiederholabfragen. Höchstdauer einer Suche damit rund 15 s plus Routing. | Gleiche Drosselung wie bisher, kein neuer Parameter. Der Entwickler prüft vor dem Schreiben des ADR die aktuellen Nutzungsbedingungen von Tankerkönig (tankerkoenig.de) und vermerkt Fundstelle und Datum im ADR; Konstanten werden bei strengeren Vorgaben dort angepasst, nicht im Code verstreut. |
| Teilausfall bei Abfragen | Meldet eine Abfrage `PriceFailure != None` (auch Rückfall auf den Cache mit `Failure`), bricht die Routensuche mit der Preis-Fehlermeldung ab; keine Teilergebnisse | Teilergebnisse würden Lücken entlang der Route verschweigen. |
| Aufhebung des Ausschlusses kombinierter Abfragen | ADR 0014 begründet, warum die Zeile "Suchradius" des Projektplans ("mehrere zusammengeführte Abfragen werden nicht umgesetzt") für die Routensuche nicht gilt: Die Radiussuche bleibt eine Abfrage; die Routensuche braucht fachlich eine Abdeckung entlang einer Strecke und ist durch die Obergrenze, die Drosselung und den Cache begrenzt. | Der Ausschluss galt für größere Radien um einen Punkt; eine Route lässt sich nicht mit einer Abfrage abdecken. Die Belastung ist begrenzt und dokumentiert. |
| Mehrdeutige Geokodierung | Wie die Adresssuche: erster Treffer (`limit=1`); der aufgelöste Ortsname wird in der Routenzusammenfassung angezeigt, damit Fehltreffer erkennbar sind | Einheitliches Verhalten (ADR 0003). |
| Start und Ziel identisch | Liegen beide Positionen weniger als 200 m auseinander: Meldung "Start und Ziel liegen am selben Ort", kein Routing-Aufruf | Spart einen externen Aufruf, klare Meldung. |
| ORS-Fehler 429 / Kontingent | Wie `TankerkoenigClient`: bis 3 Versuche mit Wartezeit `RetryBaseDelay * 2^(n-1)`; danach `RoutingFailure.QuotaExceeded` mit Meldung "Der Routendienst ist gerade ausgelastet oder das Tageskontingent ist aufgebraucht. Bitte später erneut versuchen." (keine Rohtexte des Dienstes) | Verständliche Meldung, vorübergehende Störung. 5xx und Zeitlimit: `Unreachable`; 401/403: `KeyRejected`; 404 (Code 2009/2010): `RouteNotFound`; 400 mit Distanzlimit (Code 2004): `RouteTooLong`; sonstige 4xx: `Rejected`. |
| ORS-Aufruf | `POST {BaseUrl}v2/directions/driving-car`, JSON-Body mit `coordinates` ([Länge, Breite] Start, Ziel), `instructions=false`; Schlüssel im Header `Authorization`, nie in der URL; Antwort `routes[0].geometry` (kodierte Polylinie, Genauigkeit 5) und `routes[0].summary.distance` | Kein Schlüssel in URL/Protokollen; Antwort klein (keine Anweisungen). |
| Zeitlimit Routing | `RequestTimeout` 15 s je Versuch, `MaxAttempts` 3, `RetryBaseDelay` 1 s, `MinRequestInterval` 1,5 s (ORS-Kontingent), eigener `RequestThrottle` | Muster `PriceApiOptions`; Routing dauert länger als Preisabfragen. |
| Schlüsselhandhabung | `ROUTING_API_KEY` wie Tankerkönig: neues `MSBuild/RoutingApiKey.targets` (Quellen nacheinander `TANKRADAR_ROUTING_API_KEY`, `ROUTING_API_KEY`, `routing.local.props` mit Element `RoutingApiKey`; Assembly-Metadatum `RoutingApiKey`; Log nur "vorhanden: ja/nein"). Laufzeit: `ApiKeyProvider` und `IApiKeyStore` werden um einen Schlüsselnamen parametrisiert (Standard = bisheriges Verhalten); Ablage `tankatlas.routing-api-key` in Credential Locker/Keychain; Testmodus liest nur `TANKRADAR_ROUTE_API_KEY`. Fehlt der Schlüssel: `RoutingFailure.ApiKeyMissing`, Meldung "Routensuche nicht verfügbar", übrige Suche läuft. | Gleiches Muster, aber getrennte Metadaten, Ablage und Testvariable (die Build-Variable `TANKRADAR_ROUTING_API_KEY` darf nicht mit der Laufzeit-Testvariable kollidieren, wie `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY`). Zweites `.targets` statt Verallgemeinerung der Inline-Task: der geprüfte Tankerkönig-Build bleibt unangetastet. |
| Windows-Paket | `package-windows.ps1` und die Aktion `build-and-package` leeren `TANKRADAR_ROUTING_API_KEY` und `ROUTING_API_KEY` wie die Tankerkönig-Variablen (öffentliches Repository, Windows-Pakete ohne eingebetteten Schlüssel); die Eingabe `routing-api-key` und die Secret-Referenzen der Workflows bleiben | Die Variable wird erst durch das neue Ziel wirksam; ohne Leeren würde der Schlüssel in Windows-Pakete gelangen. iOS (`package-ios`) bettet ihn ein. |
| Datenschutz | Routen, Start-/Zieltexte, Positionen und Streckenpunkte bleiben im Arbeitsspeicher des `MapViewModel`; keine Tabelle, keine Einstellung, keine Datei, keine Sicherung/kein Export, keine Protokollierung (nur Statuswerte und Zählungen). Stationen und Preise werden wie bisher über `FuelPriceService` zwischengespeichert. | Anforderung; ADR 0003-Muster. |
| Fehlerkonzept | Neue Enums `RoutingFailure` (Dienst) und `RouteSearchStatus` (Ablauf); Meldungen zentral in `SearchTexts` | Muster `PriceFailure`/`GeocodingStatus`; Enum-Testabdeckung. |
| Struktur | Service Layer `RouteSearchService` (Orchestrierung), Gateway `OpenRouteServiceClient` hinter `IRoutingService`, Value Objects `RouteProfile`/`RouteDistance`, statische Rechenklassen `PolylineDecoder`, `RouteGeometry`, `RouteSampler`; Routenlogik des Presenters in `MapViewModel.Route.cs` (partielle Klasse), da `MapViewModel` bereits 904 Zeilen hat | Testbarkeit ohne UI; kleine Dateien. |
| Entfernungsanzeige im Routenmodus | `StationInfo.DistanceKm` wird für Routenergebnisse durch die Streckenposition ersetzt (Kopie je Station); Text "12,3 km ab Start", zusätzlich "0,8 km abseits der Route" | Sortierung `Distance` im `StationResultBuilder` bleibt unverändert; Anforderung "Entfernung = Position entlang der Route". |
| Start "Aktueller Standort" bei `GpsUsage.Never` | Kein gesperrter Chip; die Auswahl bleibt möglich, die Suche meldet über `SearchTexts.GetLocationMessage` (wie Modus "Standort") und löst nichts Externes aus | Bestehendes Verhalten (`SearchE2ETests_GpsNever`), einheitlich. |
| Wiederverwendete UI-Muster | Moduschips (`ModeOptions`), Eingabefeld mit Löschen (`Search.Address.Input`/`.Clear`), Radius-Chips (`Search.Radius.{Key}`), `Search.StatusMessage`, `Search.ResolvedPlace`, Filter-/Sortier-/Ansichtschips, Liste/Karte, `Search.Station.Open`. Keine Auswahl über interne Kennungen: Orte werden als Klartext eingegeben. | Anforderung "gewohnte" Bedienung. |

## Programmabläufe

### Routensuche (Hauptablauf)

1. `MapViewModel.SearchAsync` (Modus `Route`) bricht eine laufende Suche ab, validiert **vor jedem externen Aufruf**: Abstand (`RouteDistance.IsValid`), Zieltext (`AddressInput.Validate`), Starttext (bei `RouteStartKind.Address`; `AddressInput.Validate`). Fehler -> `SearchTexts`-Meldung, kein Netzzugriff.
2. Offline (`IConnectionMonitor`) -> Meldung `SearchTexts.RouteOffline`; Abbruch.
3. Bei `RouteStartKind.CurrentLocation`: `ResolveCurrentLocationAsync` (nur wenn `GpsUsage.AllowsLocation()`, Statusmeldungen wie im Standortmodus); Position gelangt nur in den Request.
4. `IRouteSearchService.SearchAsync(RouteSearchRequest, IProgress<RouteSearchProgress>, ct)`:
   1. Schlüsselprüfung `IRoutingService.IsAvailableAsync` (Schlüssel/Endpunkt) -> sonst `RouteSearchStatus.RoutingUnavailable` ohne weitere Aufrufe.
   2. Geokodierung Ziel (und Start bei Adresse) nacheinander über `IGeocodingService.ResolveAsync` (Throttle erzwingt >= 1,1 s Abstand). `NotFound` -> `StartNotFound`/`DestinationNotFound`; `Unavailable`/`Rejected`/`InvalidResponse`/`EndpointNotConfigured` -> `GeocodingFailed` (mit `GeocodingStatus`).
   3. Start/Ziel < 200 m -> `SamePlace`.
   4. `IRoutingService.GetRouteAsync(start, ziel, ct)` -> `RoutingResult` (Fehler -> `RoutingFailed` mit `RoutingFailure`).
   5. Länge > `MaxRouteLengthKm` -> `RouteTooLong`.
   6. `RouteSampler.CreateQueryPoints(profile, distanceKm, options)` -> Liste von Abfragepunkten (`RouteQueryPoint`: Position, Radius, Streckenposition) oder `TooLong` bei > `MaxQueries`.
   7. `IRouteStationSearchService.SearchAsync(...)`: je Abfragepunkt nacheinander `IFuelPriceService.SearchNearbyAsync(StationSearchQuery(lat, lng, radius, fuelTypes))`; bei `Failure != None` Abbruch (`PriceFailed`); Fortschritt "Tankstellen werden gesucht (k von n)".
   8. Deduplizieren nach `StationInfo.Id` (erster Treffer behalten); je Station `RouteProfile.Locate(position)` -> `OffsetKm`, `AlongKm`; Filter `OffsetKm <= d`; Stationen ohne Koordinaten verwerfen; Sortierung nach `AlongKm`, dann `Id`.
   9. Ergebnis: `RouteSearchResult` (`Status`, `Route`, `StartName`, `DestinationName`, `Stations` als `RouteStation`-Liste).
5. Das `MapViewModel` erzeugt über `StationResultBuilder.BuildForRoute` die Listeneinträge (Sorten-Filter, Preise mit Alter, Hinweise, Sortierung Preis/Entfernung/Name; `Distance` = Streckenposition), setzt `MapMarkers` (`MapMarkerBuilder`), `Origin` (Start), `MapRoute` (vereinfachte Linie, Ziel), `ResolvedPlace`/`Search.Route.Summary` ("Start -> Ziel, 123 km, N Tankstellen im Umkreis von d km") und erhöht `MapResultVersion`.
6. Kein Ergebnis: Leerzustand (`Search.EmptyState`) mit Hinweis auf größeren Abstand. Fehler: Meldung über `SearchTexts.GetRouteMessage`, vorherige Ergebnisse werden entfernt, Routenzustand gelöscht.
7. Neue Suche, Moduswechsel oder Ansichtswechsel verändern den Routenzustand nur im Speicher; beim Beenden der App ist er verloren.

Beteiligte Klassen: `MapViewModel`, `RouteSearchService`, `NominatimGeocodingService`, `OpenRouteServiceClient`, `RouteSampler`, `RouteStationSearchService`, `FuelPriceService`, `StationResultBuilder`, `MapMarkerBuilder`.

### Routing-Aufruf (`OpenRouteServiceClient.GetRouteAsync`)

1. Vorabprüfungen ohne Netzzugriff: Endpunkt konfiguriert (`EndpointNotConfigured`), Schlüssel vorhanden (`ApiKeyMissing`), Positionen gültig.
2. Je Versuch: `RequestThrottle.WaitAsync`, POST mit Zeitlimit (verknüpftes `CancellationTokenSource`), Header `Authorization`, `User-Agent` aus den Optionen.
3. Antwortabbildung: 200 -> Parsen (`routes[0].geometry`, `summary.distance`), `PolylineDecoder.TryDecode` (Genauigkeit 5, Höchstzahl Punkte 50 000, Zeichenlänge begrenzt), Plausibilität (mind. 2 Punkte, Länge > 0, Länge aus Punkten statt aus `summary` für die Streckenposition); 429 und >= 500 und Zeitlimit/`HttpRequestException` wiederholbar mit `RetryBaseDelay * 2^(n-1)` über `IDelay`, nach `MaxAttempts` `QuotaExceeded` (429) bzw. `Unreachable`; 401/403 `KeyRejected`; 404 `RouteNotFound`; 400 mit Code 2004 `RouteTooLong`, sonst `Rejected`; unlesbare Antwort `InvalidResponse`.
4. Fehlertexte und Protokolle enthalten nie Schlüssel, Koordinaten oder Antworttext, nur Statuscode und Fehlerart.

### Abfragepunkte entlang der Route (`RouteSampler`)

1. Eingabe `RouteProfile` (Punkte, kumulierte Strecke, Länge L), Abstand d, Optionen (R = 25, Faktor 0.95, `MaxQueries`, `MaxRouteLengthKm`).
2. Schritt = 2 * (R - d) * 0.95; n = max(1, ceil(L / Schritt)); n > `MaxQueries` oder L > `MaxRouteLengthKm` -> `TooLong`.
3. Abschnittslänge s = L / n; Mittelpunkt i bei Streckenposition (i + 0.5) * s via `RouteProfile.PointAt`; Radius = clamp(aufrunden auf 0,5 (s / 2 / 0.95 + d), 1, 25).

### Position und Abstand (`RouteProfile.Locate`)

1. Für jede Teilstrecke wird der Punkt lokal in Meter projiziert (äquirektanguläre Projektion um die Breite der Teilstrecke), Fußpunkt mit Klemmung auf [0,1], Abstand als Haversine zwischen Station und Fußpunkt; grobe Bounding-Box-Ablehnung (Teilstrecke +- d in Grad) vermeidet unnötige Rechnung.
2. Minimaler Abstand gewinnt; bei Gleichstand (Schleifen, Überschneidungen) die kleinere Streckenposition. `AlongKm` = kumulierte Länge bis zur Teilstrecke + Fußpunktabstand.

### Zeichnen der Route (`StationMapView`)

1. `MapViewModel.MapRoute` (vereinfachte Punktfolge, Douglas-Peucker mit Toleranz abhängig von der Zoomstufe, höchstens ~1 000 Punkte; Start/Ziel-Punkt) wird an die neue bindbare Eigenschaft `Route` von `StationMapView` übergeben.
2. Das Zeichnen projiziert die Punkte mit dem vorhandenen `MapViewport` auf Bildschirmkoordinaten (reine, getestete Funktion `MapRouteProjector`), zeichnet eine Linie unter den Markern; Start (Ursprungsmarkierung) und Ziel (Zielpunkt) sichtbar.
3. Der Kartenausschnitt wird beim Ergebniswechsel auf die Begrenzungsbox der Route zuzüglich Rand eingepasst (Erweiterung der vorhandenen Anpassung an die Marker).
4. Barrierefreiheit: `Search.Map.Hint` nennt "Route von A nach B eingezeichnet, N Tankstellen".

### Schlüsselbereitstellung

1. Build: `RoutingApiKey.targets` schreibt `RoutingApiKey.g.cs` (Assembly-Metadatum), Ziel läuft vor `CoreCompile`, Import in `Tankradar.MAUI.csproj` neben dem Tankerkönig-Import (`MtouchInterpreter`/`TrimmerRootAssembly` unverändert).
2. Laufzeit: `ApiKeyProvider` (parametrisiert) liefert Build-Schlüssel (und übernimmt ihn in die Ablage) oder den gespeicherten Schlüssel; Testmodus nur `TANKRADAR_ROUTE_API_KEY` mit `InMemoryApiKeyStore`.
3. Fehlt beides: `RoutingFailure.ApiKeyMissing`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `RouteDistance` (`Models/Routing/`) | statische Wertklasse | Stufen 1/2/5/10 km, Standard 2, `IsValid`, `Options` |
| `RoutingFailure` (`Models/Routing/`) | Enum | `None`, `ApiKeyMissing`, `EndpointNotConfigured`, `KeyRejected`, `Unreachable`, `QuotaExceeded`, `RouteNotFound`, `RouteTooLong`, `Rejected`, `InvalidResponse` |
| `RoutingResult` (`Models/Routing/`) | Datenmodellklasse | `Failure`, `Route` (`RouteProfile?`), Fabriken `Success`/`Failure` |
| `RouteProfile` (`Models/Routing/`) | Value Object | Punktfolge, kumulierte Strecke, `LengthKm`, `Locate`, `PointAt`, `Bounds`, `Simplify` |
| `RouteLocation` | Value Object (struct/record) | `OffsetKm`, `AlongKm` |
| `RouteQueryPoint` | Value Object | Position, Radius, Streckenposition |
| `RouteStation` | Datenmodellklasse | `StationInfo`, `AlongKm`, `OffsetKm` |
| `RouteStartKind` | Enum | `CurrentLocation`, `Address` (Start-Chips) |
| `RouteSearchStatus` | Enum | `Success`, `StartNotFound`, `DestinationNotFound`, `GeocodingFailed`, `SamePlace`, `RoutingUnavailable`, `RoutingFailed`, `RouteTooLong`, `PriceFailed` |
| `RouteSearchRequest` / `RouteSearchResult` / `RouteSearchProgress` | Datenmodellklassen | Auftrag (Startposition oder -text, Zieltext, Abstand, Sorten), Ergebnis, Fortschritt (Phase, k von n) |
| `MapRoute` (`Models/Map/`) | Datenmodellklasse | vereinfachte Linie für die Karte, Start/Ziel |
| `RoutingOptions` (`Services/Routing/`) | Optionsklasse | `BaseUrl` (https://api.openrouteservice.org/), `EndpointNotConfigured`, `RequestTimeout` 15 s, `MaxAttempts` 3, `RetryBaseDelay` 1 s, `MinRequestInterval` 1,5 s, `UserAgent`, `AllowLoopbackHttp`; `Validate` (HTTPS-Pflicht, Loopback nur im Testmodus), `FromEnvironment` (`TANKRADAR_ROUTING_URL`) |
| `RouteSearchOptions` | Optionsklasse | `QueryRadiusKm` 25, `CoverageFactor` 0.95, `MaxQueries` 15, `MaxRouteLengthKm` 400, `SamePlaceKm` 0.2; `Validate` |
| `IRoutingService` | Interface (Gateway) | `IsAvailableAsync(ct)`, `GetRouteAsync(start, end, ct)` |
| `OpenRouteServiceClient` | Klasse | HTTP, Fehlerabbildung, Wiederholung |
| `PolylineDecoder` | statische Klasse | `TryDecode(string, precision, maxPoints)` |
| `RouteGeometry` | statische Klasse | Haversine, lokale Projektion, Punkt-Strecke-Abstand, Douglas-Peucker |
| `RouteSampler` | statische Klasse | Abfragepunkte, Abdeckungsformel, `TooLong` |
| `IRouteStationSearchService` / `RouteStationSearchService` | Interface / Service | sequenzielle Abfragen, Deduplizierung, Filter, Sortierung |
| `IRouteSearchService` / `RouteSearchService` | Interface / Service Layer | Gesamtablauf Geokodierung -> Routing -> Stationen |
| `MapRouteProjector` (`Services/Map/`) | statische Klasse | Projektion/Clipping der Linie auf Bildschirmkoordinaten |
| `MapViewModel.Route.cs` | partielle Klasse | Routenzustand und Befehle des Presenters |
| `RoutingApiKey.targets` (`MSBuild/`) | MSBuild-Datei | Build-Einbindung des Schlüssels |
| `MockOpenRouteServiceServer` (`src/TestSupport`) | Testhilfsklasse | Mock auf `LoopbackHttpListener` |
| `PolylineEncoder` (`src/TestSupport`) | Testhilfsklasse | kodiert Punkte für den Mock (Gegenstück zum Decoder) |
| `docs/adr/0014-route-search.md` | Dokument | Entscheidungen dieses Plans |

## Änderungen an bestehenden Klassen

### `SearchMode` (Enum)
- **Neuer Wert:** `Route` (nach `Address`; Reihenfolge bestimmt die Chip-Reihenfolge).

### `SearchTexts` (statische Textklasse)
- **Neue Texte/Methoden:** `GetModeLabel(Route)` = "Route", Beschriftungen für Start/Ziel/Abstand/Startart, `RoutingAttribution` ("Routen: OpenRouteService, Geodaten (c) OpenStreetMap-Mitwirkende"), `FormatRouteDistance` ("12,3 km ab Start"), `FormatRouteOffset` ("0,8 km abseits der Route"), `FormatRouteSummary`, Fortschrittstexte, `GetRouteMessage(RouteSearchResult)` für alle `RouteSearchStatus`, `GetRoutingMessage(RoutingFailure)` für alle Werte, `RouteOffline`, `RouteTooLong` (mit Grenze), `RouteSamePlace`.
- **Geändert:** `FormatShowMore` bzw. die Barrierefreiheit der Schaltfläche (siehe Härtungen).

### `MapViewModel` (ViewModel)
- **Neue Eigenschaften:** `IsRouteMode`, `RouteStartKind`/`RouteStartOptions`, `StartInput`, `DestinationInput`, `RouteDistanceKm`/`RouteDistanceOptions`, `RouteSummary`, `MapRoute`, `RouteProgressText`, `RoutingAttributionText`, `IsRoutingAttributionVisible`.
- **Neue Methoden:** `RunRouteSearchAsync`, `SelectStartKind`, `ClearStart`, `ClearDestination`.
- **Geänderte Methoden:** `ModeOptions`/`OnModeSelected` (Sichtbarkeit Felder: Adressfeld nur im Modus `Address`, Radiuschips nicht im Modus `Route`), `SearchAsync` (Routenzweig, Validierung), `ApplyResult`/`PublishVisible` (Route und Streckenposition, `MapRoute`), `AttributionText` (Routenquelle im Modus `Route`).
- **Konstruktor:** neue Abhängigkeit `IRouteSearchService`; `MapViewModelTestBase` und DI werden angepasst.

### `StationResultBuilder` (statische Klasse)
- **Neue Methode:** `BuildForRoute(routeStations, fuelTypes, filter, sortOrder, nowUtc)` - kopiert `DistanceKm` = `AlongKm`, setzt `RouteOffsetKm`; ruft dieselbe Filter-, Preis-, Hinweis- und Sortierlogik.

### `StationListItem` (Datenmodellklasse)
- **Neue Eigenschaften:** `RouteOffsetKm` (`double?`), `RouteOffsetText`, `HasRouteOffset`; `DistanceText` im Routenmodus "ab Start".

### `StationInfo`
- Unverändert (Kopie mit angepasster `DistanceKm` im Routenzweig).

### `StationMapView` (Control) und `MapPage.xaml`
- **Neue bindbare Eigenschaft:** `Route` (`MapRoute?`); Zeichnen der Linie, Anpassung des Ausschnitts, Zugänglichkeitstext.
- `MapPage.xaml`: Bereich "Route" (sichtbar nur im Routenmodus), Quellenangabe, Fortschritt, Zusammenfassung.

### `MapMarkerBuilder` / `MapMarkerSelection`
- Prüfen, ob die Begrenzung der Marker eine Entfernung zum Ursprung verwendet; im Routenmodus gilt `DistanceKm` (= Streckenposition). Keine Verhaltensänderung für andere Modi.

### `ApiKeyProvider`, `IApiKeyStore`-Implementierungen, `ApiKeyStoreSelector`
- Parametrisierung um Schlüsselname (Speicherschlüssel), Build-Metadatumname und Testumgebungsvariable; Standardwerte = bisheriges Verhalten. `CredentialLockerApiKeyStore` und `SecureStorageApiKeyStore` erhalten den Speicherschlüssel als Konstruktorparameter.

### `MauiProgram`
- Neue `AddRoutingServices`: `RoutingOptions.FromEnvironment`, `RouteSearchOptions`, eigener `RequestThrottle`, `HttpClient` (`AllowAutoRedirect=false`), routing-spezifische Schlüsselablage/-provider, `IRoutingService`, `IRouteStationSearchService`, `IRouteSearchService`.

### `Tankradar.MAUI.csproj`
- Import von `MSBuild\RoutingApiKey.targets`. `MtouchInterpreter` und `TrimmerRootAssembly` unverändert, keine neue Abhängigkeit.

### `TestDataPaths` (`src/TestSupport`)
- Neue Konstanten `TANKRADAR_ROUTING_URL`, `TANKRADAR_ROUTE_API_KEY`.

### `MockTankerkoenigServer`
- Bei Bedarf Hilfsmethode `AddStationAt(id, name, lat, lng, preise)` für Stationen an definierten Positionen entlang der Mock-Route.

### `TransientWait` (`src/TestSupport`)
- Behandelt `ElementNotAvailableException` wie einen UIA-Timeout ("noch nicht erfüllt", weiter bis zur Frist) und liefert die in Timeouts verbrachte Zeit (`out TimeSpan timeoutTime`), damit E2E-Grenzen auf die Messzeit bezogen werden können.

### Skripte und CI
- `scripts/package-windows.ps1`: zusätzlich `TANKRADAR_ROUTING_API_KEY` und `ROUTING_API_KEY` aus der Umgebung entfernen.
- `.github/actions/build-and-package/action.yml`: beim Windows-Build `TANKRADAR_ROUTING_API_KEY` auf `''` (wie die Tankerkönig-Variablen); keine Änderung an Secrets, Eingaben oder Workflows. `scripts/local-ci.ps1` prüfen (kein Schlüsselbezug erwartet).

## Datenbankmigrationen

Keine. Der Abstand zur Route wird nicht persistiert; Routen, Adressen und Positionen werden nicht gespeichert. Ein Test belegt, dass nach einer Routensuche weder Schema noch Einstellungen noch Tabellen anders sind (`RoutePrivacyTests_Persistence`).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Ziel, Start (Adresse) | `AddressInput.Validate` (3 bis 120 Zeichen, erlaubte Zeichen) vor jedem externen Aufruf | Meldung wie Adresssuche, kein Netzzugriff |
| Abstand | nur 1, 2, 5, 10 km (`RouteDistance.IsValid`) | Meldung, kein Netzzugriff |
| Start aktueller Standort | `GpsUsage.AllowsLocation()` und Berechtigung | `GetLocationMessage` |
| Start/Ziel-Position | `GeoPosition.TryCreate`; Abstand >= 200 m | `RouteSearchStatus.SamePlace` |
| `RoutingOptions` | HTTPS Pflicht (Loopback nur im Testmodus), Zeitlimit > 0, `MaxAttempts` >= 1, `MinRequestInterval` >= 1 s | `Validate` wirft; Endpunkt nicht konfiguriert -> `EndpointNotConfigured` |
| `RouteSearchOptions` | `QueryRadiusKm` <= 25, `CoverageFactor` in (0,1], `MaxQueries` >= 1 | `Validate` wirft |
| ORS-Antwort | Polylinie nicht leer, <= 50 000 Punkte, gültige Koordinaten, Länge > 0 | `InvalidResponse` |
| Routenlänge/Abfragezahl | L <= 400 km und n <= 15 | `RouteTooLong` mit Meldung |
| Tankerkönig-Abfragen | Radius <= 25 km (bestehend in `FuelPriceService`) | bestehende Validierung |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `RoutingOptions.BaseUrl` / Umgebungsvariable `TANKRADAR_ROUTING_URL` | URL | `https://api.openrouteservice.org/` | Endpunkt (Loopback nur im Testmodus) |
| `RoutingOptions.RequestTimeout` | Dauer | 15 s | Zeitlimit je Versuch |
| `RoutingOptions.MaxAttempts` / `RetryBaseDelay` | int / Dauer | 3 / 1 s | Wiederholung mit zunehmender Wartezeit |
| `RoutingOptions.MinRequestInterval` | Dauer | 1,5 s | Drosselung |
| `RouteSearchOptions.MaxQueries` / `MaxRouteLengthKm` | int / double | 15 / 400 | Obergrenzen |
| `RouteSearchOptions.QueryRadiusKm` / `CoverageFactor` | double | 25 / 0.95 | Abfragegeometrie |
| `ROUTING_API_KEY` bzw. `TANKRADAR_ROUTING_API_KEY` | Umgebungsvariable (Build) | - | Build-Einbindung (Secret) |
| `routing.local.props` (Element `RoutingApiKey`) | nicht versionierte Datei (fällt unter `*.local.props` in `.gitignore`) | - | lokale Build-Einbindung |
| `TANKRADAR_ROUTE_API_KEY` | Umgebungsvariable (nur Testmodus) | - | Laufzeitschlüssel in Tests |
| Speicherschlüssel `tankatlas.routing-api-key` | Credential Locker / Keychain | - | Laufzeitablage |

## Seiteneffekte und Risiken

- **`MapViewModel`-Konstruktor:** neue Abhängigkeit; `MapViewModelTestBase` und alle ableitenden Tests sowie die DI-Registrierung müssen angepasst werden (zentral über die Basisklasse).
- **`SearchMode.Route`:** `ModeOptions` erzeugt den Chip automatisch; Tests mit fester Chipzahl (`MapViewModelTests_Chips`, E2E mit `SelectChip`) sowie `SettingsEnumTests`/Enum-Abdeckung (`enum-coverage-check.py`) müssen den neuen Wert abdecken.
- **`ApiKeyProvider`/Store-Parametrisierung:** darf das Tankerkönig-Verhalten nicht ändern (Regressionstests `ApiKeyProviderTests_Resolution`, `ApiKeyStoreSelectorTests_TestMode`, `ApiKeyBuildTests_*`).
- **Preiscache:** `FuelPriceService` speichert die Stationen der Abfragen (öffentliche Daten) in der Datenbank; bis zu 15 Abfragen vergrößern den Bestand "bekannter Stationen" und können Stationen außerhalb des Bandes enthalten. Das ist akzeptiert (wie bei Radius- und Adresssuche), wird im ADR und in der Datenschutzbeschreibung der Hilfe genannt; Tests belegen, dass keine Routen-/Adressdaten gespeichert werden.
- **Last auf Tankerkönig:** Routensuche erzeugt bis zu 15 Abfragen; Gegenmaßnahmen siehe Entscheidungen; die Nutzungsbedingungen werden im ADR mit Fundstelle belegt.
- **Dauer:** bis ca. 15 s Abfragezeit durch den 1-s-Takt; Fortschrittsanzeige und Abbruch (neue Suche, Seitenwechsel) sind Pflicht, Abbruch muss `RequestThrottle`-Wartezeiten sofort beenden.
- **Windows-Paket:** Ohne Leeren der Routing-Variable würde der Schlüssel in öffentlich verteilte Pakete gelangen (siehe Entscheidungen).
- **Kartenzeichnung:** Linie mit bis zu 50 000 Punkten kann die Zeichnung verlangsamen; deshalb Vereinfachung auf ~1 000 Punkte und Begrenzung auf den sichtbaren Ausschnitt.
- **ORS-Verfügbarkeit/Kontingent:** Meldungen statt Absturz; App bleibt nutzbar.
- **iOS:** keine neue Abhängigkeit, Dekodierung/Geometrie selbst implementiert (keine Reflexion/Generierung), `MtouchInterpreter`/`TrimmerRootAssembly` unverändert; iOS-Build wird hier nicht ausgeführt (kein Mac), das Risiko ist durch reinen verwalteten Code gering.
- **UIA/E2E:** neue Elemente erhöhen den UIA-Baum nur geringfügig; keine Container-AutomationIds für Listen (siehe Abnahme 16).
- **`MapViewModel`-Größe:** Aufteilung in `MapViewModel.Route.cs`; `MapViewModel` muss dafür `partial` sein (falls nicht, mit anpassen).

## Umsetzungsreihenfolge

1. **Grundlagen: Modelle, Enums, Optionen**
   - Voraussetzungen: Keine
   - Beschreibung: `RouteDistance`, `RoutingFailure`, `RouteStartKind`, `RouteSearchStatus`, `RoutingOptions` (mit `Validate`/`FromEnvironment`), `RouteSearchOptions`, `RouteLocation`, `RouteQueryPoint`, `RoutingResult`, Request-/Result-Klassen; XML-Dokumentation (CS1591). Unit-Tests dazu (Optionsvalidierung, `RouteDistance`, Enum-Abdeckung folgt mit den Texten).

2. **`PolylineDecoder` und `PolylineEncoder` (TestSupport)**
   - Voraussetzungen: Schritt 1 (`GeoPosition` vorhanden)
   - Beschreibung: Dekodierung nach Google-Algorithmus mit Genauigkeit 5, Schutz gegen Überlauf, Abbruch bei fehlerhaften Daten und Punktgrenze; Encoder in `src/TestSupport` für Mock und Tests. Unit-Tests (Bekanntes Beispiel, Round-Trip, negative Werte, Fehlerfälle, Grenzen).

3. **`RouteGeometry` und `RouteProfile`**
   - Voraussetzungen: Schritt 2
   - Beschreibung: Haversine, lokale Projektion, Punkt-Strecke-Abstand, `Locate`, `PointAt`, `Bounds`, Douglas-Peucker `Simplify`. Unit-Tests (Abstand, Streckenposition, Enden, entartete Teilstrecken, Schleifen, 52 Grad Breite).

4. **`RouteSampler` mit Abdeckungstest**
   - Voraussetzungen: Schritt 3
   - Beschreibung: Abfragepunkte, Radius, `TooLong`. Unit-Tests: Anzahl je Stufe, Obergrenze 400 km/15, Mittelpunkte, Radius <= 25, Determinismus und eigenschaftsbasierter Abdeckungstest (zufällige Punkte im Band liegen in mindestens einem Abfragekreis, auch bei Kurven und Enden).

5. **`IRoutingService`, `OpenRouteServiceClient` und `MockOpenRouteServiceServer`**
   - Voraussetzungen: Schritte 1, 2; `RequestThrottle`/`IDelay` (vorhanden)
   - Beschreibung: Client mit Fehlerabbildung, Wiederholung, Zeitlimit; Mock in `src/TestSupport` (Authorization-Prüfung, `SetRoute`, `EnqueueStatuses`, `ResponseDelay`, Zähler, `LastRequestBody`, ORS-Fehlerformat), Konstanten in `TestDataPaths`. Unit-Tests (Basis nach Muster `TankerkoenigClientTestBase`): Parsing, Wiederholung, Fehlerarten, Endpunkt/Schlüssel fehlt, kein Schlüssel in URL/Fehlertext.

6. **Schlüsselbereitstellung (Build, Laufzeit, Skripte)**
   - Voraussetzungen: Schritt 5 (Client braucht `IApiKeyProvider`)
   - Beschreibung: `RoutingApiKey.targets` und Import im csproj; Parametrisierung von `ApiKeyProvider`, Stores und Selector; `scripts/package-windows.ps1`, Aktion `build-and-package`; `.gitignore` prüfen (`*.local.props` genügt). Tests: `ApiKeyBuildTests_Routing` (echter MSBuild-Lauf, Quellenreihenfolge, Log ohne Wert, Trennung von Tankerkönig), `ApiKeyProviderTests_Routing`, Regression der vorhandenen Tests.

7. **`RouteStationSearchService`**
   - Voraussetzungen: Schritte 3, 4; `IFuelPriceService` (vorhanden)
   - Beschreibung: sequenzielle Abfragen, Deduplizierung, Filter, Sortierung, Abbruch bei Fehler, Fortschritt, Abbruchtoken. Unit-Tests mit Fake-Preisdienst.

8. **`RouteSearchService`**
   - Voraussetzungen: Schritte 5, 7; `IGeocodingService` (vorhanden)
   - Beschreibung: Gesamtablauf und Statusabbildung. Unit-Tests aller `RouteSearchStatus`, Reihenfolge der Aufrufe (keine Folgeaufrufe nach Fehler), Validierung, `SamePlace`.

9. **DI-Registrierung `AddRoutingServices`**
   - Voraussetzungen: Schritte 5 bis 8
   - Beschreibung: Registrierung in `MauiProgram`; Test der Auflösbarkeit (vorhandenes Muster der DI-Tests).

10. **Texte und Listenaufbereitung**
    - Voraussetzungen: Schritt 1
    - Beschreibung: `SearchTexts`-Erweiterungen, `StationListItem`, `StationResultBuilder.BuildForRoute`; Tests inkl. Übersetzungs-/Enum-Abdeckung aller `RoutingFailure`/`RouteSearchStatus`/`SearchMode.Route`/`RouteStartKind`.

11. **`MapViewModel` Routenmodus**
    - Voraussetzungen: Schritte 8, 9, 10
    - Beschreibung: `MapViewModel.Route.cs`, Modus, Eingaben, Validierung, Standortstart, Ergebnis-/Fehlerbehandlung, Fortschritt, Privacy. `MapViewModelTestBase` anpassen; Tests `MapViewModelTests_Route*`.

12. **Karte: `MapRoute`, `MapRouteProjector`, `StationMapView.Route`**
    - Voraussetzungen: Schritt 3
    - Beschreibung: Linie, Ausschnitt, Zugänglichkeitstext; Tests für Projektion/Vereinfachung/Begrenzungsbox.

13. **UI `MapPage.xaml`**
    - Voraussetzungen: Schritte 11, 12
    - Beschreibung: Bereich Route mit AutomationIds (siehe unten), Sichtbarkeitsregeln, Quellenangabe. Bestehende Muster (Moduschips, Eingabefeld mit Löschen, Radius-Chips) per Vorlage kopieren.

14. **Integrationstests mit Mock-Servern**
    - Voraussetzungen: Schritte 5, 9, 11
    - Beschreibung: `RouteSearchMockServerTests_Flow`, `_Errors`, `RoutePrivacyTests_Logging`, `RoutePrivacyTests_Persistence` (echte Dienste gegen `MockNominatimServer`, `MockOpenRouteServiceServer`, `MockTankerkoenigServer`).

15. **E2E-Tests Route**
    - Voraussetzungen: Schritte 13, 14; Mock-Erweiterungen
    - Beschreibung: `RouteE2ETestBase` und Szenarien (siehe Tests). Entwicklung nur mit Filter auf die neuen Klassen.

16. **Härtungen aus Abnahme 16**
    - Voraussetzungen: Schritt 15 (nutzt `TransientWait`)
    - Beschreibung: konkrete Aufgaben: (a) `TransientWait` um `ElementNotAvailableException` und `timeoutTime` erweitern, Unit-Tests; (b) `SearchE2ETests_LargeResult`: Namensreihenfolge nach dem Sortierwechsel gegen die erwartete Reihenfolge prüfen, nach "Weitere anzeigen" und Ruhe der App einmal 50 Einträge zählen, 10-s-Grenze auf Messzeit abzüglich Timeoutzeit beziehen (30-s-Ausnahme entfällt); (c) `TankbookEntriesE2ETests_Receipt`: Lesen von Name/Zustand zwischen Suche und Auswertung über `TransientWait` kapseln; (d) Schaltfläche "Weitere anzeigen (N weitere)": Ursache der doppelten Sprachausgabe in `MapPage.xaml` klären (Text und `SemanticProperties` doppelt), Name nur einmal liefern, E2E-Lesen von N und `SearchTextsTests` anpassen; (e) Doku: `docs/help/ci-cd/workflows.md` (Abschnitt UIA-Timeouts: `TransientWait`, Messzeit ohne Timeouts), ADR 0013 (nur Vorgangsseite des betroffenen Fahrzeugs schließt, `ITankbookNavigator.CloseAsync(vehicleId)`), `docs/help/Sicherung/ablauf-technisch.md` (Abonnent von `IRestoreNotifier` prüfen, bei Bedarf ergänzen). Das Verhalten in künftigen CI-Läufen wird im Zwischenstand-Bericht vermerkt.

17. **Dokumentation**
    - Voraussetzungen: Schritte 1 bis 16
    - Beschreibung: `docs/adr/0014-route-search.md` (Kontext, Entscheidungen der Tabelle oben mit Rechenbeispiel und Abdeckungsbeweis, Tankerkönig-Bedingungen mit Fundstelle/Datum, Aufhebung des Ausschlusses kombinierter Abfragen, Datenschutz, Schlüssel); Verweise in ADR 0003/0005; `docs/help/Suche/` (beschreibung, ablauf-anwender, ablauf-technisch), `docs/help/Preisdaten/api-schluessel.md` (Routing-Schlüssel), `docs/help/ci-cd/einrichtung.md` ("optional, bis die App ihn nutzt" entfernen), `docs/help/ci-cd/lokaler-pruefung.md` bei Bedarf; `README.md` (Funktion, Schlüssel `ROUTING_API_KEY`, `routing.local.props`, Quellen); `changes.log` (Eintrag Schritt 17); Test-Fokus-Hinweis in der Hilfe zu E2E.

18. **Prüfläufe und Zwischenstand**
    - Voraussetzungen: alle vorherigen
    - Beschreibung: `dotnet format`, XML-Doku, Übersetzungs-/Enum-Prüfung der Hooks; Unit und Integration vollständig; E2E: neue und betroffene Klassen gezielt, ein Gesamtlauf nur einmal zum Abschluss (Auslieferung nur mit grünen Tests); Windows-Zwischenstand mit `scripts/create-review-version.ps1` nach `review-versions/<Version>_<JJJJ-MM-TT>/` (nicht committen).

## Tests

**Test-Fokus-Hinweis (für alle E2E-Tests dieses Schritts):** E2E-Tests dürfen dem Anwender weder Tastatur- noch Maus- noch Fensterfokus nehmen: keine Aufrufe von `Focus()`, `SetForeground`, Tastatur- oder Mausaktionen; Eingaben ausschließlich per UIA-Value-Muster (`SettingsE2ETestBase.SetFieldValue`), Chips/Schaltflächen per `Invoke`; Fenstermodus `TANKATLAS_TEST_WINDOW` (Standard off-screen) bleibt unberührt. `E2EInputPolicyTests_NoFocus` prüft das per Quelltext und muss grün bleiben. In Wartephasen keine breiten `FindAll`-Aufrufe über das Fenster (Ergebnislisten sind groß): gezielte Suche nach AutomationId, Warten über `TransientWait`, Listen nur einmal lesen, sobald die App ruht (Muster `SearchE2ETests_LargeResult`). Während der Entwicklung nur die betroffenen E2E-Klassen mit Filter laufen lassen (der Anwender arbeitet parallel); keine unnötigen Vollläufe.

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Dekodierung, Fehler, Grenzen, Round-Trip | `PolylineDecoderTests_Decode`, `_Invalid` | Bekannte Polylinie, negative Werte, abgeschnittene Eingabe, zu viele Punkte, Überlauf |
| Haversine, Projektion, Punkt-Strecke-Abstand | `RouteGeometryTests_Distance` | Abstand bekannter Punkte, Fußpunkt-Klemmung, entartete Strecke |
| `Locate`, `PointAt`, `Bounds`, `Simplify` | `RouteProfileTests_Locate` | Streckenposition, Abstand, Schleife (kleinere Position), Vereinfachung hält Form |
| Abfragepunkte je Stufe, Obergrenzen, Radius, Determinismus | `RouteSamplerTests_Points` | n, Mittelpunkte, Radius <= 25, `TooLong` bei > 400 km / > 15 |
| Lückenlose Überdeckung des Bandes | `RouteSamplerTests_Coverage` | Zufallspunkte im Abstandsband liegen immer in einem Abfragekreis (für 1/2/5/10 km, gerade und gekrümmte Routen) |
| Parsing, Fehlerarten, Wiederholung, Schlüssel, Endpunkt | `OpenRouteServiceClientTests_Parsing`, `_Errors`, `_Retry`, `_Key`, `_EndpointNotConfigured` | Abbildung 200/400/401/403/404/429/5xx/Zeitlimit; Wartezeit 1 s, 2 s; Schlüssel nur im Header, nie im Fehlertext |
| Optionen | `RoutingOptionsTests_Validation`, `RouteSearchOptionsTests_Validation`, `RouteDistanceTests_Validation` | HTTPS, Loopback nur im Testmodus, Grenzen, Stufen |
| Auswahl der Stationen | `RouteStationSearchServiceTests_Selection` | Deduplizierung, Filter nach Abstand, Sortierung nach Streckenposition, Station am Start/Ziel, Station knapp außerhalb |
| Abfrageablauf | `RouteStationSearchServiceTests_Queries` | sequenzielle Abfragen, Anzahl, Abbruch bei Fehler, Abbruch durch Token, Fortschritt |
| Gesamtablauf | `RouteSearchServiceTests_Flow`, `_Failures` | alle `RouteSearchStatus`, kein Folgeaufruf nach Fehler, Validierung vor Aufrufen, `SamePlace` |
| Schlüsselquellen | `ApiKeyBuildTests_Routing`, `ApiKeyProviderTests_Routing` | Reihenfolge der Quellen, Log ohne Wert, Trennung von Tankerkönig-Schlüssel, Testmodus nur `TANKRADAR_ROUTE_API_KEY` |
| Listenaufbereitung | `StationResultBuilderTests_Route` | Streckenposition als Entfernung, Abstandstext, Sortierungen, Filter |
| Presenter | `MapViewModelTests_Route`, `_RouteValidation`, `_RouteFailures`, `_RoutePrivacy` | Modus/Chips, Sichtbarkeiten, Standortstart, `GpsUsage.Never`, Offline, Fehlermeldungen, keine Aufrufe bei ungültiger Eingabe, nichts gespeichert/protokolliert |
| Texte/Enums | `SearchTextsTests_Route` | Meldung für jeden `RoutingFailure`/`RouteSearchStatus`, `GetModeLabel(Route)`, Übersetzungs-/Enum-Abdeckung |
| Kartenlinie | `MapRouteProjectorTests_Projection`, `MapViewportTests_RouteBounds` | Projektion, Clipping, Ausschnittsanpassung |
| Mock/Encoder | `MockOpenRouteServiceServerTests`, `PolylineEncoderTests` | Authorization-Prüfung, Fehlerfolgen, Round-Trip mit Decoder |
| `TransientWait` | `TransientWaitTests_ElementNotAvailable` | `ElementNotAvailableException` gilt als "noch nicht erfüllt", `timeoutTime` korrekt |
| Integration Ablauf | `RouteSearchMockServerTests_Flow` | echte Dienste gegen die drei Mock-Server: Reihenfolge, Zählung der Abfragen (<= 15), Mindestabstand der Tankerkönig-Abfragen (>= 1 s), Ergebnisfilter |
| Integration Fehler | `RouteSearchMockServerTests_Errors` | 429/5xx/Zeitlimit/Schlüssel falsch/404/zu lang/Ort unbekannt |
| Integration Datenschutz | `RoutePrivacyTests_Persistence`, `_Logging` | Datenbank und Einstellungen unverändert (außer Stationen/Preisen), keine Adresse/Koordinate/Route/Schlüssel im Protokoll |
| Migration | keine | - |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `MapViewModelTestBase` und abgeleitete Klassen | neue Konstruktorabhängigkeit `IRouteSearchService` (Fake) |
| `MapViewModelTests_Chips` | zusätzlicher Chip `Route` |
| `SearchTextsTests_*`, Enum-/Übersetzungsprüfung | neuer Enum-Wert und neue Texte |
| `SearchE2ETestBase` | Mock-Routingserver und Umgebungsvariablen (Hilfsmethoden `SelectRouteMode`, `SetRouteStart`, `SetRouteDestination`, `SelectRouteDistance`) |
| `SearchE2ETests_LargeResult` | Härtungen (siehe Schritt 16) |
| `TankbookEntriesE2ETests_Receipt` | Härtung gegen `ElementNotAvailableException` |
| E2E-Tests mit `Search.ShowMore` | Name der Schaltfläche (Schritt 16d) |
| `ApiKeyProviderTests_Resolution`, `ApiKeyStoreSelectorTests_TestMode`, `ApiKeyBuildTests_*` | Regression der Parametrisierung (Verhalten unverändert; nur falls Signaturen sich ändern) |
| `E2EInputPolicyTests_NoFocus` | bleibt unverändert, muss die neuen E2E-Dateien bestehen |

### E2E-Tests (primärer Funktionsnachweis)

Neue Dateien im Muster von `SearchE2ETests_*` (Basis `RouteE2ETestBase : SearchE2ETestBase` mit `MockOpenRouteServiceServer`, Umgebung `TANKRADAR_ROUTING_URL`, `TANKRADAR_ROUTE_API_KEY`). Mock-Route: Berlin (10115) nach Oranienburg über eine feste Polylinie; Mock-Tankerkönig erhält Stationen mit definierten Abständen zur Route (nah, 4 km abseits, 12 km abseits, hinter dem Ziel).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Happy Path: Modus Route, Start "10115", Ziel "Oranienburg", Abstand 2 km -> Stationen in Reihenfolge der Streckenposition, nur Stationen innerhalb 2 km, Entfernungstext "ab Start", Zusammenfassung, Quellenangabe OpenRouteService/OSM sichtbar | `RouteE2ETests_Basic` | Eingabe, Route, Stationsauswahl, Sortierung nach Position, Quellenangabe | Gesamter Benutzerfluss über echte Dienste-Kette und UI |
| Pflicht | Abstand ändern (1/5/10 km) ändert die Treffermenge; Abfragezahl am Mock <= 15 | `RouteE2ETests_Distance` | einstellbarer Abstand, Obergrenze | Chips, Neuberechnung, Zählung am Mock |
| Pflicht | Start "Aktueller Standort" (`TANKATLAS_TEST_LOCATION`) -> Route ohne Startadresse | `RouteE2ETests_CurrentLocation` | Standortstart | Zusammenspiel Standortdienst/UI |
| Pflicht | `GpsUsage.Never` + Start Standort -> Meldung, keine externen Aufrufe | `RouteE2ETests_GpsNever` | GPS-Einstellung | Einstellung -> Verhalten in der UI |
| Pflicht | Filter nach Sorte, Sortierung Preis/Name/Entfernung, Karte/Liste-Wechsel inkl. Hinweis "Route eingezeichnet" in `Search.Map.Hint` | `RouteE2ETests_FilterSort` | gewohnte Filter/Sortierungen, Karte | Interaktion der Ergebnisansichten |
| Pflicht | Antippen einer Station öffnet die Detailansicht | `RouteE2ETests_OpenDetail` | Detailansicht per Antippen | Navigation |
| Pflicht | Fehler: Ziel unbekannt ("Nirgendwoburg"); Eingabe zu kurz (Mock `Requests` = 0); ORS 404; ORS 429 dreimal (Wiederholung, Quota-Meldung); ORS nicht erreichbar; Route zu lang (Mock liefert 600 km; Tankerkönig `ListRequests` = 0); Tankerkönig-Fehler | `RouteE2ETests_Errors` | verständliche Fehlermeldungen, Validierung vor externen Aufrufen | Meldungen und Zustand der UI nach Fehlern |
| Pflicht | Schlüssel fehlt: Routenmodus meldet "nicht verfügbar", Modus Standort/Adresse funktionieren weiter | `RouteE2ETests_KeyMissing` | fehlender Schlüssel, Rest nutzbar | App-Start ohne Schlüssel |
| Pflicht | Neustart der App: Routenfelder leer, keine Route sichtbar (nichts gespeichert) | `RouteE2ETests_NotPersisted` | Datenschutz | nur beim echten Neustart prüfbar |
| Pflicht | Barrierefreiheit: AutomationIds und Namen der Route-Elemente, Fortschritt, Fehlermeldung als `Search.StatusMessage` | `RouteE2ETests_Accessibility` | Zugänglichkeit | UIA-Baum |

AutomationIds (neu): `Search.Mode.Route`, `Search.Route.StartKind.CurrentLocation`, `Search.Route.StartKind.Address`, `Search.Route.Start.Input`, `Search.Route.Start.Clear`, `Search.Route.Destination.Input`, `Search.Route.Destination.Clear`, `Search.RouteDistance.{Key}`, `Search.Route.Summary`, `Search.Route.Progress`, `Search.Attribution.Routing`, `Search.Station.RouteOffset`; bestehend wiederverwendet: `Search.Submit`, `Search.StatusMessage`, `Search.Map.Hint`, `Search.Station.Name`/`.Distance`/`.Open`.

Betroffene bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `SearchE2ETestBase` | Erweiterung um Routing-Mock und Hilfsmethoden (alle Suchtests nutzen sie; Verhalten ohne Routing unverändert) |
| `SearchE2ETests_LargeResult` | Härtungen aus Abnahme 16 |
| `TankbookEntriesE2ETests_Receipt` | Härtung gegen `ElementNotAvailableException` |
| `SearchE2ETests_*` mit Chip-Auswahl | falls Chipanzahl oder -reihenfolge abgefragt wird |

## Offene Punkte

Keine.
