# Umsetzungsplan: Kartenansicht umstrukturieren (Issue #19)

## Übersicht

Die Ansicht „Karte“ (`MapPage`) wird zur Vollflächenkarte: `StationMapView` füllt den gesamten Bereich. Die bisherige Eingabe-`ScrollView` entfällt; Suchart, Route- und Adresseingabe wandern in ein Overlay über einer Symbolschaltfläche, die Trefferliste in ein am unteren Rand angedocktes Bottom-Sheet-Panel (eingeklappt mit Trefferanzahl, ausgeklappt mit Liste). Neu ist außerdem das Nachladen der Tankstellen des sichtbaren Kartenausschnitts per ausdrücklicher Schaltfläche.

Betroffen sind `MapPage` (XAML und Code-Behind), `StationMapView` (Ausschnittsereignis, Vollflächenbetrieb), `MapViewport` (Begrenzung und Umkehrprojektion), `MapViewModel` (Zustand und Befehle), neue Steuerelemente für Panel und Overlay, die Texte, die Tests (Unit, Integration, E2E) und die Dokumentation (ADR 0005, Hilfe „Karte“). Datenbank, Konfiguration und Preisdienst-Schnittstelle bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Overlay für Route und Adresse | Overlay als Grid-Überlagerung innerhalb von `MapPage` (Sichtbarkeit über `IsSearchOverlayOpen`), kein `ContentPage`-Modal und kein Popup-Paket | Karte und ViewModel-Zustand bleiben beim Öffnen erhalten; keine neue NuGet-Abhängigkeit (ADR 0005). Ein modales Navigieren würde die Karte aus dem Baum nehmen und Markierungen neu aufbauen. |
| Bottom-Sheet-Panel | Eigenes Steuerelement `StationBottomSheet` (Views/Controls) mit zwei Höhenzuständen, Ziehgeste (`PanGestureRecognizer`) und einer Auf-/Zuklapp-Schaltfläche am Kopf | Es gibt im Projekt kein Bottom-Sheet; eine eigene Komponente ist ohne Paket möglich. Die Schaltfläche ist Pflicht, damit die Bedienung ohne Geste funktioniert (ADR 0005, Abweichung „Verschieben auch über Schaltflächen“). |
| Höhen des Panels | Eingeklappt: feste Höhe für Kopfzeile mit Trefferanzahl; ausgeklappt: höchstens 60 % der Seitenhöhe | Die Quellenangabe (ADR 0005: dauerhaft sichtbar) und die Kartenfläche bleiben oberhalb des Panels sichtbar. |
| Trefferliste im Panel | Die vorhandene Liste mit `Stations`, `ShowMoreCommand` und `ShowMoreText` (25 je Seite) wird in das Panel verlegt; keine neue Listenlogik | Wiederverwendung der bestehenden Seitenlogik. |
| Filter und Sortierung | Im Kopf des ausgeklappten Panels (Chips wie bisher); Radius, Suchart und Eingaben im Overlay | Filter und Sortierung wirken auf die Trefferliste, Radius und Suchart auf die Suche selbst. Vorschlag, siehe offene Punkte Nr. 3. |
| `ResultView` (Liste/Karte) | Enum und Einstellung bleiben unverändert (Persistenzvertrag, kein Migration). Die Chips „Liste/Karte“ entfallen aus der Oberfläche. Die Standardansicht steuert nur den Startzustand des Panels: „Liste“ öffnet ausgeklappt, „Karte“ eingeklappt. `SelectView` / `ViewOptions` werden entfernt bzw. nicht mehr angezeigt. | Vermeidet eine Migration und Änderung der Einstellungsdarstellung; die Standardansicht behält einen erkennbaren Effekt. |
| Panel-Zustand | Rein sitzungsbezogen im ViewModel (`IsBottomSheetExpanded`), beim Erscheinen aus der Standardansicht gesetzt, nicht persistiert | Anforderung: keine Speicherung von Zuständen; ADR 0014 verlangt, dass keine Positionen gespeichert werden. |
| Nachladen (Ausschnittssuche) | Eine einzige Umkreisabfrage: Mittelpunkt = Mitte des sichtbaren Ausschnitts, Radius = ganze Kilometer, aufgerundet, aus der halben Diagonale des Ausschnitts; liegt der Radius über 25 km, ist die Schaltfläche deaktiviert und ein Hinweis „Bitte näher heranzoomen“ erscheint | Die Preisquelle kennt nur Kreise bis 25 km; eine Teilabdeckung ohne Hinweis wäre irreführend. Eine Abfrage pro Nutzeraktion entspricht ADR 0014 (keine automatischen Abfragen). Neue Klasse `MapAreaQuery` (Services/Map) mit reiner Funktion `From(MapBounds, fuelTypes)`, unit-testbar. |
| Auslösung Nachladen | `StationMapView` meldet ein Ereignis `ViewportChanged` nur bei Nutzeraktionen (Ziehen, Wischen, Kneifen, Mausrad, Zoom- und Verschiebeschaltflächen, Zurücksetzen). Programmatisches Einpassen (`FitToContent`, `RequestFit`) löst es nicht aus. Das ViewModel zeigt die Schaltfläche, wenn sich Mitte oder Zoom gegenüber der letzten Suche geändert haben. | Trennt Anwenderbewegung vom automatischen Einpassen. |
| Einpassen nach Nachladen | `MapViewModel` setzt für Ausschnittssuchen den Modus `KeepViewport`; `StationMapView` führt dann kein `RequestFit` aus. Nur neue Suchen (Standort, Adresse, Route) passen den Ausschnitt an. | Sonst würde jedes Nachladen den Ausschnitt des Anwenders zurücksetzen (Befund der Bestandsaufnahme). |
| Ergebnis des Nachladens | Ersetzt Liste, Marker und Zähler vollständig; die Herkunftsmarkierung (`MapOrigin`) bleibt die der letzten Suche | Vorschlag, siehe offene Punkte Nr. 2. Vermeidet Mischzustände aus zwei Ausschnitten. |
| Nachladen im Routenmodus | Schaltfläche nur in den Modi Standort und Adresse | Routenergebnisse sind streckenbezogen; eine Kreisabfrage würde sie verwerfen. Vorschlag, siehe offene Punkte Nr. 5. |
| Takt des Nachladens | Nach jeder Nachladeabfrage ist die Schaltfläche 60 s gesperrt (über `TimeProvider`, im ViewModel); keine Wiederverwendung des `RouteQueryPacer` | `RouteQueryPacer` ist an die Routensuche gebunden. Tankerkönig nennt 1 Abfrage pro Minute (ADR 0014). Vorschlag, siehe offene Punkte Nr. 6. |
| Tippen auf Panel-Eintrag | Öffnet die Detailansicht über das bestehende `OpenStationCommand` (wie die Markierung) | Bestehendes Verhalten (ADR 0005); keine zusätzliche Hervorhebung, da nicht gefordert. |
| Quellenangabe und Statusmeldungen | Quellenangabe (Link OpenStreetMap, ggf. Routing-Quelle) bleibt als Overlay unten links über der Karte, oberhalb des Panels. Offline-Banner und Statustext stehen als Zeile am Kopf des Panels. | ADR 0005 verlangt dauerhafte Sichtbarkeit der Quelle. |
| Legende | Als kleine Kartenlegende oben rechts über der Karte | Legende war bisher unterhalb der Karte; bleibt bei Vollfläche sichtbar. |
| Symbolschaltflächen | Oben links: Suche (öffnet Overlay). Oben rechts: Zoom- und Verschiebeschaltflächen bleiben bestehen (`Map.*`-IDs unverändert). | Bedienung ohne Geste bleibt vollständig. |
| Texte | Neue Einträge in `MapTexts` (Schaltflächen, Panel-Zustände, Hinweise, Overlay-Titel) und `SearchTexts` (Anzahl im Panel, Nachlade-Hinweise) | Vorhandene Textklassen der Kartenansicht. |

