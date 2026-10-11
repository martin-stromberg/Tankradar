# Logik, ViewModels und Oberfläche

## `VehicleService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/VehicleService.cs` – implementiert `IVehicleService`, Singleton in
`MauiProgram`. Abhängigkeiten: `IDbContextFactory<TankradarDbContext>`, `IDatabaseInitializer`, `TimeProvider`,
`ILogger<VehicleService>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetVehiclesAsync` | public | alle Fahrzeuge, im Speicher nach Namen (de-DE, ohne Groß-/Kleinschreibung) sortiert |
| `GetVehicleAsync` | public | ein Fahrzeug oder `null` |
| `CreateVehicleAsync` | public | `VehicleValidator.Validate`, Namensprüfung, Anlage, `VehicleSaveResult` |
| `UpdateVehicleAsync` | public | wie oben, ändert alle Felder; **keine** Prüfung gegen Kilometerstände vorhandener Vorgänge |
| `CountEntriesAsync` | public | Zahl der Vorgänge eines Fahrzeugs |
| `DeleteVehicleAsync(long)` | public | löscht das Fahrzeug, Vorgänge per `ON DELETE CASCADE`; **ohne** erneute Zählung/Prüfung der Vorgangszahl |
| `NameTakenAsync` | private | Namensvergleich im Speicher (`OrdinalIgnoreCase`) |
| `SaveAsync` | private | `SaveChangesAsync`, SQLite-Fehler 2067 → `DuplicateName` |
| `OpenAsync` | private | `IDatabaseInitializer.InitializeAsync()` und neuer Kontext |
| `ToVehicle` | private | Entität → `Vehicle`, unbekannte Kraftstoffart → Benzin + Warnung ohne Namen |

Fachliche Fehler als Enum-Ergebnis, Protokolle nennen nur Ausnahmetypen.

## `VehicleValidator` / `VehicleInputParser`
Dateien: `src/Tankradar.MAUI/Services/Tankbook/VehicleValidator.cs`, `VehicleInputParser.cs` (beide statisch).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `VehicleValidator.Validate(VehicleInput)` | public | Schutzprüfung im Dienst → `VehicleResult` |
| `VehicleValidator.ValidateForm(name, fuelKind, odometerText, consumptionText)` | public | alle Feldfehler gleichzeitig, `VehicleFormValidation` |
| `VehicleValidator.CheckConsumption(decimal)` | public | > 0, < 100, max. 2 Nachkommastellen |
| `VehicleValidator.Normalize(decimal)` | internal | entfernt nachlaufende Nullen |
| `VehicleInputParser.ParseOdometer(text, out int)` | public | ganze Zahl, Tausenderpunkte, `Required`/`NotANumber`/`Negative`/`OutOfRange` |
| `VehicleInputParser.ParseConsumption(text, out decimal?)` | public | leer erlaubt, Komma oder einzelner Punkt, Regex `^-?(\d+([,.]\d+)?|[,.]\d+)$` |

Wiederverwendbar für Kilometerstand und Dezimaleingaben der Vorgänge.

