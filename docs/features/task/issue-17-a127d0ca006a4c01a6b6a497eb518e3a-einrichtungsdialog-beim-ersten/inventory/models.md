# Datenmodell

## `AppSettings`
Datei: `src/Tankradar.MAUI/Models/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelTypes` | `IReadOnlyList<FuelTypeSelection>` | Spritsorten in Prioritätsreihenfolge |
| `GpsUsage` | `GpsUsage` | Standortnutzung (Standard `WhileInUse`) |
| `ResultView` | `ResultView` | Standardansicht der Ergebnisse (Standard `List`) |
| `ResultSortOrder` | `ResultSortOrder` | Standardsortierung (Standard `Price`) |
| `ShowNearby` | `bool` | Bereich „In der Nähe“ auf der Startseite (Standard `false`) |

Methoden (statisch bzw. Instanz):

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateDefault()` | `public static` | Liefert die Standardwerte; alle Spritsorten ausgewählt, Reihenfolge aus `Enum.GetValues<FuelType>()` |
| `Normalize()` | `public` | Ergänzt fehlende Spritsorten unausgewählt am Ende |
| `NormalizeFuelTypes(IEnumerable<FuelTypeSelection>)` | `public static` | Wie `Normalize`, auf einer Liste; vorhandene Reihenfolge bleibt erhalten |
| `Validate()` | `public` | Wirft `ArgumentException` bei undefiniertem Enum-Wert, doppelter Sorte, keiner ausgewählten Sorte (`"Mindestens eine Spritsorte muss ausgewählt sein."`) oder undefinierter Standort-, Ansicht- bzw. Sortierungsoption |

Die Regel „mindestens eine Sorte“ ist also bereits in `Validate()` enthalten. Der Dialog kann sie wiederverwenden.

## `FuelTypeSelection`
Datei: `src/Tankradar.MAUI/Models/FuelTypeSelection.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelType` | `FuelType` | Die Spritsorte |
| `IsSelected` | `bool` | Auswahlzustand |

Konstruktor: `FuelTypeSelection(FuelType fuelType, bool isSelected)`. Sealed Record.

## `UserSettingsEntity`
Datei: `src/Tankradar.MAUI/Data/UserSettingsEntity.cs`. Tabelle `UserSettings` (konfiguriert in `TankradarDbContext.OnModelCreating`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `SingletonId` | `const int` (1) | Id der einzigen Zeile |
| `Id` | `int` | Primärschlüssel, `ValueGeneratedNever`, Standard `SingletonId` |
| `GpsUsage` | `string` | Name des `GpsUsage`-Werts, Pflichtfeld |
| `ResultView` | `string` | Name des `ResultView`-Werts, Pflichtfeld |
| `ResultSortOrder` | `string` | Name des `ResultSortOrder`-Werts, Pflichtfeld |
| `ShowNearby` | `bool` | Schalter „In der Nähe“, Standard `false` |

Methode: `Update(AppSettings settings)` (öffentlich) übernimmt die skalaren Einstellungen in die Zeile. Die Spritsorten sind nicht Teil dieser Entität.

Hinweis für die Erststart-Erkennung: Die Zeile wird nur durch `SettingsService.SaveAsync` angelegt (siehe [Logik](logic.md)).

## `FuelTypeSettingEntity`
Datei: `src/Tankradar.MAUI/Data/FuelTypeSettingEntity.cs`. Tabelle `FuelTypeSettings`, Primärschlüssel `FuelTypeKey`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelTypeKey` | `string` | Name des `FuelType`-Werts (Primärschlüssel); unbekannte Schlüssel bleiben erhalten |
| `SortOrder` | `int` | Position in der Reihenfolge (0 = oberste) |
| `IsSelected` | `bool` | Auswahlzustand |

## `TankradarDbContext` (Ausschnitt)
Datei: `src/Tankradar.MAUI/Data/TankradarDbContext.cs`

| DbSet | Entität | Hinweis |
|-------|---------|---------|
| `UserSettings` | `UserSettingsEntity` | Einstellungstabelle |
| `FuelTypeSettings` | `FuelTypeSettingEntity` | Spritsortentabelle |

Der Kontext definiert keine weiteren Einstellungsfelder. Bei einer neuen Eigenschaft in `UserSettingsEntity` wäre eine Änderung in `OnModelCreating` und im `TankradarDbContextModelSnapshot` sowie eine Migration nötig.

## Migrationen (Einstellungsbezug)
Verzeichnis: `src/Tankradar.MAUI/Data/Migrations/`

| Migration | Bezug |
|-----------|-------|
| `20261004180805_InitialCreate` | Legt `UserSettings` an |
| `20261004180847_AddFuelTypeSettings` | Legt `FuelTypeSettings` an |
| `20261006124841_AddNearbySetting` | Ergänzt `UserSettings.ShowNearby` |

Die Migrationen enthalten keine Dateneinfügungen für `UserSettings` oder `FuelTypeSettings`. Die einzige Datenaktualisierung per SQL betrifft `TankbookEntries` (Migration `20261011170920_AddTankbookEntryDetails`). Die letzte Migration ist `20261018094636_AddStationCustomName`.
