### Fachliche Zusammenfassung

Die Ansicht „Karte“ (`MapPage`) wird so umgebaut, dass die `StationMapView` den gesamten verfügbaren Bereich einnimmt. Die bisherigen Eingabebereiche (Suchart, Eingaben, Radius, Filter, Sortierung) werden aus dem Seitenfluss gelöst: Route bzw. Adresse werden über eine Symbolschaltfläche in einem Overlay-Dialog gewählt, die Trefferliste liegt in einem am unteren Rand angedockten, per Geste hochziehbaren Bottom-Sheet-Panel. Das Panel zeigt mindestens einen eingeklappten Zustand mit der Trefferanzahl und einen ausgeklappten Zustand mit der Liste der Tankstellen. Bewegt der Anwender den sichtbaren Kartenausschnitt, erscheint eine textbasierte Aktionsschaltfläche („Tankstellen in diesem Bereich laden“ o. ä.), die die Tankstellen des aktuellen Kartenausschnitts nachlädt. Alle bestehenden Funktionen (Standort-, Adress- und Routensuche, Radius/Abstand, Filter, Sortierung, seitenweise Anzeige, Quellenangaben, Offline-Hinweise) bleiben erhalten; die Bedienung muss zusätzlich ohne Gesten über Schaltflächen möglich sein.

### Betroffene Klassen und Komponenten

**Views / UI-Komponenten**
- `src/Tankradar.MAUI/Views/MapPage.xaml` und `MapPage.xaml.cs`: Layout wird umgebaut (Karte füllt Ansicht; Symbolschaltflächen; Bottom-Sheet-Panel als Overlay über der Karte; Overlay-Dialog für Route/Adresse). Die bisherige `ScrollView` mit allen Eingabekarten entfällt bzw. wandert in den Overlay-Dialog.
- `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml` und `StationMapView.xaml.cs` (ggf. `StationMapView.Pointer.cs`): Vollflächiges Einbetten; neues Ereignis bzw. Eigenschaft für Kartenausschnittsänderung (Verschieben/Zoomen), das den Nachlade-Button ein-/ausblendet. Die Komponente setzt Kacheln und Marker auf einem `AbsoluteLayout` auf (ADR 0005).
- Neue Komponente (Annahme): Bottom-Sheet-Panel, z. B. `StationBottomSheet` (Views/Controls) mit Zuständen eingeklappt/ausgeklappt, Ziehgeste und Auf-/Zuklapp-Schaltfläche.
- Neue Komponente (Annahme): Overlay-Dialog für Routen-/Adresswahl, z. B. `MapSearchOverlay` (Views/Controls), mit den Feldern Start, Ziel, Abstand (Routensuche) bzw. Adresse.
- `src/Tankradar.MAUI/Views/Controls/MapRouteDrawable.cs`: keine Logikänderung erwartet, nur Darstellung innerhalb der Vollflächenkarte.

**ViewModel**
- `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`: neue Eigenschaften für Panelzustand (`IsBottomSheetExpanded` o. ä.), Overlay-Zustand (`IsSearchOverlayOpen` o. ä.), Sichtbarkeit des Nachlade-Buttons (`ShowAreaReloadButton` o. ä.), Befehle (`ReloadVisibleAreaCommand`, `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`). Die bestehende Umschaltung `ResultView` (`ViewOptions`, `IsListView`, `IsMapView`, `ShowMap`) ist zu prüfen, da die Karte nun dauerhaft im Fokus steht (siehe Offene Fragen).
- `src/Tankradar.MAUI/ViewModels/MapViewModel.Route.cs`: bleibt für die Routensuche zuständig; Start-/Ziel-/Abstandsfelder werden an den Overlay-Dialog gebunden, Logik unverändert.
- `src/Tankradar.MAUI/ViewModels/RouteSearchSettingsViewModel.cs`: unverändert (Routensuche bleibt nur bei aktivierter Schlüsselablage sichtbar).

