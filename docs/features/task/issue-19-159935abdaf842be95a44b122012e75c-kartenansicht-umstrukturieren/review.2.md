# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Geprüft wurden `plan.md`, `inventory.md`, die Tasks-Datei, das vorige Review `review.1.md` und der Code unter `src/`. Die Dokumentationsschritte (ADR 0005, Hilfeseiten `docs/help`, Tasks 46 und 47) sind laut Vorgabe nicht Teil der offenen Implementierungsaufgaben und wurden nicht bewertet.

Die offene Aufgabe aus `review.1.md` (Vergleichsausschnitt nach neuer Suche) ist fachlich erfüllt (siehe unten). Zwei Planelemente sind weiterhin nur teilweise umgesetzt.

## Bewertung der Nachbesserung aus review.1.md

Der Suchausschnitt wird nun über `StationMapView.ViewportFitted` gemeldet, nachdem `FitToResult` den Ausschnitt eingepasst hat (`StationMapView.xaml.cs`, `FitToResult()`), und über `MapViewModel.OnViewportFitted` übernommen (`MapViewModel.AreaReload.cs`). `MapPage.xaml` verdrahtet das Ereignis mit `OnMapViewportFitted`. Damit gilt:

- Eine neue Suche setzt `_searchedViewport` auf den tatsächlich eingepassten Ausschnitt (`ApplyResult` setzt weiterhin nur `null` zurück, dann folgt das Ereignis).
- Die Schaltfläche erscheint erst bei Abweichung von diesem Ausschnitt; „Zurücksetzen“ (`OnRecenterClicked` → `FitToContent` → `ViewportChanged`) führt zur Ausblendung, sofern die Markierungen unverändert sind.
- Das Nachladen (`KeepViewport`) setzt `_searchedViewport` auf den Ausschnitt zum Zeitpunkt der Abfrage; das Ereignis `ViewportFitted` wird dabei nicht ausgelöst.
- Das Verhalten ist durch die Unit-Tests `OnViewportFitted_KeepsButtonHidden_AfterNewSearch`, `OnViewportChanged_ShowsButton_WhenMovedFromFittedSearchView`, `OnViewportChanged_HidesButton_WhenBackAtFittedSearchView` und `Search_HidesButton_AfterMovedView` abgedeckt.

Die Übernahme erfolgt über ein Ereignis statt in `ApplyResult` (Plan, Abschnitt „Änderungen an bestehenden Klassen“, `MapViewModel`). Das ist technisch eine Abweichung von der Stelle, nicht vom Ergebnis: Nur die Karte kennt den tatsächlich eingepassten Ausschnitt, und das Einpassen läuft im UI-Takt (`Dispatcher`) nach der Bindung. Fachlich ist die Plananforderung „Schaltfläche nur bei Abweichung von der letzten Suche“ erfüllt.

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

- [x] Ereignis `ViewportChanged` — vorhanden, ausgelöst bei Zoom, Verschieben, Ziehen, Kneifen, Mausrad und Zurücksetzen (`RaiseViewportChanged`)
- [x] `FitToContent` löst `ViewportChanged` nicht aus (Zurücksetzen über `OnRecenterClicked` löst es aus, wie im Plan vorgesehen)
- [x] Ereignis `ViewportFitted` (Nachbesserung, siehe oben) — vorhanden
- [x] Bindbare Eigenschaft `MapFitMode` — vorhanden
- [x] Öffentliche Eigenschaft `Viewport` — vorhanden
- [x] Privat `RaiseViewportChanged()` — vorhanden
- [x] Einpassen nur bei `FitToResult` — vorhanden (in `OnResultChanged()`, siehe Hinweise)

**MapViewModel**

