# Interfaces

## `ISettingsService`
Datei: `src/Tankradar.MAUI/Services/ISettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `LoadAsync` | `CancellationToken cancellationToken = default` | `Task<AppSettings>` | Lädt die Einstellungen; fehlende oder ungültige Werte werden durch Standardwerte ersetzt. |
| `SaveAsync` | `AppSettings settings`, `CancellationToken cancellationToken = default` | `Task` | Speichert atomar; wirft `ArgumentException` bei ungültigen Einstellungen. |

Implementierung: `SettingsService` (`src/Tankradar.MAUI/Services/SettingsService.cs`, Singleton).
Testdouble: `FakeSettingsService` (`src/Tankradar.Tests.Unit/Unit/Support/FakeSettingsService.cs`) hält den Stand im Speicher, protokolliert `LoadCount` und `Saved` und kann Fehler auslösen (`LoadException`, `SaveException`).

Die Anforderung nennt eine mögliche Erweiterung um eine Abfrage „Einrichtung nötig?“. Eine solche Methode existiert nicht.

## `IDatabaseInitializer`
Datei: `src/Tankradar.MAUI/Services/IDatabaseInitializer.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InitializeAsync` | keine | `Task` | Stellt Datenbank und Schema sicher; mehrfache Aufrufe teilen sich einen Lauf. |

Implementierung: `DatabaseInitializer` (`src/Tankradar.MAUI/Services/DatabaseInitializer.cs`, Singleton).
Aufrufer: `SettingsService` (vor jedem Zugriff), `App` (Start im Hintergrund), `FavoritesService`, `BackupService`, `PriceRepository`.
