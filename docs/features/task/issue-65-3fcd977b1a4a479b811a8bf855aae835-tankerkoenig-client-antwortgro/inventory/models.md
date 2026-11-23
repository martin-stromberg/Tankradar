# Datenmodell

## `PriceApiOptions`
Datei: `src/Tankradar.MAUI/Services/Pricing/PriceApiOptions.cs`

Einstellungen für den Preisdienst-Zugriff. `sealed`, alle Eigenschaften `init`-only; `Validate()` prüft HTTPS-Pflicht (Loopback-HTTP nur mit `AllowLoopbackHttp` im Testmodus) und nichtnegative Zeit-/Wiederholungswerte. `FromEnvironment` liest Testmodus (`TANKATLAS_TEST_DATA_PATH`), Endpunkt-Override (`PriceApiOptions.BaseUrlEnvironmentVariable` = `TestDataPaths.PriceApiUrlEnvironmentVariable`) und verkürzt dann Zeitwerte. **Keine Eigenschaft für ein Antwortgrößenlimit vorhanden.**

| Eigenschaft / Member | Typ | Beschreibung / Zweck |
|----------------------|-----|----------------------|
| `DefaultBaseUrl` (const) | `string` | Produktive Tankerkönig-Adresse `https://creativecommons.tankerkoenig.de/json/` (Zeile 14) |
| `BaseUrlEnvironmentVariable` (const) | `string` | Umgebungsvariable des Test-Endpunkts (Zeile 21) |
| `ApiKeyEnvironmentVariable` (const) | `string` | Umgebungsvariable des Test-Schlüssels (Zeile 26) |
| `BaseUrl` | `Uri` | Basisadresse mit abschließendem Schrägstrich (Zeile 32) |
| `EndpointNotConfigured` | `bool` | Testmodus ohne Adresse → Abruf verweigert (Zeile 37) |
| `RequestTimeout` | `TimeSpan` | Zeitlimit je Anfrage, Standard 10 s (Zeile 43) |
| `MaxAttempts` | `int` | Höchstzahl Versuche inkl. erstem, Standard 3 (Zeile 48) |
| `RetryBaseDelay` | `TimeSpan` | Basiswartezeit, verdoppelt je Versuch, Standard 1 s (Zeile 54) |
| `MinRequestInterval` | `TimeSpan` | Drosselungsabstand, Standard 1 s (Zeile 60) |
| `CacheLifetime` | `TimeSpan` | Frischefenster des lokalen Caches, Standard 5 min (Zeile 66) |
| `MaxRadiusKm` | `int` | Größter zulässiger Suchradius, Standard 25 (Zeile 71) |
| `AllowLoopbackHttp` | `bool` | HTTP zu Loopback nur im Testmodus (Zeile 76) |
| `Validate()` | `void` | Wirft `InvalidOperationException` bei unsicheren/ungültigen Werten (Zeile 82-101) |
| `FromEnvironment(getEnvironmentVariable)` | `static PriceApiOptions` | Testmodus-Erkennung und Endpunkt-Override (Zeile 109-134) |

Vergleichswerte anderer Dienste (für die Begründung des Limits relevant): `GeocodingOptions`-Client 256 KB, `RoutingOptions`-Client 4 MB (jeweils `MaxResponseContentBufferSize` in `MauiProgram.cs`), `TileServerOptions.MaxTileBytes` = 512 KB als Options-Eigenschaft mit `Validate()`-Prüfung (`src/Tankradar.MAUI/Services/Map/TileServerOptions.cs:67,120`).

## `StationInfo`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationInfo.cs`

Vom `TankerkoenigClient` erzeugtes Stationsmodell (Rückgabe von `SearchAsync`/`GetDetailAsync`). `sealed`, `init`-Eigenschaften; Kopiermethoden `WithDetails`, `WithCustomName`, `WithoutStaleDetails`, `With`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` (required) | Kennung der Tankstelle (UUID der Quelle) |
| `Name` | `string` (required) | Name der Tankstelle |
| `Brand` | `string?` | Marke, sofern geliefert |
| `CustomName` | `string?` | Eigener Anzeigename des Anwenders |
| `DisplayName` / `SubName` | `string` (get only) | Abgeleitete Anzeigenamen über `StationNaming` |
| `Street` / `HouseNumber` / `PostCode` / `Place` | `string?` | Adressangaben |
| `Latitude` / `Longitude` | `double` | Position |
| `Position` | `GeoPosition?` (get only) | Abgeleitete Position, `null` bei (0,0)/ungültig |
| `DistanceKm` | `double?` | Entfernung zur Suchposition (nur Umkreissuche) |
| `IsOpen` | `bool?` | Geöffnet laut Quelle |
| `WholeDay` | `bool?` | Durchgehend geöffnet (nur Detailabruf) |
| `OpeningTimes` | `IReadOnlyList<OpeningTimeEntry>` | Öffnungszeiten (nur Detailabruf) |
| `Prices` | `IReadOnlyList<FuelPrice>` | Preise je Sorte mit Abrufzeitpunkt |
| `DetailsUpdatedUtc` | `DateTime?` | Zeitpunkt des letzten Detailabrufs |

## `FuelPrice`
Datei: `src/Tankradar.MAUI/Models/Pricing/FuelPrice.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelType` | `FuelType` | Spritsorte (Positional-Record-Parameter) |
| `Price` | `decimal` | Preis (Positional-Record-Parameter) |
| `RetrievedUtc` | `DateTime` | Abrufzeitpunkt (Positional-Record-Parameter) |

`public sealed record FuelPrice(FuelType FuelType, decimal Price, DateTime RetrievedUtc)` (Zeile 10).

## `OpeningTimeEntry`
Datei: `src/Tankradar.MAUI/Models/Pricing/OpeningTimeEntry.cs`

`public sealed record OpeningTimeEntry(string Text, string Start, string End)` (Zeile 10) — Öffnungszeiten-Zeile aus `detail.php`.

## Ergebnisrecords des `FuelPriceService`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationQueryResults.cs` und `src/Tankradar.MAUI/Models/Pricing/StationRefreshResult.cs`

| Record | Parameter | Zweck |
|--------|-----------|-------|
| `StationSearchQuery` | `Latitude`, `Longitude`, `RadiusKm`, `FuelTypes` | Eingabe der Umkreissuche |
| `StationSearchResult` | `Stations`, `Source`, `Failure` | Ergebnis der Umkreissuche; `Failure` trägt den `PriceFailure`-Grund |
| `StationDetailResult` | `Station`, `Source`, `Failure` | Ergebnis des Detailabrufs |
| `StationRefreshResult` | `Stations`, `Source`, `Failure` | Ergebnis der Preisaktualisierung |

Über `Failure` fließt jeder `PriceFailure`-Wert (inkl. eines künftigen Größenlimit-Fehlers) unverändert bis zu den Anzeigetexten `PriceTexts`/`SearchTexts`.
