# Interfaces

## `IVehicleService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/IVehicleService.cs` – implementiert von `VehicleService`, genutzt von
`TankbookViewModel`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetVehiclesAsync` | `CancellationToken` | `Task<IReadOnlyList<Vehicle>>` | Fahrzeuge nach Namen |
| `GetVehicleAsync` | `long vehicleId`, `CancellationToken` | `Task<Vehicle?>` | ein Fahrzeug |
| `CreateVehicleAsync` | `VehicleInput`, `CancellationToken` | `Task<VehicleSaveResult>` | anlegen |
| `UpdateVehicleAsync` | `long vehicleId`, `VehicleInput`, `CancellationToken` | `Task<VehicleResult>` | ändern |
| `CountEntriesAsync` | `long vehicleId`, `CancellationToken` | `Task<int>` | Zahl der Vorgänge |
| `DeleteVehicleAsync` | `long vehicleId`, `CancellationToken` | `Task<VehicleResult>` | löschen inkl. Vorgänge |

## `IFavoritesService`
Datei: `src/Tankradar.MAUI/Services/Favorites/IFavoritesService.cs` – u. a. `GetGroupsAsync`, `GetEntriesAsync(long groupId)`
→ `IReadOnlyList<FavoriteEntry>` (`StationId`, `StationName`, `AddressText`, `Note`, `Priority`), `GetOverviewAsync`. Keine
Methode für alle Favoriten-Tankstellen über Gruppen hinweg.

## `IDatabaseFileProtector`
Datei: `src/Tankradar.MAUI/Services/IDatabaseFileProtector.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Protect` | `string databasePath` | `void` | Verzeichnis, Datenbank und Journaldateien schützen (iOS) |

Fake: `FakeDatabaseFileProtector` (Unit, `ProtectedPaths`, `OnProtect`).

## `IAppDataPathProvider`
Datei: `src/Tankradar.MAUI/Services/IAppDataPathProvider.cs` – `GetDataDirectory()`, `GetCacheDirectory()`.

## `IDatabaseInitializer`
Datei: `src/Tankradar.MAUI/Services/IDatabaseInitializer.cs` – `InitializeAsync()` (einmalig `MigrateAsync` + Dateischutz).

## `IScreenReaderAnnouncer`
Datei: `src/Tankradar.MAUI/Services/IScreenReaderAnnouncer.cs` – `Announce(string text)`.

## `IFavoriteGroupNavigator` / `IStationNavigator`
Dateien: `src/Tankradar.MAUI/Services/Navigation/` – Muster für Shell-Navigation mit Parametern
(`OpenGroupAsync(long)`, `CloseAsync()`; `OpenDetailAsync(StationListItem)`).

## `ILocationService`
Datei: `src/Tankradar.MAUI/Services/Location/ILocationService.cs` – Plattform-/Testimplementierung per
`LocationServiceSelector` (Vorbild für eine austauschbare Fotoauswahl).

Ein Interface für Vorgänge, Fotoablage oder Fotoauswahl existiert nicht.
