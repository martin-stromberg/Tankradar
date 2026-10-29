# Tasks: Kartenansicht umstrukturieren (Issue #19)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Ausgangszustand | E2E-Ausgangslauf vollständig herstellen (Ursache des Testhost-Absturzes klären) | Offen | — |
| 2 | Modelle | `MapBounds` als `readonly record struct` in `Models/Map/` anlegen | Offen | — |
| 3 | Modelle | `MapFitMode` Enum (`FitToResult`, `KeepViewport`) in `Models/Map/` anlegen | Offen | — |
| 4 | Kartenmathematik | `MapViewport.ToLatLon(x, y)` als Umkehrprojektion ergänzen | Offen | — |
| 5 | Kartenmathematik | `MapViewport.GetBounds()` ergänzen | Offen | — |
| 6 | Logik | `MapAreaQuery.From(MapBounds, fuelTypes)` anlegen (Haversine, Aufrunden, Grenze 25 km, `null` darüber) | Offen | — |
| 7 | Logik | `MapViewModel.AreaReloadMinInterval` (60 s) als Konstante festlegen | Offen | — |
| 8 | Steuerelemente | `ViewportChangedEventArgs` anlegen | Offen | — |
| 9 | Steuerelemente | `StationMapView`: Ereignis `ViewportChanged` bei Nutzeraktionen auslösen, nicht bei `FitToContent` | Offen | — |
| 10 | Steuerelemente | `StationMapView`: bindbare Eigenschaft `MapFitMode`, `RefreshMarkers` führt `RequestFit` nur bei `FitToResult` aus | Offen | — |
| 11 | ViewModel | Eigenschaften `IsBottomSheetExpanded`, `IsSearchOverlayOpen`, `ShowAreaReloadButton`, `AreaReloadHint`, `IsAreaReloading`, `MapFitMode` ergänzen | Offen | — |
| 12 | ViewModel | Befehle `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand` ergänzen | Offen | — |
| 13 | ViewModel | Startzustand des Panels aus `ResultView` in `OnAppearing`/`SelectView` setzen; `ViewOptions`, `IsMapView`, `ShowMap`, `ShowMapHint` entfernen bzw. nicht mehr binden | Offen | — |
| 14 | ViewModel | `OnViewportChanged` (Abweichung zur letzten Suche, Modus, Abfragegrenze) implementieren | Offen | — |
| 15 | ViewModel | `ReloadVisibleAreaCommand` mit einmaliger Abfrage, Ergebnisersetzung und `KeepViewport` implementieren | Offen | — |
| 16 | ViewModel | 60-s-Sperre über `TimeProvider` implementieren | Offen | — |
| 17 | ViewModel | `ChangeMode` verwirft laufendes Nachladen und blendet Schaltfläche aus | Offen | — |
| 18 | ViewModel | `ShowMoreVisible` auf `HasMore && IsBottomSheetExpanded` umstellen | Offen | — |
| 19 | ViewModel | `ApplyResult` setzt `MapFitMode = FitToResult` bei neuen Suchen; `SearchAsync` schließt Overlay bei Suchstart | Offen | — |
| 20 | Texte | Einträge in `MapTexts` anlegen (Suche, Nachladen, Hinweise, Panel, Overlay) | Offen | — |
| 21 | Texte | Einträge in `SearchTexts` anlegen (Anzahl im Panel, Keine Ergebnisse) | Offen | — |
| 22 | Steuerelemente | `StationBottomSheet` (XAML + Code-Behind) mit Kopfzeile, Auf-/Zuklapp-Schaltfläche, Ziehgeste, Liste und AutomationIds | Offen | — |
| 23 | Steuerelemente | `MapSearchOverlay` (XAML + Code-Behind) mit Suchart-Chips, Eingaben, Radius, Suchen- und Schließen-Schaltfläche | Offen | — |
| 24 | View | `MapPage.xaml` auf Vollflächen-Grid umstellen (ScrollView entfernen, Overlays, Legende, Quellenangabe) | Offen | — |
| 25 | View | `MapPage.xaml.cs`: Verdrahtung `ViewportChanged` → `OnViewportChanged` | Offen | — |
| 26 | Tests Unit | `MapViewportTests_Math`: Roundtrip `ToLatLon`/`ToScreen` | Offen | — |
| 27 | Tests Unit | Neue Testklasse `MapViewportTests_Bounds` (Begrenzung, Pole) | Offen | — |
| 28 | Tests Unit | Neue Testklasse `MapAreaQueryTests_Radius` (Radius, Grenze, Mindestradius, Kraftstoffarten) | Offen | — |
| 29 | Tests Unit | Neue Testklasse `MapViewModelTests_AreaReload` (Schaltfläche, keine Automatik, Routenmodus, Abfragegrenze, Ersetzung, Sperre, Moduswechsel) | Offen | — |
| 30 | Tests Unit | Neue Testklasse `MapViewModelTests_Panel` (Umschalten, Startzustand, Weitere anzeigen) | Offen | — |
| 31 | Tests Unit | Neue Testklasse `MapViewModelTests_SearchOverlay` (Öffnen, Schließen, Eingaben bleiben, Schließen bei Suchstart) | Offen | — |
| 32 | Tests Unit | Bestehende Tests anpassen: `MapViewModelTests_MapView` (Default_IsListView, DefaultViewMap, SessionView, SwitchView, EmptyResult) | Offen | — |
| 33 | Tests Unit | Bestehende Tests anpassen: `MapViewModelTests_Settings`, `MapViewModelTests_Sort`, `MapViewModelTests_FilterSort` (ShowMoreVisible, Standardansicht) | Offen | — |
| 34 | Tests Unit | Bestehende Such- und Routentests auf `ShowMoreVisible` und `IsListView` prüfen und anpassen | Offen | — |
| 35 | Tests Testbasis | `MapViewModelTestBase` um Hilfsmethode für Ausschnitt und Modus erweitern | Offen | — |
| 36 | Tests Integration | Neue Testklasse `AreaSearchMockServerTests_Flow` (Mittelpunkt, Radius, Fehlerfall, Ergebnisersetzung) | Offen | — |
| 37 | E2E-Tests | Neue Testklasse `MapE2ETests_Layout` (Vollfläche, Panel eingeklappt, Quellenangabe) | Offen | — |
| 38 | E2E-Tests | Neue Testklasse `MapE2ETests_Panel` (Ausklappen, Liste, Eintrag öffnet Detail) | Offen | — |
| 39 | E2E-Tests | Neue Testklasse `MapE2ETests_SearchOverlay` (Adresse, Route bei aktivierter Routensuche) | Offen | — |
| 40 | E2E-Tests | Neue Testklasse `MapE2ETests_AreaReload` (Schaltfläche erscheint, Nachladen, Ausschnitt bleibt, keine Automatik, Hinweis über 25 km) | Offen | — |
| 41 | E2E-Tests | `MapE2ETests_Navigation`: Bedienung ohne Geste über Panel- und Zoomschaltflächen prüfen | Offen | — |
| 42 | E2E-Tests | `MapE2ETests_View`: Umschaltung auf Panel-Ausklappen umstellen, Standardansicht und Quellenangabe prüfen | Offen | — |
| 43 | E2E-Tests | `MapE2ETests_Markers`: Adresssuche und Standortsuche über Overlay, Filter im Panel-Kopf, Tap auf Marker | Offen | — |
| 44 | E2E-Tests | `MapE2ETestBase`: `ShowMapView`/`ShowListView` ersetzen durch `OpenSearchOverlay`, `ExpandPanel`, `CollapsePanel` | Offen | — |
| 45 | E2E-Tests | `RouteE2ETests_*` und `SearchE2ETests_*`: Suche über Overlay öffnen, Locator anpassen | Offen | — |
| 46 | Dokumentation | ADR 0005 um Bedienstruktur ergänzen (Vollfläche, Overlay, Panel, Nachladen) | Offen | — |
| 47 | Dokumentation | Hilfe „Karte“ (`docs/help/Navigation/beschreibung.md`) und `docs/help/Suche/` anpassen | Offen | — |
