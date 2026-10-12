# Logik

## `ConsumptionCalculator`
Datei: `src/Tankradar.MAUI/Services/Tankbook/ConsumptionCalculator.cs` (`public static class`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Calculate(IEnumerable<TankbookEntry> entries)` | public static | Sortiert mit `TankbookEntryOrder.Ascending`, führt je `TankbookEntryKind` eine eigene Kette (`lastFull`, `partialSums`) und liefert `IReadOnlyList<TankbookEntryOverview>` aufsteigend (ältester zuerst) |

Verhalten (aus dem Quelltext):
- Teilbetankung: Menge wird nur aufsummiert, wenn für die Art schon ein Vollvorgang existiert; Ergebnis `Partial`, ohne Strecke/Verbrauch.
- Erster Vollvorgang der Art: `NoPreviousFullEntry`.
- Strecke = `OdometerKm` minus `OdometerKm` des vorherigen Vollvorgangs derselben Art; <= 0 ergibt `NoDistance` (Strecke wird als Wert mitgeliefert).
- Verbrauch = `Math.Round((Quantity + partialSums) / distance * 100m, 2, MidpointRounding.AwayFromZero)`; danach Reset der Teilsumme.
- Es gibt weder Kostenberechnung (`TotalAmount` wird nicht ausgewertet), noch Durchschnitt/Aggregation über mehrere Vorgänge, noch Verwendung von `Vehicle.StartConsumption`, noch Monatsgruppierung. Keine Teilbetankung geht bisher mit ihren Kosten in einen Wert ein.
- Aufrufer: ausschließlich `TankbookEntryService.GetEntriesAsync` (`ConsumptionCalculator.Calculate(entries).Reverse().ToList()`), nicht DI-registriert (statisch).

## `TankbookEntryOrder`
Datei: `src/Tankradar.MAUI/Services/Tankbook/TankbookEntryOrder.cs` (`public static class`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Ascending` | public static (Eigenschaft, `IComparer<TankbookEntry>`) | Aufsteigende Ordnung (`OccurredAt`, `OdometerKm`, `Id`) |
| `Compare(TankbookEntry, TankbookEntry)` | public static | Vergleich nach dem Schlüssel; wirft bei `null` |

## `TankbookEntryService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/TankbookEntryService.cs` (`public sealed class`, Umsetzung von `ITankbookEntryService`; Konstruktor `IDbContextFactory<TankradarDbContext>`, `IDatabaseInitializer`, `IReceiptPhotoStore`, `TimeProvider`, `ILogger`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetEntriesAsync` | public | Lädt Vorgänge des Fahrzeugs, rechnet mit `ConsumptionCalculator`, kehrt die Reihenfolge um (neueste zuerst); Foto-Bereinigung beim ersten Aufruf |
| `CreateEntryAsync`, `UpdateEntryAsync`, `DeleteEntryAsync`, `GetKnownStationsAsync` | public | siehe `interfaces.md`; Ergebnis-Typen statt Ausnahmen, Protokoll nur mit Ausnahmetyp |

Weitere Klassen im Ordner `Services/Tankbook/`: `AmountCalculator` (zwei-aus-drei-Rechnung, Rundung Gesamtbetrag 2 / Preis 3 / Menge 2 Nachkommastellen, Toleranz 0,02 €; Rundungsausnahme für den exakten gerundeten Quotienten laut ADR 0009), `TankbookEntryValidator`, `DecimalInputParser`, `VehicleService`, `VehicleValidator`, `VehicleInputParser`, `ReceiptPhotoStore`, `ReceiptPhotoPickerSelector`, `MauiReceiptPhotoPicker`, `TestReceiptPhotoPicker`.

## `ReceiptPhotoStore`
Datei: `src/Tankradar.MAUI/Services/Tankbook/ReceiptPhotoStore.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DetectExtension(string path)` | public static | Erkennt JPEG (`FF D8 FF`), PNG und `ftyp`-Container: Marken `heic/heix/hevc/hevx/heim/heis` ergeben `.heic`, `mif1/msf1` ergeben `.heif`, sonst `null`. Unit-Test nur für `heic` (siehe `tests.md`); die Marke `mif1` ist ungetestet. |

## ViewModels (siehe auch `ui.md`)

### `TankbookEntriesViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookEntriesViewModel.cs` (`sealed`, `BaseViewModel`, `IQueryAttributable`; Transient, DI; abhängig von `IVehicleService`, `ITankbookEntryService`, `IReceiptPhotoStore`, `TankbookEntryFormViewModel`, `ITankbookNavigator`, `IScreenReaderAnnouncer`, `ILogger`)

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `ApplyQueryAttributes` | public | liest `vehicleId` (`long`) und ruft `Show` |
| `Show(long)` | public | setzt Zustand zurück (`Form.Cancel`, `Vehicle`/`Entries` leer, `IsLoaded=false`) |
| `OnAppearing()` | public override | `LastTask = LoadAsync()` |
| `LoadAsync()` | public | lädt Fahrzeug (`GetVehicleAsync`), Vorgänge (`GetEntriesAsync`), bekannte Tankstellen; baut `TankbookEntryItemViewModel`-Liste; Fehler -> `StatusMessage = TankbookEntryTexts.LoadFailed` + Ansage; fehlendes Fahrzeug -> Meldung und `CloseAsync` |
| `DeleteAsync` (private) | private | Löschen; bei Ergebnis `Ok`/`EntryNotFound` Neuladen via `LoadAsync`; sonst `StatusMessage = GetResultMessage(result, null)`; bei Ausnahme `TankbookTexts.SaveFailed` (Test `Delete_StorageFailure_...` deckt Ausnahmepfad ab; der Pfad „Ergebnis ungleich Ok/EntryNotFound“ ist nicht direkt getestet, siehe `tests.md`) |
| `OnFormSaved` (private) | private | abonniert `Form.Saved`, löst `LoadAsync()` aus (Aktualisierung nach Erfassen/Korrigieren) |
| Eigenschaften | public | `Vehicle`, `HasVehicle`, `VehicleName`, `VehicleDetails`, `AddEntryHint`, `Entries`, `HasEntries`, `ShowEmptyHint`, `IsLoaded`, `IsDeleting`, `StatusMessage`, `HasStatusMessage`, `AddEntryCommand`, `Form`, `LastTask` |

Abonnierte Events: `TankbookEntryFormViewModel.Saved`. Publiziert: `PropertyChanged` (über `BaseViewModel`). Es gibt kein Ereignis für „Vorgänge geändert“; Löschen und Speichern laden vollständig neu.

### `TankbookEntryItemViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookEntryItemViewModel.cs` (`sealed`, `BaseViewModel`). Hält `TankbookEntryOverview Overview`; formatiert `DateText`, `TotalText`, `UnitPriceText`, `QuantityText`, `DistanceText` (nur bei `Calculated` und Strecke > 0), `ConsumptionText` (`TankbookEntryTexts.GetConsumptionLine`), `NoteText`, `KindText` (nur bei Hybrid oder unpassender Art), Rückfrage- und Belegzustand, Befehle `EditCommand`, `DeleteCommand`, `ConfirmDeleteCommand`, `CancelDeleteCommand`, `ShowReceiptCommand`.

### `TankbookEntryFormViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookEntryFormViewModel.cs`. Formular zum Erfassen/Korrigieren mit Ereignis `Saved`, Eigenschaften u. a. `IsPartial` (Schalter „Teilbetankung“). Ein erklärender Hinweis zu Teilbetankungen vor dem ersten Vollvorgang ist im Formular nicht vorhanden; vorhanden sind nur die Beschriftung `TankbookEntryTexts.PartialLabel` („Teilbetankung – nicht voll getankt bzw. geladen“) und in der Liste `PartialBadge` sowie der Verbrauchstext „Teilbetankung: zählt beim nächsten vollen Vorgang mit“ (`GetConsumptionLine`, unabhängig davon, ob ein Vollvorgang davor liegt).

### `TankbookViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookViewModel.cs`. Fahrzeugliste und -formular (Seite `TankbookPage`); öffnet über `ITankbookNavigator` die Vorgangsseite.