## Programmabläufe

### Ansicht öffnen (Vollfläche und Panel-Startzustand)

1. `MapPage` zeigt `StationMapView` in voller Fläche; Overlays (Suchschaltfläche, Quellenangabe, Legende, Panel) liegen darüber.
2. Beim Erscheinen (`MapViewModel.OnAppearing`) wird die Standardansicht aus den Einstellungen gelesen; `IsBottomSheetExpanded` wird daraus gesetzt (`ResultView.List` → ausgeklappt, `ResultView.Map` → eingeklappt).
3. Das Panel zeigt in der eingeklappten Kopfzeile die Trefferanzahl (`TotalStationCount`) bzw. „Keine Ergebnisse“, in der ausgeklappten Ansicht die Liste mit Seitenumschaltung.

Beteiligte Klassen/Komponenten: `MapPage`, `MapViewModel`, `StationBottomSheet`

### Suche über Overlay

1. Anwender tippt die Suchschaltfläche; `OpenSearchOverlayCommand` setzt `IsSearchOverlayOpen = true`.
2. Das Overlay (`MapSearchOverlay`) bindet an die vorhandenen Eigenschaften: `Mode`-Chips (`SearchMode`, Route nur bei aktivierter Routensuche), Adresseingabe, Start-/Ziel-/Abstandsfelder (`RouteDistanceOptions`), Radius (`SearchRadius`) und `SearchCommand`.
3. `SearchCommand` führt die bestehende Logik aus (`SearchAsync`), schließt das Overlay bei gültiger Eingabe und zeigt Fehler im Overlay bzw. als Statuszeile; ein Abbruch über `CancelRouteSearchCommand` bleibt erhalten.
4. Schließen ohne Suche (Schaltfläche „Schließen“ oder Rückwärtstaste) setzt `IsSearchOverlayOpen = false`; Eingaben bleiben im ViewModel erhalten.

Beteiligte Klassen/Komponenten: `MapPage`, `MapSearchOverlay`, `MapViewModel`, `MapViewModel.Route`

### Panel auf- und zuklappen

1. Tippen auf den Kopf oder die Schaltfläche am Panel ruft `ToggleBottomSheetCommand` auf.
2. Die Ziehgeste (`PanGestureRecognizer`, Vertikal) setzt bei Schwelle (Hälfte der Differenz der Höhenzustände) den Zustand um; die Höhe wird animiert (`TranslateTo`).
3. `IsBottomSheetExpanded` wird geändert; `ShowMoreVisible` folgt (`HasMore && IsBottomSheetExpanded`).

Beteiligte Klassen/Komponenten: `StationBottomSheet`, `MapViewModel`

### Antippen eines Panel-Eintrags

1. Der Eintrag ruft `OpenStationCommand` mit dem `StationListItem` auf.
2. `OpenStation` öffnet die Detailansicht über `IStationNavigator.OpenDetailAsync` (einmalig, `_isOpeningStation`).

Beteiligte Klassen/Komponenten: `StationBottomSheet`, `MapViewModel`, `IStationNavigator`

### Ausschnitt verändern und Nachladen

1. Der Anwender verschiebt oder zoomt die Karte (Geste, Schaltfläche, Mausrad, Kneifen). `StationMapView` aktualisiert `_viewport` und löst `ViewportChanged` mit dem neuen `MapViewport` aus; programmatisches Einpassen löst kein Ereignis aus.
2. `MapPage` leitet das Ereignis an `MapViewModel.OnViewportChanged` weiter. Das ViewModel vergleicht Mitte und Zoomstufe mit dem Ausschnitt der letzten Suche (`_searchedViewport`).
3. Bei Abweichung und Modus Standort oder Adresse wird `ShowAreaReloadButton = true` gesetzt. Liegt die berechnete Abfragegrenze über 25 km, wird `AreaReloadHint` gesetzt und die Schaltfläche deaktiviert. Im Routenmodus bleibt sie ausgeblendet.
4. Der Anwender tippt „Tankstellen in diesem Bereich laden“; `ReloadVisibleAreaCommand` prüft die 60-s-Sperre und setzt `IsAreaReloading`.
5. `MapAreaQuery.From(bounds, fuelTypes)` erzeugt die `StationSearchQuery` (Mittelpunkt, Radius ≤ 25 km, Kraftstoffarten aus den Einstellungen).
6. `IFuelPriceService.SearchNearbyAsync` wird genau einmal aufgerufen; Abbruch über die Suchsperre (`CancellationTokenSource`) bei neuer Suche oder Modus-Wechsel.
7. Das Ergebnis wird mit `ApplyResult` übernommen (Liste, Marker, Zähler), `_searchedViewport` aktualisiert, `MapFitMode = KeepViewport` gesetzt, `ShowAreaReloadButton = false`, Sperre gestartet.
8. `StationMapView` aktualisiert nur Marker (`RefreshMarkers`), ohne `RequestFit`.

