## Datenbankschema (`TankradarDbContext`)
Datei: `src/Tankradar.MAUI/Data/TankradarDbContext.cs`; Migrationen unter `src/Tankradar.MAUI/Data/Migrations/` (`InitialCreate`, `AddFuelTypeSettings`, `AddPriceCache`, `AddFavorites`, `AddNearbySetting`, `AddVehicles`, `AddTankbookEntryDetails`, `TankradarDbContextModelSnapshot`).

| Tabelle / Entität | Inhalt | Besonderheit |
|---|---|---|
| `UserSettings` / `UserSettingsEntity` | `GpsUsage`, `ResultView`, `ResultSortOrder`, `ShowNearby` (Singleton `Id = 1`) | Präferenz „Standortnutzung“, kein Standort |
| `FuelTypeSettings` / `FuelTypeSettingEntity` | Spritsorten mit `SortOrder`, `IsSelected` | |
| `Stations` / `StationEntity` | `Id`, `Name`, `Brand`, Adresse, `Latitude`, `Longitude`, `WholeDay`, `OpeningTimesJson`, `DetailsUpdatedUtc` | öffentliche Stammdaten; Ziel des Fremdschlüssels aus Favoriten (`Restrict`) |
| `PriceEntries` / `PriceEntryEntity` | `StationId`, `FuelTypeKey`, `Price`, `RetrievedUtc` | Preisstände mit Zeitstempel; Kaskade von der Station |
| `FavoriteGroups` / `FavoriteGroupEntity` | Gruppen (Name NOCASE, eindeutig) | |
| `FavoriteEntries` / `FavoriteEntryEntity` | `GroupId`, `StationId`, `Priority`, Notiz | Kaskade von der Gruppe |
| `Vehicles` / `VehicleEntity` | Name, `FuelKind`, `StartOdometerKm`, `StartConsumption`, `CreatedUtc` | |
| `TankbookEntries` / `TankbookEntryEntity` | Vorgänge inkl. Verweis auf Belegfoto (Dateiname in `receipts/`) | Kaskade vom Fahrzeug |

Verbindungszeichenfolge: `TankradarDbContext.BuildConnectionString(path, pooling)` mit `Foreign Keys=True`; Pfad über `GetDatabasePath(dataDirectory)`.

## `ReceiptPhotoStore` (Dateiablage)
Datei: `src/Tankradar.MAUI/Services/Tankbook/ReceiptPhotoStore.cs`

| Eigenschaft | Beschreibung |
|---|---|
| `DirectoryName` = `receipts` | Unterverzeichnis im Datenverzeichnis |
| `IsValidFileName` | `<Guid N>` plus erlaubte Endung aus `TankbookEntryLimits.ReceiptPhotoExtensions`; schützt vor Pfadangaben |
| `DetectExtension` | Erkennung von JPEG/PNG/HEIC/HEIF an der Signatur |
| `TankbookEntryLimits.MaxReceiptPhotoBytes` | Obergrenze je Foto |
