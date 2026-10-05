### Fachliche Zusammenfassung

Der Bereich „Karte“ (Suche) erhält die Umkreissuche am aktuellen Standort: `MapViewModel` ermittelt über eine neue Standortabstraktion (`ILocationService`) den Gerätestandort, ruft `IFuelPriceService.SearchNearbyAsync` mit Radius (1–25 km, Standard 5 km) und den gewählten Spritsorten auf und zeigt die Tankstellen als sortierbare, nach Spritsorte filterbare Ergebnisliste. Je Tankstelle erscheinen Name, Entfernung, die Preise der in den Einstellungen gewählten Sorten (in Einstellungsreihenfolge) mit Altersangabe „vor X Min.“ (ab 60 Minuten in Amber), Zusatzhinweise („Preis unbestätigt“, „Automatentankstelle“) sowie bei fehlender Verbindung ein Offline-Hinweis in der Kopfzeile. Der Standort wird nur bei Bedarf und gemäß `GpsUsage` abgefragt (bei `GpsUsage.Never` keine Abfrage, stattdessen Hinweis) und nie dauerhaft gespeichert. Zusätzlich wird die Umgebungsvariable des Testmodus von `TEST_DATA_PATH` auf `TANKATLAS_TEST_DATA_PATH` umbenannt, sodass der Testmodus nur noch über den neuen Namen aktiviert wird.

### Betroffene Klassen und Komponenten

- Neue Modelle/Enums: Ergebniszeile der Liste (z. B. `StationListItem` mit Preiszeilen `StationPriceLine`), Standortergebnis (`LocationResult` mit Status-Enum `LocationStatus`: Available, PermissionDenied, DisabledBySetting, Unavailable), Suchfilter/Radiusvalidierung (z. B. `SearchRadius`)
- Neue Interfaces/Services: `ILocationService` (+ MAUI-Implementierung auf `Geolocation`, plus Test-Implementierung mit festem Standort aus der Testkonfiguration), Aufbereitung/Filter/Sortierung (z. B. `StationResultBuilder`, rein und testbar)
- Bestehende Klassen: `MapViewModel`/`MapPage` (Suchoberfläche statt Platzhalter), `PriceTexts`/neue Suchtexte, `MauiProgram` (DI), `TestDataPaths`, `AppDataPathProvider`, `PriceApiOptions`, `ApiKeyStoreSelector`, `MockTankerkoenigServer` (Mock-Daten für Umkreissuche), E2E-/Integrationsbasen, Skripte, Dokumentation
- Plattformkonfiguration: `Platforms/iOS/Info.plist` (`NSLocationWhenInUseUsageDescription`), `Platforms/Windows/Package.appxmanifest` (Capability `location`)
- Tests: Unit, Integration (Mock-Server), FlaUI-E2E (Suchablauf mit festem Standort), Test zur Umbenennung der Umgebungsvariable

### Implementierungsansatz

- Radius: Validierung (1 ≤ r ≤ 25, ganzzahlig) vor dem API-Aufruf; `FuelPriceService`/`PriceApiOptions.MaxRadiusKm` (25) existieren bereits, die UI darf keinen größeren Wert zulassen. Entscheidung zu 5–50 km der Originalanforderung: maßgeblich ist die Projektleiter-Entscheidung 1–25 km.
- Standort: nur auf Anforderung der Suche; Berechtigungslogik gemäß `GpsUsage` (Always/WhileInUse: Abfrage zulässig, Never: keine Abfrage und Hinweis). Koordinaten leben nur im Arbeitsspeicher der Anfrage (kein Schreiben in Datenbank, Log oder Einstellungen); gespeichert werden höchstens Entfernungen/Tankstellen-IDs (bestehender Preis-Cache speichert Tankstellenkoordinaten, nicht den Gerätestandort).
- Anzeige: Wiederverwendung von `PriceFreshness` (Alter, Amber ab 60 Min.), `StationHints`, `IConnectionMonitor` (Offline-Hinweis, automatische Aktualisierung bei `ConnectionRestored`), `ResultSortOrder` (Standard aus `AppSettings`), `FuelTypeSelection` (Reihenfolge).
- Standardansicht Liste/Karte: Die Kartenansicht ist erst Schritt 9; bis dahin zeigt die Suche immer die Liste.
- Testmodus: `TestDataPaths.TestDataPathEnvironmentVariable` wird auf `TANKATLAS_TEST_DATA_PATH` geändert; alle Verwendungen laufen über die Konstante, hinzu kommen Dokumentation, README, Testtexte; der alte Name aktiviert den Testmodus nicht mehr (Test).
- Auswahl-/Identifikationsanforderung: Filter nach Spritsorte ist eine Auswahl aus den in den Einstellungen gewählten Sorten (keine freie Eingabe); Radius über begrenzte Auswahl/Eingabe mit Validierung.

### Konfiguration

- Benutzerspezifisch (bestehend): Spritsorten, `GpsUsage`, Standardsortierung, Standardansicht.
- Suchradius (1–25 km, Standard 5 km) und Spritsortenfilter sind Eingaben der Suche (Annahme: nicht dauerhaft gespeichert).
- Test-Konfiguration über Umgebungsvariablen (nur im Testmodus wirksam): `TANKATLAS_TEST_DATA_PATH`, Preisdienst-Adresse, fester Teststandort.

### Offene Fragen

- Soll der Radius dauerhaft gespeichert werden (Annahme: nein, Standard 5 km je Sitzung)?
- Verhalten von `ResultView.Map` vor Schritt 9 (Annahme: Liste)?
- Formulierung der Hinweistexte bei verweigerter Berechtigung und bei „Nie“ (Annahme: Projektstil, deutsch, zentral in Texten)?
