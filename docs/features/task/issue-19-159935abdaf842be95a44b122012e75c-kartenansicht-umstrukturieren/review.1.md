# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Geprüft wurden `plan.md`, die Tasks-Datei und der Code unter `src/`. Die Dokumentationsschritte (ADR 0005, Hilfeseiten `docs/help`, Tasks 46 und 47) sind laut Vorgabe des Anwenders nicht Teil der offenen Implementierungsaufgaben und wurden nicht bewertet.

Der Großteil des Plans ist umgesetzt. Eine Lücke betrifft die Auslösung des Nachladens nach einer neuen Suche (`_searchedViewport`).

## Umgesetzte Planelemente

**Neue Klassen und Dateien**

- [x] `MapBounds` (`readonly record struct`, `Models/Map/`) — angelegt, Felder Min/Max Breite und Länge
- [x] `MapFitMode` (Enum, `Models/Map/`) — angelegt, Werte `FitToResult`, `KeepViewport`
- [x] `MapAreaQuery` (statische Klasse, `Services/Map/`) — angelegt, Methode `From(MapBounds, IReadOnlyList<FuelType>)`, Haversine, Aufrunden, Mindestradius 1 km, `null` über 25 km
- [x] `StationBottomSheet` (`Views/Controls/`, XAML + Code-Behind) — angelegt, Kopfzeile, Auf-/Zuklapp-Schaltfläche, Ziehgeste, Liste, Höchsthöhe 60 %
- [x] `MapSearchOverlay` (`Views/Controls/`, XAML + Code-Behind) — angelegt, Suchart, Adresse, Route, Radius, Suchen- und Schließen-Schaltfläche
- [x] `ViewportChangedEventArgs` (`Views/Controls/`) — angelegt, Feld `Viewport`

**MapViewport**

- [x] Methode `ToLatLon(double x, double y)` — vorhanden (Umkehrprojektion)
- [x] Methode `GetBounds()` — vorhanden, liefert `MapBounds`
- [x] Verhalten von `Create`, `Fit`, `ZoomBy`, `PanByPixels` unverändert

**StationMapView**

- [x] Ereignis `ViewportChanged` (`EventHandler<ViewportChangedEventArgs>`) — vorhanden, ausgelöst in Zoom, Verschieben, Ziehen, Kneifen, Mausrad, Zurücksetzen (`RaiseViewportChanged`)
- [x] `FitToContent` löst das Ereignis nicht aus — bestätigt (nur Zurücksetzen über `OnRecenterClicked` löst aus, wie im Plan vorgesehen)
- [x] Bindbare Eigenschaft `MapFitMode` — vorhanden
- [x] Öffentliche Eigenschaft `Viewport` — vorhanden
- [x] Privat `RaiseViewportChanged()` — vorhanden
- [x] Einpassen bei `FitToResult` über `OnResultChanged` (Hinweis siehe unten)

**MapViewModel**

- [x] Eigenschaften `IsBottomSheetExpanded`, `IsSearchOverlayOpen`, `ShowAreaReloadButton`, `AreaReloadHint`, `IsAreaReloading`, `AreaReloadRemainingSeconds`, `MapFitMode` — vorhanden
- [x] Befehle `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand`, `ReloadVisibleAreaCommand` — vorhanden
- [x] Konstante `AreaReloadMinInterval` (60 s) — vorhanden
- [x] `ShowMoreVisible` = `HasMore && IsBottomSheetExpanded` — vorhanden
- [x] `ChangeMode` verwirft Nachladen und blendet die Schaltfläche aus — vorhanden
- [x] `SearchAsync` schließt das Overlay beim Suchstart (über `RunGuardedSearchAsync`, gilt auch für die Routensuche) — vorhanden
- [x] `SelectView` setzt nur den Startzustand des Panels — vorhanden
- [x] `ViewOptions`, `IsMapView`, `ShowMap`, `ShowMapHint`, `OnViewSelected` im Kartenbereich entfernt (`ViewOptions` in `SettingsViewModel` betrifft nur die Einstellungsseite und ist beabsichtigt)
- [x] `MapViewModel.Route`: `IsRouteModeSearchable` — vorhanden
- [x] Private Methoden `ReloadVisibleAreaAsync`, `IsAreaReloadBlocked` (über `TimeProvider`) — vorhanden

