# Logik

## `SettingsService`
Datei: `src/Tankradar.MAUI/Services/SettingsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(CancellationToken)` | `public` | Ruft `IDatabaseInitializer.InitializeAsync` auf, liest `UserSettings` (Id 1) und `FuelTypeSettings` (sortiert nach `SortOrder`). Fehlende Werte ersetzen die Standardwerte aus `AppSettings.CreateDefault()`. Eine fehlende Zeile wird nicht als Erststart erkannt. |
| `SaveAsync(AppSettings, CancellationToken)` | `public` | Normalisiert und validiert (`Validate` wirft bei Verstoß), legt bei Bedarf die `UserSettings`-Zeile an, übernimmt Skalare und schreibt Spritsorten-Zeilen mit `SortOrder` und `IsSelected`. |
| `MapFuelTypes(rows, defaults)` | `private static` | Übersetzt Zeilen in `FuelTypeSelection`; unbekannte Schlüssel und Duplikate werden verworfen. Ohne Zeilen oder ohne ausgewählte Sorte gelten die Standardwerte. |
| `ParseOrDefault<TEnum>(text, fallback)` | `private static` | Parst Enum-Namen; ungültige Werte liefern den Standard. |

Abonnierte Events: keine.
Publizierte Events: keine.

Wichtig für die Erststart-Erkennung: `SaveAsync` ist die einzige Stelle, die eine `UserSettings`-Zeile anlegt. `LoadAsync` schreibt nicht.

Aufrufer von `ISettingsService`:

- `SettingsViewModel` ruft `LoadAsync` (Laden) und `SaveAsync` (Speichern je Änderung) auf.
- `FavoritesViewModel` ruft `LoadAsync` auf (zweimal, Zeilen 282 und 321).
- `MapViewModel` ruft `LoadAsync` auf (Zeile 866).
- `StationDetailViewModel` ruft `LoadAsync` auf (Zeile 478).

## `SettingsViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/SettingsViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor `SettingsViewModel(ISettingsService, ILogger, DataSourceViewModel?, BackupViewModel?, RouteSearchSettingsViewModel?)` | `public` | Initialisiert Optionen mit `CreateDefault()`; abonniert `Backup.Restored`. |
| `OnAppearing()` | `public override` | Startet Karten und `LoadAsync` (ohne Speichern). |
| `OnDisappearing()` | `public override` | Meldet das Verlassen an `RouteSearch`. |
| `LoadAsync()` | `public async` | Lädt Einstellungen, übernimmt sie ohne Speichern und setzt `IsLoaded`. |
| `OnBackupRestored(object?, EventArgs)` | `private` | Lädt nach einer Wiederherstellung neu. |
| `CreateOptions<TValue>(Func<TValue,string>)` | `private` | Erzeugt Auswahloptionen je Enum. |
| `ApplySelection<TValue>(options, selected)` | `private` | Setzt die Auswahl einer Optionsgruppe. |
| `ApplyFuelTypes(IReadOnlyList<FuelTypeSelection>)` | `private` | Füllt `FuelTypes` mit `FuelTypeItemViewModel`. |
| `UpdateMoveFlags()` | `private` | Aktualisiert `CanMoveUp` / `CanMoveDown`. |
| `OnOptionSelected<TValue>(selected, group)` | `private` | Wählt in einer Gruppe genau eine Option und speichert. |
| `UpdateNearbyGpsHint()` | `private` | Zeigt den Hinweis, dass „In der Nähe“ bei `GpsUsage.Never` nicht wirkt. |
| `OnFuelTypeSelectionChanged(FuelTypeItemViewModel)` | `private` | Verarbeitet das Umschalten einer Sorte; laut Testname `DeselectLastFuel_IsRevertedWithMessageAndNotSaved` wird die letzte ausgewählte Sorte zurückgesetzt und mit Meldung nicht gespeichert. |
| `OnFuelTypeMoved(FuelTypeItemViewModel, int)` | `private` | Verschiebt eine Sorte um eine Position und speichert. |
| `SaveAsync()` | `private async` | Baut `AppSettings` aus der Oberfläche und ruft `ISettingsService.SaveAsync`; Fehler setzen `StatusMessage`. |

