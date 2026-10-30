# Interfaces

## `ITileSource`
Datei: `src/Tankradar.MAUI/Services/Map/ITileSource.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetTileAsync` | `TileKey key`, `CancellationToken cancellationToken = default` | `Task<byte[]?>` | Liefert PNG-Daten einer Kachel; `null`, wenn nicht verfügbar (Fehler führen nie zu einer Ausnahme) |

Verwendet von: `StationMapView.LoadTileAsync` (über die Eigenschaft `TileSource`). Für die Anforderung nicht berührt: Das Zwischenstufen-Rendering während der Kneifgeste soll laut Anforderung keine Kachelabfragen auslösen.