Beteiligte Klassen/Komponenten: `StationMapView`, `MapPage`, `MapViewModel`, `MapAreaQuery`, `MapViewport`, `IFuelPriceService`

### Neue Suche mit Einpassen

1. Bestehender Ablauf (`SearchAsync`, `ApplyResult`, `ApplyFilterAndSort`) bleibt erhalten; `MapFitMode` wird auf `FitToResult` gesetzt.
2. `StationMapView` führt `RequestFit` wie bisher aus.
3. Filter- oder Sortierwechsel (`ApplyFilterAndSort`) verwendet den aktuellen `MapFitMode` nicht neu; der Ausschnitt bleibt. Nur neue Suchen setzen `FitToResult`.

Beteiligte Klassen/Komponenten: `MapViewModel`, `StationMapView`

### Ausschnittsgrenzen berechnen

1. `MapViewport.GetBounds()` ermittelt die vier Eckpunkte über die neue Umkehrprojektion `ToLatLon(x, y)` (Web-Mercator, Gegenstück zu `ToScreen`).
2. `MapBounds` (neues Modell) hält Minimal- und Maximal-Breite und -Länge.
3. `MapAreaQuery.From` berechnet Mittelpunkt und Halbdiagonale (Haversine), rundet auf ganze Kilometer auf (mindestens 1) und lehnt Werte über 25 km ab (liefert `null`).

Beteiligte Klassen/Komponenten: `MapViewport`, `MapBounds`, `MapAreaQuery`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `MapBounds` | Datenmodellklasse (`readonly record struct`, `Models/Map/`) | Begrenzung des sichtbaren Kartenausschnitts (Min/Max Breite und Länge) |
| `MapAreaQuery` | Statische Klasse (`Services/Map/`) | Erzeugt aus `MapBounds` und Kraftstoffarten eine `StationSearchQuery` mit Kreis (Mitte, Radius); gibt `null` bei Radius über 25 km zurück |
| `MapFitMode` | Enum (`Models/Map/`) | `FitToResult` (neue Suche, passt Ausschnitt an) und `KeepViewport` (Nachladen, behält Ausschnitt) |
| `StationBottomSheet` | Steuerelement (`Views/Controls/`, XAML + Code-Behind) | Panel mit Kopfzeile (Trefferanzahl, Auf-/Zuklappen), Ziehgeste und Liste |
| `MapSearchOverlay` | Steuerelement (`Views/Controls/`, XAML + Code-Behind) | Overlay für Suchart, Route-/Adresseingabe, Radius und Suchen-Schaltfläche |
| `ViewportChangedEventArgs` | Datenmodellklasse (`Views/Controls/` oder `Models/Map/`) | Trägt den neuen `MapViewport` für das Ereignis `ViewportChanged` |

## Änderungen an bestehenden Klassen

### `MapPage` (XAML, Code-Behind)

