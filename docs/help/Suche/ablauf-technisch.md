← [Zurück zur Übersicht](index.md)

# Umkreissuche — Technischer Ablauf

## Übersicht

`MapViewModel` (Bereich „Karte“, `MapPage`) prüft den Radius, holt den Standort über `ILocationService`, ruft `IFuelPriceService.SearchNearbyAsync` auf und bereitet das Ergebnis mit `StationResultBuilder` zu `StationListItem`-Zeilen auf. Alle Texte stehen in `SearchTexts`.

| Komponente | Aufgabe |
|------------|---------|
| `MapViewModel` | Suchablauf, `RadiusText`, `SearchCommand`, `Stations`, `FuelFilterOptions`, `SortOptions`, `StatusMessage`, `IsOffline`, `SourceNote` |
| `SearchRadius` | `Min` 1, `Max` 25, `Default` 5; `TryParse` akzeptiert nur Ziffern (nach `Trim`) im Bereich 1–25 |
| `ILocationService` | liefert `LocationResult` (`LocationStatus`: `Available`, `PermissionDenied`, `DisabledBySetting`, `Unavailable`; `GeoPosition`) |
| `MauiLocationService` | Berechtigung `Permissions.LocationWhenInUse`, `Geolocation` (Genauigkeit Medium, Zeitlimit 10 s) |
| `TestLocationService` | fester Standort im Testmodus |
| `LocationServiceSelector` | wählt die Implementierung (`MauiProgram`, Singleton) und parst `TANKATLAS_TEST_LOCATION` |
| `GpsUsageExtensions.AllowsLocation` | erlaubt nur `Always` und `WhileInUse` (Fail Secure) |
| `StationResultBuilder` | Filter, Preiszeilen, Hinweise, Sortierung (rein, ohne Zustand) |

## Ablauf `MapViewModel.SearchAsync`

1. Eine laufende Suche wird abgebrochen. Schlägt `SearchRadius.TryParse(RadiusText)` fehl: `SearchTexts.RadiusInvalid`, kein Standort- und kein API-Aufruf.
2. Einstellungen werden bei Bedarf geladen (`ISettingsService`); Fehler: `SettingsTexts.LoadFailed`.
3. `GetCurrentLocationAsync(settings.GpsUsage)`: Bei `AllowsLocation() == false` kommt `DisabledBySetting` ohne Berechtigungsabfrage. Sonst Berechtigung prüfen/anfragen, dann Position. Status ungleich `Available`: Liste leeren, Hinweis aus `SearchTexts.GetLocationMessage`.
4. `StationSearchQuery(lat, lon, radiusKm, gewählte FuelTypes)` an `IFuelPriceService.SearchNearbyAsync` (Cache, Live, Offline-Rückfall, siehe [Preisdaten](../Preisdaten/ablauf-technisch.md)).
5. `ApplyResult`: `StationResultBuilder.Build`, Meldung aus `SearchTexts.GetFailureMessage(Failure, hasStations)`, `SourceNote` bei `PriceDataSource.OfflineFallback` mit Treffern, `IsOffline` bei fehlender Verbindung oder Rückfall.
6. Ausnahmen (außer Abbruch durch eine neuere Suche bzw. `OnDisappearing`): Liste leeren, `SearchTexts.SearchFailed`; protokolliert wird nur der Ausnahmetyp.

`OnAppearing` lädt nur Einstellungen und abonniert `IConnectionMonitor.ConnectionChanged`; `OnDisappearing` meldet ab und bricht die Suche ab. Filter-/Sortieränderungen rufen `ApplyFilterAndSort` auf (kein Standort-, kein API-Aufruf). Die Standardsortierung kommt aus `AppSettings.ResultSortOrder`.

```mermaid
flowchart TD
    A[Suchen] --> B{Radius 1-25?}
    B -- Nein --> X[RadiusInvalid]
    B -- Ja --> C{AllowsLocation?}
    C -- Nein --> Y[DisabledBySetting-Hinweis]
    C -- Ja --> D{Berechtigung und Position?}
    D -- Nein --> Z[PermissionDenied / Unavailable-Hinweis]
    D -- Ja --> E[SearchNearbyAsync]
    E --> F[StationResultBuilder.Build]
    F --> G[Liste, ggf. Offline-Hinweis]
```