## `TankbookViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/TankbookViewModel.cs` (555 Zeilen) – Seite „Tankbuch“. Abhängigkeiten:
`IVehicleService`, `ILogger`, `IScreenReaderAnnouncer`.

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing` / `LoadAsync` | public | lädt Fahrzeuge, baut `VehicleItemViewModel`s |
| `Vehicles`, `HasVehicles`, `ShowEmptyHint`, `IsLoaded`, `IsSaving`, `IsFormOpen`, `FormTitle`, `Form*`, `*Error`, `StatusMessage` | public | Zustand von Liste und Formular |
| `AddVehicleCommand`, `SaveCommand`, `CancelCommand` | public | Formularbefehle |
| `LastTask` | public | zuletzt gestarteter Vorgang (für Tests) |
| `BeginCreate`, `BeginEdit`, `SaveAsync` | private | Formularablauf |
| `BeginDeleteAsync(item)` | private | `CountEntriesAsync` → `item.ShowDeleteConfirmation(count)` → Ansage |
| `DeleteAsync(item)` | private | ruft direkt `DeleteVehicleAsync(item.Id)`; zählt die Vorgänge **nicht** erneut (Restpunkt 12a) |

## `VehicleItemViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/VehicleItemViewModel.cs`

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Vehicle`, `Id`, `Name`, `Details` | public | Anzeige |
| `EditDescription` („Golf bearbeiten“), `DeleteDescription` („Golf löschen“), `CancelDeleteDescription` („Löschen von „Golf“ abbrechen“), `ConfirmDeleteDescription` | public | Sprachausgabe-Beschreibungen (`SemanticProperties.Description`) |
| `IsConfirmingDelete`, `DeleteQuestion`, `ConfirmDeleteLabel` | public | Rückfrage |
| `EditCommand`, `DeleteCommand`, `ConfirmDeleteCommand`, `CancelDeleteCommand` | public | Befehle |
| `ShowDeleteConfirmation(int entryCount)` | public | setzt Frage, Beschriftung, Beschreibung; die gezeigte Zahl wird nicht gespeichert |
| `HideDeleteConfirmation`, `RefreshCommandStates` | public | – |

## `TankbookTexts`
Datei: `src/Tankradar.MAUI/Resources/Texts/TankbookTexts.cs` – zentrale Oberflächentexte (u. a. `ConfirmDelete = "Ja, löschen"`,
`ConfirmDeleteWithEntries = "Fahrzeug und Vorgänge löschen"`, `Cancel = "Abbrechen"`,
`CancelFormDescription = "Formular schließen, ohne zu speichern"`, `FormatConfirmDeleteDescription` → „Ja, Fahrzeug „X“ löschen“
bzw. „Fahrzeug „X“ und Vorgänge löschen“, `FormatDeleteQuestion`, `GetConsumptionUnit` (Elektro → kWh/100 km),
Fehlertexte, `KilometerUnit`, `LiterConsumptionUnit`, `ElectricConsumptionUnit`, `FormatOdometerInput`).
Die Fehlertexte duzen, die Hilfe siezt.

## `TankbookPage.xaml`
Datei: `src/Tankradar.MAUI/Views/TankbookPage.xaml` – `ScrollView` mit Kopf, Status, Formular (Bereich in der Seite),
Fahrzeugkarten (`BindableLayout`), Rückfrage je Karte, „Fahrzeug hinzufügen“. Styles `TankbookCard`, `TankbookButton`,
`TankbookOutlineButton`, `TankbookDangerButton`, `FuelKindChip`, `UnitFieldBorder` (Rahmen, Fokus 2 px Teal, Fehler rot),
`FieldLabel`, `FieldError`, `UnitLabel`.

WCAG-relevante Stellen (vorgelesener Name = `SemanticProperties.Description`):

| AutomationId | Sichtbar | Description heute |
|---|---|---|
| `Tankbook.Vehicle.DeleteConfirm` | „Ja, löschen“ / „Fahrzeug und Vorgänge löschen“ | `ConfirmDeleteDescription` (beginnt nicht mit dem sichtbaren Text) |
| `Tankbook.Vehicle.DeleteCancel` | „Abbrechen“ | „Löschen von „X“ abbrechen“ |
| `Tankbook.Form.Cancel` | „Abbrechen“ | `CancelFormDescription` |
| `Tankbook.Vehicle.Edit` / `.Delete` | „Bearbeiten“ / „Löschen“ | „X bearbeiten“ / „X löschen“ |

Weitere AutomationIds: `TankbookPage.Headline`, `Tankbook.Status`, `Tankbook.Form.Title|Name|NameError|FuelKind.{Key}|Odometer|
OdometerUnit|OdometerError|Consumption|ConsumptionUnit|ConsumptionError|Save`, `Tankbook.Empty`, `Tankbook.Vehicle.Name|Details|
DeleteQuestion`, `Tankbook.AddVehicle`. Chips nutzen `SemanticProperties.Hint` für „Ausgewählt“ (`ChoiceOptionBase.SelectionHint`).

