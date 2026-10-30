# Enums

## `MapFitMode`
Datei: `src/Tankradar.MAUI/Models/Map/MapFitMode.cs`

| Wert | Bedeutung |
|------|-----------|
| `FitToResult` | Neue Suche: Ausschnitt wird auf Markierungen, Suchposition und Route eingepasst (Standardwert der Bindung `MapFitModeProperty`) |
| `KeepViewport` | Nachladen des sichtbaren Bereichs: Ausschnitt des Anwenders bleibt erhalten |

Relevanz für die Anforderung: Doppeltipp und Kneifen dürfen nur über `ViewportChanged` den Ausschnitt verändern, nicht `OnResultChanged`/`RequestFit`. Der Modus ist davon nicht berührt.
