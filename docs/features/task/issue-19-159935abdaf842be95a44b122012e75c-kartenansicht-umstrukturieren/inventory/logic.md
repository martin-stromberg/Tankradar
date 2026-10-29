# Logik (Kartenansicht)

Die Logik der Karte verteilt sich auf Seite, ViewModel, Steuerelement und Kartenmathematik. Die Ausnahmen für Code-Behind-Dateien (XAML) sind in der Coverage ausgenommen; ihr Verhalten wird nur über die E2E-Tests geprüft.

## `MapPage` (Code-Behind)
Datei: `src/Tankradar.MAUI/Views/MapPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MapPage(MapViewModel viewModel, ITileSource tileSource)` | öffentlich (Konstruktor) | Initialisiert die Seite, setzt `ResultMap.TileSource` und den `BindingContext` |
| `OnStationTapped(object?, EventArgs)` | privat | Leitet das Antippen einer Ergebniszeile an `MapViewModel.OpenStationCommand` weiter |

Im XAML (`MapPage.xaml`, 547 Zeilen) liegen aktuell alle Eingabebereiche untereinander in einer `ScrollView`: Ansicht (`Search.View.*`), Suchart (`Search.Mode.*`), Adresse, Route (Start, Ziel, Abstand), Radius, Suchen-Schaltfläche, Filter, Sortierung, Quellen- und Statushinweise, die Karte `controls:StationMapView` mit `HeightRequest="440"` (Bindungen `Markers`, `Origin`, `Route`, `ResultVersion`, `MarkerCommand`), die Legende und die Trefferliste (`Stations`, `IsListView`) mit `Search.ShowMore`. Die Karte ist damit kein Vollflächenelement.

## `MapViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/MapViewModel.cs` (973 Zeilen, `partial class`; Routenteil in `MapViewModel.Route.cs`)

Öffentliche Mitglieder (Auswahl, relevant für die Anforderung):

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|-----------------------|-------------|------------------|
| `SearchAsync()` | öffentlich | Startet die Suche je nach `SearchMode` (Route, Standort oder Adresse); prüft Radius und Adresseingabe vor dem Aufruf des Preisdienstes |
| `ApplyFilterAndSort()` | öffentlich | Baut Ergebnisliste, Markierungen (`MapMarkerBuilder.Build`) und Sortierung/Filter neu auf; setzt die Seitenzählung zurück |
| `ShowMore()` | öffentlich | Erhöht die sichtbare Anzahl um `PageSize` (25) |
| `SelectStartKind(RouteStartKind)` | öffentlich (Route-Teil) | Wählt die Startart der Route |
| `ClearStart()`, `ClearDestination()` | öffentlich (Route-Teil) | Leeren die Eingabefelder der Route |
| `OnAppearing()`, `OnDisappearing()` | öffentlich (überschrieben) | Laden der Einstellungen und Abonnements (Verbindung `ConnectionChanged`, Routenzugang `Changed`) |
| `OpenStation(StationListItem?)` | privat | Öffnet die Detailansicht einmalig (`_isOpeningStation`); über `OpenStationCommand` auch aus Karten-Markierungen |
| `SelectView(ResultView)` | privat | Schaltet zwischen Liste und Karte um (`IsListView`, `IsMapView`, `ShowMap`) |
| `ChangeMode(SearchMode)` | privat | Wechselt die Suchart, bricht laufende Suchen ab und leert das Ergebnis |
| `RunSearchAsync(int, CancellationToken)` | privat | Ermittelt die Position (Standort oder Adresse) und ruft `IFuelPriceService.SearchNearbyAsync` auf |
| `ApplyResult(StationSearchResult, MapOrigin)` | privat | Übernimmt ein Suchergebnis und setzt `MapOrigin` |
| `PublishVisible()` | privat | Aktualisiert die dargestellte Liste und die Eigenschaften `HasMore`, `ShowMoreVisible`, `ShowMoreText` |
| `OnConnectionChanged`, `UpdateOfflineState` | privat | Offline-Zustand und Offline-Banner |

Eigenschaften mit Bezug zur Karte: `ViewOptions`, `IsListView`, `IsMapView`, `ShowMap`, `ShowMapHint`, `HasMapContent`, `MapMarkers`, `MapResultVersion`, `Origin`, `MapRoute`, `HasMapRoute`, `TotalStationCount`, `HasMore`, `ShowMoreVisible`, `OpenStationCommand`, `ShowMoreCommand`, `SearchCommand`.

Abonnierte Ereignisse: `IConnectionMonitor.ConnectionChanged` (`OnConnectionChanged`) und `IRouteSearchAccess.Changed` (`OnRouteAccessChanged`). Keine Ereignisse werden publiziert (Benachrichtigung über `INotifyPropertyChanged`).

