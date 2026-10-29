# Datenmodelle (Kartenansicht)

Die Modelle für die Kartenansicht liegen unter `src/Tankradar.MAUI/Models/Map/`, die Routen- und Suchmodelle unter `Models/Routing/`, `Models/Search/` und `Models/Pricing/`. Ein eigenes Modell für einen Kartenausschnitt (Begrenzungsbox oder Mittelpunkt mit Radius) existiert nicht; der sichtbare Ausschnitt ist als `MapViewport` nur in Form von Mitte und Zoomstufe modelliert (siehe [Logik](logic.md)).

## `MapOrigin`
Datei: `src/Tankradar.MAUI/Models/Map/MapOrigin.cs` (`sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Kind` | `MapOriginKind` | Herkunft des Bezugspunkts (aktueller Standort oder gesuchte Position) |
| `Latitude` | `double` | Breitengrad des Bezugspunkts |
| `Longitude` | `double` | Längengrad des Bezugspunkts |

Wird von `MapViewModel.RunSearchAsync` erzeugt und über die Bindung `Origin` an `StationMapView` übergeben.

## `MapRoute`
Datei: `src/Tankradar.MAUI/Models/Map/MapRoute.cs` (`sealed class`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Points` | `IReadOnlyList<GeoPosition>` | Stützpunkte der Route |
| `Start` | `GeoPosition` | Startpunkt |
| `Destination` | `GeoPosition` | Zielpunkt |
| `Bounds` | `RouteBounds` | Begrenzungsbox der Route (im Konstruktor berechnet) |

Wird über die Bindung `MapRoute` (aus `MapViewModel.Route`) an `StationMapView.Route` gebunden; `StationMapView.FitToContent` nutzt `Bounds`.

## `RouteBounds`
Datei: `src/Tankradar.MAUI/Models/Routing/RouteBounds.cs` (`readonly record struct`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MinLatitude` | `double` | Südlichster Breitengrad |
| `MinLongitude` | `double` | Westlichster Längengrad |
| `MaxLatitude` | `double` | Nördlichster Breitengrad |
| `MaxLongitude` | `double` | Östlichster Längengrad |

Dies ist der einzige vorhandene Typ für eine Begrenzungsbox; er ist routenbezogen und nicht für den sichtbaren Kartenausschnitt vorgesehen.

## `MapMarker`
Datei: `src/Tankradar.MAUI/Models/Map/MapMarker.cs` (`sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Station` | `StationListItem` | Zugehörige Tankstelle (wird an `MarkerCommand` übergeben) |
| `Latitude` | `double` | Breitengrad |
| `Longitude` | `double` | Längengrad |
| `Level` | `PriceLevel` | Preisniveau für die Farbgebung |
| `PriceText` | `string` | Anzeigetext des Preises |
| `HasPrice` | `bool` (berechnet) | `true`, wenn `PriceText` nicht leer ist |
| `Description` | `string` | Beschreibung für Barrierefreiheit und Automatisierung |

## `ScreenPoint`
Datei: `src/Tankradar.MAUI/Models/Map/ScreenPoint.cs` (`readonly record struct`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `X` | `double` | Horizontale Lage in Pixeln im Ausschnitt |
| `Y` | `double` | Vertikale Lage in Pixeln im Ausschnitt |

## `TileKey`
Datei: `src/Tankradar.MAUI/Models/Map/TileKey.cs` (`readonly record struct`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Zoom` | `int` | Zoomstufe der Kachel |
| `X` | `int` | Spaltenindex (Web-Mercator-Schema) |
| `Y` | `int` | Zeilenindex |

Schlüssel für `ITileSource.GetTileAsync`.

## `PlacedTile`
Datei: `src/Tankradar.MAUI/Models/Map/PlacedTile.cs` (`readonly record struct`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `TileKey` | Kachel-Schlüssel |
| `Left` | `double` | Linker Rand im Ausschnitt in Pixeln |
| `Top` | `double` | Oberer Rand im Ausschnitt in Pixeln |

Ergebnis von `MapViewport.VisibleTiles()`.

## `StationSearchQuery`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationQueryResults.cs` (`sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Latitude` | `double` | Mittelpunkt der Umkreissuche |
| `Longitude` | `double` | Mittelpunkt der Umkreissuche |
| `RadiusKm` | `int` | Radius in Kilometern (ein Kreis) |
| `FuelTypes` | `IReadOnlyList<FuelType>` | Abzufragende Kraftstoffarten |

Die Abfrage kennt nur einen Kreis um einen Mittelpunkt; eine Abfrage über einen rechteckigen Kartenausschnitt ist nicht vorgesehen.

## `SearchRadius` (statische Klasse)
Datei: `src/Tankradar.MAUI/Models/Search/SearchRadius.cs`

| Mitglied | Typ | Beschreibung / Zweck |
|----------|-----|----------------------|
| `Min` | `const int` | Kleinster zulässiger Radius (1 km) |
| `Max` | `const int` | Größter zulässiger Radius (25 km, von der Preisquelle vorgegeben) |
| `Steps` | `IReadOnlyList<int>` | Wählbare Stufen: 1, 2, 5, 10, 15, 25 km |
| `IsValid(int)` | `bool` (Methode) | Prüft, ob ein Radius von 1 bis 25 km liegt |

Enums der Kartenansicht (`SearchMode`, `ResultView`, `MapOriginKind`, `PriceLevel`, `ResultSortOrder`, `RouteStartKind`) sind unter [Enums](enums.md) dokumentiert.
