# Datenmodell

## `ViewportChangedEventArgs`
Datei: `src/Tankradar.MAUI/Views/Controls/ViewportChangedEventArgs.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Viewport` | `MapViewport` | Der neue Kartenausschnitt (Daten der Ereignisse `ViewportChanged` und `ViewportFitted`) |

Wird ausgelöst von: `StationMapView` (`RaiseViewportChanged`, `FitToResult`). Verwendet von: `MapPage.xaml.cs` (`OnMapViewportChanged`, `OnMapViewportFitted`).

## `MapViewport`
Datei: `src/Tankradar.MAUI/Services/Map/MapViewport.cs`

Unveränderliche Klasse mit der gesamten Kartenmathematik. Eigenschaften:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Zoom` | `int` | Zoomstufe, auf 4..18 begrenzt |
| `Width` | `double` | Breite in Pixeln (mindestens 1) |
| `Height` | `double` | Höhe in Pixeln (mindestens 1) |
| `CenterLatitude` | `double` | Breitengrad der Mitte (berechnet aus Weltkoordinate) |
| `CenterLongitude` | `double` | Längengrad der Mitte (berechnet aus Weltkoordinate) |

Hinweis: Die Weltkoordinaten `_centerX` / `_centerY` sind privat; die Mitte wird als Breiten-/Längengrad nach außen gegeben.

## `PlacedTile`
Datei: `src/Tankradar.MAUI/Models/Map/PlacedTile.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `TileKey` | Kachelschlüssel |
| `Left` | `double` | Position der Kachel links, in Pixeln |
| `Top` | `double` | Position der Kachel oben, in Pixeln |

Erzeugt von `MapViewport.VisibleTiles()`.

## `TileKey`
Datei: `src/Tankradar.MAUI/Models/Map/TileKey.cs` (`readonly record struct`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Zoom` | `int` | Zoomstufe der Kachel |
| `X` | `int` | Spaltenindex (umgebrochen) |
| `Y` | `int` | Zeilenindex |

## `ScreenPoint`
Datei: `src/Tankradar.MAUI/Models/Map/ScreenPoint.cs` (`readonly record struct`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `X` | `double` | Horizontale Position in Pixeln |
| `Y` | `double` | Vertikale Position in Pixeln |

Verwendet von: `MapViewport.ToScreen` (Rückgabe) und `MapPointerTracker.Move` (Verschiebung).

## `MapBounds`
Datei: `src/Tankradar.MAUI/Models/Map/MapBounds.cs`
Rückgabe von `MapViewport.GetBounds()`, Konstruktor mit (MinLatitude, MinLongitude, MaxLatitude, MaxLongitude). Die Eigenschaftsnamen wurden nicht im Detail geprüft. Die Routenbegrenzung (`route.Bounds`) wird in `StationMapView.FitToContent` genutzt; ob sie diesen Typ hat, ist nicht geprüft.

## `GeoPosition`
Datei: `src/Tankradar.MAUI/Models/Search/GeoPosition.cs` (`sealed record`)
Rückgabe von `MapViewport.ToLatLon(x, y)`; Eigenschaften `Latitude`, `Longitude`. Die Anforderung nutzt diese Rückgabe für die neue Mitte am Tippunkt.

## `MapMarker`
Datei: `src/Tankradar.MAUI/Models/Map/MapMarker.cs` (`sealed record`)
Eigenschaften u. a.: `Station` (`StationListItem`), `Latitude`, `Longitude`, `Level` (`PriceLevel`), `PriceText`, `Description`, `HasPrice`. Verwendet von `StationMapView` für die Tankstellenmarkierungen (Buttons).