**Views**

- [x] `MapPage.xaml`: `ScrollView` entfernt, `HeightRequest="440"` entfernt, Vollflächen-Grid mit `StationMapView`, Suchschaltfläche oben links, Legende oben rechts, Quellenangabe unten links, Nachlade-Schaltfläche, `StationBottomSheet`, `MapSearchOverlay`
- [x] `MapPage.xaml.cs`: `OnMapViewportChanged` leitet an `OnViewportChanged` weiter; Rückwärtstaste schließt zuerst das Overlay
- [x] Texte `MapTexts` (`SearchButton`, `ReloadArea`, `ReloadAreaWait`, `ReloadAreaTooLarge`, `PanelExpand`, `PanelCollapse`, `OverlayTitle`, `OverlayClose`, `LegendPlacement`) — vorhanden
- [x] Texte `SearchTexts` (`FormatStationCount`, `NoResults`) — vorhanden
- [x] AutomationIds `Search.Panel`, `Search.Panel.Toggle`, `Search.ShowMore`, `Search.Overlay`, `Search.Overlay.Close`, `Map.ReloadArea`, `Map.*` unverändert — vorhanden

**Tests**

- [x] Unit `MapViewportTests_Math.ToLatLon_RoundTripsToScreen`
- [x] Unit `MapViewportTests_Bounds` (`GetBounds_ContainsCenterAndCorners`, `GetBounds_ClampsAtPoles`)
- [x] Unit `MapAreaQueryTests_Radius` (`From_UsesCenterAndHalfDiagonal`, `From_ReturnsNull_WhenRadiusExceeds25Km`, `From_UsesMinimumOneKilometer`, `From_PassesFuelTypes`)
- [x] Unit `MapViewModelTests_AreaReload` (7 Tests laut Plan)
- [x] Unit `MapViewModelTests_Panel` (`ToggleBottomSheet_ChangesExpandedState`, `DefaultView_List_OpensPanelExpanded`, `DefaultView_Map_OpensPanelCollapsed`, `ShowMore_OnlyVisible_WhenPanelExpanded`) plus Ersatz für `SessionView`/`SwitchView`
- [x] Unit `MapViewModelTests_SearchOverlay` (`OpenSearchOverlay_SetsFlag_AndCloseKeepsInputs`, `Search_ClosesOverlay_OnStart`)
- [x] Hilfsmethode `AppearAndViewportAsync` in `MapViewModelTestBase`
- [x] Ehemalige Tests `MapViewModelTests_MapView` (`Default_IsListView`, `DefaultViewMap_FromSettings_IsPreselected`, `SessionView_...`, `SwitchView_...`) entfernt; Abdeckung durch `MapViewModelTests_Panel` vorhanden
- [x] Integration `AreaSearchMockServerTests_Flow` (`ReloadArea_SendsCenterAndRadiusOfVisibleArea`, `ReloadArea_ReplacesResult_KeepsOriginAndViewport`, `ReloadArea_Offline_SendsNoRequestAndShowsMessage`)
- [x] E2E `MapE2ETests_Layout`, `MapE2ETests_Panel`, `MapE2ETests_SearchOverlay` (Adresse, Route, Schließen), `MapE2ETests_AreaReload` (vier Szenarien) — alle im E2E-Lauf nach Umsetzung bestanden
- [x] E2E `MapE2ETests_Navigation` (Panel-Schaltfläche), `MapE2ETests_View` (Standardansicht, Quellenangabe), `MapE2ETests_Markers` (über Overlay), `RouteE2ETests_*` und `SearchE2ETests_*` (Overlay-Öffnung über `OpenSearchOverlay`)
- [x] E2E-Basisklassen: `ShowMapView`/`ShowListView` ersetzt durch `OpenSearchOverlay`, `ExpandPanel`, `CollapsePanel`

