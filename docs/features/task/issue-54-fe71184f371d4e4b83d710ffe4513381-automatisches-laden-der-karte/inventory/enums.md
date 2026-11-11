# Enums und Konstanten

Betroffene Aufzählungen und Konstanten der Anforderung „Automatisches Laden der Karte“.

## `SearchMode`
Datei: `src/Tankradar.MAUI/Models/Search/SearchMode.cs`

| Wert | Bedeutung |
|------|-----------|
| `CurrentLocation` | Suche am aktuellen Standort (benötigt die Standortnutzung); Ausgangszustand `_searchMode` im `MapViewModel` und Modus der geforderten Startsuche. |
| `Address` | Suche rund um eine eingegebene Adresse, einen Ort oder eine PLZ (kein Standort). |
| `Route` | Suche entlang einer Route (Start: aktueller Standort oder Adresse); nur bei aktivierter Routensuche wählbar. |

## `SearchRadius`
Datei: `src/Tankradar.MAUI/Models/Search/SearchRadius.cs` (statische Klasse, keine Enum)

| Konstante / Member | Wert / Bedeutung |
|--------------------|------------------|
| `Min` | `1` (kleinster zulässiger Radius in km). |
| `Max` | `25` (größter, von der Preisquelle vorgegebener Radius). |
| `Default` | `5` — Vorbelegung von `MapViewModel._radiusKm` und fester Radius von `NearbySectionViewModel.LoadAsync` sowie der geforderten Startsuche. |
| `Steps` | `[1, 2, 5, 10, 15, 25]` — wählbare Radiusstufen (Chips). |
| `IsValid(radiusKm)` | `true` für 1–25 km; wird in `SearchAsync` vor jeder Abfrage geprüft. |
| `TryParse(text, out radiusKm)` | Parst eine Eingabe auf ganze km im Bereich. |

## `LocationStatus`
Datei: `src/Tankradar.MAUI/Models/Search/LocationStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Available` | Der Standort wurde ermittelt (`LocationResult.Position` gesetzt). |
| `DisabledBySetting` | Einstellung „Nie“ verbietet die Standortnutzung; es wurde nichts abgefragt. Meldung: `SearchTexts.LocationDisabledBySetting`. |
| `PermissionDenied` | Der Anwender hat die Berechtigung verweigert. Meldung: `SearchTexts.LocationPermissionDenied`. |
| `Unavailable` | Standort nicht ermittelbar (Zeitüberschreitung/Fehler). Meldung: `SearchTexts.LocationUnavailable`. |

Zuordnung Status → Text: `SearchTexts.GetLocationMessage` (`src/Tankradar.MAUI/Resources/Texts/SearchTexts.cs` Zeile 293); wird von `MapViewModel.ResolveCurrentLocationAsync` und `NearbySectionViewModel.LoadAsync` genutzt.

## `GpsUsage` und `GpsUsageExtensions`
Datei: `src/Tankradar.MAUI/Models/GpsUsage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Always` | Der Standort wird immer verwendet. |
| `WhileInUse` | Der Standort wird nur verwendet, solange die App genutzt wird (Vorgabe von `AppSettings.CreateDefault`). |
| `Never` | Der Standort wird nie verwendet. |

`GpsUsageExtensions.AllowsLocation()` liefert `true` nur für `Always` und `WhileInUse` (Fail Secure: undefinierte Werte sperren). Die Wertnamen sind ein Persistenzvertrag.

## `MapOriginKind`
Datei: `src/Tankradar.MAUI/Models/Map/MapOrigin.cs` (Zeilen 6–17)

| Wert | Bedeutung |
|------|-----------|
| `CurrentLocation` | Die Markierung ist der eigene Standort (Standortsuche; Anzeige „Mein Standort“, `MapTexts.OriginCurrentLocation`). |
| `SearchedPlace` | Die Markierung ist die gesuchte Position (Adresssuche; „Gesuchte Position“). |

## `MapFitMode`
Datei: `src/Tankradar.MAUI/Models/Map/MapFitMode.cs`

| Wert | Bedeutung |
|------|-----------|
| `FitToResult` | Neue Suche: Die Karte passt den Ausschnitt auf Markierungen, Suchposition und Route ein (Standard nach `SearchAsync`). |
| `KeepViewport` | Nachladen des sichtbaren Bereichs: Der Ausschnitt des Anwenders bleibt erhalten (`ReloadVisibleAreaAsync`). |

## `PriceDataSource`
Datei: `src/Tankradar.MAUI/Models/Pricing/PriceDataSource.cs`

| Wert | Bedeutung |
|------|-----------|
| `Live` | Frisch von der Preis-API abgerufen. |
| `Cache` | Aus dem Such-Zwischenspeicher (Daten jünger als `CacheLifetime`, kein erneuter Abruf) — Nachweispunkt der Kombinationsvariante. |
| `OfflineFallback` | Zuletzt bekannte Werte aus dem lokalen Speicher bei nicht möglichem Abruf; setzt `SourceNote`/`IsOffline`. |

## `ResultView`
Datei: `src/Tankradar.MAUI/Models/ResultView.cs`

| Wert | Bedeutung |
|------|-----------|
| `List` | Ergebnisse standardmäßig als Liste (Trefferliste ausgeklappt). |
| `Map` | Ergebnisse standardmäßig auf der Karte (Trefferliste eingeklappt). |

Wertnamen sind Persistenzvertrag; wirkt über `SelectView` auf den Startzustand von `IsBottomSheetExpanded`.

## `ResultSortOrder`
Datei: `src/Tankradar.MAUI/Models/ResultSortOrder.cs`

| Wert | Bedeutung |
|------|-----------|
| `Price` | Sortierung nach Preis (Vorgabe; auch Fallback in `StationResultBuilder` und `NearbySectionViewModel` nutzt `Distance`). |
| `Distance` | Sortierung nach Entfernung. |
| `Name` | Sortierung nach Name. |
