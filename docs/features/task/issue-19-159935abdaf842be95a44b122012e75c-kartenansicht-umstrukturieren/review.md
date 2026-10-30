# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Geprüft wurden `plan.md` und `inventory.md` gegen den Code unter `src/`. Eine Planeigenschaft fehlt; alle anderen geprüften Planelemente sind vorhanden. Die Dokumentationsschritte (ADR 0005, Hilfe „Karte“) sind wie vorgegeben nicht gewertet.

Zu den beiden offenen Punkten aus `review.2.md` (60-s-Sperre mit inaktivem Befehl, animierte Panel-Höhe bei Auf-/Zuklappen per Schaltfläche) wurde die Umsetzung im Code bestätigt:
- `MapViewModel.AreaReload.cs`: `CanReloadVisibleArea` berücksichtigt `IsAreaReloadBlocked`; ein Zeitgeber über `TimeProvider` aktualisiert die Restzeit und gibt den Befehl nach Ablauf frei.
- `StationBottomSheet.xaml.cs`: `AnimateExpandedAsync` animiert Auf- und Zuklappen über `TranslateToAsync`; die Sichtbarkeit der Liste steuert der Code-Behind.

## Umgesetzte Planelemente

**Neue Klassen und Typen**
- [x] `MapBounds` (`readonly record struct`, `Models/Map/`) — angelegt
- [x] `MapFitMode` (Enum mit `FitToResult`, `KeepViewport`, `Models/Map/`) — angelegt
- [x] `MapAreaQuery` (statische Klasse, `Services/Map/`) — angelegt; `From` und `IsTooLarge` vorhanden
- [x] `StationBottomSheet` (`Views/Controls/`, XAML + Code-Behind) — angelegt
- [x] `MapSearchOverlay` (`Views/Controls/`, XAML + Code-Behind) — angelegt
- [x] `ViewportChangedEventArgs` (`Views/Controls/`) — angelegt, mit `Viewport`

**Änderungen an bestehenden Klassen**
- [x] `MapViewport` — `ToLatLon(x, y)` und `GetBounds()` vorhanden
- [x] `MapViewModel` — Eigenschaften `IsBottomSheetExpanded`, `IsSearchOverlayOpen`, `ShowAreaReloadButton`, `AreaReloadHint`, `IsAreaReloading`, `MapFitMode`, `AreaReloadRemainingSeconds` vorhanden
- [x] `MapViewModel` — Befehle `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand`, `ReloadVisibleAreaCommand` vorhanden
- [x] `MapViewModel` — Methoden `OnViewportChanged` (öffentlich), `ReloadVisibleAreaAsync` (privat), `IsAreaReloadBlocked` (privat, `TimeProvider`) vorhanden; zusätzlich `OnViewportFitted` (siehe Hinweise)
- [x] `MapViewModel` — `ShowMoreVisible` => `HasMore && IsBottomSheetExpanded`; `ChangeMode` verwirft das Nachladen; `SearchAsync` schließt das Overlay über `RunGuardedSearchAsync` (auch bei Routensuche)
- [x] `MapViewModel` — `ViewOptions`, `IsMapView`, `ShowMap`, `ShowMapHint` entfernt; `ApplyFilterAndSort` setzt `MapFitMode` nicht zurück
- [x] `MapViewModel.Route` — `IsRouteModeSearchable` vorhanden
- [x] `MapViewModel.AreaReloadMinInterval` (Konstante, 60 s) — vorhanden
- [x] `StationMapView` — Ereignis `ViewportChanged` bei Nutzeraktionen (`Zoom`, `Pan`, `PanByPixels`, Wischen, Kneifen, Mausrad, Zurücksetzen) — vorhanden
- [x] `StationMapView` — bindbare Eigenschaft `MapFitMode` — vorhanden
- [x] `StationMapView` — privat `RaiseViewportChanged()` — vorhanden
- [x] `StationMapView` — Einpassen (`FitToContent`) löst `ViewportChanged` nicht aus — eingehalten
- [x] `MapPage.xaml` — `ScrollView` entfernt, Vollflächen-Grid, `HeightRequest="440"` entfernt; Suchschaltfläche oben links (`Search.Overlay.Open`), Legende oben rechts, Quellenangabe unten links, `StationBottomSheet` unten, `MapSearchOverlay` als Überlagerung
- [x] `MapPage.xaml.cs` — Verdrahtung `ViewportChanged` → `OnViewportChanged`
- [x] `MapTexts` — `SearchButton`, `ReloadArea`, `ReloadAreaWait`, `ReloadAreaTooLarge`, `PanelExpand`, `PanelCollapse`, `OverlayTitle`, `OverlayClose`, `LegendPlacement` vorhanden
- [x] `SearchTexts` — `FormatStationCount(int)`, `NoResults` vorhanden
- [x] Bestehende Bindungen `Map.ZoomIn`, `Map.ZoomOut`, `Map.Recenter`, `Map.Pan.*` in `StationMapView.xaml` bleiben erhalten
- [x] Panel: AutomationIds `Search.Panel`, `Search.Panel.Toggle`, `Search.ShowMore`; Overlay: `Search.Overlay`, `Search.Overlay.Close`
- [x] Panel: Auf-/Zuklappen per Schaltfläche und Ziehgeste; Höhe begrenzt auf 60 % (`MaximumHeightRequest`); animiert
- [x] Panel: Zeilen-Tap über `OpenStationCommand`

