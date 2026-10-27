## `TankbookViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookViewModel.cs`

Heute: Fahrzeugliste mit Anlegen, Bearbeiten und Löschen (Rückfrage mit Zählung). Ist der Tab-Inhalt des Reiters „Tankbuch“ (`TankbookPage`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `TankbookViewModel(IVehicleService, ITankbookNavigator, ILogger, IScreenReaderAnnouncer, IRestoreNotifier)` | `public` | Setzt Titel `TankbookTexts.PageTitle`, abonniert `IRestoreNotifier.Restored`. |
| `OnAppearing()` | `public override` | Löscht Statusmeldung, startet `LoadAsync` über `LastTask`. |
| `LoadAsync()` | `public` | Lädt alle Fahrzeuge über `IVehicleService.GetVehiclesAsync` und erzeugt je ein `VehicleItemViewModel`. |
| `OnRestored(...)` | `private` | Lädt nach einer Wiederherstellung neu. |
| `BeginCreate()` | `private` | Öffnet das Formular „Neues Fahrzeug“. |
| `BeginEdit(VehicleItemViewModel)` | `private` | Öffnet das Formular zum Bearbeiten. |
| `SelectFuelKindSilently(VehicleFuelKind)` / `OnFuelKindSelected(...)` | `private` | Auswahl der Kraftstoffart. |
| `SaveAsync()` | `private` | Speichert Anlegen oder Bearbeiten über `IVehicleService`. |
| `BeginDeleteAsync(VehicleItemViewModel)` | `private` | Zählt Vorgänge über `CountEntriesAsync` und zeigt die Rückfrage; kündigt sie über `IScreenReaderAnnouncer` an. |
| `DeleteAsync(VehicleItemViewModel)` | `private` | Löscht über `DeleteVehicleAsync(id, shownCount)`; behandelt `Ok`, `VehicleNotFound`, `EntryCountChanged`. |
| `ClearFieldErrors()` / `CloseConfirmations(...)` | `private` | Hilfsfunktionen für Formular- und Rückfragenzustand. |
| `OpenEntries(VehicleItemViewModel)` | `private` | Ruft `ITankbookNavigator.OpenEntriesAsync(item.Id)` auf (Einstieg über die Fahrzeugkarte). |

Öffentliche Eigenschaften (Auswahl): `Vehicles`, `HasVehicles`, `ShowEmptyHint` (`IsLoaded && Vehicles.Count == 0`), `IsLoaded`, `IsFormOpen`, `FormName`, `FormOdometer`, `FormConsumption`, `SelectedFuelKind`, `NameError`, `OdometerError`, `ConsumptionError`, `StatusMessage`, `AddVehicleCommand`, `SaveCommand`, `CancelCommand`.

Abonnierte Events: `IRestoreNotifier.Restored` (Neuladen).
Publizierte Events: keine.

Hinweis: `TankbookViewModel` kennt keine „zuletzt gewählte“ Fahrzeug-Kennung. Es gibt keinen Speicher dafür.

## `TankbookEntriesViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookEntriesViewModel.cs`

Implementiert `IQueryAttributable`. Zeigt Kopf, Vorgänge, Formular, Auswertung (`Statistics`) und Export (`Export`) für ein Fahrzeug. Der Fahrzeug-Bezug kommt ausschließlich über den Navigationsparameter `vehicleId` (`ApplyQueryAttributes`) oder über `Show(long)`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `TankbookEntriesViewModel(IVehicleService, ITankbookEntryService, IReceiptPhotoStore, TankbookEntryFormViewModel, TankbookStatisticsViewModel, TankbookExportViewModel, IRestoreNotifier, ITankbookNavigator, IScreenReaderAnnouncer, ILogger)` | `public` | Abonniert `IRestoreNotifier.Restored` und `Form.Saved`; Titel `TankbookEntryTexts.PageTitle`. |
| `ApplyQueryAttributes(IDictionary<string, object>)` | `public` | Liest `vehicleId` (`long`) aus der Navigation und ruft `Show` auf. |
| `Show(long vehicleId)` | `public` | Setzt `_vehicleId`, bricht Formular ab, setzt `Statistics`/`Export` zurück, leert Vorgänge. |
| `OnAppearing()` | `public override` | Startet `LoadAsync`. |
| `LoadAsync()` | `public` | Ohne `VehicleId` kein Effekt. Lädt Fahrzeug (`GetVehicleAsync`), Vorgänge, bekannte Tankstellen; ist das Fahrzeug weg, Meldung und `CloseAsync`. Lädt danach `Statistics`. |
| `CloseAsync(long)` | `private` | Ruft `ITankbookNavigator.CloseAsync(vehicleId)` auf. |
| `BeginCreate()` / `BeginEdit(...)` / `BeginDelete(...)` / `DeleteAsync(...)` | `private` | Vorgangsformular und Löschen einzelner Vorgänge. |
| `OnFormSaved(...)` / `OnRestored(...)` / `CloseConfirmations(...)` | `private` | Ereignisbehandlung und Rückfragenzustand. |

Öffentliche Eigenschaften (Auswahl): `Vehicle`, `HasVehicle`, `VehicleName`, `VehicleDetails`, `AddEntryHint`, `Entries`, `HasEntries`, `ShowEmptyHint`, `IsLoaded`, `IsDeleting`, `StatusMessage`, `AddEntryCommand`, `VehicleId`, `Form`, `Statistics`, `Export`.

Abonnierte Events: `IRestoreNotifier.Restored`, `TankbookEntryFormViewModel.Saved`.
Publizierte Events: keine.

Abhängigkeit: Kein Fahrzeugwechsel vorhanden. Ein Wechsel würde `Show` mit einer anderen Kennung erneut aufrufen; `Statistics`/`Export` werden dabei bereits zurückgesetzt (`Statistics.Reset()`, `Export.Reset()`).

## `VehicleItemViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/VehicleItemViewModel.cs`

Eintrag der Fahrzeugliste (`TankbookViewModel.Vehicles`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `VehicleItemViewModel(Vehicle, Action edit, Action openEntries, Action delete, Action confirmDelete, Func<bool> canConfirmDelete)` | `public` | Erzeugt Befehle und Hinweistexte. |
| `ShowDeleteConfirmation(int entryCount)` | `public` | Setzt Rückfrage, Beschriftung, Hinweis und `ShownEntryCount`. |
| `HideDeleteConfirmation()` | `public` | Blendet die Rückfrage aus. |
| `RefreshCommandStates()` | `public` | Aktualisiert `ConfirmDeleteCommand`. |

Befehle: `EditCommand`, `OpenEntriesCommand` (Einstieg „Tank- und Ladevorgänge“), `DeleteCommand`, `ConfirmDeleteCommand`, `CancelDeleteCommand`.
Hinweis für Sprachausgabe: `VehicleHint` = `TankbookTexts.FormatVehicleHint(Name)`, z. B. `Fahrzeug „Golf“`.

## `ShellTankbookNavigator`
Datei: `src/Tankradar.MAUI/Services/Navigation/ShellTankbookNavigator.cs`

Umsetzung von `ITankbookNavigator` über die Shell.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OpenEntriesAsync(long vehicleId)` | `public` | `Shell.Current.GoToAsync("tankbookentries", { vehicleId })`. |
| `CloseAsync(long vehicleId)` | `public` | Sucht die Reiter-Section mit der Seite des Fahrzeugs; entfernt sie aus dem Stapel bzw. führt `GoToAsync("..")` aus, wenn sie oben liegt. |
| `SelectPages<T>(IEnumerable<T>, Func<T, object?>, long)` | `public static` | Filtert Seiten nach Fahrzeug-Kennung (testbar ohne Shell). |
| `ShowsVehicle(object?, long)` | `private static` | Prüft `BindingContext is TankbookEntriesViewModel { VehicleId }`. |

Konstanten: `EntriesRoute = "tankbookentries"`, `VehicleIdParameter = "vehicleId"`.

Verwendet von: `TankbookViewModel.OpenEntries`, `TankbookEntriesViewModel.CloseAsync`. DI-Registrierung: `src/Tankradar.MAUI/MauiProgram.cs` (`AddSingleton<ITankbookNavigator, ShellTankbookNavigator>`).

Der Stapel-Annahme der Anforderung („beide setzen einen Stapel voraus, der mit der Fahrzeugliste beginnt“) entspricht die Implementierung: `CloseAsync` arbeitet mit dem Stapel des Reiters, `OpenEntriesAsync` legt eine Seite auf den aktuellen Stapel.

## `AppShell` (Shell-Routen)
Dateien: `src/Tankradar.MAUI/AppShell.xaml`, `src/Tankradar.MAUI/AppShell.xaml.cs`

- `AppShell.xaml`: Reiter „Tankbuch“ (`Route="tankbook"`, `ContentTemplate="{DataTemplate views:TankbookPage}"`).
- `AppShell.xaml.cs`: `Routing.RegisterRoute(ShellTankbookNavigator.EntriesRoute, typeof(TankbookEntriesPage))`; blendet beim ersten Start das Einrichtungs-Overlay (`SetupViewModel`, ADR 0017) ein.

## `SetupStatusService`
Datei: `src/Tankradar.MAUI/Services/SetupStatusService.cs`

`IsSetupRequiredAsync` prüft, ob die Zeile `UserSettings` mit `Id = 1` fehlt. Wird von `AppShell.CheckSetupAsync` aufgerufen. Kein Bezug zur Fahrzeugauswahl.

## Ungelesene, aber betroffene Klassen
- `TankbookPage` / `TankbookEntriesPage` (`.xaml` / `.xaml.cs`): Die `.xaml.cs` setzen nur `BindingContext`. `TankbookPage.xaml` enthält die Fahrzeugliste mit den AutomationIds `Tankbook.Vehicle.*`, `Tankbook.Empty`, `Tankbook.AddVehicle`.
- `TankbookTexts` (`src/Tankradar.MAUI/Resources/Texts/TankbookTexts.cs`): `PageTitle = "Tankbuch"`, `NoVehicles`, `Entries = "Tank- und Ladevorgänge"`, `FormatVehicleHint`, `FormatConfirmDeleteHint`, `FormatCancelDeleteHint`.
- `TankbookEntryTexts.PageTitle = "Tank- und Ladevorgänge"`, `VehicleNotFound`.