- **Layout (XAML):** `ScrollView` und alle Eingabekarten entfallen aus dem Seitenfluss. Neu: `Grid` mit `controls:StationMapView` in Vollfläche (ohne `HeightRequest`), darüber Overlays: Suchschaltfläche oben links, Legende oben rechts, Quellenangabe unten links, `StationBottomSheet` unten, `MapSearchOverlay` als Vollflächen-Überlagerung mit Abdunklung (`IsVisible` an `IsSearchOverlayOpen`).
- **Neue Bindungen:** `ShowAreaReloadButton`, `ReloadVisibleAreaCommand`, `AreaReloadHint`, `IsBottomSheetExpanded`, `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `IsSearchOverlayOpen`, `MapFitMode` (an `StationMapView`).
- **Geänderte Methoden:** `MapPage(MapViewModel, ITileSource)` – unverändert; zusätzlich Verdrahtung `ViewportChanged` → `MapViewModel.OnViewportChanged`.
- **Neue Ereignis-Handler:** `OnMapViewportChanged(object?, ViewportChangedEventArgs)` – reicht den Ausschnitt an das ViewModel weiter.
- **Entfällt:** `OnStationTapped` bleibt (Panel-Eintrag leitet an dieselbe Logik weiter).

### `StationMapView` (Steuerelement, `.xaml.cs`, `.Pointer.cs`)

- **Neue Ereignisse:** `ViewportChanged` (`EventHandler<ViewportChangedEventArgs>`) – ausgelöst nach Nutzeraktionen (Ziehen, Wischen, Kneifen, Mausrad, Zoom- und Verschiebeschaltflächen, Zurücksetzen).
- **Neue Eigenschaft:** `MapFitMode` (`MapFitMode`, bindbar) – steuert, ob `RequestFit` nach Markierungsänderungen läuft.
- **Geänderte Methoden:** `RefreshMarkers()` – führt `RequestFit` nur bei `FitToResult` aus. `Zoom(int)`, `Pan(double, double)`, `OnPanUpdated`, `OnPinchUpdated`, `OnPointerWheelChanged`, `OnRecenterClicked` u. a. – rufen am Ende `RaiseViewportChanged()` auf (privat). Programmatisches Einpassen über `FitToContent` ruft das Ereignis nicht auf.
- **Neue Methoden:** `RaiseViewportChanged()` (privat) – meldet den aktuellen Ausschnitt; `Viewport` (öffentliche Leseeigenschaft) – aktueller `MapViewport` für das ViewModel.
- **Vollflächenbetrieb:** Keine feste Höhe; Kartenfläche bleibt ohne Innenabstand, damit Overlays über der Karte liegen. Die Quellenangabe bleibt über dem Panel (Ausrichtung im Grid, nicht im Steuerelement).

### `MapViewport` (`Services/Map/MapViewport.cs`)

- **Neue Methoden:** `ToLatLon(double x, double y)` – Umkehrprojektion (Pixel → Breite/Länge) als Gegenstück zu `ToScreen`; `GetBounds()` – liefert `MapBounds` der Eckpunkte. Radiusberechnung (Haversine) liegt bewusst in `MapAreaQuery`, nicht im Viewport.
- **Geänderte Methoden:** keine. Verhalten von `Create`, `Fit`, `ZoomBy`, `PanByPixels` bleibt unverändert.

### `MapViewModel` (`ViewModels/MapViewModel.cs`)

- **Neue Eigenschaften:** `IsBottomSheetExpanded` (bool), `IsSearchOverlayOpen` (bool), `ShowAreaReloadButton` (bool), `AreaReloadHint` (string?), `IsAreaReloading` (bool), `MapFitMode` (`MapFitMode`), `AreaReloadRemainingSeconds` (int, für die Sperre).
- **Neue Befehle:** `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand`, `ReloadVisibleAreaCommand`.
- **Neue Methoden:**
  - `OnViewportChanged(MapViewport viewport)` (öffentlich) – prüft Modus und Abweichung zu `_searchedViewport`, setzt `ShowAreaReloadButton`, `AreaReloadHint`.
  - `ReloadVisibleAreaAsync(CancellationToken)` (privat) – erzeugt die Abfrage über `MapAreaQuery.From`, ruft `IFuelPriceService.SearchNearbyAsync` auf und übernimmt das Ergebnis über `ApplyResult` mit `FitMode = KeepViewport`.
  - `IsAreaReloadBlocked` (privat, `TimeProvider`) – 60-s-Sperre nach der letzten Nachladeabfrage.
- **Geänderte Methoden:**
  - `OnAppearing` / `SelectView(ResultView)` – setzt `IsBottomSheetExpanded` aus der Standardansicht statt `IsListView` für die Liste; `ViewOptions` und `OnViewSelected` werden entfernt.
  - `ApplyFilterAndSort` – setzt `MapFitMode` nicht zurück; keine Änderung der Sortierung.
  - `ApplyResult(StationSearchResult, MapOrigin)` – speichert zusätzlich `_searchedViewport` (nur bei neuen Suchen) und setzt `MapFitMode = FitToResult`, wenn kein Nachladen läuft.
  - `ShowMoreVisible` – `=> HasMore && IsBottomSheetExpanded` (bisher `IsListView && HasMore`).
  - `ChangeMode(SearchMode)` – verwirft laufendes Nachladen, setzt `ShowAreaReloadButton = false`.
  - `SearchAsync` – schließt das Overlay bei erfolgreichem Start der Suche; `CancelRouteSearch` bleibt unverändert.
- **Entfällt:** `ViewOptions`, `IsListView` (bleibt nur intern als Berechnung für `ShowMoreVisible`, falls nötig), `IsMapView`, `ShowMap`, `ShowMapHint` (durch Vollfläche ersetzt; Prüfung in Implementierung, ob Bindungen entfallen). Die Eigenschaft `ResultView` selbst bleibt als Einstellung.

### `MapViewModel.Route` (Teilklasse, `ViewModels/MapViewModel.Route.cs`)

- **Geänderte Methoden:** keine Logikänderung. Die Eigenschaften `StartInput`, `DestinationInput`, `RouteDistanceKm`, `SelectStartKind` und die Befehle werden vom Overlay gebunden.
- **Neue Eigenschaft:** `IsRouteModeSearchable` (bool) – `IsRouteMode && IsRouteSearchAccessible`, nur für die Overlay-Bindung.

### `MapRouteDrawable` (`Views/Controls/MapRouteDrawable.cs`)

- Keine Änderung (Routendarstellung innerhalb der Vollflächenkarte unverändert).

### `MapTexts` und `SearchTexts` (`Resources/Texts/`)

- **Neue Einträge in `MapTexts`:** `SearchButton` („Suche“), `ReloadArea` („Tankstellen in diesem Bereich laden“), `ReloadAreaWait` (Hinweis während der Sperre), `ReloadAreaTooLarge` („Bitte näher heranzoomen“), `PanelExpand`, `PanelCollapse`, `OverlayTitle`, `OverlayClose`, `LegendPlacement`.
- **Neue Einträge in `SearchTexts`:** `FormatStationCount(int)` („N Tankstellen“), `NoResults`.
- **Geänderte Einträge:** keine Änderung bestehender Texte, die in Tests über AutomationId oder Inhalt geprüft werden (`Map.Zoom`, `Map.StationCount`, `Map.Legend` bleiben gleich).

### `MapMarkerSelection` (`Services/Map/MapMarkerSelection.cs`)

- Keine Änderung. Die Auswahl nach Ausschnitt funktioniert mit dem neuen Modus weiter.

### `StationResultBuilder` (`Services/Search/`)

- Keine Änderung. Das Nachladen verwendet dieselbe Aufbereitung wie die Umkreissuche.

### `IFuelPriceService` (`Services/Pricing/FuelPriceService.cs`)

- Keine Änderung. Es wird nur `SearchNearbyAsync` mit einem Kreis aufgerufen.

## Datenbankmigrationen

Keine. Panelzustand, Overlay-Zustand und Nachladeberechnung sind sitzungsbezogen. `ResultView` und die Standardansicht bleiben unverändert (siehe Designentscheidungen).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `MapAreaQuery.From` (Radius) | Halbdiagonale des Ausschnitts in ganzen Kilometern, aufgerundet, mindestens 1 km | Ergebnis `null` bei Radius über 25 km; Schaltfläche deaktiviert, `ReloadAreaTooLarge` angezeigt |
| `MapAreaQuery.From` (Mittelpunkt) | Mittelpunkt innerhalb der Web-Mercator-Grenzen (±85,0511°) | Ausschnitt wird bereits durch `MapViewport` begrenzt; kein zusätzlicher Fehlerfall |
| `ReloadVisibleAreaCommand` | Nur im Modus Standort oder Adresse; nicht während laufender Suche; nur nach Ablauf der 60-s-Sperre | Befehl inaktiv; Hinweis `ReloadAreaWait` mit Restzeit |
| `OnViewportChanged` | Nur bei Abweichung zur letzten Suche (Mitte oder Zoom) | Schaltfläche bleibt ausgeblendet |
| `StationBottomSheet` | Höhenzustände begrenzt: eingeklappt fest, ausgeklappt höchstens 60 % der Seitenhöhe | Keine Fehlermeldung; Ziehen wird begrenzt |

## Konfigurationsänderungen

Keine. Der Routen-Abstand und der Suchradius bleiben bestehende Einstellungen. Die 60-s-Sperre für das Nachladen ist eine Konstante (`MapViewModel.AreaReloadMinInterval`) und keine Einstellung.

## Seiteneffekte und Risiken

- **Routensuche (ADR 0014):** Das Overlay muss die Suchart `Route` nur bei aktivierter Routensuche zeigen (`IRouteSearchAccess`). Die Sichtbarkeitslogik bleibt im ViewModel; das Overlay bindet nur daran.
- **Tankerkönig-Limit:** Jede Nachladeabfrage ist eine Anfrage an den Preisdienst. Die 60-s-Sperre reduziert die Häufigkeit; die bestehende Drosselung (`RequestThrottle`, 1 s) bleibt wirksam. Restrisiko bei mehrfachen Umkreissuchen (siehe ADR 0014 „Altbestand“) bleibt bestehen.
- **Größe des Ausschnitts:** Bei Standardzoom liegt die halbe Diagonale eines Telefonbildschirms häufig über 25 km. Die Schaltfläche erscheint dann nur in größeren Zoomstufen. Das ist beabsichtigt, muss aber im Hinweistext klar sein; ggf. Anpassung in der Abnahme.
- **Marker-Limit (ADR 0005):** Die Auswahl ist auf 100 Markierungen begrenzt; das gilt auch nach Nachladen. Der Zähler „X von Y Stationen sichtbar“ nennt alle Stationen im Ausschnitt.
- **Barrierefreiheit und UI Automation:** Panel, Overlay und Symbolschaltflächen brauchen AutomationIds; die Trefferliste im Panel bleibt auffindbar (`Search.ShowMore`).
- **Sichtbarkeit der Quellenangabe:** Bei ausgeklapptem Panel darf die Quellenangabe nicht verdeckt werden (Höhenbegrenzung 60 %).
- **`ResultView` / Standardansicht:** Die Bedeutung der Standardansicht ändert sich (Panel-Startzustand statt Umschaltung). Die Einstellung „Liste/Karte“ im Optionsbereich bleibt; Texte dort ggf. anpassen (Abnahme).
- **Vorhandene Routen- und Suchlogik:** Die Logik in `MapViewModel.Route.cs` und `SearchAsync` bleibt unverändert; Änderungen nur an Eigenschaftsbindung und Sichtbarkeit.
- **Preisdienst-Cache:** Nachladen nutzt den Such-Cache (5 Minuten), wenn der Ausschnitt bereits abgefragt wurde; das ist gewollt.
- **Gleichzeitige Bedienelemente:** Während das Overlay offen ist, dürfen Karte und Panel keine Eingaben empfangen (Abdunklung mit Eingabeabfang im Grid).
- **E2E-Stabilität:** Der bisherige E2E-Lauf endete mit Testhost-Absturz (siehe Bestandsaufnahme). Ohne vollständigen Ausgangslauf sind die Änderungen nicht gegen Regressionen belegbar (siehe Umsetzungsreihenfolge, Schritt 0).

## Umsetzungsreihenfolge

0. **E2E-Ausgangslauf vollständig herstellen**
   - Voraussetzungen: Keine. Testhost und Testumgebung wie in der Bestandsaufnahme (Release-Build, `TANKRADAR_E2E_WINDOW=offscreen`).
   - Beschreibung: Ursache des Testhost-Absturzes klären (Blame-Diagnose, Ereignisprotokoll), so dass der E2E-Bereich (161 `[Fact]`-Methoden) vollständig läuft. Ergebnis als Ausgangsnachweis in `inventory/test-results/` ablegen. Ohne diesen Schritt ist der Ausgangszustand des E2E-Bereichs für die Kartentests unbekannt.

1. **Umkehrprojektion und Begrenzung im Viewport**
   - Voraussetzungen: Schritt 0 (nur Ausgangszustand, keine Abhängigkeit im Code).
   - Beschreibung: `MapViewport.ToLatLon(x, y)` implementieren (Gegenstück zu `ToScreen`, Web-Mercator, Grenzen wie `MaxLatitude`). `MapBounds` als `readonly record struct` in `Models/Map/` anlegen. `MapViewport.GetBounds()` ergänzen.

2. **Unit-Tests für Umkehrprojektion und Begrenzung**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `MapViewportTests_Math` um Roundtrip-Tests erweitern, neue Testklasse `MapViewportTests_Bounds`.

3. **`MapAreaQuery` und Radius-Berechnung**
   - Voraussetzungen: Schritt 1 (`MapBounds`).
   - Beschreibung: Statische Klasse `MapAreaQuery` mit `From(MapBounds, IReadOnlyList<FuelType>)` (Haversine, Aufrunden auf ganze Kilometer, Grenze 25 km, `null` bei Überschreitung). Nutzt vorhandenes `StationSearchQuery`.

4. **Unit-Tests `MapAreaQuery`**
   - Voraussetzungen: Schritt 3.
   - Beschreibung: Neue Testklasse `MapAreaQueryTests_Radius` (Radius, Grenzwert 25 km, Mindestradius 1 km, Mittelpunkt, Kraftstoffarten).

5. **`MapFitMode` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Enum `MapFitMode` in `Models/Map/` mit `FitToResult` und `KeepViewport`.

6. **`ViewportChangedEventArgs` anlegen**
   - Voraussetzungen: Schritt 1 (`MapViewport`).
   - Beschreibung: Ereignisargument mit dem neuen `MapViewport` (Datei in `Views/Controls/` oder `Models/Map/`, einheitlich mit den anderen Ereignisargumenten im Projekt festlegen).

7. **`StationMapView`: Ausschnittsereignis und Einpassmodus**
   - Voraussetzungen: Schritt 1 (`GetBounds`, `ToLatLon`), Schritt 5 (`MapFitMode`), Schritt 6 (`ViewportChangedEventArgs`).
   - Beschreibung: Ereignis `ViewportChanged` bei Nutzeraktionen auslösen (nicht bei `FitToContent`); `MapFitMode` als bindbare Eigenschaft; `RefreshMarkers` führt `RequestFit` nur bei `FitToResult` aus. Die Prüfung erfolgt über E2E (Schritt 19), da das Code-Behind nicht Coverage-geprüft wird.

9. **`MapViewModel`: Zustände und Befehle für Panel und Overlay**
   - Voraussetzungen: Schritt 5 (`MapFitMode`).
   - Beschreibung: Eigenschaften `IsBottomSheetExpanded`, `IsSearchOverlayOpen`, Befehle `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand`. Startzustand aus `ResultView` (in `OnAppearing`/`SelectView`). `ViewOptions`, `IsMapView`, `ShowMap`, `ShowMapHint` entfernen bzw. nicht mehr verwenden.

10. **`MapViewModel`: Nachladen**
    - Voraussetzungen: Schritt 3 (`MapAreaQuery`), Schritt 9.
    - Beschreibung: `OnViewportChanged`, `ReloadVisibleAreaAsync`, `ReloadVisibleAreaCommand`, 60-s-Sperre mit `TimeProvider`, `_searchedViewport`, Abbruch bei Modus-Wechsel, `ApplyResult` mit `FitMode`.

11. **`MapViewModel`: Sichtbarkeitsregeln**
    - Voraussetzungen: Schritt 10.
    - Beschreibung: `ShowMoreVisible` auf `HasMore && IsBottomSheetExpanded` umstellen; `ChangeMode` verwirft Nachladen; `SearchAsync` schließt Overlay bei Erfolg.

12. **Texte anlegen**
    - Voraussetzungen: Keine.
    - Beschreibung: Einträge in `MapTexts` und `SearchTexts` gemäß Designentscheidungen.

13. **Unit-Tests ViewModel: Panel, Overlay, Nachladen**
    - Voraussetzungen: Schritte 9 bis 12.
    - Beschreibung: Neue Testklassen `MapViewModelTests_Panel`, `MapViewModelTests_SearchOverlay`, `MapViewModelTests_AreaReload` (Test-Basisklasse `MapViewModelTestBase`, `ManualTimeProvider`, `FakeFuelPriceService`).

14. **`StationBottomSheet` anlegen**
    - Voraussetzungen: Schritt 9 (Zustände), Schritt 12 (Texte).
    - Beschreibung: Steuerelement (XAML und Code-Behind) mit Kopfzeile (Trefferanzahl), Auf-/Zuklapp-Schaltfläche, Ziehgeste, Liste mit `Stations`, `ShowMoreCommand`, `ShowMoreText`, Zeilen-Tap über `OpenStationCommand`. AutomationIds (`Search.Panel`, `Search.Panel.Toggle`, `Search.ShowMore`).

15. **`MapSearchOverlay` anlegen**
    - Voraussetzungen: Schritt 12, Schritt 9.
    - Beschreibung: Overlay-Steuerelement mit Suchart-Chips (vorhandene Chips-Muster der Suchseite wiederverwenden), Adresseingabe, Start-/Ziel-/Abstandsfelder (Route), Radius-Chips (`SearchRadius.Steps`), Suchen-Schaltfläche, Schließen-Schaltfläche. AutomationIds (`Search.Overlay`, `Search.Overlay.Close`).

16. **`MapPage.xaml`: Vollflächen-Layout**
    - Voraussetzungen: Schritte 6, 9 bis 15 (Steuerelemente vorhanden).
    - Beschreibung: `ScrollView` entfernen; `Grid` mit `StationMapView` als Vollfläche; Symbolschaltfläche Suche oben links; Legende oben rechts; Quellenangabe unten links über dem Panel; `StationBottomSheet` unten; `MapSearchOverlay` als Vollflächen-Überlagerung; Trefferliste aus dem alten Seitenfluss entfernen. `HeightRequest="440"` entfällt.

17. **`MapPage.xaml.cs`: Ereignisverdrahtung**
    - Voraussetzungen: Schritt 16.
    - Beschreibung: `ViewportChanged` an `MapViewModel.OnViewportChanged` weiterreichen; `OnStationTapped` für Panel-Einträge beibehalten.

18. **Integrationstests Nachladen**
    - Voraussetzungen: Schritt 10 (Logik), vorhandener Mock-Server (`SearchMockServerTests_*`).
    - Beschreibung: Neue Testklasse `AreaSearchMockServerTests_Flow`: Anfrage mit korrektem Mittelpunkt und Radius, Fehlerfall (offline), Ergebnisersetzung.

19. **E2E-Tests (siehe Abschnitt „E2E-Tests“)**
    - Voraussetzungen: Schritte 14 bis 17 und 0.
    - Beschreibung: Neue Szenarien und Anpassung der betroffenen Tests (siehe unten).

20. **Dokumentation ADR 0005 und Hilfe „Karte“**
    - Voraussetzungen: Schritte 14 bis 17 (Umsetzung abgeschlossen).
    - Beschreibung: ADR 0005 um Bedienstruktur ergänzen (Vollfläche, Overlay, Panel, Nachladen); `docs/help/Navigation/beschreibung.md` (Abschnitt „Karte“) und `docs/help/Suche/` anpassen. Folgt im Schritt 9 (`update-docs`).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ToLatLon_RoundTripsToScreen` | `MapViewportTests_Math` | Umkehrprojektion ist Gegenstück zu `ToScreen` |
