# Tasks: Schritt 17 - Tankstellen entlang einer Route

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `RouteDistance` (Stufen 1/2/5/10, Standard 2) anlegen | Offen | — |
| 2 | Datenmodell | Enum `RoutingFailure` anlegen | Offen | — |
| 3 | Datenmodell | Enums `RouteStartKind` und `RouteSearchStatus` anlegen | Offen | — |
| 4 | Datenmodell | `RoutingResult`, `RouteLocation`, `RouteQueryPoint`, `RouteStation` anlegen | Offen | — |
| 5 | Datenmodell | `RouteSearchRequest`, `RouteSearchResult`, `RouteSearchProgress` anlegen | Offen | — |
| 6 | Datenmodell | `RouteProfile` (Punkte, Länge, `Locate`, `PointAt`, `Bounds`, `Simplify`) anlegen | Offen | — |
| 7 | Datenmodell | `MapRoute` anlegen | Offen | — |
| 8 | Datenmodell | `SearchMode.Route` ergänzen | Offen | — |
| 9 | Datenmodell | `StationListItem` um `RouteOffsetKm`, `RouteOffsetText`, `HasRouteOffset` erweitern | Offen | — |
| 10 | Konfiguration | `RoutingOptions` mit `Validate` und `FromEnvironment` anlegen | Offen | — |
| 11 | Konfiguration | `RouteSearchOptions` mit `Validate` anlegen | Offen | — |
| 12 | Logik | `PolylineDecoder` implementieren | Offen | — |
| 13 | Logik | `RouteGeometry` (Haversine, Projektion, Punkt-Strecke, Douglas-Peucker) implementieren | Offen | — |
| 14 | Logik | `RouteSampler` (Abfragepunkte, Radius, `TooLong`) implementieren | Offen | — |
| 15 | Logik | `IRoutingService` und `OpenRouteServiceClient` implementieren | Offen | — |
| 16 | Logik | `IRouteStationSearchService`/`RouteStationSearchService` implementieren | Offen | — |
| 17 | Logik | `IRouteSearchService`/`RouteSearchService` implementieren | Offen | — |
| 18 | Logik | `StationResultBuilder.BuildForRoute` ergänzen | Offen | — |
| 19 | Logik | `MapRouteProjector` implementieren | Offen | — |
| 20 | Logik | `MauiProgram.AddRoutingServices` und Registrierungen ergänzen | Offen | — |
| 21 | Schlüssel/Build | `MSBuild/RoutingApiKey.targets` anlegen und im csproj importieren | Offen | — |
| 22 | Schlüssel/Build | `ApiKeyProvider`, Stores und `ApiKeyStoreSelector` um Schlüsselname parametrisieren | Offen | — |
| 23 | Schlüssel/Build | `scripts/package-windows.ps1` und Aktion `build-and-package` für Routing-Variablen anpassen | Offen | — |
| 24 | UI | `MapViewModel.Route.cs` mit Routenzustand und `RunRouteSearchAsync` anlegen | Offen | — |
| 25 | UI | `MapViewModel` (Konstruktor, `SearchAsync`, `ApplyResult`, Moduslogik, Attribution) anpassen | Offen | — |
| 26 | UI | `SearchTexts` um Routentexte, Meldungen und Quellenangabe erweitern | Offen | — |
| 27 | UI | `StationMapView.Route` und Zeichnen der Linie, Ausschnittsanpassung | Offen | — |
| 28 | UI | `MapPage.xaml` Bereich Route mit AutomationIds | Offen | — |
| 29 | Fehlerbehandlung | Meldungen für alle `RoutingFailure`/`RouteSearchStatus` und Abbruch-/Offlineverhalten | Offen | — |
| 30 | Härtung 16 | `TransientWait`: `ElementNotAvailableException` und `timeoutTime` | Offen | — |
| 31 | Härtung 16 | `SearchE2ETests_LargeResult` schärfen (Reihenfolge, 50 Einträge, Messzeit) | Offen | — |
| 32 | Härtung 16 | `TankbookEntriesE2ETests_Receipt` gegen `ElementNotAvailableException` härten | Offen | — |
| 33 | Härtung 16 | Schaltfläche "Weitere anzeigen (N weitere)" für Sprachausgabe entdoppeln, Tests anpassen | Offen | — |
| 34 | Testinfrastruktur | `MockOpenRouteServiceServer` und `PolylineEncoder` in `src/TestSupport` anlegen | Offen | — |
| 35 | Testinfrastruktur | `TestDataPaths` um Routing-Variablen erweitern; `MockTankerkoenigServer.AddStationAt` | Offen | — |
| 36 | Testinfrastruktur | `MapViewModelTestBase` und `SearchE2ETestBase` anpassen, `RouteE2ETestBase` anlegen | Offen | — |
| 37 | Unit-Tests | `PolylineDecoderTests_*`, `PolylineEncoderTests` | Offen | — |
| 38 | Unit-Tests | `RouteGeometryTests_Distance`, `RouteProfileTests_Locate` | Offen | — |
| 39 | Unit-Tests | `RouteSamplerTests_Points`, `RouteSamplerTests_Coverage` | Offen | — |
| 40 | Unit-Tests | `OpenRouteServiceClientTests_*` | Offen | — |
| 41 | Unit-Tests | `RoutingOptionsTests_Validation`, `RouteSearchOptionsTests_Validation`, `RouteDistanceTests_Validation` | Offen | — |
| 42 | Unit-Tests | `RouteStationSearchServiceTests_*`, `RouteSearchServiceTests_*` | Offen | — |
| 43 | Unit-Tests | `ApiKeyProviderTests_Routing` und Regression Tankerkönig | Offen | — |
| 44 | Unit-Tests | `StationResultBuilderTests_Route`, `SearchTextsTests_Route` (Enum-/Übersetzungsabdeckung) | Offen | — |
| 45 | Unit-Tests | `MapViewModelTests_Route*` inkl. Privacy | Offen | — |
| 46 | Unit-Tests | `MapRouteProjectorTests_Projection`, `MapViewportTests_RouteBounds`, `TransientWaitTests_ElementNotAvailable`, `MockOpenRouteServiceServerTests` | Offen | — |
| 47 | Integrationstests | `ApiKeyBuildTests_Routing` | Offen | — |
| 48 | Integrationstests | `RouteSearchMockServerTests_Flow` und `_Errors` | Offen | — |
| 49 | Integrationstests | `RoutePrivacyTests_Persistence` und `_Logging` | Offen | — |
| 50 | E2E-Tests | `RouteE2ETests_Basic` | Offen | — |
| 51 | E2E-Tests | `RouteE2ETests_Distance` | Offen | — |
| 52 | E2E-Tests | `RouteE2ETests_CurrentLocation` und `_GpsNever` | Offen | — |
| 53 | E2E-Tests | `RouteE2ETests_FilterSort` und `_OpenDetail` | Offen | — |
| 54 | E2E-Tests | `RouteE2ETests_Errors` und `_KeyMissing` | Offen | — |
| 55 | E2E-Tests | `RouteE2ETests_NotPersisted` und `_Accessibility` | Offen | — |
| 56 | Dokumentation | ADR `docs/adr/0014-route-search.md` (inkl. Tankerkönig-Bedingungen, Aufhebung des Ausschlusses) | Offen | — |
| 57 | Dokumentation | `docs/help/Suche/*` aktualisieren | Offen | — |
| 58 | Dokumentation | `docs/help/Preisdaten/api-schluessel.md`, `docs/help/ci-cd/einrichtung.md` aktualisieren | Offen | — |
| 59 | Dokumentation | `docs/help/ci-cd/workflows.md`, ADR 0013, `docs/help/Sicherung/ablauf-technisch.md` (Härtungen 16) | Offen | — |
| 60 | Dokumentation | `README.md` und `changes.log` aktualisieren; Test-Fokus-Hinweis | Offen | — |
| 61 | Abschluss | Hook-Prüfungen (Format, XML-Doku, Übersetzung, Enum), Unit/Integration, gezielte E2E, ein Gesamtlauf | Offen | — |
| 62 | Abschluss | Windows-Zwischenstand unter `review-versions/` (nicht committen) | Offen | — |