- [x] Eigenschaften `IsBottomSheetExpanded`, `IsSearchOverlayOpen`, `ShowAreaReloadButton`, `AreaReloadHint`, `IsAreaReloading`, `AreaReloadRemainingSeconds`, `MapFitMode` — vorhanden
- [x] Befehle `ToggleBottomSheetCommand`, `OpenSearchOverlayCommand`, `CloseSearchOverlayCommand`, `ReloadVisibleAreaCommand` — vorhanden
- [x] Konstante `AreaReloadMinInterval` (60 s) — vorhanden
- [x] `OnViewportChanged` und `OnViewportFitted` — vorhanden
- [x] `ShowMoreVisible` = `HasMore && IsBottomSheetExpanded` — vorhanden
- [x] `ChangeMode` verwirft Nachladen und blendet die Schaltfläche aus — vorhanden
- [x] `SearchAsync` schließt das Overlay beim Suchstart (über `RunGuardedSearchAsync`, gilt auch für die Routensuche) — vorhanden
- [x] `SelectView` setzt nur den Startzustand des Panels — vorhanden
- [x] `ViewOptions`, `IsMapView`, `ShowMap`, `ShowMapHint`, `OnViewSelected` im Kartenbereich entfernt (`ViewOptions` in `SettingsViewModel` betrifft nur die Einstellungsseite und ist beabsichtigt)
- [x] `MapViewModel.Route`: `IsRouteModeSearchable` — vorhanden
- [x] Private Methoden `ReloadVisibleAreaAsync`, `IsAreaReloadBlocked` (über `TimeProvider`) — vorhanden

**Views**

- [x] `MapPage.xaml`: `ScrollView` entfernt, `HeightRequest="440"` entfernt, Vollflächen-Grid mit `StationMapView`, Suchschaltfläche oben links, Legende oben rechts, Quellenangabe unten links, Nachlade-Schaltfläche, `StationBottomSheet`, `MapSearchOverlay`
- [x] `MapPage.xaml.cs`: `OnMapViewportChanged` und `OnMapViewportFitted` leiten an das ViewModel weiter; Rückwärtstaste schließt zuerst das Overlay
- [x] Texte `MapTexts` (`SearchButton`, `ReloadArea`, `ReloadAreaWait`, `ReloadAreaTooLarge`, `PanelExpand`, `PanelCollapse`, `OverlayTitle`, `OverlayClose`, `LegendPlacement`) — vorhanden
- [x] Texte `SearchTexts` (`FormatStationCount`, `NoResults`) — vorhanden
- [x] AutomationIds `Search.Panel`, `Search.Panel.Toggle`, `Search.ShowMore`, `Search.Overlay`, `Search.Overlay.Close`, `Map.ReloadArea`, `Map.*` unverändert — vorhanden

**Tests**

- [x] Unit `MapViewportTests_Math.ToLatLon_RoundTripsToScreen`
- [x] Unit `MapViewportTests_Bounds` (`GetBounds_ContainsCenterAndCorners`, `GetBounds_ClampsAtPoles`)
- [x] Unit `MapAreaQueryTests_Radius` (`From_UsesCenterAndHalfDiagonal`, `From_ReturnsNull_WhenRadiusExceeds25Km`, `From_UsesMinimumOneKilometer`, `From_PassesFuelTypes`)
- [x] Unit `MapViewModelTests_AreaReload` (Schaltfläche, keine Automatik, Routenmodus, Abfragegrenze, Ersetzung, Sperre, Moduswechsel, Ausschnitt nach neuer Suche)
- [x] Unit `MapViewModelTests_Panel` (Umschalten, Startzustand, Weitere anzeigen, Ersatz für `SessionView`/`SwitchView`)
- [x] Unit `MapViewModelTests_SearchOverlay` (`OpenSearchOverlay_SetsFlag_AndCloseKeepsInputs`, `Search_ClosesOverlay_OnStart`)
- [x] Hilfsmethode `AppearAndViewportAsync` in `MapViewModelTestBase`
- [x] Integration `AreaSearchMockServerTests_Flow` (Mittelpunkt und Radius, Offline-Fall, Ergebnisersetzung)
- [x] E2E `MapE2ETests_Layout`, `MapE2ETests_Panel`, `MapE2ETests_SearchOverlay` (Adresse, Route), `MapE2ETests_AreaReload` (Schaltfläche, Nachladen, Hinweis über 25 km, keine Automatik)
- [x] E2E `MapE2ETests_Navigation` (Panel-Schaltfläche), `MapE2ETests_View`, `MapE2ETests_Markers` (über Overlay), `RouteE2ETests_*` und `SearchE2ETests_*` (Overlay-Öffnung)
- [x] E2E-Basisklassen: `ShowMapView`/`ShowListView` ersetzt durch `OpenSearchOverlay`, `ExpandPanel`, `CollapsePanel`