| `GetBounds_ContainsCenterAndCorners` | `MapViewportTests_Bounds` (neu) | Begrenzung des Ausschnitts, Mitte innerhalb |
| `GetBounds_ClampsAtPoles` | `MapViewportTests_Bounds` | Breitengrad bleibt innerhalb ±85,05° |
| `From_UsesCenterAndHalfDiagonal` | `MapAreaQueryTests_Radius` (neu) | Mittelpunkt und Radius (ganze km, aufgerundet) |
| `From_ReturnsNull_WhenRadiusExceeds25Km` | `MapAreaQueryTests_Radius` | Grenze 25 km |
| `From_UsesMinimumOneKilometer` | `MapAreaQueryTests_Radius` | Mindestradius |
| `From_PassesFuelTypes` | `MapAreaQueryTests_Radius` | Kraftstoffarten werden übernommen |
| `OnViewportChanged_ShowsReloadButton_WhenMovedFromLastSearch` | `MapViewModelTests_AreaReload` (neu) | Schaltfläche erscheint nur bei Abweichung |
| `OnViewportChanged_DoesNotQuery` | `MapViewModelTests_AreaReload` | Keine automatische Abfrage |
| `OnViewportChanged_HidesButton_InRouteMode` | `MapViewModelTests_AreaReload` | Routenmodus ohne Nachladen |
| `OnViewportChanged_DisablesButtonWithHint_WhenTooLarge` | `MapViewModelTests_AreaReload` | Hinweis bei Radius über 25 km |
| `ReloadVisibleArea_QueriesOnce_ReplacesResult_KeepsViewport` | `MapViewModelTests_AreaReload` | Eine Abfrage, Ergebnisersetzung, `MapFitMode.KeepViewport` |
| `ReloadVisibleArea_IsBlocked_For60Seconds` | `MapViewModelTests_AreaReload` | 60-s-Sperre mit `ManualTimeProvider` |
| `ChangeMode_DiscardsReload` | `MapViewModelTests_AreaReload` | Modus-Wechsel verwirft Nachladen |
| `ToggleBottomSheet_ChangesExpandedState` | `MapViewModelTests_Panel` (neu) | Auf- und Zuklappen |
| `DefaultView_List_OpensPanelExpanded` | `MapViewModelTests_Panel` | Standardansicht steuert Startzustand |
| `DefaultView_Map_OpensPanelCollapsed` | `MapViewModelTests_Panel` | Startzustand eingeklappt |
| `ShowMore_OnlyVisible_WhenPanelExpanded` | `MapViewModelTests_Panel` | Weitere-Anzeigen nur im ausgeklappten Panel |
| `OpenSearchOverlay_SetsFlag_AndCloseKeepsInputs` | `MapViewModelTests_SearchOverlay` (neu) | Overlay öffnen und schließen, Eingaben bleiben |
| `Search_ClosesOverlay_OnStart` | `MapViewModelTests_SearchOverlay` | Schließen bei Suchstart |
| `Hilfsmethode: AppearAndViewportAsync(...)` | `MapViewModelTestBase` (erweitert) | Bereitstellung von Ausschnitt und Modus |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `MapViewModelTests_MapView.Default_IsListView` | Standardansicht steuert jetzt Panel-Startzustand statt `IsListView` |
| `MapViewModelTests_MapView.DefaultViewMap_FromSettings_IsPreselected` | Entfällt bzw. Bedeutung wechselt zu Panel-Zustand |
| `MapViewModelTests_MapView.SessionView_SurvivesReappearingUntilDefaultChanges` | Umbenennung der Eigenschaft (Panel-Zustand) |
| `MapViewModelTests_MapView.SwitchView_DoesNotSearchAgain` | Liste/Karte-Umschaltung entfällt; Test wird auf Panel-Ausklappen umgestellt |
| `MapViewModelTests_MapView.EmptyResult_ShowsMapWithOriginOnly` | Karte ist immer sichtbar; Assertion ohne `IsMapView` |
| `MapViewModelTests_Settings` (Einstellungsänderung der Standardansicht) | Bedeutung der Standardansicht |
| `MapViewModelTests_Sort`, `MapViewModelTests_FilterSort` | Prüfen, dass `ShowMoreVisible` bei Filter/Sortierung folgt; Assertion auf `HasMore && IsBottomSheetExpanded` |
| `MapViewModelTests_Search` / `_Address` / `_Route*` (falls `IsListView` oder `ShowMoreVisible` geprüft) | Sichtbarkeitsregel `ShowMoreVisible` geändert |
| `SettingsTextsTests_Labels`, `SettingsEnumTests_Persistence` | Keine Änderung erwartet (Enum und Beschriftungen bleiben); nur als Regressionsprüfung |

