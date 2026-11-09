# Logik

## `BackupViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/BackupViewModel.cs`

ViewModel der Karte „Sicherung“ in den Optionen. Erbt von `BaseViewModel`. Wird per `AddTransient<BackupViewModel>()` (`MauiProgram.cs:118`) registriert und in der `SettingsPage` innerhalb der Bindung `Backup` (`SettingsPage.xaml:261`) angezeigt.

Konstruktorparameter: `IBackupService service`, `IBackupFilePicker picker`, `IBackupSharer sharer`, `IRestoreNotifier restoreNotifier`, `IScreenReaderAnnouncer announcer`, `ILogger<BackupViewModel> logger` (alle Pflicht, Zeilen 34–40).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing` | `public override` | `LastTask = LoadAsync()` (Zeile 160). |
| `LoadAsync` | `public` | Lädt die Sicherungsliste über `_service.ListAsync()` (Zeilen 167–179). |
| `CreateAsync` | `public` | Erstellt eine Sicherung (Zeilen 185–214). |
| `BeginRestoreAsync` | `public` | Zeigt die Rückfrage zur Wiederherstellung (Zeilen 221–237). |
| `ConfirmRestoreAsync` | `public` | Stellt nach Bestätigung wieder her. Bei `BackupResult.Ok`: `SetStatus(Restored)`, `LoadAsync()`, dann **beide** Benachrichtigungswege — `Restored?.Invoke(this, EventArgs.Empty)` (Zeile 259) und `_restoreNotifier.NotifyRestored()` (Zeile 260). Bei anderen Ergebnissen nur Statusmeldung (Zeilen 244–279). |
| `BeginDelete` | `public` | Rückfrage zum Löschen (Zeilen 285–297). |
| `ConfirmDeleteAsync` | `public` | Löscht nach Bestätigung (Zeilen 304–330). |
| `ShareAsync` | `public` | Weitergabe über die Systemfunktion (Zeilen 337–366). |
| `ImportAsync` | `public` | Datei einspielen und Rückfrage zeigen (Zeilen 372–425). |
| `ShowRestoreQuestionAsync` | `private` | Prüft die Sicherung per `InspectAsync` und zeigt die Rückfrage (Zeilen 427–458). |
| `TryBeginOperation` / `EndOperation` | `private` | `IsBusy`-Verwaltung (Zeilen 460–478). |
| `CloseQuestions` / `RefreshCommandStates` / `ApplyList` / `SetStatus` | `private` | Hilfsmethoden (Zeilen 480–523). |

Publizierte Events:
- `event EventHandler? Restored` (Zeilen 53–56) — eigenes Ereignis; einziger Abonnent im Produktivcode ist `SettingsViewModel` (`SettingsViewModel.cs:45`). Wird in `ConfirmRestoreAsync` (Zeile 259) ausgelöst.
- `_restoreNotifier.NotifyRestored()` → `IRestoreNotifier.Restored` (Zeile 260) — zentraler Benachrichtigungsdienst; Abonnenten `TankbookViewModel`, `TankbookEntriesViewModel`.

Abonnierte Events: keine.

## `SettingsViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/SettingsViewModel.cs`

ViewModel des Bereichs „Optionen“. Erbt von `BaseViewModel`. Registriert per `AddTransient<SettingsViewModel>()` (`MauiProgram.cs:126`), per DI in `SettingsPage` injiziert (`SettingsPage.xaml.cs:14`).

Konstruktorparameter (Zeilen 31–36): `ISettingsService settingsService`, `ILogger<SettingsViewModel> logger` (Pflicht); `DataSourceViewModel? dataSource = null`, `BackupViewModel? backup = null`, `RouteSearchSettingsViewModel? routeSearch = null` (optional).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor | `public` | Setzt `Backup = backup`; abonniert `backup.Restored += OnBackupRestored`, **nur wenn `backup is not null`** (Zeilen 43–46). Kein `IRestoreNotifier`. |
| `OnAppearing` | `public override` | Leitet an `DataSource?.OnAppearing()`, `Backup?.OnAppearing()`, `RouteSearch?.OnAppearing()` weiter und startet `_ = LoadAsync()` (Zeilen 219–225). |
| `OnDisappearing` | `public override` | Leitet an `RouteSearch?.OnDisappearing()` weiter (Zeilen 230–233). |
| `LoadAsync` | `public` | Lädt die Einstellungen über `_settingsService.LoadAsync()` (semaphor-geschützt über `_gate`, Zeilen 239–274). |
| `OnBackupRestored` | `private` | Handler des `BackupViewModel.Restored`-Ereignisses: `_ = LoadAsync()` — Feuer-und-Vergiss ohne `LastTask`-Verfolgung (Zeilen 276–279). |
| `SaveAsync` | `private` | Speichert die Einstellungen sofort (Zeilen 384–408). |
| `CreateOptions` / `ApplySelection` / `ApplyFuelTypes` / `UpdateMoveFlags` / `OnOptionSelected` / `UpdateNearbyGpsHint` / `OnFuelTypeSelectionChanged` / `OnFuelTypeMoved` | `private` | Options- und Spritsorten-Verwaltung. |

Abonnierte Events:
- `BackupViewModel.Restored` → `OnBackupRestored` (Zeile 45), gekoppelt an das Vorhandensein der Karte „Sicherung“.

Publizierte Events: keine.

Besitzt kein `LastTask`-Feld (nur `LastSaveTask`, Zeile 184) — das Neuladen nach Wiederherstellung ist daher in Tests nur indirekt beobachtbar (Ladezähler/Warten auf `IsLoaded`).

## `TankbookViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookViewModel.cs`

ViewModel der Fahrzeugverwaltung. Erbt von `BaseViewModel`, implementiert `IQueryAttributable`. Registriert per `AddTransient<TankbookViewModel>()` (`MauiProgram.cs:103`).

Konstruktorparameter (Zeile 46): `IVehicleService service`, `ILogger<TankbookViewModel> logger`, `IScreenReaderAnnouncer announcer`, `IRestoreNotifier restoreNotifier` (alle Pflicht).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor | `public` | Abonniert `restoreNotifier.Restored += OnRestored` (Zeile 51) — Referenzmuster für die Vereinheitlichung. |
| `OnRestored` | `private` | `LastTask = LoadAsync()` (Zeilen 385–388) — Instanzmethode (schwache Referenz des `WeakEventManager`). |

Abonnierte Events: `IRestoreNotifier.Restored`.

## `TankbookEntriesViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookEntriesViewModel.cs`

ViewModel des Tabs „Tankbuch“ (Vorgänge). Erbt von `BaseViewModel`. Registriert per `AddTransient<TankbookEntriesViewModel>()` (`MauiProgram.cs:104`).

Konstruktorparameter (Zeilen 51–62): `IVehicleService`, `ITankbookEntryService`, `IReceiptPhotoStore`, `TankbookEntryFormViewModel`, `TankbookStatisticsViewModel`, `TankbookExportViewModel`, `IRestoreNotifier restoreNotifier`, `ITankbookNavigator`, `IScreenReaderAnnouncer`, `IVehicleSelectionStore`, `ILogger<TankbookEntriesViewModel>` (alle Pflicht).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor | `public` | Abonniert `restoreNotifier.Restored += OnRestored` (Zeile 74) und `Form.Saved += OnFormSaved` (Zeile 75). |
| `OnRestored` | `private` | `_selectionStore.Clear(); LastTask = LoadAsync()` (Zeilen 567–571). |
| `OnFormSaved` | `private` | `LastTask = LoadAsync()` (Zeilen 562–565). |

Abonnierte Events: `IRestoreNotifier.Restored`, `TankbookEntryFormViewModel.Saved`.

## `RestoreNotifier`
Datei: `src/Tankradar.MAUI/Services/Backup/RestoreNotifier.cs`

Einzige Umsetzung von `IRestoreNotifier`; `sealed`. Hält Abonnenten schwach über `WeakEventManager` (Feld `_manager`, Zeile 11). Hinweis im Klassenkommentar (Zeilen 4–5): Abonnenten müssen Instanzmethoden verwenden, da ein Lambda sonst vorzeitig freigegeben würde.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `Restored` (Ereignis) | `public` | `add`/`remove` über `_manager.AddEventHandler`/`RemoveEventHandler` mit `EventName = nameof(Restored)` (Zeilen 15–19). |
| `Generation` | `public` (get) | Zähler der gemeldeten Wiederherstellungen (Zeilen 22–25). |
| `NotifyRestored` | `public` | `Interlocked.Increment(ref _generation)` + `_manager.HandleEvent(this, EventArgs.Empty, EventName)` (Zeilen 28–32). |

## `BackupItemViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/BackupItemViewModel.cs`

Eintrag der Sicherungsliste; `sealed`, erbt von `BaseViewModel`. Erhält die Aktionen als Delegaten im Konstruktor (Zeilen 29–38) — erzeugt in `BackupViewModel.ApplyList` (`BackupViewModel.cs:499–517`).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `RestoreCommand` | `public` | `Command` → `restore(this)` → `BeginRestoreAsync` (Zeile 48). |
| `ConfirmRestoreCommand` | `public` | `Command` → `confirmRestore(this)` → `ConfirmRestoreAsync` (Zeile 49). |
| `CancelRestoreCommand` | `public` | `Command` → `HideRestoreQuestion` (Zeile 53). |
| `ShareCommand` / `DeleteCommand` / `ConfirmDeleteCommand` / `CancelDeleteCommand` | `public` | Teilen/Löschen-Befehle. |
| `ShowRestoreQuestion` / `HideRestoreQuestion` / `ShowDeleteQuestion` / `HideDeleteQuestion` | `public` | Rückfragen im Eintrag ein-/ausblenden (Zeilen 223–256). |
| `Info` | `public` | Die Sicherung (`BackupFileInfo`), u. a. `FileName` für `RestoreAsync`. |

## `MauiProgram` (DI-Registrierung)
Datei: `src/Tankradar.MAUI/MauiProgram.cs`

| Registrierung | Zeile | Lebensdauer |
|---------------|-------|-------------|
| `AddSingleton<IRestoreRecovery, RestoreRecovery>()` | 84 | Singleton |
| `AddSingleton<IRestoreNotifier, RestoreNotifier>()` | 85 | Singleton — steht zur Laufzeit immer bereit |
| `AddSingleton<IBackupService, BackupService>()` | 86 | Singleton |
| `AddTransient<TankbookViewModel>()` | 103 | Transient |
| `AddTransient<TankbookEntriesViewModel>()` | 104 | Transient |
| `AddTransient<BackupViewModel>()` | 118 | Transient |
| `AddTransient<SettingsViewModel>()` | 126 | Transient — Konstruktorparameter werden vom Container versorgt |

## `SettingsPage` / `TankradarContentPage`
Dateien: `src/Tankradar.MAUI/Views/SettingsPage.xaml.cs`, `src/Tankradar.MAUI/Views/TankradarContentPage.cs`

- `SettingsPage` erhält `SettingsViewModel` per Konstruktorinjektion und setzt es als `BindingContext` (Zeile 14); die Karte „Sicherung“ bindet über `{Binding Backup}` (`SettingsPage.xaml:261`).
- `TankradarContentPage` leitet `OnAppearing`/`OnDisappearing` der Seite an das `BaseViewModel` im `BindingContext` weiter (`TankradarContentPage.cs:12–22`).
