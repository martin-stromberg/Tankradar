# Datenmodell

Betroffene Datenmodellklassen der Anforderung „Automatisches Laden der Karte“. Alle positionshaltenden Typen leben ausschließlich im Arbeitsspeicher und werden nie gespeichert oder protokolliert (`GeoPosition` und `MapOrigin` geben in `ToString()` bewusst keine Koordinaten aus).

## `GeoPosition`
Datei: `src/Tankradar.MAUI/Models/Search/GeoPosition.cs`

Sealed record mit Wertebereichsprüfung im Konstruktor; `TryCreate` für fehlertolerantes Erzeugen.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Latitude` | `double` | Breitengrad, −90 bis 90 (sonst `ArgumentOutOfRangeException`). |
| `Longitude` | `double` | Längengrad, −180 bis 180 (sonst `ArgumentOutOfRangeException`). |
| `TryCreate(latitude, longitude, out position)` | `static bool` | Erzeugt die Position ohne Ausnahme; wird von `LocationServiceSelector.TryParseLocation` verwendet. |
| `ToString()` | `string` | Gibt nur den Typnamen zurück (Schutz vor versehentlichem Protokollieren der Koordinaten). |

Verwendung: Rückgabe von `ILocationService.GetCurrentLocationAsync` über `LocationResult.Position`; Grundlage der `StationSearchQuery` und des `MapOrigin` in `MapViewModel.RunSearchAsync`.

## `LocationResult`
Datei: `src/Tankradar.MAUI/Models/Search/LocationResult.cs`

Sealed record `LocationResult(LocationStatus Status, GeoPosition? Position = null)`; `Position` ist nur bei `LocationStatus.Available` gesetzt.

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `Status` | `LocationStatus` | Ergebnis der Standortabfrage (siehe [Enums](enums.md)). |
| `Position` | `GeoPosition?` | Die ermittelte Position; `null` bei jedem Fehlerstatus. |
| `Success(position)` | `static LocationResult` | Erstellt ein Ergebnis mit `LocationStatus.Available`. |
| `Failure(status)` | `static LocationResult` | Erstellt ein Ergebnis ohne Position. |

Verwendung: `MapViewModel.ResolveCurrentLocationAsync` (Karte) und `FavoritesViewModel.RefreshAsync`/`NearbySectionViewModel.LoadAsync` („In der Nähe“) werten `Status`/`Position` getrennt aus. Es gibt keinen Mechanismus, der ein `LocationResult` zwischen den ViewModels teilt oder sitzungslang hält.

## `MapOrigin`
Datei: `src/Tankradar.MAUI/Models/Map/MapOrigin.cs`

Sealed record; die auf der Karte markierte Suchposition. Wird nur im Arbeitsspeicher gehalten und bei jeder neuen Suche, jedem Moduswechsel und jedem Fehlschlag verworfen (`MapViewModel.ClearResult`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `MapOriginKind` | Art der Position: `CurrentLocation` oder `SearchedPlace`. |
| `Latitude` | `double` | Breitengrad der Suchposition. |
| `Longitude` | `double` | Längengrad der Suchposition. |
| `ToString()` | `string` | Nur der Typname (keine Koordinaten). |

Verwendung: In `MapViewModel.RunSearchAsync` aus der Position erzeugt (`MapOriginKind.CurrentLocation` bei Standortsuche, `SearchedPlace` bei Adresssuche) und über `Origin` an die Karte gebunden; beim Nachladen des Ausschnitts bleibt die alte `Origin` bestehen (`ReloadVisibleAreaAsync` übergibt `_origin`).

## `MapMarker`
Datei: `src/Tankradar.MAUI/Models/Map/MapMarker.cs`

Sealed record; eine Tankstellenmarkierung der Karte.

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `Station` | `StationListItem` | Die Tankstelle (wird beim Antippen an die Detailansicht übergeben). |
| `Latitude` / `Longitude` | `double` | Position der Markierung. |
| `Level` | `PriceLevel` | Preisniveau (bestimmt die Farbe). |
| `PriceText` | `string` | Preis der maßgeblichen Sorte; leer, wenn die Tankstelle sie nicht führt. |
| `Description` | `string` | Beschreibung für Bedienhilfen. |
| `HasPrice` | `bool` | Gibt an, ob ein Preis angezeigt wird. |

Verwendung: `MapMarkerBuilder.Build` erzeugt sie aus `StationListItem`s; `MapViewModel.MapMarkers` hält die aktuelle Menge sitzungslang (`OnDisappearing` verwirft sie nicht).

## `StationSearchQuery`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationQueryResults.cs` (Zeile 11)