Hinweis: Die genaue Liste aller Assertions auf `IsListView`, `IsMapView`, `ShowMap` und `ShowMapHint` ist bei der Umsetzung per Suche zu ermitteln (Stand Bestandsaufnahme: nur `MapViewModelTests_MapView` und `MapViewModelTests_Settings` prüfen diese Eigenschaften direkt).

### E2E-Tests (primärer Funktionsnachweis)

Die Vollflächenkarte, das Panel und das Overlay sind die Benutzerflüsse der Anforderung; der Funktionsnachweis erfolgt primär über FlaUI. Die Unit- und Integrationstests ergänzen die Logik.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Karte füllt die Ansicht; Panel ist eingeklappt und zeigt Trefferanzahl | `MapE2ETests_Layout` (neu) | Vollfläche, Panel-Startzustand | Layout nur über UI-Automation prüfbar |
| Pflicht | Panel ausklappen zeigt Liste, Eintrag antippen öffnet Detailansicht | `MapE2ETests_Panel` (neu) | Trefferliste im Panel, Detailöffnung | Benutzerfluss |
| Pflicht | Symbolschaltfläche öffnet Overlay; Adresse suchen schließt Overlay und markiert Position | `MapE2ETests_SearchOverlay` (neu) | Adresssuche über Overlay | Benutzerfluss |
| Pflicht | Route über Overlay suchen (Abschnitt bei aktivierter Routensuche) | `MapE2ETests_SearchOverlay` | Routensuche über Overlay | Benutzerfluss mit Schlüssel |
| Pflicht | Verschieben oder Zoomen zeigt Schaltfläche „Tankstellen in diesem Bereich laden“ | `MapE2ETests_AreaReload` (neu) | Nachladen nach Kartenbewegung | Benutzerfluss; Ereignis nur über UI auslösbar |
| Pflicht | Klick auf Nachladen aktualisiert Anzahl und Marker; Ausschnitt bleibt erhalten | `MapE2ETests_AreaReload` | Nachladen, kein Einpassen | Benutzerfluss mit Mock-Server |
| Pflicht | Ohne Bewegung erscheint keine Nachlade-Schaltfläche | `MapE2ETests_AreaReload` | Keine automatische Abfrage | Negativfall über UI |
| Pflicht | Ausschnitt über 25 km: Hinweis, Schaltfläche deaktiviert | `MapE2ETests_AreaReload` | Abfragegrenze | Sichtbarer Fehlerfall |
| Pflicht | Bedienung ohne Geste: Panel-Schaltfläche und Zoom-/Verschiebeschaltflächen | `MapE2ETests_Navigation` (angepasst) | Bedienung ohne Gesten | Barrierefreiheit über UI-Automation |
| Pflicht | Quellenangabe bleibt sichtbar bei ausgeklapptem Panel | `MapE2ETests_View` (angepasst) | Quellenangabe | Layout über UI |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `MapE2ETests_View.SwitchView_BetweenListAndMap_ShowsTheSameResults` | Liste/Karte-Umschaltung entfällt; Szenario auf Panel-Ausklappen umstellen |
| `MapE2ETests_View.DefaultViewFromSettings_Map_OpensSearchAsMap` | Standardansicht steuert nur den Panel-Startzustand |
| `MapE2ETests_View.Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly` | Legende und Quellenangabe liegen jetzt als Overlay |
| `MapE2ETestBase.ShowMapView()`, `ShowListView()` | Umschaltung entfällt; Hilfsmethoden ersetzen (Panel auf-/zuklappen) |
| `MapE2ETests_Markers.TapMarker_OpensDetailsAndBackReturnsToMap` | Panel bleibt sichtbar; Assertion auf Panel ergänzen |
| `MapE2ETests_Markers.AddressSearch_MarksSearchedPosition` | Suche läuft über Overlay (Öffnen vor Eingabe) |
| `MapE2ETests_Markers.LocationSearch_MarksOwnLocation` | Suche läuft über Overlay |
| `MapE2ETests_Markers.Filter_RebuildsMarkersForFilteredFuel` | Filter im Panel-Kopf (neuer Ort) |
| `MapE2ETests_Markers.Markers_ShowPriceAndLevelOfFirstSelectedFuel` | Keine Änderung der Logik; Locator ggf. angepasst |
| `MapE2ETests_Navigation.*` (Zoom, Verschieben) | Locator bleiben; Ausführung nach Umbau prüfen |
| `RouteE2ETests_FilterSort.MapView_DescribesDrawnRouteAndShowsMarkers` | Route über Overlay; Ansichtsumschaltung entfällt |
| `RouteE2ETests_*` (Chips, Eingaben im Seitenfluss) | Eingaben liegen nun im Overlay; Suchen über Overlay öffnen |
| `SearchE2ETests_*` (Chips und Eingaben im Seitenfluss) | Gleiche Änderung wie oben, bei Ausführung je Test prüfen |

