## `IAppDataPathProvider`
Datei: `src/Tankradar.MAUI/Services/IAppDataPathProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `GetDataDirectory` | – | `string` | Datenverzeichnis (Datenbank, `receipts/`) |
| `GetCacheDirectory` | – | `string` | Cache-Verzeichnis (Kacheln) |

## `IDatabaseInitializer`
| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `InitializeAsync` | – | `Task` | Migration und Dateischutz |

## `IDatabaseFileProtector`
| Methode | Parameter | Rückgabewert | Zweck |
|---|---|---|---|
| `Protect` | `string databasePath` | `void` | Schutz für Datenbankdateien |
| `ProtectFile` | `string filePath` | `void` | Schutz für eine Datei |

## `IReceiptPhotoStore`
Datei: `src/Tankradar.MAUI/Services/Tankbook/IReceiptPhotoStore.cs` — `ImportAsync(Stream, string, CancellationToken)`, `GetFullPath(string)`, `Exists(string)`, `Delete(string)`, `DeleteUnreferenced(IReadOnlySet<string>)`.

## `IReceiptPhotoPicker`
Datei: `src/Tankradar.MAUI/Services/Tankbook/IReceiptPhotoPicker.cs` — `IsCaptureSupported`, `PickAsync`, `CaptureAsync` (liefert `PickedReceiptPhoto?` mit Endung und Stream-Fabrik); Implementierungen `MauiReceiptPhotoPicker`, `TestReceiptPhotoPicker`.

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs` — `SearchNearbyAsync`, `GetStationDetailAsync`, `RefreshStationsAsync`, `GetKnownStationsAsync`, `CheckAvailabilityAsync`; keine Methode zum Verwerfen der Zwischenspeicher.

## `ISettingsService`
`LoadAsync(CancellationToken)` liefert `AppSettings`, `SaveAsync(AppSettings, CancellationToken)`.

## `IScreenReaderAnnouncer`
`Announce(string)` für die Sprachausgabe von Meldungen und Rückfragen.

## Nicht vorhanden
`IBackupService`, `IBackupFilePicker`, `IBackupSharer` und vergleichbare Verträge existieren nicht.
