# Bestandsaufnahme: Schritt 6 – Umkreissuche nach aktuellem Standort mit Ergebnisliste

Analysiert wurden Suche/Karte (`MapPage`, `MapViewModel`), Preisdienst und Preis-Cache aus Schritt 5, Einstellungen aus Schritt 4 sowie die Mechanik des Testmodus (`TEST_DATA_PATH`), bezogen auf `requirement.md`.

## Zusammenfassung

- Der Bereich „Karte“ ist nur ein Platzhalter: `MapViewModel` setzt nur den Titel, `MapPage` zeigt einen Text und `MapPage.Headline`; keine Suche, keine Liste, keine Filter.
- Vorhanden und wiederverwendbar: `IFuelPriceService.SearchNearbyAsync` (Validierung Radius 1..`PriceApiOptions.MaxRadiusKm` = 25, Spritsorten, Cache, Offline-Rückfall, Entfernungen in `StationInfo.DistanceKm`), `PriceFreshness` (Alter „vor X Min.“, Grenze 60 Min.), `StationHints` („Automatentankstelle“, „Preis unbestätigt“), `IConnectionMonitor` (`IsOnline`, `ConnectionRestored`), `PriceTexts` (Hinweistexte), `AppSettings` (Spritsorten in Reihenfolge, `GpsUsage`, `ResultView`, `ResultSortOrder`), `PriceRepository.DistanceKm` (Haversine).
- Es fehlt: jede Standortabstraktion (`ILocationService` o. Ä.), jede Geolocation-Nutzung, ein Suchradius im UI, Filter/Sortierung/Ergebnisaufbereitung, Anzeige von Preisalter/Amber in der UI, Offline-Hinweis in der Kopfzeile (kein Bestand außer `IConnectionMonitor`; kein ViewModel abonniert dessen Events), `NSLocationWhenInUseUsageDescription` in `Info.plist` und Capability `location` in `Package.appxmanifest` (nur `runFullTrust`).
- Die Windows-App läuft unpackaged (`WindowsPackageType=None`); das Manifest wirkt für diese Startart nicht (Befund).
- Der Testmodus wird über `TestDataPaths.TestDataPathEnvironmentVariable` (`"TEST_DATA_PATH"`) aktiviert; Verwendungen im Code: `AppDataPathProvider`, `PriceApiOptions.FromEnvironment`, `ApiKeyStoreSelector`, `E2ETestBase.LaunchApplication`. Der Name steht außerdem in `README.md` (Zeilen 121, 124), `changes.log` (Zeilen 19, 58; historisch), `docs/help/Einstellungen/ablauf-technisch.md`, `docs/help/Navigation/ablauf-technisch.md`, `docs/help/Preisdaten/ablauf-technisch.md`, `docs/help/Preisdaten/api-schluessel.md` sowie XML-Kommentaren in `ApiKeys.cs` und `PriceApiOptions.cs`. In Skripten, Workflows und `.github` kommt er nicht vor (`git grep`); Tests verwenden nur die Konstante. Ein Test, der belegt, dass ein anderer Name den Testmodus nicht aktiviert, existiert nicht.
- `MockTankerkoenigServer` liefert für `list.php` immer dieselben zwei Stationen (Alpha, Beta) mit festen Entfernungen und ignoriert `lat`, `lng` und `rad`; der Radius wird nicht protokolliert.
- Versionierung: `Tankradar.MAUI.csproj` enthält weiterhin 0.1.0; die Schrittversion steckt im Parameter `-Version` von `scripts/create-review-version.ps1` (Ablage `review-versions/<Version>_<Datum>/`, zuletzt `0.1.6_2026-10-05`) und in den Überschriften von `changes.log`.

Test-Ausgangszustand: Build Release ohne Warnungen und Fehler; Unit 204/204, Integration 33/33, E2E 12/12 erfolgreich, keine Fehlschläge, nichts übersprungen. Testlücken: Standort, Berechtigung, Ergebnisaufbereitung, Suchoberfläche, Radiusprüfung in der UI, Umbenennung des Testmodus-Namens. Nachweis: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