Die Locator-Änderungen werden in `MapE2ETestBase` und `SearchE2ETestBase` gebündelt (neue Hilfsmethoden `OpenSearchOverlay()`, `ExpandPanel()`, `CollapsePanel()`), damit die Tests nicht einzeln angepasst werden müssen.

## Offene Punkte

Vorschläge sind als Empfehlung formuliert; die Umsetzung folgt ihnen, solange nichts anderes entschieden wird.

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Größe des Kartenausschnitts beim Nachladen (Anforderung Offene Frage 1) | Eine Umkreisabfrage mit Radius aus der halben Diagonale, deaktiviert bei über 25 km mit Hinweis „Bitte näher heranzoomen“. Keine Mehrfachabfragen. |
| 2 | Ergebnisverhalten Nachladen (Offene Frage 2) | Ersetzen von Liste, Markern und Zähler; Herkunftsmarkierung bleibt. |
| 3 | Platz für Filter, Sortierung und Radius (Offene Frage 5) | Filter und Sortierung im Panel-Kopf, Radius und Suchart im Overlay. |
| 4 | Antippen eines Panel-Eintrags (Offene Frage 4) | Öffnet die Detailansicht (bestehendes Verhalten); kein Einklappen des Panels, keine Hervorhebung auf der Karte. |
| 5 | Nachladen im Routenmodus (neu) | Schaltfläche nur in den Modi Standort und Adresse. |
| 6 | Takt des Nachladens (neu, betrifft Tankerkönig-Limit) | 60 s Sperre nach jeder Nachladeabfrage. Klärung mit dem Anbieter nicht nötig, sofern der Mindestabstand eingehalten wird. |
| 7 | Text der Nachlade-Schaltfläche (Offene Frage 3) | „Tankstellen in diesem Bereich laden“ (Anforderung nennt diesen Wortlaut als Beispiel). |
| 8 | Bedeutung von „Liste“ als Standardansicht (Offene Fragen 6 und 8) | Liste öffnet das Panel ausgeklappt, Karte eingeklappt; Einstellung bleibt unverändert. |
| 9 | Verknüpftes Issue mit Symbolschaltflächen (Offene Frage 9) | Die Symbolschaltfläche ist Teil dieser Anforderung und wird hier umgesetzt. Das Issue selbst ist nicht benannt; ein Abgleich mit dem Issue-Text vor der Umsetzung wird empfohlen. |
| 10 | Quellenangabe dauerhaft sichtbar (Offene Frage 10) | Dauerhaft unten links über dem Panel (ADR 0005). |
| 11 | Bedienung ohne Gesten (Offene Frage 7) | Auf-/Zuklapp-Schaltfläche am Panel-Kopf genügt; keine Tastatur-Kurzbefehle. |
| 12 | Standardzustand Panel (Offene Frage 8) | Siehe Punkt 8. Zustand wird nicht gespeichert. |
| 13 | Ausgangszustand des E2E-Bereichs (Bestandsaufnahme) | Vor der Umsetzung Schritt 0 ausführen. Kein Vorschlag möglich, bis die Ursache des Testhost-Absturzes bekannt ist. |