Abonnierte Events: `BackupViewModel.Restored` (→ `OnBackupRestored`).
Publizierte Events: keine (Zustandsänderungen über `INotifyPropertyChanged`).

Öffentliche Properties mit Bezug zur Spritsortenauswahl: `FuelTypes` (`ObservableCollection<FuelTypeItemViewModel>`), `FuelTypesHeading`, `FuelTypesHint`, `StatusMessage`, `HasStatusMessage`, `LastSaveTask`.

## `FuelTypeItemViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/FuelTypeItemViewModel.cs`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| Konstruktor `FuelTypeItemViewModel(FuelType, bool, Action<FuelTypeItemViewModel>, Action<FuelTypeItemViewModel,int>)` | `public` | Erstellt eine Zeile; `DisplayName` aus `SettingsTexts.GetLabel`, `AutomationKey` = Enum-Name. |
| `IsSelected` (Setter) | `public` | Bei Änderung wird der Callback `onSelectionChanged` aufgerufen. |
| `MoveUpCommand` / `MoveDownCommand` | `public` | Rufen `onMove` mit -1 bzw. 1 auf. |
| `CanMoveUp` / `CanMoveDown` | `public` (`internal` setter) | Steuerung der Verschiebe-Schaltflächen. |

Abonnierte Events: keine. Publizierte Events: keine.

## `DatabaseInitializer`
Datei: `src/Tankradar.MAUI/Services/DatabaseInitializer.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync()` | `public` | Startet die Initialisierung einmalig (Cache im Feld `_initialization`, Lock `_gate`). |
| `RunAsync()` | `private` | Legt das Datenverzeichnis an, ruft `IRestoreRecovery.RecoverIfNeeded`, führt `context.Database.MigrateAsync()` aus und setzt den Dateischutz. Nach einem Fehler wird der Cache zurückgesetzt. |

Abonnierte Events: keine. Publizierte Events: keine.

Aufrufer: `SettingsService`, `FavoritesService`, `BackupService`, `PriceRepository` und `App`.

## `App`
Datei: `src/Tankradar.MAUI/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor `App(AppConfiguration, IDatabaseInitializer, ILogger<App>)` | `public` | Lädt die Konfiguration nach und startet `InitializeDatabaseAsync` ohne Warten (Fire-and-forget). |
| `CreateWindow(IActivationState?)` | `protected override` | Erzeugt `new Window(new AppShell())`. Eine Prüfung des Startzustands gibt es nicht. |
| `InitializeDatabaseAsync(IDatabaseInitializer)` | `private async` | Ruft `InitializeAsync` auf und protokolliert Fehler. |
| `OnRequestedThemeChanged(object?, AppThemeChangedEventArgs)` | `private` | Aktualisiert `CurrentTheme`. |

Abonnierte Events: `RequestedThemeChanged`. Publizierte Events: keine.

Hinweis: Die Startlogik für eine Einrichtung müsste hier oder in `AppShell` ansetzen. Heute wird vor dem Fenster keine Einstellung geladen.

## `AppShell`
Datei: `src/Tankradar.MAUI/AppShell.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor `AppShell()` | `public` | Registriert die Routen `StationDetailPage`, `FavoriteGroupPage`, `TankbookEntriesPage`; abonniert `Navigated`. |
| `OnNavigated(object?, ShellNavigatedEventArgs)` | `private` | Setzt den Titel aus der aktuellen Seite. |

Die Shell ist eine TabBar mit vier Bereichen (Favoriten, Karte, Tankbuch, Optionen). Es gibt keine Erststart-Route.

## DI-Registrierung
Datei: `src/Tankradar.MAUI/MauiProgram.cs`

| Registrierung | Lebensdauer | Zeile |
|---------------|-------------|-------|
| `IDatabaseInitializer` → `DatabaseInitializer` | Singleton | 67 |
| `ISettingsService` → `SettingsService` | Singleton | 68 |
| `SettingsViewModel` | Transient | 120 |
| `SettingsPage` | Transient | 126 |