**Testmethoden (Unit)**
- [x] `ToLatLon_RoundTripsToScreen` (`MapViewportTests_Math`)
- [x] `GetBounds_ContainsCenterAndCorners`, `GetBounds_ClampsAtPoles` (`MapViewportTests_Bounds`, neu)
- [x] `From_UsesCenterAndHalfDiagonal`, `From_ReturnsNull_WhenRadiusExceeds25Km`, `From_UsesMinimumOneKilometer`, `From_PassesFuelTypes` (`MapAreaQueryTests_Radius`, neu)
- [x] `OnViewportChanged_DoesNotQuery`, `OnViewportChanged_HidesButton_InRouteMode`, `OnViewportChanged_DisablesButtonWithHint_WhenTooLarge`, `ReloadVisibleArea_QueriesOnce_ReplacesResult_KeepsViewport`, `ChangeMode_DiscardsReload` (`MapViewModelTests_AreaReload`, neu)
- [x] Sperre 60 s: `ReloadVisibleArea_DisablesCommand_DuringBlock`, `Timer_UpdatesRemainingTime_WhileBlocked`, `Timer_EnablesCommand_AfterBlock` (`MapViewModelTests_AreaReloadBlock`, neu, nicht im Plan genannt)
- [x] `ToggleBottomSheet_ChangesExpandedState`, `DefaultView_List_OpensPanelExpanded`, `DefaultView_Map_OpensPanelCollapsed`, `ShowMore_OnlyVisible_WhenPanelExpanded` (`MapViewModelTests_Panel`, neu)
- [x] `OpenSearchOverlay_SetsFlag_AndCloseKeepsInputs`, `Search_ClosesOverlay_OnStart` (`MapViewModelTests_SearchOverlay`, neu)
- [x] Hilfsmethode `AppearAndViewportAsync` (`MapViewModelTestBase`)
- [x] Bestehende Tests angepasst: `MapViewModelTests_MapView` (Datei vorhanden; Nachweise laut Tasks-Zeile 32 in `MapViewModelTests_Panel`), `MapViewModelTests_Settings`, `MapViewModelTests_Sort`/`_FilterSort`; keine Restverweise mehr auf `IsListView`, `IsMapView`, `ShowMap`, `ShowMapHint` in den Tests

**Testmethoden (Integration)**
- [x] `AreaSearchMockServerTests_Flow`: `ReloadArea_SendsCenterAndRadiusOfVisibleArea`, `ReloadArea_Offline_SendsNoRequestAndShowsMessage`, `ReloadArea_ReplacesResult_KeepsOriginAndViewport`

