# Enums (Kartenansicht)

## `SearchMode`
Datei: `src/Tankradar.MAUI/Models/Search/SearchMode.cs`

| Wert | Bedeutung |
|------|-----------|
| `CurrentLocation` | Umkreissuche um den aktuellen Standort |
| `Address` | Umkreissuche um eine eingegebene Adresse |
| `Route` | Routensuche (nur sichtbar, wenn die Routensuche aktiviert ist; `IRouteSearchAccess`) |

## `ResultView`
Datei: `src/Tankradar.MAUI/Models/ResultView.cs`

| Wert | Bedeutung |
|------|-----------|
| `List` | Ergebnisse als Liste (Standardansicht) |
| `Map` | Ergebnisse auf der Karte |

Der Name ist laut Kommentar ein Persistenzvertrag (Standardansicht aus den Einstellungen). Die Umschaltung läuft über `MapViewModel.SelectView` und die Bindungen `IsListView`, `IsMapView` und `ShowMap`.

## `MapOriginKind`
Datei: `src/Tankradar.MAUI/Models/Map/MapOrigin.cs`

| Wert | Bedeutung |
|------|-----------|
| `CurrentLocation` | Bezugspunkt ist der aktuelle Standort (Markierung "Mein Standort") |
| `SearchedPlace` | Bezugspunkt ist die gesuchte Adresse (Markierung "Gesuchte Position") |

## `PriceLevel`
Datei: `src/Tankradar.MAUI/Models/Map/PriceLevel.cs`

| Wert | Bedeutung |
|------|-----------|
| `Cheapest` | Günstigster Preis (Legende "Günstigster Preis") |
| `Normal` | Mittlerer Preis |
| `Expensive` | Hoher Preis |
| `Closed` | Geschlossen |

## `ResultSortOrder`
Datei: `src/Tankradar.MAUI/Models/ResultSortOrder.cs`

| Wert | Bedeutung |
|------|-----------|
| `Price` | Nach Preis sortiert (Standardsortierung in `MapViewModel.ApplyFilterAndSort`) |
| `Distance` | Nach Entfernung sortiert |
| `Name` | Nach Name sortiert |

## `RouteStartKind`
Datei: `src/Tankradar.MAUI/Models/Routing/RouteStartKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `CurrentLocation` | Routenstart ist der aktuelle Standort |
| `Address` | Routenstart ist eine eingegebene Adresse |
