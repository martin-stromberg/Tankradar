## `Vehicle`
Datei: `src/Tankradar.MAUI/Models/Tankbook/Vehicle.cs`

Record `Vehicle(long Id, string Name, VehicleFuelKind FuelKind, int StartOdometerKm, decimal? StartConsumption)`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Kennung des Fahrzeugs (intern, wird nie angezeigt; Navigationsparameter `vehicleId`). |
| `Name` | `string` | Anzeigename, Auswahl erfolgt über den Namen. |
| `FuelKind` | `VehicleFuelKind` | Kraftstoffart. |
| `StartOdometerKm` | `int` | Start-Kilometerstand in ganzen km. |
| `StartConsumption` | `decimal?` | Optionaler Startverbrauch pro 100 km. |

Verwendet von: `TankbookViewModel`, `TankbookEntriesViewModel`, `VehicleItemViewModel`, `IVehicleService`.

## `VehicleDeleteResult`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleDeleteResult.cs`

Record `VehicleDeleteResult(VehicleResult Result, int EntryCount)`. Rückgabe von `IVehicleService.DeleteVehicleAsync`; `EntryCount` ist die beim Bestätigen neu gezählte Zahl der Vorgänge.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Result` | `VehicleResult` | Ergebnis (u. a. `Ok`, `VehicleNotFound`, `EntryCountChanged`). |
| `EntryCount` | `int` | Aktuelle Zahl der Vorgänge des Fahrzeugs. |

Verwendet von: `TankbookViewModel.DeleteAsync` (`src/Tankradar.MAUI/ViewModels/TankbookViewModel.cs`).

## `UserSettingsEntity`
Datei: `src/Tankradar.MAUI/Data/UserSettingsEntity.cs`

Tabelle `UserSettings`, einzige Zeile mit `Id = 1` (`SingletonId`). Enthält die skalaren Einstellungen der App.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel, immer `SingletonId` (1). |
| `GpsUsage` | `string` | Name des `GpsUsage`-Werts. |
| `ResultView` | `string` | Name des `ResultView`-Werts. |
| `ResultSortOrder` | `string` | Name des `ResultSortOrder`-Werts. |
| `ShowNearby` | `bool` | Anzeige des Bereichs „In der Nähe“ auf der Startseite. |

Methode: `Update(AppSettings settings)` übernimmt die Werte aus `AppSettings`.

Querverweis: Die Existenz der Zeile ist das Erststart-Kennzeichen (`SetupStatusService.IsSetupRequiredAsync`, ADR 0017). Eine neue Spalte für die gemerkte Fahrzeugauswahl gibt es nicht; es existiert kein Feld dafür.

Nicht vorhanden: Eine Verwendung von `Microsoft.Maui.Storage.Preferences` im Quellcode (Suche nach `Preferences.` ohne Treffer in `src/`).

## `Vehicle`-Dienst-Ergebnisse (nicht gelesen im Detail)
`VehicleResult`, `VehicleSaveResult`, `VehicleResult`-Werte und `VehicleInput` liegen im Ordner `src/Tankradar.MAUI/Models/Tankbook/`. Nur die im Code verwendeten Namen wurden bestätigt (`VehicleResult.Ok`, `VehicleResult.VehicleNotFound`, `VehicleResult.EntryCountChanged`); die übrigen Felder wurden nicht im Detail gelesen.