Sealed record `StationSearchQuery(double Latitude, double Longitude, int RadiusKm, IReadOnlyList<FuelType> FuelTypes)`; die Position wird nur für die Anfrage verwendet.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Latitude` / `Longitude` | `double` | Suchposition; wird von `FuelPriceService.Validate` geprüft (±90/±180, endlich). |
| `RadiusKm` | `int` | Suchradius; muss zwischen 1 und `PriceApiOptions.MaxRadiusKm` (25) liegen. |
| `FuelTypes` | `IReadOnlyList<FuelType>` | Gewünschte Spritsorten; mindestens eine gültige Sorte ist Pflicht. |

Verwendung: `MapViewModel.RunSearchAsync` (Radius `_radiusKm`), `MapViewModel.ReloadVisibleAreaAsync` über `MapAreaQuery.From` (Ausschnitt), `NearbySectionViewModel.LoadAsync` (fest `SearchRadius.Default` = 5 km) — alle rufen `IFuelPriceService.SearchNearbyAsync`. Der Cache-Schlüssel des `FuelPriceService` enthält Radius und auf 3 Dezimalstellen gerundete Koordinaten, nicht die Sorten.

## `StationSearchResult`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationQueryResults.cs` (Zeile 20)

Sealed record `StationSearchResult(IReadOnlyList<StationInfo> Stations, PriceDataSource Source, PriceFailure Failure)`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Stations` | `IReadOnlyList<StationInfo>` | Gefundene Tankstellen mit Preisen der angefragten Sorten, nach Entfernung sortiert. |
| `Source` | `PriceDataSource` | Herkunft der Daten (`Live`, `Cache`, `OfflineFallback`); steuert `IsOffline`/`SourceNote` im `MapViewModel`. |
| `Failure` | `PriceFailure` | Grund, warum nicht live abgerufen wurde; `None` bei Live- und Cache-Daten. |

Verwendung: In `MapViewModel` über `_lastResult` gehalten (steuert `ShowEmptyState` und wird nicht durch `OnDisappearing` verworfen); `ApplyResult` übernimmt es in Liste und Karte. Auch `ApplyRouteResult` legt ein `StationSearchResult` aus der Routensuche in `_lastResult` ab.

## `AppSettings`
Datei: `src/Tankradar.MAUI/Models/AppSettings.cs`

Sealed record der persistierten Einstellungen; `CreateDefault()` liefert `GpsUsage.WhileInUse`, `ResultView.List`, `ResultSortOrder.Price`, alle Sorten gewählt, `ShowNearby = false`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelTypes` | `IReadOnlyList<FuelTypeSelection>` | Spritsorten in Prioritätsreihenfolge; `IsSelected` steuert Anfrage und Filter. |
| `GpsUsage` | `GpsUsage` | Standortnutzung; maßgeblich für die Standortabfrage (`AllowsLocation`, Fail Secure). |
| `ResultView` | `ResultView` | Standardansicht („Liste“/„Karte“); legt den Startzustand der Trefferliste fest. |
| `ResultSortOrder` | `ResultSortOrder` | Standardsortierung der Ergebnisliste. |
| `ShowNearby` | `bool` | Schaltet den Bereich „In der Nähe“ der Startseite (wirkt nur bei erlaubter Standortnutzung). |

Relevante Methoden: `Normalize()` (fehlende Sorten unausgewählt anfügen), `NormalizeFuelTypes(...)`, `Validate()` (Persistenzvertrag).

Verwendung: `MapViewModel` lädt sie über `ISettingsService` in `LoadSettingsAsync`/`GetSettingsOrReportAsync`; `GpsUsage` wird unverändert an `ILocationService.GetCurrentLocationAsync` weitergereicht. Es existiert keine Einstellung für eine automatische Startsuche.