**Unverändert wie geplant:** `MapMarkerSelection`, `StationResultBuilder`, `IFuelPriceService`, `MapRouteDrawable`, `SettingsTexts`; keine Datenbank-, Konfigurations- oder Enum-Änderungen. Keine Datei aus der Liste „keine Änderung“ ist im Git-Status verändert.

## Offene Aufgaben

- [ ] `ApplyResult` / `OnViewportChanged` (Tasks 14 und 19) — teilweise umgesetzt: Der Plan verlangt, dass `ApplyResult` bei neuen Suchen `_searchedViewport` mit dem Suchausschnitt belegt, damit die Schaltfläche „Tankstellen in diesem Bereich laden“ nur bei Abweichung von der Suchansicht erscheint. Der Code setzt `_searchedViewport` bei neuen Suchen nur auf `null` (`MapViewModel.cs`, Methode `ApplyResult` und `ChangeMode`). Der Ausschnitt nach dem Einpassen wird nur in `StationMapView.FitToContent` berechnet und erreicht das ViewModel nicht. Folge: Nach jeder neuen Suche erscheint die Schaltfläche bei der ersten Bewegung und auch nach „Zurücksetzen“, obwohl der Ausschnitt der Suchansicht entspricht. Der Test `OnViewportChanged_ShowsReloadButton_WhenMovedFromLastSearch` setzt keinen Suchausschnitt voraus und prüft diesen Fall nicht; sein Name beschreibt das Verhalten daher ungenau.

## Hinweise

- **Einpassen über `OnResultChanged` statt `RefreshMarkers`:** Der Plan legt die Bedingung `FitToResult` in `RefreshMarkers()` ab. Umgesetzt ist sie in `OnResultChanged()`, das über `ResultVersion` aufgerufen wird. `RefreshMarkers()` passt den Ausschnitt nie ein. Das Verhalten entspricht dem Plan (Nachladen und Filter behalten den Ausschnitt), die Stelle weicht nur ab. Für die Abnahme genügt eine Klarstellung im Plan.
- **Sperre des Nachladens:** Der Plan verlangt „Befehl inaktiv“ während der 60-s-Sperre. Der Code lässt den Befehl ausführbar, startet aber keine Abfrage, sondern zeigt den Hinweis mit der Restzeit. Der Test `ReloadVisibleArea_IsBlocked_For60Seconds` bestätigt genau dieses Verhalten. Die Anforderung „keine Abfrage während der Sperre“ ist erfüllt.
- **Höhenanimation des Panels:** Der Plan sieht vor, dass die Höhe beim Auf- und Zuklappen animiert wird (`TranslateTo`). Der Code animiert nur das Zurückschnappen nach einer Geste (`TranslateToAsync`). Den Zustandswechsel vollzieht er durch Sichtbarkeit der Liste ohne Animation. Das ist eine gestalterische Abweichung.
- **Testnachweise:** Die Unit- und Integrationsläufe unter `inventory/test-results/` stammen aus dem Ausgangslauf (vor der Umsetzung, 18:54 und 19:10). Nach der Umsetzung liegt nur der E2E-Lauf vor (173 von 173 bestanden, 0 Fehlschläge). Die in der Tasks-Datei genannten Unit- und Integrationstests existieren im Code, ein aktueller Lauf fehlt. Empfehlung: nach der Nachbesserung Unit und Integration erneut ausführen (`run-tests`).
- **Lücke in der Aufgabenliste:** Die Tasks-Datei führte bisher „Unit/Integration/E2E grün“ als Testnachweis für mehrere Zeilen. Diese Angabe wurde durch konkrete Testnamen ersetzt.
- **Dokumentation:** ADR 0005 und `docs/help` (Tasks 46 und 47) sind wie vereinbart im Schritt `update-docs` zu erledigen.