**Testmethoden (E2E)**
- [x] `MapE2ETests_Layout.Map_FillsView_PanelCollapsedShowsCount`
- [x] `MapE2ETests_Panel.ExpandPanel_ShowsList_AndEntryOpensDetails`
- [x] `MapE2ETests_SearchOverlay.OverlayButton_AddressSearch_ClosesOverlayAndMarksPosition`, `OverlayRouteSearch_ShowsRouteOnMap`
- [x] `MapE2ETests_AreaReload.PanOrZoom_ShowsReloadButtonWithoutQuery`, `ReloadArea_UpdatesCountAndMarkers_KeepsViewport`, `TooLargeArea_ShowsHintAndDisablesButton`
- [x] `MapE2ETests_Navigation.PanelToggleButton_CollapsesAndExpandsWithoutGesture`
- [x] `MapE2ETests_View.SwitchView_BetweenListAndMap_ShowsTheSameResults`, `DefaultViewFromSettings_Map_OpensSearchAsMap`, `Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly`
- [x] `MapE2ETests_Markers.AddressSearch_MarksSearchedPosition`
- [x] `RouteE2ETests_FilterSort.MapView_DescribesDrawnRouteAndShowsMarkers`, `SearchE2ETests_Radius.RadiusChips_AreExclusiveAndDefaultIsFive`
- [x] Hilfsmethoden `OpenSearchOverlay()`, `ExpandPanel()`, `CollapsePanel()` in `SearchE2ETestBase`; `MapE2ETestBase` nutzt sie

## Offene Aufgaben

- [ ] `StationMapView.Viewport` (öffentliche Leseeigenschaft vom Typ `MapViewport`) — fehlt vollständig. Der Plan nennt sie unter „Neue Methoden“ von `StationMapView` als aktuellen Ausschnitt für das ViewModel. Im Code gibt es keine solche Eigenschaft (`public MapViewport` in `Views` nicht vorhanden). Die Ausschnitte werden stattdessen über `ViewportChangedEventArgs` an `MapPage` und von dort an das ViewModel übergeben, daher ist die Funktion nicht beeinträchtigt. Entweder die Eigenschaft ergänzen oder den Plan-Eintrag streichen (Tasks-Zeile 49).

## Hinweise

**Abweichungen vom Plan ohne Lücke (Funktion vorhanden):**
- `OnStationTapped` liegt im Code in `StationBottomSheet` (privat) statt in `MapPage`. Der Panel-Eintrag ruft `OpenStationCommand` auf, das Verhalten entspricht dem Plan.
- Die Prüfung „nur bei `FitToResult` einpassen“ liegt in `StationMapView.OnResultChanged` statt in `RefreshMarkers` (Tasks-Zeile 10). `RefreshMarkers` aktualisiert nur noch die Markierungen; Filter- und Sortierwechsel passen den Ausschnitt also nicht neu ein, wie geplant.
- Der Plan sieht das Setzen von `_searchedViewport` in `ApplyResult` vor. Umgesetzt über das zusätzliche Ereignis `StationMapView.ViewportFitted` und `MapViewModel.OnViewportFitted` (Tasks-Zeile 19), weil das Einpassen erst nach dem Binden der Markierungen erfolgt. Der Plan nennt dieses Ereignis nicht; es ist fachlich gleichwertig.
- Partial-Aufteilung: Die Logik liegt in `MapViewModel.AreaReload.cs` und `MapViewModel.Panels.cs`. Das ist im Plan nicht vorgesehen, verändert aber keine Schnittstelle.
- Der Testname `OnViewportChanged_ShowsReloadButton_WhenMovedFromLastSearch` aus dem Plan existiert nicht. Sein Verhalten ist unter `OnViewportChanged_ShowsButton_WhenMovedFromFittedSearchView` abgedeckt (Tasks-Zeile 11, 14).
- Der Plan-Test `ReloadVisibleArea_IsBlocked_For60Seconds` existiert nicht; die 60-s-Sperre wird von `ReloadVisibleArea_DisablesCommand_DuringBlock` geprüft. Die Tasks-Datei nannte bisher den nicht vorhandenen Namen als Nachweis (Zeilen 7 und 20); der Nachweis ist korrigiert.

**Dokumentation (nicht gewertet):** ADR 0005 und die Hilfeseiten (Tasks-Zeilen 46 und 47) bleiben wie vorgegeben Teil des Dokumentationsschritts.

**Nicht erneut verifiziert:** Die Testergebnisse (Unit 2012/2012, Integration 297/297, E2E 173/173) wurden wie vorgegeben übernommen. Der Abgleich beschränkt sich auf das Vorhandensein der Testmethoden im Code.
