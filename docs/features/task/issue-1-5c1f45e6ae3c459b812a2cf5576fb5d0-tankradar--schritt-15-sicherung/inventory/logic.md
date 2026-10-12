## `DatabaseInitializer`
Datei: `src/Tankradar.MAUI/Services/DatabaseInitializer.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `InitializeAsync` | public | Einmalig (zwischengespeichert, nach Fehler wiederholbar): Datenverzeichnis anlegen, `MigrateAsync`, `IDatabaseFileProtector.Protect` |

## `AppDataPathProvider`
Datei: `src/Tankradar.MAUI/Services/AppDataPathProvider.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `GetDataDirectory` | public | `TANKATLAS_TEST_DATA_PATH` oder `FileSystem.AppDataDirectory` |
| `GetCacheDirectory` | public | im Testmodus dasselbe Verzeichnis, sonst `FileSystem.CacheDirectory` (Kacheln) |

## `DatabaseFileProtector`
Datei: `src/Tankradar.MAUI/Services/DatabaseFileProtector.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `Protect(databasePath)` | public | iOS: Dateischutz für Verzeichnis, Datenbank, `-wal`, `-shm`; sonst nur Debug-Log |
| `ProtectFile(filePath)` | public | iOS: Dateischutz für eine Datei (Belegfotos) |

## `ReceiptPhotoStore`
Datei: `src/Tankradar.MAUI/Services/Tankbook/ReceiptPhotoStore.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---|---|---|
| `ImportAsync` | public | Foto übernehmen, Signatur prüfen, `ProtectFile` |
| `GetFullPath`, `Exists`, `Delete` | public | Zugriff über validierten Dateinamen |
| `DeleteUnreferenced` | public | löscht Dateien in `receipts/`, die nicht referenziert sind |
| `IsValidFileName`, `DetectExtension` | public static | siehe Datenmodell |

## `FuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

Hält `_searchCache` und `_refreshed` (`ConcurrentDictionary`) im Arbeitsspeicher; innerhalb der Cache-Lebensdauer aktualisierte Stationen werden ohne `force` nicht erneut abgerufen. Es gibt keine Methode zum Verwerfen. Preise liest und schreibt `PriceRepository` über `IDbContextFactory<TankradarDbContext>`.

## `SettingsViewModel` und `SettingsPage`
Dateien: `src/Tankradar.MAUI/ViewModels/SettingsViewModel.cs`, `src/Tankradar.MAUI/Views/SettingsPage.xaml`

`SettingsViewModel(ISettingsService, ILogger, DataSourceViewModel? = null)`; `OnAppearing` ruft `DataSource?.OnAppearing()` und `LoadAsync`; `LoadAsync` setzt `IsLoaded` (UI-Signal `Settings.Loaded`). AutomationIds: `SettingsPage.Headline`, `Settings.Loaded`, `Settings.StatusMessage`, `Settings.FuelType.<Name>.Switch/MoveUp/MoveDown`, `Settings.Gps.<Key>`, `Settings.Nearby.Switch/Hint/GpsHint`, `Settings.View.<Key>`, `Settings.Sort.<Key>`, `Settings.Attribution`, `Settings.PriceService.KeyStatus/Check/Status`. ViewModels werden in `MauiProgram` per `AddTransient` registriert, Dienste per `AddSingleton`.

## `TankbookViewModel`, `VehicleItemViewModel`, `TankbookTexts`
Dateien: `src/Tankradar.MAUI/ViewModels/TankbookViewModel.cs` (`BeginDeleteAsync`, `DeleteAsync`), `VehicleItemViewModel.cs` (`ShowDeleteConfirmation(count)`, `DeleteQuestion`), `Resources/Texts/TankbookTexts.cs`

`TankbookTexts.FormatDeleteQuestion(name, entryCount)` liefert „Fahrzeug „X“ löschen?“ und bei Vorgängen den Zusatz zum endgültigen Mitlöschen; kein Sicherungshinweis. Bestätigung bei Vorgängen: `ConfirmDeleteWithEntries` („Fahrzeug und Vorgänge löschen“).

## Auswertungskacheln
Dateien: `Views/TankbookEntriesPage.xaml` (ca. Zeilen 420–500), `Resources/TankbookStyles.xaml` (`StatisticsTile`), `Resources/DesignSystem.xaml` (`price-hero`: 28 pt, Bold, `InterSemibold`); Werttexte aus dem Statistik-ViewModel (`AverageValueText`, `CostPer100KmValueText`).

## Testmodus-Selektoren (Muster)
`ReceiptPhotoPickerSelector.Create(getEnv, createPlatformPicker)`, `LocationServiceSelector`, `ApiKeyStoreSelector`: im Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) nie die Plattformklasse. `TestDataPaths` (`src/TestSupport/TestDataPaths.cs`) sammelt die Variablennamen (`TANKATLAS_TEST_DATA_PATH`, `TANKATLAS_TEST_RECEIPT_PHOTO`, `TANKATLAS_TEST_LOCATION`, `TANKATLAS_TEST_WINDOW`, `TANKRADAR_PRICE_API_URL` …).

## Plattform
`Platforms/iOS/Info.plist`: `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription`, `NSLocationWhenInUseUsageDescription`, `ITSAppUsesNonExemptEncryption=false`; keine Dokumenttypen, kein `UIFileSharingEnabled`. `Tankradar.MAUI.csproj`: `MtouchInterpreter=all`, `TrimmerRootAssembly` für App, EF Core, `Microsoft.Data.Sqlite`; Pakete `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10, `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12.
