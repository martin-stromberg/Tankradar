# Tasks: Schritt 17 - Tankstellen entlang einer Route

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `RouteDistance` (Stufen 1/2/5/10, Standard 2) anlegen | Erledigt | Unit/Integration grün |
| 2 | Datenmodell | Enum `RoutingFailure` anlegen | Erledigt | Unit/Integration grün |
| 3 | Datenmodell | Enums `RouteStartKind` und `RouteSearchStatus` anlegen | Erledigt | Unit/Integration grün |
| 4 | Datenmodell | `RoutingResult`, `RouteLocation`, `RouteQueryPoint`, `RouteStation` anlegen | Erledigt | Unit/Integration grün |
| 5 | Datenmodell | `RouteSearchRequest`, `RouteSearchResult`, `RouteSearchProgress` anlegen | Erledigt | Unit/Integration grün |
| 6 | Datenmodell | `RouteProfile` (Punkte, Länge, `Locate`, `PointAt`, `Bounds`, `Simplify`) anlegen | Erledigt | Unit/Integration grün |
| 7 | Datenmodell | `MapRoute` anlegen | Erledigt | Unit/Integration grün |
| 8 | Datenmodell | `SearchMode.Route` ergänzen | Erledigt | Unit/Integration grün |
| 9 | Datenmodell | `StationListItem` um `RouteOffsetKm`, `RouteOffsetText`, `HasRouteOffset` erweitern | Erledigt | Unit/Integration grün |
| 10 | Konfiguration | `RoutingOptions` mit `Validate` und `FromEnvironment` anlegen | Erledigt | Unit/Integration grün |
| 11 | Konfiguration | `RouteSearchOptions` mit `Validate` anlegen | Erledigt | Unit/Integration grün |
| 12 | Logik | `PolylineDecoder` implementieren | Erledigt | Unit/Integration grün |
| 13 | Logik | `RouteGeometry` (Haversine, Projektion, Punkt-Strecke, Douglas-Peucker) implementieren | Erledigt | Unit/Integration grün |
| 14 | Logik | `RouteSampler` (Abfragepunkte, Radius, `TooLong`) implementieren | Erledigt | Unit/Integration grün |
| 15 | Logik | `IRoutingService` und `OpenRouteServiceClient` implementieren | Erledigt | Unit/Integration grün |
| 16 | Logik | `IRouteStationSearchService`/`RouteStationSearchService` implementieren | Erledigt | Unit/Integration grün |
| 17 | Logik | `IRouteSearchService`/`RouteSearchService` implementieren | Erledigt | Unit/Integration grün |
| 18 | Logik | `StationResultBuilder.BuildForRoute` ergänzen | Erledigt | Unit/Integration grün |
| 19 | Logik | `MapRouteProjector` implementieren | Erledigt | Unit/Integration grün |
| 20 | Logik | `MauiProgram.AddRoutingServices` und Registrierungen ergänzen | Erledigt | Unit/Integration grün |
| 21 | Schlüssel/Build | `MSBuild/RoutingApiKey.targets` anlegen und im csproj importieren | Erledigt | Unit/Integration grün |
| 22 | Schlüssel/Build | `ApiKeyProvider`, Stores und `ApiKeyStoreSelector` um Schlüsselname parametrisieren | Erledigt | Unit/Integration grün |
| 23 | Schlüssel/Build | `scripts/package-windows.ps1` und Aktion `build-and-package` für Routing-Variablen anpassen | Erledigt | Unit/Integration grün |
| 24 | UI | `MapViewModel.Route.cs` mit Routenzustand und `RunRouteSearchAsync` anlegen | Erledigt | Unit/Integration grün |
| 25 | UI | `MapViewModel` (Konstruktor, `SearchAsync`, `ApplyResult`, Moduslogik, Attribution) anpassen | Erledigt | Unit/Integration grün |
| 26 | UI | `SearchTexts` um Routentexte, Meldungen und Quellenangabe erweitern | Erledigt | Unit/Integration grün |
| 27 | UI | `StationMapView.Route` und Zeichnen der Linie, Ausschnittsanpassung | Erledigt | Unit/Integration grün |
| 28 | UI | `MapPage.xaml` Bereich Route mit AutomationIds | Erledigt | Unit/Integration grün |
| 29 | Fehlerbehandlung | Meldungen für alle `RoutingFailure`/`RouteSearchStatus` und Abbruch-/Offlineverhalten | Erledigt | Unit/Integration grün |
| 30 | Härtung 16 | `TransientWait`: `ElementNotAvailableException` und `timeoutTime` | Erledigt | Unit/Integration grün |
| 31 | Härtung 16 | `SearchE2ETests_LargeResult` schärfen (Reihenfolge, 50 Einträge, Messzeit) | Erledigt | Unit/Integration grün |
| 32 | Härtung 16 | `TankbookEntriesE2ETests_Receipt` gegen `ElementNotAvailableException` härten | Erledigt | Unit/Integration grün |
| 33 | Härtung 16 | Schaltfläche "Weitere anzeigen (N weitere)" für Sprachausgabe entdoppeln, Tests anpassen | Erledigt | Unit/Integration grün |
| 34 | Testinfrastruktur | `MockOpenRouteServiceServer` und `PolylineEncoder` in `src/TestSupport` anlegen | Erledigt | Unit/Integration grün |
| 35 | Testinfrastruktur | `TestDataPaths` um Routing-Variablen erweitern; `MockTankerkoenigServer.AddStationAt` | Erledigt | Unit/Integration grün |
| 36 | Testinfrastruktur | `MapViewModelTestBase` und `SearchE2ETestBase` anpassen, `RouteE2ETestBase` anlegen | Erledigt | Unit/Integration grün |
| 37 | Unit-Tests | `PolylineDecoderTests_*`, `PolylineEncoderTests` | Erledigt | Unit/Integration grün |
| 38 | Unit-Tests | `RouteGeometryTests_Distance`, `RouteProfileTests_Locate` | Erledigt | Unit/Integration grün |
| 39 | Unit-Tests | `RouteSamplerTests_Points`, `RouteSamplerTests_Coverage` | Erledigt | Unit/Integration grün |
| 40 | Unit-Tests | `OpenRouteServiceClientTests_*` | Erledigt | Unit/Integration grün |
| 41 | Unit-Tests | `RoutingOptionsTests_Validation`, `RouteSearchOptionsTests_Validation`, `RouteDistanceTests_Validation` | Erledigt | Unit/Integration grün |
| 42 | Unit-Tests | `RouteStationSearchServiceTests_*`, `RouteSearchServiceTests_*` | Erledigt | Unit/Integration grün |
| 43 | Unit-Tests | `ApiKeyProviderTests_Routing` und Regression Tankerkönig | Erledigt | Unit/Integration grün |
| 44 | Unit-Tests | `StationResultBuilderTests_Route`, `SearchTextsTests_Route` (Enum-/Übersetzungsabdeckung) | Erledigt | Unit/Integration grün |
| 45 | Unit-Tests | `MapViewModelTests_Route*` inkl. Privacy | Erledigt | Unit/Integration grün |
| 46 | Unit-Tests | `MapRouteProjectorTests_Projection`, `MapViewportTests_RouteBounds`, `TransientWaitTests_ElementNotAvailable`, `MockOpenRouteServiceServerTests` | Erledigt | Unit/Integration grün |
| 47 | Integrationstests | `ApiKeyBuildTests_Routing` | Erledigt | Unit/Integration grün |
| 48 | Integrationstests | `RouteSearchMockServerTests_Flow` und `_Errors` | Erledigt | Unit/Integration grün |
| 49 | Integrationstests | `RoutePrivacyTests_Persistence` und `_Logging` | Erledigt | Unit/Integration grün |
| 50 | E2E-Tests | `RouteE2ETests_Basic` | Erledigt | E2E gezielt grün |
| 51 | E2E-Tests | `RouteE2ETests_Distance` | Erledigt | E2E gezielt grün |
| 52 | E2E-Tests | `RouteE2ETests_CurrentLocation` und `_GpsNever` | Erledigt | E2E gezielt grün |
| 53 | E2E-Tests | `RouteE2ETests_FilterSort` und `_OpenDetail` | Erledigt | E2E gezielt grün |
| 54 | E2E-Tests | `RouteE2ETests_Errors` und `_KeyMissing` | Erledigt | E2E gezielt grün |
| 55 | E2E-Tests | `RouteE2ETests_NotPersisted` und `_Accessibility` | Erledigt | E2E gezielt grün |
| 56 | Dokumentation | ADR `docs/adr/0014-route-search.md` (inkl. Tankerkönig-Bedingungen, Aufhebung des Ausschlusses) | Erledigt | Unit/Integration grün |
| 57 | Dokumentation | `docs/help/Suche/*` aktualisieren | Erledigt | Unit/Integration grün |
| 58 | Dokumentation | `docs/help/Preisdaten/api-schluessel.md`, `docs/help/ci-cd/einrichtung.md` aktualisieren | Erledigt | Unit/Integration grün |
| 59 | Dokumentation | `docs/help/ci-cd/workflows.md`, ADR 0013, `docs/help/Sicherung/ablauf-technisch.md` (Härtungen 16) | Erledigt | Unit/Integration grün |
| 60 | Dokumentation | `README.md` und `changes.log` aktualisieren; Test-Fokus-Hinweis | Erledigt | Unit/Integration grün |
| 61 | Abschluss | Hook-Prüfungen (Format, XML-Doku, Übersetzung, Enum), Unit/Integration, gezielte E2E, ein Gesamtlauf | Erledigt | Unit 1764, Integration 295 grün; E2E gezielt (Gesamtlauf später per local-ci) |
| 62 | Abschluss | Windows-Zwischenstand unter `review-versions/` (nicht committen) | Offen | — |
| 63 | Tests | Test der Auflösbarkeit der Routing-Dienste aus der DI-Registrierung (`AddRoutingServices`) | Erledigt | Unit grün |