**Services / Logik**
- `src/Tankradar.MAUI/Services/Map/MapViewport.cs`: Ermittlung des sichtbaren Kartenausschnitts (Bounding Box in Breite/Länge) und Ableitung von Mittelpunkt und Radius für die Abfrage. Unit-getestet.
- `src/Tankradar.MAUI/Services/Search/` (vorhandene Umkreissuche, Klassen zur Suche/Ergebnisbildung wie `StationResultBuilder`): Erweiterung um eine Suche nach Kartenausschnitt (Mittelpunkt plus Radius ≤ 25 km; ggf. mehrere Abfragen bei größerem Ausschnitt, siehe Offene Fragen).
- Bestehende Dienste aus `src/Tankradar.MAUI/Services/Routing/` (`RouteSearchService`, `RouteStationSearchService`, `RouteSearchOptions`) bleiben unverändert.

**Models**
- `src/Tankradar.MAUI/Models/Map/`: ggf. neues Modell für Kartenausschnitt (z. B. `MapBounds`, Annahme) bzw. Nutzung eines vorhandenen Typs.
- `src/Tankradar.MAUI/Models/Search/SearchMode.cs` und `SearchRadius.cs`: unverändert, Suchart `Route` nur bei Aktivierung.

**Texte / Ressourcen**
- `src/Tankradar.MAUI/Resources/Texts/MapTexts.cs` und `SearchTexts.cs`: neue Texte für Nachlade-Schaltfläche, Panel-Zustände (eingeklappt/ausgeklappt mit Anzahl), Overlay-Titel und Schaltflächenbeschriftungen, Barrierefreiheits-Hinweise.

**Tests**
- Unit-Tests: `MapViewport` (Bounding Box, Radius aus Ausschnitt), `MapViewModel` (Sichtbarkeit Nachlade-Button, Panelzustände, Befehle).
- Integrationstests: Suchablauf des Kartenausschnitts gegen Mock-Server (Analog zu `SearchMockServerTests_Flow.cs`).
- E2E (FlaUI, UI Automation): `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_View.cs`, `MapE2ETests_Navigation.cs`, `MapE2ETestBase.cs`, `MapE2ETests_Markers.cs`; ggf. `SearchE2ETests_*`, da deren Locator auf das bisherige Layout zugreifen können. Neue AutomationIds für Panel, Overlay und Symbolschaltflächen.

**Dokumentation**
- `docs/help/Navigation/beschreibung.md` (Abschnitt „Karte“) und `docs/help/Suche/beschreibung.md` bzw. `ablauf-anwender.md`/`ablauf-technisch.md` (Kartenansicht, Overlay, Panel, Nachladen).
- `docs/adr/0005-map-view-decisions.md` ggf. um die neue Bedienstruktur ergänzen.

### Implementierungsansatz

- **Layout:** `MapPage.xaml` wird auf ein `Grid` mit Karte in voller Fläche umgestellt. Darüber liegen per `Grid`-Überlagerung (gleiche Zelle, Ausrichtung) die Symbolschaltflächen oben, das Bottom-Sheet unten und der Overlay-Dialog. Die bestehende Entfernung/Sortierung/Filter-Logik in `MapViewModel.ApplyFilterAndSort` bleibt unverändert.
- **Bottom-Sheet:** Eine eigene Komponente mit zwei Höhenzuständen (eingeklappt mit Trefferanzahl, ausgeklappt mit Liste). Ziehen per `PanGestureRecognizer`; zusätzlich ist eine Schaltfläche zum Auf-/Zuklappen Pflicht (Barrierefreiheit, Tastatur unter Windows). Antippen eines Eintrags nutzt das bestehende Kommando `OpenStationCommand` bzw. hebt die Tankstelle auf der Karte hervor (siehe Offene Fragen). Die Liste nutzt weiterhin die vorhandene Seitenweise-Anzeige (`ShowMoreCommand`, `PageSize = 25`).
- **Overlay-Dialog:** Öffnet sich über eine Symbolschaltfläche. Enthält die Suchart (Standort, Adresse, Route nur bei aktivierter Routensuche gemäß ADR 0014), die Eingaben (Start/Ziel/Abstand bzw. Adresse) und die Suchen-Aktion. Die bestehende Logik aus `MapViewModel.Route.cs` und der Adresssuche (ADR 0003) wird wiederverwendet.
- **Nachladen nach Kartenbewegung:** `StationMapView` meldet Änderungen des Kartenausschnitts (Verschieben, Zoomen; Ereignis, Annahme). Das ViewModel setzt dann ein Flag, das die Textschaltfläche ohne Hintergrund einblendet. Der Klick löst eine Abfrage für den sichtbaren Bereich aus. Nach ADR 0014 gilt: Abfragen nur auf ausdrückliche Nutzeraktion, nicht automatisch; die Tankerkönig-Bedingungen (Radius ≤ 25 km, 1 Abfrage je Minute) sind einzuhalten. Dieselbe Pacing-Logik (`RouteQueryPacer`) ist zu prüfen, ob sie hier wiederverwendet werden kann.
- **Abhängigkeiten:** `MapViewModel` (Suchen, Filter, Sortierung, Quellenangabe, Offline-Hinweis), `StationMapView` (Marker-Auswahl über `MapMarkerSelection`), `IFuelPriceService` (Preisdienst), `IRouteSearchAccess` (Sichtbarkeit Suchart Route).
- **Erweiterungspunkte:** `MapViewport` für Bounds, `StationMapView` für Ausschnittsereignis, `MapViewModel` für Befehle und Zustand. Keine neue NuGet-Abhängigkeit (ADR 0005).