**Unverändert wie geplant:** `MapMarkerSelection`, `StationResultBuilder`, `IFuelPriceService`, `MapRouteDrawable`, `SettingsTexts`; keine Datenbank-, Konfigurations- oder Enum-Änderungen.

## Offene Aufgaben

- [ ] `MapViewModel.CanReloadVisibleArea` / Schaltfläche „Tankstellen in diesem Bereich laden“ (Plan: Programmablauf „Ausschnitt verändern und Nachladen“, Validierungsregel `ReloadVisibleAreaCommand` „Befehl inaktiv“, Takt-Entscheidung „Schaltfläche 60 s gesperrt“, Tasks 16) — teilweise umgesetzt: Während der 60-s-Sperre bleibt die Schaltfläche aktiv und ausführbar. Die Abfrage wird zwar unterdrückt und der Hinweis mit der Restzeit erscheint, die Schaltfläche ist aber nicht inaktiv. Umsetzung: Sperre in `CanReloadVisibleArea` aufnehmen und nach Ablauf der Sperre `ChangeCanExecute` auslösen (z. B. über einen Zeitgeber oder die bereits vorhandene Restzeit-Aktualisierung).
- [ ] `StationBottomSheet` (Plan: Programmablauf „Panel auf- und zuklappen“, Schritt 2: „Die Höhe wird animiert (`TranslateTo`)“) — teilweise umgesetzt: Auf- und Zuklappen über die Schaltfläche wechselt die Sichtbarkeit der Liste ohne Animation. Animiert wird nur das Zurückschnappen nach einer Ziehgeste (`TranslateToAsync` in `StationBottomSheet.xaml.cs`). Umsetzung: beim Zustandswechsel eine kurze Höhenanimation ausführen oder die Abweichung im Plan als gestalterische Entscheidung festhalten.

## Hinweise

- **Einpassen über `OnResultChanged` statt `RefreshMarkers`:** Der Plan legt die Bedingung `FitToResult` in `RefreshMarkers()` ab. Umgesetzt ist sie in `OnResultChanged()` (aufgerufen über `ResultVersion`). `RefreshMarkers()` passt den Ausschnitt nie ein. Das Verhalten entspricht dem Plan; für die Abnahme genügt eine Klarstellung im Plan.
- **Übernahme des Vergleichsausschnitts über Ereignis statt in `ApplyResult`:** Abweichung von der Stelle, nicht vom Ergebnis (siehe oben). Die Übernahme erfolgt nach dem Einpassen; bis dahin ist die Schaltfläche durch `ResetAreaReload` ausgeblendet.
- **Zurücksetzen nach Nachladen:** „Zurücksetzen“ fittet auf die neuen Markierungen des Nachladens. Weicht dieser Ausschnitt vom Ausschnitt der Abfrage ab, erscheint die Schaltfläche wieder. Das entspricht der Vergleichslogik des Plans, ist aber eine bewusste Folge und sollte in der Abnahme bestätigt werden.
- **Testnachweise:** Unit- und Integrationsläufe unter `inventory/test-results/` stammen aus dem Ausgangslauf (vor der Umsetzung). Nach der Umsetzung liegt nur der E2E-Lauf vor (173 von 173 bestanden). Empfehlung: nach Abschluss der beiden offenen Punkte Unit und Integration erneut ausführen (`run-tests`).
- **Tasks-Datei:** Aktualisiert. Zeile 11 nennt jetzt den tatsächlichen Testnamen `OnViewportChanged_ShowsButton_WhenMovedFromFittedSearchView` (der frühere Name `OnViewportChanged_ShowsReloadButton_WhenMovedFromLastSearch` existiert nicht mehr). Neu als Offen aufgenommen: Zeile 16 (Sperre nur teilweise) und Zeile 48 (Höhenanimation).
- **Dokumentation:** ADR 0005 und `docs/help` (Tasks 46 und 47) sind wie vereinbart im Schritt `update-docs` zu erledigen.
