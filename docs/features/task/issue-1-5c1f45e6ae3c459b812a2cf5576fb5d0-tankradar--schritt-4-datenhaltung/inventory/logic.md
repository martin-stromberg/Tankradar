## `AppDataPathProvider`
Datei: `src/Tankradar.MAUI/Services/AppDataPathProvider.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetDataDirectory()` | public | Liefert `TEST_DATA_PATH` (wenn gesetzt, nicht leer), sonst `FileSystem.AppDataDirectory`; legt das Verzeichnis nicht an |

Konstruktoren: parameterlos (Produktion) und mit `Func<string>` (Test).

## `MauiProgram`
Datei: `src/Tankradar.MAUI/MauiProgram.cs`

`CreateMauiApp()` registriert Singletons `AppConfiguration`, `IAppDataPathProvider`; Transient: vier ViewModels und vier Seiten. Keine EF-/Datenbank-Registrierung.

## `App`
Datei: `src/Tankradar.MAUI/App.xaml.cs`

Konstruktor erhält `AppConfiguration`, startet `LoadFromSettingsFileAsync()` fire-and-forget; `CreateWindow` erzeugt `AppShell`.

## `SettingsViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/SettingsViewModel.cs`

Parameterloser Konstruktor setzt `Title = "Optionen"`; keine Logik. `BaseViewModel.OnAppearing()` ist `virtual` und wird von `TankradarContentPage.OnAppearing` aufgerufen. Kein Command-Mechanismus im Projekt (MAUI-`Command` verfügbar, kein CommunityToolkit).

## `SettingsPage`
Dateien: `src/Tankradar.MAUI/Views/SettingsPage.xaml`, `.xaml.cs`

Platzhalter (`VerticalStackLayout`, Headline `SettingsPage.Headline`, Text „Hier stellst du bald Spritsorten, Standort und Ansicht ein."); Konstruktor erhält `SettingsViewModel` per DI. Nutzt `AppThemeBinding` mit den Farben des Design-Systems.

## Skripte und Hooks (geprüft)

- `scripts/create-review-version.ps1 -Version X.Y.Z [-ChangelogFile ...]` legt `review-versions/<Version>_<Datum>/bin` und `CHANGELOG.md` an (Verzeichnis gitignored). Vorhanden: 0.1.0, 0.1.1, 0.1.2.
- `.githooks/forbidden-patterns-check.py` blockiert `.db`, `.sqlite`, `.sqlite3`, `.sql`; `enum-coverage-check.py` (public/internal Enums, alle Werte in Testdateien); `csproj-xmldoc-check.py`; `translation-check.py` prüft nur `IStringLocalizer`/`.resx` (im Projekt nicht verwendet; UI-Texte stehen direkt in XAML/ViewModels).
- Lokal im NuGet-Cache: `Microsoft.EntityFrameworkCore.Sqlite`, `.Design`, `.Relational`, `Microsoft.Data.Sqlite.Core` 10.0.10 bis 10.0.12, `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10; globales Tool `dotnet-ef` 10.0.10; SDK 10.0.401.
