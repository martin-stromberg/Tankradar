# Tasks: Schritt 6 – Umkreissuche nach aktuellem Standort mit Ergebnisliste

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Konfiguration | `TestDataPaths.TestDataPathEnvironmentVariable` auf `TANKATLAS_TEST_DATA_PATH` ändern | Offen | — |
| 2 | Konfiguration | Konstante `TestDataPaths.TestLocationEnvironmentVariable` anlegen | Offen | — |
| 3 | Konfiguration | XML-Kommentare in `ApiKeys.cs` und `PriceApiOptions.cs` auf den neuen Namen ändern | Offen | — |
| 4 | Datenmodell | `GeoPosition` anlegen | Offen | — |
| 5 | Datenmodell | `LocationStatus` anlegen | Offen | — |
| 6 | Datenmodell | `LocationResult` anlegen | Offen | — |
| 7 | Datenmodell | `SearchRadius` anlegen (Min 1, Max 25, Standard 5, `TryParse`) | Offen | — |
| 8 | Datenmodell | `StationPriceLine` anlegen | Offen | — |
| 9 | Datenmodell | `StationListItem` anlegen | Offen | — |
| 10 | Texte | `SearchTexts` anlegen | Offen | — |
| 11 | Logik | `StationResultBuilder` mit Filter anlegen | Offen | — |
| 12 | Logik | `StationResultBuilder`: Preiszeilen in Einstellungsreihenfolge, Alter, Amber-Kennzeichen | Offen | — |
| 13 | Logik | `StationResultBuilder`: Hinweise „Preis unbestätigt“/„Automatentankstelle“ | Offen | — |
| 14 | Logik | `StationResultBuilder`: Sortierung nach Preis, Entfernung, Name | Offen | — |
| 15 | Logik | `ILocationService` anlegen | Offen | — |
| 16 | Logik | `MauiLocationService` anlegen (Berechtigung, Position, Fehlerabfang, kein Logging von Koordinaten) | Offen | — |
| 17 | Logik | `TestLocationService` anlegen | Offen | — |
| 18 | Logik | `LocationServiceSelector` anlegen | Offen | — |
| 19 | Konfiguration | `MauiProgram`: `ILocationService` registrieren | Offen | — |
| 20 | UI | `BaseViewModel.OnDisappearing` und `TankradarContentPage.OnDisappearing` ergänzen | Offen | — |
| 21 | UI | `FuelFilterOptionViewModel` anlegen | Offen | — |
| 22 | UI | `MapViewModel` erweitern (Einstellungen laden, Radius, Suche, Filter, Sortierung) | Offen | — |
| 23 | UI | `MapViewModel`: Offline-Banner und Abo auf `IConnectionMonitor.ConnectionChanged` | Offen | — |
| 24 | UI | `MapPage.xaml` gemäß Designentwurf (Banner, Radius, Filter, Sortierung, Liste, Leerzustand) | Offen | — |
| 25 | UI | `MapPage.xaml.cs` anpassen | Offen | — |
| 26 | Plattform | `Info.plist` (iOS und MacCatalyst): `NSLocationWhenInUseUsageDescription` | Offen | — |
| 27 | Plattform | `Package.appxmanifest`: Capability `location` | Offen | — |
| 28 | Plattform | `PrivacyInfo.xcprivacy` auf Standort prüfen und ergänzen | Offen | — |
| 29 | Testinfrastruktur | `MockTankerkoenigServer`: Stationen Gamma, Delta, Epsilon | Offen | — |
| 30 | Testinfrastruktur | `MockTankerkoenigServer`: Radius-/Positionsauswertung, `LastList*`-Eigenschaften | Offen | — |
| 31 | Testinfrastruktur | `E2ETestBase.RestartApplication` mit geänderter Umgebung | Offen | — |
| 32 | Testinfrastruktur | `SearchE2ETestBase` anlegen | Offen | — |
| 33 | Testinfrastruktur | Unit-Helfer `FakeLocationService`, `RecordingLogger`, `StationFactory` | Offen | — |
| 34 | Tests | `TestModeVariableTests_Rename` | Offen | — |
| 35 | Tests | `SearchRadiusTests_Validation` | Offen | — |
| 36 | Tests | `StationResultBuilderTests_Filter` | Offen | — |
| 37 | Tests | `StationResultBuilderTests_PriceLines` | Offen | — |
| 38 | Tests | `StationResultBuilderTests_Sorting` | Offen | — |
| 39 | Tests | `StationResultBuilderTests_Age` | Offen | — |
| 40 | Tests | `StationResultBuilderTests_Hints` | Offen | — |
| 41 | Tests | `LocationServiceTests_Permission` | Offen | — |
| 42 | Tests | `LocationServiceSelectorTests_TestMode` | Offen | — |
| 43 | Tests | `MapViewModelTests_Search` | Offen | — |
| 44 | Tests | `MapViewModelTests_FilterSort` | Offen | — |
| 45 | Tests | `MapViewModelTests_Settings` | Offen | — |
| 46 | Tests | `MapViewModelTests_Offline` | Offen | — |
| 47 | Tests | `MapViewModelTests_Privacy` | Offen | — |
| 48 | Tests | `SearchPrivacyTests_Persistence` (Integration) | Offen | — |
| 49 | Tests | `SearchMockServerTests_Flow` (Integration) | Offen | — |
| 50 | Tests | `SearchMockServerTests_Offline` (Integration) | Offen | — |
| 51 | Tests | `PlatformManifestTests_Location` | Offen | — |
| 52 | Tests | `ViewModelTests_PageTitles` anpassen | Offen | — |
| 53 | Tests | Bestehende Tests mit Mock-Daten anpassen | Offen | — |
| 54 | E2E-Tests | `SearchE2ETests_Basic` | Offen | — |
| 55 | E2E-Tests | `SearchE2ETests_Radius` | Offen | — |
| 56 | E2E-Tests | `SearchE2ETests_FilterSort` | Offen | — |
| 57 | E2E-Tests | `SearchE2ETests_GpsNever` | Offen | — |
| 58 | E2E-Tests | `SearchE2ETests_FuelOrder` | Offen | — |
| 59 | E2E-Tests | `SearchE2ETests_Offline` | Offen | — |
| 60 | E2E-Tests | `TestModeRenameE2ETests` | Offen | — |
| 61 | Dokumentation | `README.md`: neuer Variablenname, `TANKATLAS_TEST_LOCATION`, Stand Schritt 6 | Offen | — |
| 62 | Dokumentation | Hilfetexte mit altem Variablennamen anpassen (4 Dateien) | Offen | — |
| 63 | Dokumentation | `docs/help/Suche/` anlegen und in `docs/help/index.md` verlinken | Offen | — |
| 64 | Dokumentation | `changes.log` Eintrag Version 0.1.7 | Offen | — |
| 65 | Release | Windows-Zwischenstand 0.1.7 mit `scripts/create-review-version.ps1` ablegen (nicht committen) | Offen | — |