**Annahmen**
- Die bisherige Ansichtsumschaltung Liste/Karte (`ResultView`) entfällt oder wird nur noch für die Trefferliste im Panel genutzt.
- Der Nachlade-Vorgang ersetzt die Trefferliste des Panels durch die Tankstellen des neuen Ausschnitts (kein Anhängen).
- Der Standardzustand des Panels ist eingeklappt; der Zustand wird nicht persistiert.
- Keine Speicherung von Positionen, Routen oder Adressen (wie in ADR 0014).

### Konfiguration

Keine neue Konfiguration erforderlich. Der Routen-Abstand (`RouteDistance`, Standard 2 km) und der Suchradius bleiben bestehende Einstellungen. Panelzustand und Overlay-Zustand sind rein sitzungsbezogen und werden nicht gespeichert (keine Migration, keine Auswirkung auf Sicherung). Falls ein dauerhafter Standardzustand des Panels gewünscht ist, wäre dies eine anwendungsweite Einstellung in den Optionen (Annahme, nicht gefordert).

### Offene Fragen

1. **Nachladen und Abfragegrenze:** Wie groß darf der Kartenausschnitt sein? Die Umkreissuche akzeptiert nur Radien bis 25 km (ein Kreis). Ein sichtbarer Ausschnitt kann größer sein. Soll er auf einen Kreis mit maximal 25 km um die Mitte begrenzt werden, oder dürfen mehrere Abfragen (dann mit Obergrenze und Mindestabstand wie bei der Routensuche, ADR 0014) ausgelöst werden?
2. **Ergebnisverhalten Nachladen:** Ersetzt der Nachlade-Vorgang die Liste und die Karten-Marker vollständig, oder werden Ergebnisse zusammengeführt?
3. **Text der Nachlade-Schaltfläche:** Wortlaut (z. B. „In diesem Bereich suchen“)?
4. **Antippen eines Panel-Eintrags:** Öffnet er die Detailansicht, hebt er die Tankstelle auf der Karte hervor, oder beides? Soll die Karte dabei das Panel einklappen?
5. **Platz für Filter, Sortierung, Radius:** Sollen diese im Overlay-Dialog, im Panel-Kopf oder in einer eigenen Leiste liegen? Die Anforderung nennt sie nur als bestehende Funktionen.
6. **Umgang mit der Liste/Karte-Umschaltung (`ResultView`):** Soll die Listenansicht als eigene Seite entfallen und ausschließlich im Panel stattfinden?
7. **Bedienung ohne Gesten:** Genügt eine Auf-/Zuklapp-Schaltfläche am Panelkopf, oder sind zusätzliche Tastatur-Kurzbefehle (z. B. Pfeiltasten) gefordert?
8. **Panel-Standardzustand:** Eingeklappt beim Öffnen der Ansicht, oder zuletzt verwendeter Zustand (dann Speicherung nötig)?
9. **Verknüpftes Issue:** Das Issue zu Symbolschaltflächen und kompakteren Tankstellen-Panels wird erwähnt, aber nicht benannt. Soll es in demselben Arbeitsgang umgesetzt werden?
10. **Quellenangabe:** Muss die Quellenangabe (OpenStreetMap, OpenRouteService, Tankerkönig) dauerhaft sichtbar im Kartenbild bleiben, oder genügt sie im Panel bzw. Overlay?
