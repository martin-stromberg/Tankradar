# Bestandsaufnahme: Kartenansicht umstrukturieren (Issue #19)

Analysiert wurden die Kartenansicht der Suche (`MapPage`, `MapViewModel`, `StationMapView`, `MapViewport`) und die zugehörigen Modelle, Enums, Schnittstellen und Tests, bezogen auf die [Anforderung](requirement.md): Vollflächige Karte mit Symbolschaltflächen, Overlay für Route und Adresse, Bottom-Sheet-Panel mit Trefferliste und Nachladen nach Kartenbewegung.

## Zusammenfassung

**Vorhanden:**
- Suchen nach Standort, Adresse und (bei aktivierter Routensuche) Route mit Radius 1 bis 25 km, Filter, Sortierung und seitenweiser Anzeige (25 Einträge je Seite).
- Umschaltung Liste/Karte über `ResultView` mit Standardansicht aus den Einstellungen; laut Kommentar ist der Name ein Persistenzvertrag.
- Kartensteuerelement `StationMapView` mit Schaltflächen für Zoom, Verschieben und Zurücksetzen (`Map.ZoomIn`, `Map.ZoomOut`, `Map.Recenter`, `Map.Pan.*`) sowie Wisch-, Kneif- und Mausradgeste. Die Bedienung ohne Gesten für die Kartennavigation ist damit bereits vorhanden.
- Automatisches Einpassen des Ausschnitts auf Markierungen, Ursprung und Route (`MapViewport.Fit`, `FitToContent`), ausgelöst bei jeder Änderung der Markierungen.
- Auswahl der angezeigten Markierungen im Ausschnitt, höchstens 100 gezeichnet (`MapMarkerSelection`).
- Antippen einer Markierung öffnet die Detailansicht (`OpenStationCommand`, `IStationNavigator`).
- Quellenangaben, Offline-Hinweis und Legende sind vorhanden.
- Routendarstellung über `GraphicsView` (`MapRouteDrawable`, `MapRouteProjector`).
- Kartenmathematik in `MapViewport` mit Kacheln, Projektion, Verschieben, Zoomen und Einpassen (in sich vollständig, unabhängig von der Oberfläche).

**Fehlt offensichtlich:**
- Die Karte ist kein Vollflächenelement: `MapPage.xaml` legt alle Eingabekarten in einer `ScrollView` ab, die Karte hat `HeightRequest="440"`.
- Keine Symbolschaltflächen und kein Overlay-Dialog für Route und Adresse (keine entsprechende Komponente, keine Texte).
- Kein Bottom-Sheet-Panel (keine Komponente, keine Panelzustände, keine Ziehgeste).
- Keine Eigenschaften oder Befehle im `MapViewModel` für Panel-, Overlay- oder Nachlade-Zustand (`IsBottomSheetExpanded`, `IsSearchOverlayOpen`, `ShowAreaReloadButton`, `ReloadVisibleAreaCommand`, `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`).
- Kein Ereignis in `StationMapView` für Verschieben oder Zoomen; der aktuelle Ausschnitt ist nur intern (`_viewport`) bekannt.
- Keine Ermittlung von Begrenzungsbox oder Mittelpunkt mit Radius aus dem Ausschnitt in `MapViewport`; kein Modell für einen Kartenausschnitt.
- Der Preisdienst (`IFuelPriceService.SearchNearbyAsync`) kennt nur einen Kreis um Mittelpunkt und Radius (max. 25 km); eine Suche nach Kartenausschnitt existiert nicht.
- Keine Texte für Nachlade-Schaltfläche, Panelzustände, Overlay oder dessen Schaltflächen (`MapTexts`, `SearchTexts`).
- Befund für das Nachladen: Jede Änderung der Markierungen löst ein automatisches Einpassen aus (`RequestFit`). Dieses Verhalten ist beim Nachladen nach Kartenbewegung zu berücksichtigen, weil es den Ausschnitt des Anwenders verändern würde.

**Test-Ausgangszustand:**
- Unit-Tests: 1971 bestanden, 0 fehlgeschlagen, 0 übersprungen (Exit-Code 0).
- Integrationstests: 294 bestanden, 0 fehlgeschlagen, 0 übersprungen (Exit-Code 0).
- Node-Pipeline-Skripte (`npm test`): 32 bestanden, 0 fehlgeschlagen (Exit-Code 0).
- E2E-Tests (FlaUI): nicht vollständig ausführbar. Beide Läufe brachen mit abgestürztem Testhost ab (Exit-Code 124 nach Zeitlimit). Im zweiten Lauf wurden 97 Tests bestanden und 0 fehlgeschlagen gemeldet; 64 Testmethoden liefen nicht. Darunter fünf Kartentests (siehe unten).
- Bekannte Fehlschläge: keine. Der Ausgangszustand des E2E-Bereichs ist unbekannt.
- Testlücken: keine Tests für Kartenausschnitt, Nachladen, Panel oder Overlay (existieren nicht); Code-Behind von `MapPage` und `StationMapView` ist von der Coverage ausgenommen; fünf Kartentests der E2E-Suite sind im Ausgangslauf nicht gelaufen. Details, Nachweise und Befehle siehe [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md): `MapOrigin`, `MapRoute`, `RouteBounds`, `MapMarker`, `ScreenPoint`, `TileKey`, `PlacedTile`, `StationSearchQuery`, `SearchRadius`
- [Logik](inventory/logic.md): `MapPage`, `MapViewModel` (mit `MapViewModel.Route`), `StationMapView`, `MapViewport`, `MapMarkerSelection`, `MapMarkerBuilder`, `MapRouteProjector`, `RouteQueryPacer`, `StationResultBuilder`
- [Enums](inventory/enums.md): `SearchMode`, `ResultView`, `MapOriginKind`, `PriceLevel`, `ResultSortOrder`, `RouteStartKind`
- [Interfaces](inventory/interfaces.md): `IFuelPriceService`, `IRouteSearchAccess`, `IRouteSearchService`, `IStationNavigator`, `ITileSource`, `ILocationService`
- [Tests](inventory/tests.md): Test-Ausgangszustand, Testläufe mit Nachweisen unter [`inventory/test-results/`](inventory/test-results/), Testklassen und Hilfsmethoden

### Testklassen und Hilfsmethoden (Kurzübersicht)

- Unit: `MapViewModelTests_*` (18 Dateien, u. a. `_MapView`, `_Search`, `_Route*`), `MapViewportTests_Math`, `MapViewportTests_RouteBounds`, `MapMarkerSelectionTests_Limit`, `RouteQueryPacerTests`. Hilfsklasse `MapViewModelTestBase` mit Fakes (`FakeFuelPriceService`, `FakeRouteSearchAccess`, `FakeStationNavigator`, `ManualTimeProvider`).
- Integration: `SearchMockServerTests_*`, `RouteSearchMockServerTests_*`, `AddressSearchMockServerTests_Flow`.
- E2E: `MapE2ETests_View`, `MapE2ETests_Markers`, `MapE2ETests_Navigation`, Basisklasse `MapE2ETestBase` (Ansichtsumschaltung, Zählerwerte, Zoom-Lesen, Schaltflächenbedienung über AutomationIds).

Die Testklassen sind in [tests.md](inventory/tests.md) einzeln aufgeführt.
