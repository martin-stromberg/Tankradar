# Datenmodell

## `TankbookEntryEntity`
Datei: `src/Tankradar.MAUI/Data/TankbookEntryEntity.cs` – Tabelle `TankbookEntries`. Laut XML-Doku in Schritt 12 bewusst
minimal; „Schritt 13 ergänzt die übrigen Angaben per eigener Migration“.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Primärschlüssel (Autoincrement) |
| `VehicleId` | `long` | FK auf `Vehicles.Id`, `ON DELETE CASCADE`, Index `IX_TankbookEntries_VehicleId` |
| `CreatedUtc` | `DateTime` | Anlagezeitpunkt (UTC), Pflicht |
| `Vehicle` | `VehicleEntity?` | Navigation |

Es fehlen alle fachlichen Angaben eines Vorgangs (Zeitpunkt, Art, Menge, Preise, Kilometerstand, Teilbetankung,
Tankstelle, Notiz, Belegfoto).

## `VehicleEntity`
Datei: `src/Tankradar.MAUI/Data/VehicleEntity.cs` – Tabelle `Vehicles`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Primärschlüssel |
| `Name` | `string` | Pflicht, Kollation `NOCASE`, eindeutiger Index `IX_Vehicles_Name` |
| `FuelKind` | `string` | Name des Enum-Werts `VehicleFuelKind` |
| `StartOdometerKm` | `int` | Start-Kilometerstand (0–9.999.999) |
| `StartConsumption` | `decimal?` | optionaler Startverbrauch, Spalte `TEXT` |
| `CreatedUtc` | `DateTime` | Anlagezeitpunkt |
| `Entries` | `List<TankbookEntryEntity>` | Vorgänge (Navigation) |

## `StationEntity` (bekannte Tankstellen)
Datei: `src/Tankradar.MAUI/Data/StationEntity.cs` – Tabelle `Stations` (Preis-Cache, Schlüssel = Tankerkönig-ID `string`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Kennung der Quelle (nie anzeigen) |
| `Name`, `Brand` | `string`, `string?` | Anzeigename, Marke |
| `Street`, `HouseNumber`, `PostCode`, `Place` | `string?` | Adresse |
| `Latitude`, `Longitude` | `double` | Position |
| `WholeDay`, `OpeningTimesJson`, `DetailsUpdatedUtc` | – | Detaildaten |
| `PriceEntries` | `List<PriceEntryEntity>` | Preisstände (Cascade) |

Im Code gibt es keine Stelle, die Zeilen aus `Stations` löscht (`PriceRepository`, `FavoritesService` geprüft).

## `FavoriteEntryEntity`
Datei: `src/Tankradar.MAUI/Data/FavoriteEntryEntity.cs` – Tabelle `FavoriteEntries`: `Id`, `GroupId` (Cascade),
`StationId` (FK `Stations`, `Restrict`), `Note`, `Priority`, `AddedUtc`. Eindeutig je (`GroupId`, `StationId`).
Quelle für „bekannte Tankstellen aus den Favoriten“.

## `TankradarDbContext`
Datei: `src/Tankradar.MAUI/Data/TankradarDbContext.cs`

- `DbSet`s: `UserSettings`, `FuelTypeSettings`, `Stations`, `PriceEntries`, `FavoriteGroups`, `FavoriteEntries`,
  `Vehicles`, `TankbookEntries`.
- `DatabaseFileName = "tankatlas.db"`, `GetDatabasePath(dataDirectory)`.
- Dezimalwerte werden als `TEXT` gespeichert (`PriceEntries.Price`, `Vehicles.StartConsumption`).
- `TankbookEntries`: FK Cascade, Index auf `VehicleId`, `CreatedUtc` Pflicht.

## Migrationen
Verzeichnis `src/Tankradar.MAUI/Data/Migrations/` (6 Stück): `InitialCreate`, `AddFuelTypeSettings`, `AddPriceCache`,
`AddFavorites`, `AddNearbySetting`, `20261011105508_AddVehicles` (legt `Vehicles` und `TankbookEntries` an) sowie
`TankradarDbContextModelSnapshot`. Design-Time-Factory `TankradarDbContextFactory` nutzt `Data Source=:memory:`.

## Verbindungszeichenfolge
`MauiProgram.CreateMauiApp`: `options.UseSqlite($"Data Source={TankradarDbContext.GetDatabasePath(dataDirectory)}")` –
**ohne** `Foreign Keys=True`. Die Kaskaden hängen am Standard von `e_sqlite3` (`SQLITE_DEFAULT_FOREIGN_KEYS=1`, siehe
`acceptance-schritt-12.md`). Weitere Verbindungszeichenfolgen: Integration `TestDatabase` (`Data Source=…;Pooling=False`),
E2E `TankbookTestDatabase` (`…;Pooling=False;Default Timeout=5`), Unit `InMemoryDbContextFactory` (`:memory:`).

## Fachmodelle `Models/Tankbook`
Dateien in `src/Tankradar.MAUI/Models/Tankbook/`:

| Typ | Art | Inhalt |
|-----|-----|--------|
| `Vehicle` | `record` | `Id`, `Name`, `FuelKind`, `StartOdometerKm`, `StartConsumption` |
| `VehicleInput` | `record` | `Name`, `FuelKind`, `StartOdometerKm`, `StartConsumption` |
| `VehicleFormValidation` | `record` | Feldfehler `NameError`, `OdometerError`, `ConsumptionError`, `Input?`; `HasErrors` (Form `get { return …; }`) |
| `VehicleSaveResult` | `record` | `Result`, `Vehicle?` |
| `VehicleLimits` | statisch | `MaxNameLength = 40`, `MaxOdometerKm = 9_999_999`, `MaxConsumptionExclusive = 100m`, `MaxConsumptionDecimals = 2` |

Für Vorgänge (Eingabe, Ergebnis, Verbrauch, bekannte Tankstelle) gibt es keine Modelle.

## Belegfotos
Es gibt weder ein Modell noch eine Ablage für Fotos. Im gesamten `src/` gibt es keine Verwendung von `MediaPicker`,
`FilePicker`, `PickPhotoAsync` oder `CapturePhotoAsync`. `Platforms/iOS/Info.plist` enthält nur
`NSLocationWhenInUseUsageDescription` (keine Kamera-/Fotobibliotheks-Beschreibung).