Nicht vorhanden: Eigenschaften oder Befehle für Panel-Zustand, Overlay, Nachlade-Schaltfläche oder Kartenausschnitt (`IsBottomSheetExpanded`, `IsSearchOverlayOpen`, `ShowAreaReloadButton`, `ReloadVisibleAreaCommand`, `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`).

## `MapViewModel.Route` (Teilklasse)
Datei: `src/Tankradar.MAUI/ViewModels/MapViewModel.Route.cs` (633 Zeilen)

Öffentlich: `IsRouteMode`, `IsRouteSearching`, `RouteInputsEnabled`, `IsRadiusVisible`, `RouteStartOptions`, `RouteStartKind`, `StartInput`, `DestinationInput`, `RouteDistanceKm`, `RouteDistanceOptions`, `RouteSummary`, `MapRoute`, `RouteProgressText`, `CancelRouteSearchCommand`, `ClearStartCommand`, `ClearDestinationCommand`, `SelectStartKind`, `ClearStart`, `ClearDestination`.

Privat: `SearchRouteAsync` (Einstieg), `ExecuteRouteSearchAsync` (ruft `IRouteSearchService.SearchAsync` mit Fortschritt auf), `ApplyRouteResult`, `CancelRouteSearch`, `RefreshRouteAvailabilityAsync` und `ApplyRouteAvailability` (Sichtbarkeit der Suchart `Route` über `IRouteSearchAccess`).

Die Routenlogik ist bereits über Eigenschaften gebunden und kann nach Anforderung in ein Overlay verlagert werden, ohne die Logik zu ändern.

## `StationMapView` (Steuerelement)
Dateien: `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml`, `StationMapView.xaml.cs` (668 Zeilen), `StationMapView.Pointer.cs` (126 Zeilen)

Bindbare Eigenschaften: `Markers`, `Origin`, `Route`, `ResultVersion`, `MarkerCommand`; öffentlich zusätzlich `TileSource`.

Interne Bedienung (alle privat):

| Methode | Kurzbeschreibung |
|---------|------------------|
| `Zoom(int)`, `Pan(double, double)` | Zoomen und Verschieben um feste Schritte (über `MapViewport.ZoomBy` bzw. `PanByPixels`) |
| `OnZoomInClicked`, `OnZoomOutClicked`, `OnRecenterClicked` | Schaltflächen `Map.ZoomIn`, `Map.ZoomOut`, `Map.Recenter` |
| `OnPanNorthClicked`, `OnPanSouthClicked`, `OnPanEastClicked`, `OnPanWestClicked` | Schaltflächen `Map.Pan.North`, `Map.Pan.South`, `Map.Pan.East`, `Map.Pan.West` |
| `OnPanUpdated`, `OnPinchUpdated` | Wisch- und Kneifgeste |
| `OnPointerWheelChanged` (Pointer-Datei) | Mausrad unter Windows |
| `FitToContent()` | Passt den Ausschnitt an alle Markierungen, die Position und die Route an (`MapViewport.Fit`) |
| `RequestFit()` | Plant das Einpassen nach einer Änderung der Markierungen (`_fitPending`) |
| `Render()`, `RenderTiles`, `RenderRoute`, `RenderMarkers`, `RenderOrigin`, `RenderDestination` | Zeichnen von Kacheln, Route, Markierungen, Ursprung und Ziel über `AbsoluteLayout` (`TileLayer`, `MarkerLayer`) |
| `RefreshMarkers()` | Baut die Markierungen neu auf (`MapMarkerSelection.Select`, höchstens 100 gezeichnet) |

Ein Ereignis für Kartenausschnittsänderungen (Verschieben oder Zoomen) ist nicht vorhanden. Der aktuelle Ausschnitt ist nur intern (`_viewport`) und wird nicht nach außen gemeldet.

Zusätzlich zu den Gesten gibt es vorhandene Schaltflächen für Zoom, Verschieben und Zurücksetzen; die Bedienung ohne Gesten ist für die Navigation damit bereits teilweise gewährleistet.

Wichtig für die Anforderung: Bei jeder Änderung der Markierungen wird der Ausschnitt automatisch eingepasst (`RequestFit`/`FitToContent`). Dies gilt bereits für jede neue Ergebnismenge und wäre beim Nachladen eines Kartenausschnitts zu berücksichtigen.