## Navigation
- `AppShell` (Tabs `favorites`, `map`, `tankbook`, `settings`); registrierte Unterseiten: `ShellStationNavigator.DetailRoute`
  („stationdetail“) und `ShellFavoriteGroupNavigator.GroupRoute` („favoritegroup“, Parameter `groupId`, `IQueryAttributable`
  in `FavoriteGroupViewModel`). Das Tankbuch hat noch keine Unterseite.

## `FavoritesService` (bekannte Tankstellen)
Datei: `src/Tankradar.MAUI/Services/Favorites/FavoritesService.cs` – `GetEntriesAsync(groupId)` liefert `FavoriteEntry`
(`StationId`, `StationName`, `AddressText`, …) je Gruppe; eine gruppenübergreifende Liste aller Favoriten-Tankstellen gibt es nicht.

## Dateischutz
- `DatabaseFileProtector.Protect(databasePath)` (`src/Tankradar.MAUI/Services/DatabaseFileProtector.cs`): iOS setzt
  `NSFileProtectionCompleteUntilFirstUserAuthentication` auf Verzeichnis, `.db`, `-wal`, `-shm`; sonst nur Debug-Log.
  Aufgerufen von `DatabaseInitializer.RunAsync` nach `MigrateAsync`.
- `AppDataPathProvider.GetDataDirectory()/GetCacheDirectory()`: im Testmodus `TANKATLAS_TEST_DATA_PATH`, sonst
  `FileSystem.AppDataDirectory`/`CacheDirectory`.

## Testmodus-Auswahl
`LocationServiceSelector.Create(getEnv, createPlatformService)`: ohne `TANKATLAS_TEST_DATA_PATH` Plattformdienst, sonst
`TestLocationService` aus `TANKATLAS_TEST_LOCATION`. Muster für eine Test-Fotoauswahl. Konstanten in
`src/TestSupport/TestDataPaths.cs` (per `Compile Include … Link` in die App eingebunden).

## Offline-Anzeige (für das E2E-Offline-Szenario)
- `SearchTexts.OfflineBanner = "Offline: keine Verbindung zum Preisdienst."`; `FavoritesViewModel`, `MapViewModel`,
  `StationDetailViewModel` → `OfflineMessage`.
- AutomationIds: `Favorites.OfflineBanner`, `Favorites.Station.Price.{Fuel}`, `Favorites.Station.Age.{Fuel}`,
  `Detail.OfflineBanner`, `Detail.Age.{Fuel}`, `Detail.Price.{Fuel}`, `Favorites.Station.Open`.

## Sonstiges
- `IScreenReaderAnnouncer.Announce(string)` / `SemanticScreenReaderAnnouncer`.
- `ChoiceOptionViewModel<TValue>` für Chip-Auswahl.
- `csproj`: `MtouchInterpreter=all`, `TrimmerRootAssembly` für `Tankradar.MAUI`, EF Core, `Microsoft.Data.Sqlite`;
  `MauiEnableXamlCBindingWithSourceCompilation=true`; Version `0.1.0` im csproj (Zwischenstände setzen die Version über
  `scripts/create-review-version.ps1 -Version`).
- `changes.log`: oberster Eintrag iOS-Hotfix, darunter Schritt 12 (0.1.16). Letzter Zwischenstand `review-versions/0.1.16_2026-10-11/`.
- Dokumentation: `docs/adr/0008-vehicle-management.md` (Folgen für Schritt 13: Start-Kilometerstand nicht über dem ersten
  Vorgang; Umgang mit Wechsel der Kraftstoffart), `docs/help/Tankbuch/` (`index.md`, `beschreibung.md`,
  `ablauf-anwender.md`, `ablauf-technisch.md`; Index sagt „Erfassen … folgt in einer späteren Version“).