## `StationResultBuilder`

- Berücksichtigt nur gewählte, definierte `FuelType`-Werte; ein Filter außerhalb davon gilt als „Alle“.
- Filter: nur Tankstellen mit Preis der Sorte. Preiszeilen in Einstellungsreihenfolge, je mit `PriceFreshness.FormatAge`/`IsStale`; `StationHints` liefert „Preis unbestätigt“ (nur über angezeigte Preise) und „Automatentankstelle“. Preise im Format „1,859 €“, Entfernung „1,4 km“ (`de-DE`).
- Sortierung: `Price` nach Preis der Filtersorte (sonst erste gewählte Sorte; fehlender Preis zuletzt), dann Entfernung, Name, Id; `Distance` und `Name` mit den jeweils anderen als Nachrang. Namensvergleich `de-DE`, ohne Groß-/Kleinschreibung.

## Datenschutz

Der Standort wird nur im Speicher für die Anfrage verwendet, nie gespeichert oder protokolliert (`MapViewModel` und `MauiLocationService` protokollieren keine Koordinaten; Tests `SearchPrivacyTests_*`). Plattform: iOS/MacCatalyst `NSLocationWhenInUseUsageDescription` („Ermittlung von Tankstellen in der Nähe“), iOS-`PrivacyInfo.xcprivacy` mit `NSPrivacyCollectedDataTypePreciseLocation` (nicht verknüpft, kein Tracking, Zweck App-Funktionalität), Windows `DeviceCapability` `location`.

## Testmodus und manuelle Abnahme unter Windows (ohne GPS)

Windows-Rechner haben meist keinen Standortdienst; ohne Testmodus meldet die Suche dann „Der Standort konnte nicht ermittelt werden…“ (kein stiller Ersatzstandort).

| Variable | Wirkung |
|----------|---------|
| `TANKATLAS_TEST_DATA_PATH` | aktiviert den Testmodus mit isoliertem Datenverzeichnis (der frühere Name `TEST_DATA_PATH` wirkt nicht mehr) |
| `TANKATLAS_TEST_LOCATION` | fester Standort `breite,länge` mit Dezimalpunkt, z. B. `52.5200,13.4050`; nur im Testmodus; fehlend oder ungültig: Standort „nicht ermittelbar“ |
| `TANKRADAR_PRICE_API_URL`, `TANKRADAR_PRICE_API_KEY` | Adresse (HTTP nur Loopback) und Schlüssel des Preisdienstes im Testmodus; ohne Adresse kein Abruf |

Abnahme:

1. In der PowerShell-Sitzung setzen: `$env:TANKATLAS_TEST_DATA_PATH = "<leeres Testverzeichnis>"`, `$env:TANKATLAS_TEST_LOCATION = "52.5200,13.4050"`, `$env:TANKRADAR_PRICE_API_URL` und `$env:TANKRADAR_PRICE_API_KEY` auf einen erreichbaren Mock-Preisdienst (`MockTankerkoenigServer`, `src/TestSupport`).
2. App aus derselben Sitzung starten (`dotnet run --project src/Tankradar.MAUI -f net10.0-windows10.0.19041.0` oder `Tankradar.MAUI.exe`).
3. In **Optionen** Spritsorten wählen, dann **Karte** öffnen, Radius setzen, **Suchen**: Liste, Filter und Sortierung prüfen.
4. Prüffälle: **Standort und GPS** = **Nie** (Hinweis, keine Abfrage); `TANKATLAS_TEST_LOCATION` entfernen oder ungültig setzen (Hinweis „Standort nicht ermittelt“); Radius 0 oder 26 (Meldung); nicht erreichbare Preisdienst-Adresse (Offline-Hinweis).

Die automatisierten Tests nutzen dieselben Variablen: `E2ETestBase` / `SearchE2ETestBase` (FlaUI), `SearchMockServerTestBase` (Integration); Unit-Tests `MapViewModelTests_*`, `StationResultBuilderTests_*`, `LocationService*Tests`, `SearchRadiusTests_Validation`, `TestModeVariableTests_Rename`.
