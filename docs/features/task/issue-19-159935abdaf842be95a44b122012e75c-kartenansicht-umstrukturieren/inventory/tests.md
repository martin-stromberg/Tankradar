# Tests (Kartenansicht) und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-29 18:52 bis 19:20, UTC+01:00 (Ausgangszustand um 18:52:46, siehe [00-ausgangszustand.txt](test-results/00-ausgangszustand.txt))
- Branch und Commit-ID: `task/issue-19-159935abdaf842be95a44b122012e75c-kartenansicht-umstrukturieren`, Commit `bfca445d0ebeb1fa20b5757536d93c5e0160e368`
- Uncommittete Änderungen im getesteten Stand: keine am Produktivcode und an Tests. Einziger untracked Pfad ist `docs/features/` (Arbeitsverzeichnis dieser Anforderung, durch den Ablauf angelegt). Die Testläufe haben keine Quelldatei verändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 Pro 10.0.26300 (Desktop-Sitzung `DESKTOP-CM8OBSG`), .NET SDK 10.0.403, Node.js v24.15.0, Python 3.13.14, Zielframework `net10.0-windows10.0.19041.0`. Build und Tests mit `Configuration=Release`, Include*Target-Variablen wie in der Windows-Job-Simulation der CI (`IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true`). E2E mit `TANKRADAR_E2E_WINDOW=offscreen` (CI-Standard).
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, Schritte Unit, Integration, E2E) und `scripts/local-ci.ps1` (gleiche Befehle, dort Tests mit `--settings coverlet.runsettings`). Zusätzlich das Node-Testskript `npm test` (`package.json`, Pipeline-Skripte, Job `static-checks`). Nicht ausgeführt: Formatprüfung (`dotnet format`), Sicherheitsprüfung (`scripts/check-vulnerabilities.mjs`, Netzwerkzugriff), `scripts/validate-workflows.py`, `scripts/test-ios-deployment.ps1` und die iOS-Compile-Prüfung. Gründe siehe unten.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build | `dotnet restore Tankradar.sln -p:Configuration=Release`, danach `dotnet build Tankradar.sln --configuration Release --no-restore` | Repo-Wurzel | 0 / 0 | – | 0 Fehler, 0 Warnungen (Build ohne `TreatWarningsAsErrors`) | – | [01-restore-build.log](test-results/01-restore-build.log) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-unit.trx"` (ohne Filter) | Repo-Wurzel | 0 | 1971 | 0 | 0 | [02-unit.log](test-results/02-unit.log), [TRX](test-results/test-results-unit.trx), [Coverage](test-results/coverage-unit.cobertura.xml) (Zeilenabdeckung 89,61 %) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-integration.trx"` (ohne Filter) | Repo-Wurzel | 0 | 294 | 0 | 0 | [03-integration.log](test-results/03-integration.log), [TRX](test-results/test-results-integration.trx), [Coverage](test-results/coverage-integration.cobertura.xml) (Zeilenabdeckung 56,96 %) |
| Node-Pipeline-Skripte | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Wurzel | 0 | 32 | 0 | 0 | [04-npm-test.log](test-results/04-npm-test.log) |
| E2E, Lauf 1 | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --results-directory TestResults-inventory --logger "trx;LogFileName=test-results-e2e.trx"`, Zeitlimit 580 s | Repo-Wurzel | 124 (Zeitlimit, Testhost abgestürzt) | unbekannt (kein Ergebnisreport) | unbekannt | unbekannt | [05-e2e.log](test-results/05-e2e.log) |
| E2E, Lauf 2 (Wiederholung) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --results-directory TestResults-inventory-e2e-retry --logger "trx;LogFileName=test-results-e2e-retry.trx" --blame-hang-timeout 300s`, Zeitlimit 900 s | Repo-Wurzel | 124 (Zeitlimit, Testhost abgestürzt) | 97 (abgebrochen, unvollständig) | 0 | 0 | [06-e2e-wiederholung.log](test-results/06-e2e-wiederholung.log), [TRX (partiell)](test-results/test-results-e2e-retry.trx) |

Hinweis: Lauf 2 enthielt das Argument `--blame-hang-timeout 300s`, das in der CI nicht verwendet wird; es sollte nur Hänger-Diagnose liefern. Das Ergebnis weicht dadurch vom CI-Befehl ab (Abweichung offengelegt).

Die Coverage-Zusammenfassung über alle Suiten (Mindestabdeckung 70 % laut CI) wurde nicht erzeugt; die Zahlen oben beziehen sich auf je eine Suite.

### Nachgewiesene bestehende Testfehler

Keine. In allen abgeschlossenen Läufen gab es keine fehlgeschlagenen Tests. Die E2E-Läufe sind unvollständig (siehe unten); ein Fehlschlag in den nicht gelaufenen E2E-Tests ist daher nicht ausgeschlossen, aber nicht nachgewiesen.

Entsprechend der Nachweisregel gilt kein Fehler als vorhanden (preexisting).

### Testlücken und Ausführungsprobleme

1. **E2E (FlaUI), Testhost-Absturz in beiden Läufen.** Beide Läufe endeten mit Exit-Code 124 (Zeitlimit von `timeout`) und der Meldung „Der aktive Testlauf wurde abgebrochen. Grund: Der Testhostprozess ist abgestürzt.“ Ursache nicht ermittelt. Lauf 1 hat keinen Ergebnisreport erzeugt. Lauf 2 hat einen TRX-Report mit 97 bestandenen Tests bis zum Abbruch hinterlassen.
   - Ausgeführt und bestanden (97 Testmethoden laut TRX): unter anderem `MapE2ETests_View.SwitchView_BetweenListAndMap_ShowsTheSameResults`, `MapE2ETests_View.DefaultViewFromSettings_Map_OpensSearchAsMap`, `MapE2ETests_Markers.Filter_RebuildsMarkersForFilteredFuel`, `MapE2ETests_Markers.LocationSearch_MarksOwnLocation`, `MapE2ETests_Markers.TapMarker_OpensDetailsAndBackReturnsToMap`, `MapE2ETests_Navigation.Pan_MovesMarkersOutOfAndBackIntoView`, `MapE2ETests_Navigation.Zoom_ChangesLevelWithinLimits`.
   - Nicht ausgeführt (Kartenbezug): `MapE2ETests_Markers.AddressSearch_MarksSearchedPosition`, `MapE2ETests_Markers.Markers_ShowPriceAndLevelOfFirstSelectedFuel`, `MapE2ETests_Navigation.ZoomIn_HidesDistantMarkers_AndRecenterShowsAll`, `MapE2ETests_View.Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly`, `RouteE2ETests_FilterSort.MapView_DescribesDrawnRouteAndShowsMarkers`.
   - Nicht ausgeführt insgesamt: 64 von 161 `[Fact]`-Testmethoden sowie der `[Theory]` (Datenzeilen nicht gezählt). Der gesamte E2E-Bereich ist damit nicht als Ausgangszustand belegt.
   - Diagnoseordner `e2e-diagnostics` wurde nicht erzeugt.
2. **Coverage-Schwelle.** Die kombinierte Schwelle von 70 % wurde nicht geprüft (kein `reportgenerator`-Lauf über beide Suiten).
3. **Nicht ausgeführte Prüfungen außerhalb der Tests.** Formatprüfung, Sicherheitsprüfung (Netzwerkzugriff nötig), Workflow-Validierung (`scripts/validate-workflows.py` mit Tests), iOS-Deployment-Prüfung (`scripts/test-ios-deployment.ps1`) und die statische Analyse mit `TreatWarningsAsErrors=true`. Grund: nicht Teil dieser Bestandsaufnahme bzw. nicht ohne Mac oder Netzwerk möglich; die Build-Warnungen liegen im Build-Log vor (0 Warnungen).
4. **Kein Test für das Kartenmodul im Code-Behind.** `MapPage` und `StationMapView` sind über Unit-Tests nicht prüfbar; die Coverage schließt XAML-Code-Behind aus (`coverlet.runsettings`). Ihr Verhalten ist nur über die E2E-Tests abgedeckt, die im Lauf 2 teilweise nicht gelaufen sind.

## Testklassen (Bestand für die Kartenansicht)

### Unit-Tests

Ansicht und ViewModel (`src/Tankradar.Tests.Unit/Unit/`, Basisklasse `MapViewModelTestBase`):

- `MapViewModelTests_MapView` (9): `Default_IsListView`, `DefaultViewMap_FromSettings_IsPreselected`, `SessionView_SurvivesReappearingUntilDefaultChanges`, `SwitchView_DoesNotSearchAgain`, `LocationSearch_MarksOwnLocationAndAllStations`, `AddressSearch_MarksSearchedPlace_WithoutLocationQuery`, `Markers_CoverWholeResultSet_AndFilterRebuildsWithoutRefit`, `Origin_IsDiscardedOnFailureAndModeChange_AndNeverLogged`, `EmptyResult_ShowsMapWithOriginOnly`.
- `MapViewModelTests_FilterSort` (5), `MapViewModelTests_Sort` (4): Filter und Sortierung, Markierungen folgen der gefilterten Menge.
- `MapViewModelTests_Search` (9), `MapViewModelTests_Address` (17), `MapViewModelTests_Chips` (8), `MapViewModelTests_Failures` (6), `MapViewModelTests_Concurrency` (2), `MapViewModelTests_Offline` (8), `MapViewModelTests_Settings` (5), `MapViewModelTests_Privacy` (3), `MapViewModelTests_OpenStation` (3): Suche, Adresse, Radius-Chips, Fehler, Nebenläufigkeit, Offline-Zustand, Einstellungen, Datenschutz, Öffnen der Detailansicht.
- `MapViewModelTests_Route` (14), `MapViewModelTests_RouteAvailability` (10), `MapViewModelTests_RouteFailures` (8), `MapViewModelTests_RouteRequest` (8), `MapViewModelTests_RouteValidation` (9), `MapViewModelTests_RoutePrivacy` (4): Routensuche und ihre Sichtbarkeit.

Karte und Kartenmathematik:

- `MapViewportTests_Math` (11): `Create_PlacesCenterInMiddleOfView`, `VisibleTiles_AtBerlin_ContainsKnownTile`, `ToScreen_NorthIsUpAndEastIsRight`, `PanByPixels_MovesPointsByDelta`, `PanByPixels_ClampsAtPolesAndWrapsLongitude`, `ZoomBy_KeepsCenterAndStopsAtLimits`, `Fit_ContainsAllPointsWithPadding`, `Fit_SinglePointAndNoPoints`, `Fit_FarApartPoints_EndAtMinZoom`, `VisibleTiles_CoverViewWrapColumnsAndSkipRowsOutsideWorld`, `Resize_KeepsCenter_ContainsHonorsMargin`.
- `MapViewportTests_RouteBounds` (3): Einpassen auf die Routenbegrenzung.
- `MapMarkerSelectionTests_Limit` (5): Begrenzung auf 100 Markierungen im Ausschnitt, Auswahl nach Nähe zur Mitte.
- `RouteQueryPacerTests` (13): Mindestabstand der Routenabfragen.
- `StationResultBuilderTests_*` (u. a. `_Route`, `_Filter`, `_Sorting`): Aufbereitung der Ergebnisliste.
- `SearchRadiusTests_Validation`: Radiusgrenzen 1 bis 25 km.
- `SearchTextsTests_Address`, `SearchTextsTests_Route`: Suchtexte.

### Integrationstests (Mock-Server)

- `SearchMockServerTests_Flow` (8), `SearchMockServerTests_Offline` (4), `SearchMockServerTests_OwnerName`, `SearchMockServerTests_RealFormat`: Umkreissuche gegen Mock-Server.
- `RouteSearchMockServerTests_Flow` (7), `RouteSearchMockServerTests_Errors`, `RouteSearchMockServerTests_Limit`: Routensuche.
- `AddressSearchMockServerTests_Flow`, `GeocodingMockServerTests_Flow`: Adresssuche.

Kein Integrationstest deckt einen Kartenausschnitt-Abruf ab (dieser existiert nicht).

### E2E-Tests (FlaUI)

Basisklassen: `MapE2ETestBase` (erbt von `SearchE2ETestBase`, `SettingsE2ETestBase`).

- `MapE2ETests_View` (3): Ansichtsumschaltung, Standardansicht aus den Einstellungen, Attribution und Legende.
- `MapE2ETests_Markers` (5): Markierungen mit Preis und Preisniveau, Filter, Standort- und Adressmarkierung, Antippen einer Markierung.
- `MapE2ETests_Navigation` (3): Zoom, Herauszoomen mit Zurücksetzen, Verschieben.

Die Tests greifen über AutomationIds auf die bestehende Oberfläche zu (`Search.Map`, `Map.ZoomIn`, `Map.ZoomOut`, `Map.Recenter`, `Map.Pan.*`, `Map.StationCount`, `Map.Zoom`, `Map.Legend`, `Search.View.*`, `Search.Mode.*`). Eine Änderung des Layouts (Overlay, Panel) betrifft diese Locator.

## Hilfsmethoden

### `MapViewModelTestBase` (`src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`)
- Erzeugt `MapViewModel` mit Fakes für Einstellungen, Standort, Geocoding, Preisdienst, Routensuche, Routenzugang, Verbindung, Zeit, Navigator und Logger.
- `TwoStations()`: zwei Testtankstellen; `Result(...)` und `RouteSuccess(...)`: Testergebnisse; `AppearAsync()` und `SearchAsync()`: Abläufe.

### `MapE2ETestBase` (`src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETestBase.cs`)
- `ShowMapView()`, `ShowListView()`: Umschaltung der Ansicht per UI.
- `WaitForMapCount(string)`, `ReadMapCount()`, `ReadZoom()`: Auslesen von Anzeigewerten.
- `PressMapButton(string)`, `ReadMarkerLevel(string)`: Bedienung und Auslesen der Markierung.
- Konstanten `AlphaMarker` und `BetaMarker` mit Mock-Tankstellen-IDs.

### Fakes (Unit-Tests)
- `FakeRouteSearchService`, `FakeRouteSearchAccess`, `FakeFuelPriceService`, `FakeStationNavigator`, `ManualTimeProvider` (alle in `src/Tankradar.Tests.Unit/Unit/Support/`, über `MapViewModelTestBase` eingebunden).