## `MapViewport` (Kartenmathematik)
Datei: `src/Tankradar.MAUI/Services/Map/MapViewport.cs` (260 Zeilen, `sealed class`, unveränderlich)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Create(lat, lon, zoom, width, height)` | öffentlich, statisch | Ausschnitt um eine Position |
| `Fit(points, width, height, padding, maxZoom)` | öffentlich, statisch | Passt den Ausschnitt an alle Punkte an; leere Liste zentriert Mitteleuropa |
| `Resize(width, height)` | öffentlich | Gleiche Mitte und Zoomstufe, neue Größe |
| `PanByPixels(dx, dy)` | öffentlich | Verschiebt den Ausschnitt |
| `ZoomBy(delta)` | öffentlich | Ändert die Zoomstufe bei gleicher Mitte (Grenzen 4 bis 18) |
| `ToScreen(lat, lon)` | öffentlich | Bildschirmposition einer Koordinate |
| `Contains(lat, lon, margin)` | öffentlich | Prüft, ob eine Koordinate im Ausschnitt liegt |
| `VisibleTiles()` | öffentlich | Sichtbare Kacheln mit Lage |
| `CenterLatitude`, `CenterLongitude`, `Zoom`, `Width`, `Height` | öffentlich (Eigenschaften) | Mitte und Größe des Ausschnitts |

Eine Ermittlung der Begrenzungsbox (Breite und Länge der Ecken) oder eines Mittelpunkts mit Radius aus dem Ausschnitt ist nicht vorhanden. Vorhanden ist nur die Projektion und das Einpassen. Sichtbarkeitsprüfung über `Contains` und Markierungsauswahl über `MapMarkerSelection`.

## `MapMarkerSelection`
Datei: `src/Tankradar.MAUI/Services/Map/MapMarkerSelection.cs` (59 Zeilen, statische Klasse)

| Methode / Mitglied | Sichtbarkeit | Kurzbeschreibung |
|--------------------|-------------|------------------|
| `MaxRendered` (`const int`, 100) | öffentlich | Obergrenze der gezeichneten Markierungen |
| `Select(markers, viewport)` | öffentlich, statisch | Wählt die Markierungen im Ausschnitt; bei Überschreitung die nahe der Mitte |

## `MapMarkerBuilder`
Datei: `src/Tankradar.MAUI/Services/Map/MapMarkerBuilder.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Build(stations, fuelType)` | öffentlich, statisch | Erzeugt die Kartenmarkierungen aus der Ergebnisliste |

## `MapRouteProjector`
Datei: `src/Tankradar.MAUI/Services/Map/MapRouteProjector.cs`

| Methode / Mitglied | Sichtbarkeit | Kurzbeschreibung |
|--------------------|-------------|------------------|
| `MinPointDistance` (`const double`, 1,5) | öffentlich | Mindestabstand zwischen Stützpunkten |
| `Project(viewport, points, margin)` | öffentlich, statisch | Projiziert die Route in Bildschirmkoordinaten |

## `RouteQueryPacer`
Datei: `src/Tankradar.MAUI/Services/Routing/RouteQueryPacer.cs` (77 Zeilen, `sealed class`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `WaitAsync(CancellationToken)` | öffentlich | Wartet bis zum nächsten reservierten Abfragezeitpunkt der Routensuche (Mindestabstand über `RouteSearchOptions`) |

Der Preisdienst drosselt zusätzlich über `PriceApiOptions.MinRequestInterval` (1 s) und `CacheLifetime` (5 min); die Routensuche verwendet einen eigenen Mindestabstand (`RouteSearchOptions.DefaultMinQueryInterval`, 10 s).

## `StationResultBuilder`
Datei: `src/Tankradar.MAUI/Services/Search/StationResultBuilder.cs` (200 Zeilen, statische Klasse)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Build(stations, fuelTypes, filter, sort, now)` | öffentlich, statisch | Erzeugt die Ergebnisliste (Filter und Sortierung) für Umkreissuche |
| `BuildForRoute(stations, fuelTypes, filter, sort, now)` | öffentlich, statisch | Wie `Build`, für Routensuchergebnisse |
| `ResolveFuelType(fuelTypes, filter)` | öffentlich, statisch | Ermittelt die Kraftstoffart für die Markierungsfarbe |

Aufgerufen von `MapViewModel.ApplyFilterAndSort`. Die Methoden sind vollständig unabhängig von der Oberfläche.

## Nicht berührt, aber relevant
- `MapRouteDrawable.cs` (`src/Tankradar.MAUI/Views/Controls/MapRouteDrawable.cs`, 45 Zeilen): Zeichnet die Route über `GraphicsView` (`RouteLayer`); keine Logikänderung nötig nach Anforderung.
- `StationTile.xaml`/`.cs` (`Views/Controls/`): Tankstellenkachel; nicht Gegenstand der Anforderung.
