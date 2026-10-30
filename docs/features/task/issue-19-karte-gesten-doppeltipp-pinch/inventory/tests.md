# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): Lauf 2026-10-30, 07:34:16 bis 07:35:38 (+01:00); Build-Beginn 07:34:16, Unit 07:35:03 bis 07:35:12, Integration 07:35:12 bis 07:35:38
- Branch und Commit-ID: `task/issue-19-karte-gesten-doppeltipp-pinch`, `4fee2a30d6d394a888032515bf47147ec2949067`
- Uncommittete Änderungen im getesteten Stand: keine an Quelltext und Tests. Einziger Eintrag in `git status`: `?? docs/features/` (Anforderungsdokumente der Aufgabe, vor diesem Lauf vorhanden). Die Dateien unter `docs/features/task/issue-19-karte-gesten-doppeltipp-pinch/inventory/` entstanden in diesem Lauf.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 Pro 10.0.26300; .NET SDK 10.0.403; Ziel-Framework `net10.0-windows10.0.19041.0`; `Microsoft.Maui.Controls` 10.0.120 (`MauiVersion`); Konfiguration `Release`; Umgebungsvariablen `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false`.
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/pr-staging-ci.yml` (Zeilen 145–154 für Unit/Integration, 174–177 für E2E) und `scripts/local-ci.ps1` (Zeilen 166–170). Weitere Suiten im Projekt: `src/Tankradar.Tests.E2E` (nicht ausgeführt, siehe unten).

Vorlauf: `dotnet restore Tankradar.sln -p:Configuration=Release` (Exit 0), danach `dotnet build Tankradar.sln --configuration Release --no-restore` (Exit 0, 0 Warnungen, 0 Fehler, Laufzeit 14,17 s).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --logger "trx;LogFileName=unit-run1.trx" --results-directory <Scratchpad>/unit-run1 --logger "console;verbosity=normal"` (kein Filter) | Repo-Wurzel | 0 | 2016 | 0 | 0 | [Log](test-results/unit-run1.log), [TRX](test-results/unit-run1.trx) |
| 1 Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --logger "trx;LogFileName=integration-run1.trx" --results-directory <Scratchpad>/integ-run1 --logger "console;verbosity=normal"` (kein Filter) | Repo-Wurzel | 0 | 297 | 0 | 0 | [Log](test-results/integration-run1.log), [TRX](test-results/integration-run1.trx) |
| Build | `dotnet build Tankradar.sln --configuration Release --no-restore` | Repo-Wurzel | 0 | n. a. | n. a. | n. a. | [Log](test-results/build-release.log) |
| Restore | `dotnet restore Tankradar.sln -p:Configuration=Release` | Repo-Wurzel | 0 | n. a. | n. a. | n. a. | [Log](test-results/restore.log) |

Der TRX-Bericht bestätigt die Konsolenangaben: Unit 2016 Ergebnisse, alle `Passed`; Integration 297 Ergebnisse, alle `Passed`.

Die Ausgaben liegen unverändert im Ordner [test-results](test-results/). Die Logs enthalten nur Testnamen und Laufzeiten; geprüft auf Zugangsdaten (Muster `key`, `token`, `password`, `secret`): keine Werte gefunden.

Das Verzeichnis für die Scratch-Ausgaben liegt außerhalb des Repos (Scratchpad des Laufs); die gesicherten Kopien stehen unter `inventory/test-results/`.

### Nachgewiesene bestehende Testfehler

Keine. In Unit und Integration gab es im Ausgangslauf keinen fehlgeschlagenen und keinen übersprungenen Test. Nach der Nachweisregel gibt es damit keinen bereits vorhandenen Fehler, auf den sich spätere Arbeiten berufen dürfen.

### Testlücken und Ausführungsprobleme

- **E2E-Suite (`src/Tankradar.Tests.E2E`, 173 Tests laut CI) nicht ausgeführt.** Begründung: Für die Bestandsaufnahme nicht zwingend, Laufzeit etwa 18 min; die letzte CI-Auswertung (PR #26) war grün. Der Ausgangszustand der E2E-Suite ist damit lokal **unbekannt**, nicht als bestanden zu werten. Relevanter Teilbestand: `MapE2ETests_*` (21 Testmethoden, siehe unten).
- **Keine Tests für Gesten** (Doppeltipp, Kneifen): weder in Unit noch in E2E. Die E2E-Quelltextregel `E2EInputPolicyTests_NoFocus` verbietet `.Click(`, `.DoubleClick(` und `Mouse.` in E2E-Quellen.
- **Keine Unit-Tests** für `StationMapView` selbst (Oberfläche); `OnPinchUpdated` und die Zeigerbehandlung sind nur über E2E und manuell prüfbar.
- Keine nicht ausführbaren oder deaktivierten Testfälle festgestellt (Suche nach `[Skip`, `Ignore`, `Explicit` in Unit und Integration: keine Treffer für Testmethoden).

## Testklassen

### Unit: `MapViewportTests_Math` (`src/Tankradar.Tests.Unit/Unit/MapViewportTests_Math.cs`)
- `Create_PlacesCenterInMiddleOfView` — Mitte liegt in der Bildmitte, Mitte und Stufe stimmen mit der Erstellung überein
- `VisibleTiles_AtBerlin_ContainsKnownTile` — Berlin enthält eine bekannte Kachel
- `ToScreen_NorthIsUpAndEastIsRight` — Achsenrichtung
- `PanByPixels_MovesPointsByDelta` — Verschiebung um Delta
- `PanByPixels_ClampsAtPolesAndWrapsLongitude` — Grenzen an den Polen, Umlauf der Länge
- `ZoomBy_KeepsCenterAndStopsAtLimits` — `ZoomBy` behält die Mitte, Grenzen 4 und 18, unveränderter Ausschnitt an der Grenze (`Assert.Same`). Dies ist der einzige Test, der das bestehende Zoom-Verhalten um die Mitte abdeckt.
- `Fit_ContainsAllPointsWithPadding` — Einpassen mit Rand
- `Fit_SinglePointAndNoPoints` — ein Punkt und leere Liste
- `Fit_FarApartPoints_EndAtMinZoom` — weit entfernte Punkte enden bei der kleinsten Stufe
- `VisibleTiles_CoverViewWrapColumnsAndSkipRowsOutsideWorld` — Kacheln, Umbruch der Spalten, Zeilen außerhalb der Welt
- `Resize_KeepsCenter_ContainsHonorsMargin` — Größenänderung und Randprüfung
- `ToLatLon_RoundTripsToScreen` — Umkehrung von `ToScreen`

### Unit: `MapViewportTests_Bounds` (`MapViewportTests_Bounds.cs`)
- `GetBounds_ContainsCenterAndCorners`
- `MapBounds_ToString_HidesCoordinates`
- `GetBounds_ClampsAtPoles`

### Unit: `MapViewportTests_RouteBounds` (`MapViewportTests_RouteBounds.cs`)
- `Fit_ToRouteBounds_ContainsAllRoutePoints`
- `Fit_LongerRoute_UsesSmallerZoom`
- `Fit_ThreeHundredKilometers_FitsPhoneSizedViewport`

### Unit: `MapPointerTrackerTests_Mouse` (`src/Tankradar.Tests.Unit/Unit/MapPointerTrackerTests_Mouse.cs`)
- `Drag_ReportsIncrementalMovementOnlyWhilePressed` — Ziehen liefert Verschiebung seit letzter Meldung, nur bei gedrückter Taste
- `Drag_Restart_UsesNewStartPosition`
- `Wheel_FullNotches_ZoomByWholeLevels`
- `Wheel_PartialNotches_AccumulateUntilFullNotch`
- `Attribution_LinksToOpenStreetMapCopyrightPage`

### Unit: `MapMarkerSelectionTests_Limit` (`MapMarkerSelectionTests_Limit.cs`)
- `Select_With300MarkersInView_RendersOnlyLimit`
- `Select_OverLimit_PrefersMarkersNearestToCenter`
- `Select_MarkersOutsideViewport_AreIgnored`
- `Select_BelowLimit_RendersAllInView`
- `FormatStationCountLimited_MentionsShownAndZoom`

### Unit: `MapRouteProjectorTests_Projection` (`MapRouteProjectorTests_Projection.cs`)
- `Project_LineInsideViewport_ReturnsSingleLineThroughCenter`
- `Project_LineLeavingViewport_IsClippedToViewportWithMargin`
- `Project_LineLeavingAndReentering_ReturnsTwoLines`
- `Project_LineOutsideViewport_ReturnsNoLines`
- `Project_ZoomedOut_MergesNearbyPointsAndKeepsLastPoint`
- `Project_TooFewPoints_ReturnsNoLines`
- `MapRoute_ExposesStartDestinationAndBounds`

### Unit: `MapViewModelTests_AreaReload` (Nachladen des Ausschnitts, `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReload.cs`)
Nicht im Detail gelesen; Bezug zu `OnViewportChanged` / `OnViewportFitted` im ViewModel. Relevant für die Frage, ob Doppeltipp und Kneifen den Nachlade-Schalter auslösen.

### Integration: `AreaSearchMockServerTests_Flow` (`src/Tankradar.Tests.Integration/Integration/AreaSearchMockServerTests_Flow.cs`)
Nicht im Detail gelesen; verwendet `MapViewport`, Ablauf der Bereichssuche mit Mock-Server.

### E2E (nicht ausgeführt; nur Bestand)
- `MapE2ETests_Navigation`: `Zoom_ChangesLevelWithinLimits`, `ZoomIn_HidesDistantMarkers_AndRecenterShowsAll`, `PanelToggleButton_CollapsesAndExpandsWithoutGesture`, `Pan_MovesMarkersOutOfAndBackIntoView` (Bedienung per Schaltfläche, keine Geste)
- `MapE2ETests_Markers`: `Markers_ShowPriceAndLevelOfFirstSelectedFuel`, `Filter_RebuildsMarkersForFilteredFuel`, `LocationSearch_MarksOwnLocation`, `AddressSearch_MarksSearchedPosition`, `TapMarker_OpensDetailsAndBackReturnsToMap` (Antippen einer Markierung; relevant für Offene Frage 1 der Anforderung)
- `MapE2ETests_AreaReload`: `WithoutMovement_NoReloadButton`, `PanOrZoom_ShowsReloadButtonWithoutQuery`, `ReloadArea_UpdatesCountAndMarkers_KeepsViewport`, `TooLargeArea_ShowsHintAndDisablesButton`
- `MapE2ETests_View`: `SwitchView_BetweenListAndMap_ShowsTheSameResults`, `DefaultViewFromSettings_Map_OpensSearchAsMap`, `Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly`
- `MapE2ETests_Layout`: `Map_FillsView_PanelCollapsedShowsCount`
- `MapE2ETests_Panel`: `ExpandPanel_ShowsList_AndEntryOpensDetails`
- `MapE2ETests_SearchOverlay`: `OverlayButton_AddressSearch_ClosesOverlayAndMarksPosition`, `CloseOverlay_KeepsInputsWithoutSearching`, `OverlayRouteSearch_ShowsRouteOnMap`

## Hilfsmethoden

### `MapViewportTests_Math` / `MapViewModelTestBase`
Testdaten: feste Koordinaten und Ausschnittgrößen (Berlin-Bezug laut Testname `VisibleTiles_AtBerlin_...`). `MapViewModelTestBase` (`src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`) stellt Test-ViewModels bereit; nicht im Detail gelesen.

### `E2EInputPolicyTests_NoFocus` (Quelltextprüfung, `src/Tankradar.Tests.Unit/Unit/E2EInputPolicyTests_NoFocus.cs`)
- Prüft per Regex im Quelltext des E2E-Projekts, dass keine Fokus-, Tastatur-, Maus- oder Klick-Aufrufe vorkommen (Muster u. a. `.Click(`, `.DoubleClick(`, `Mouse.`). Gilt für eine Doppelklick-Prüfung im E2E-Projekt als Einschränkung.
