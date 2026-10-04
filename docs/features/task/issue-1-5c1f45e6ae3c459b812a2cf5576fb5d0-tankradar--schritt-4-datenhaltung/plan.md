# Umsetzungsplan: Lokale Datenhaltung und Einstellungen (Schritt 4)

## Übersicht

Die App erhält eine lokale SQLite-Datenbank (`tankatlas.db`) über Entity Framework Core 10.0.10 im Hauptprojekt `src/Tankradar.MAUI` (Ordner `Data/`). Beim App-Start läuft `MigrateAsync`; unter iOS wird der Dateischutz gesetzt. Als erste Nutzerdaten werden die vier Einstellungsgruppen über `ISettingsService` persistiert, und die Platzhalterseite „Optionen" (`SettingsPage`/`SettingsViewModel`) wird zur funktionalen Einstellungsseite mit sofortigem Speichern. Zusätzlich: `<Product>Tankatlas</Product>`, README-Stand, Doku unter `docs/help/Einstellungen/`, `changes.log`, Windows-Zwischenstand 0.1.3.

Grundlage ist die Bestandsaufnahme: Es gibt weder DbContext noch Pakete, Enums, Modelle oder Auswahl-UI; `IAppDataPathProvider`, `TestDataContext`, `E2ETestBase` und das Design-System (Farben, `space-*`, `MinimumTouchTarget`, Label-Styles) sind wiederzuverwenden. Ein bestehendes UI-Muster für Auswahlsteuerelemente gibt es nicht (Designentwurf enthält keinen Optionen-Screen); die Seite wird deshalb schlicht aus Standard-MAUI-Steuerelementen (`Switch`, `RadioButton`, `Button`) mit den vorhandenen Design-System-Ressourcen aufgebaut. Alle Auswahlen erfolgen in Klartext (keine Kennungen).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Persistenz der Einstellungen | Zwei typisierte Tabellen: `UserSettings` (eine Zeile, `Id` = 1, Spalten `GpsUsage`, `ResultView`, `ResultSortOrder`) und `FuelTypeSettings` (eine Zeile je Spritsorte: `FuelTypeKey` PK, `SortOrder`, `IsSelected`) statt Zeile-pro-Schlüssel | Typisierung und Schema-Migration statt Stringschlüsseln; Auswahl und Reihenfolge der Spritsorten sind eine Liste und passen nicht in eine Einzelzeile. Neue Spritsorten werden ohne Migration als zusätzliche Zeilen ergänzt |
| Enum-Speicherung | Enum-Namen als Text (`HasConversion<string>()` nicht nötig: Entität hält `string`, Mapping per `Enum.TryParse`) | Robust gegen spätere Enum-Erweiterung und Umsortierung; unbekannte oder ungültige Werte fallen auf den Standardwert zurück, unbekannte Spritsorten-Schlüssel bleiben unangetastet erhalten (Datenverlustschutz bei Auf-/Abwärtswechsel). Die Enum-Namen sind damit ein Persistenzvertrag und werden durch einen Test festgeschrieben |
| Standardwerte | Keine Seed-Daten in Migrationen; `SettingsService.LoadAsync` liefert `AppSettings.CreateDefault()`, wenn Zeilen fehlen; erste Speicherung legt Zeilen an | Standardwerte liegen an einer Stelle im Code und können später geändert werden, ohne Daten zu überschreiben |
| Zugriffsschicht | Service Layer (`SettingsService`) mit Data Mapper zwischen Entitäten (`UserSettingsEntity`, `FuelTypeSettingEntity`) und Domänenmodell (`AppSettings`, unveränderlicher Record); Speichern nach dem Load-and-Patch-Muster aus dem Skill (Zeilen laden, per `Update`-Methode der Entität patchen, ein `SaveChangesAsync`) | Skill `entityframework-database`; ViewModels sehen keine EF-Typen. Die `Synchronize`-Hilfe entfällt, da nichts entfernt wird (alle `FuelType`-Werte bleiben immer als Zeilen vorhanden) |
| DbContext-Lebensdauer | `AddDbContextFactory<TankradarDbContext>` (Singleton-Factory, kurzlebige Kontexte je Operation); Connection-String ausschließlich aus `IAppDataPathProvider.GetDataDirectory()` + `TankradarDbContext.DatabaseFileName` | MAUI-Apps haben keinen Request-Scope; Pfad-Isolation für Tests/E2E über `TEST_DATA_PATH` ist damit automatisch gegeben |
| Initialisierung | `IDatabaseInitializer.InitializeAsync()` mit zwischengespeichertem Task (bei Fehler wird zurückgesetzt, Wiederholung möglich): Verzeichnis anlegen, `Database.MigrateAsync()`, Dateischutz setzen. `App` stößt sie fire-and-forget an (Muster von `AppConfiguration.LoadFromSettingsFileAsync`); `SettingsService` wartet vor jedem Zugriff auf denselben Task | Migrationslauf liegt garantiert vor dem ersten Lesen/Schreiben der Einstellungen, ohne den UI-Thread beim Start zu blockieren; Fehler werden nicht verschluckt, sondern im ViewModel angezeigt |
| iOS Data Protection | `IDatabaseFileProtector`/`DatabaseFileProtector`: unter `#if IOS` wird `NSFileProtectionCompleteUntilFirstUserAuthentication` über `NSFileManager.SetAttributes` auf das Datenverzeichnis sowie `tankatlas.db`, `-wal` und `-shm` (soweit vorhanden) gesetzt; alle anderen Plattformen: keine Aktion | Die Stufe „CompleteUntilFirstUserAuthentication" schützt die Daten bis zur ersten Entsperrung und erlaubt spätere Hintergrundzugriffe (Preisabruf); „Complete" würde den Zugriff bei gesperrtem Gerät blockieren. Plattformcode über `#if IOS` im Hauptprojekt (Single-Project), da kein separater Plattformordner nötig ist |
| iOS-Entitlement / Info.plist | Keine Änderung: Das Dateischutzattribut wird per Code gesetzt; das Entitlement `com.apple.developer.default-data-protection` würde nur die Voreinstellung ändern (iOS-Standard ist ohnehin `CompleteUntilFirstUserAuthentication`) und erzeugte zusätzlichen Signierungsbedarf. `Info.plist` braucht für Data Protection keinen Eintrag; die Stakeholder-Vorgabe „keine zusätzliche Verschlüsselung" bleibt gewahrt | Vermeidet Provisioning-/Signierungsabhängigkeiten (Apple-Kennungen fehlen noch). Das Verhalten ist nur auf einem iOS-Gerät prüfbar; in diesem Schritt wird der iOS-Code durch den iOS-Build in CI kompiliert und die Geräteprüfung in der Doku (`ablauf-technisch.md`) beschrieben |
| Auswahl-Steuerelemente | `Switch` je Spritsorte plus Schaltflächen ▲/▼ (je 44 × 44, `SemanticProperties.Description` „Nach oben/unten verschieben"); `RadioButton` je Wert für GPS, Ansicht und Sortierung (jeweils höchstens drei sichtbare Optionen, eine Gruppe je Einstellung) | Alle Optionen sind gleichzeitig sichtbar, per UI Automation (FlaUI) zuverlässig bedienbar und per Tastatur/Screenreader zugänglich; `Picker` würde unter FlaUI Popups benötigen |
| UI-Texte | Zentral in der statischen Klasse `SettingsTexts` (Überschriften, Hinweise, Anzeigenamen je Enum-Wert über `GetLabel`); XAML bindet daran über das ViewModel | Texte werden zentral gepflegt; das Projekt nutzt weder `IStringLocalizer` noch `.resx` (`translation-check.py` greift daher nicht), die Texte sind einheitlich deutsch mit korrekten Umlauten |
| Migrationen im Test des Upgrade-Pfads | Zwei echte Migrationen: `InitialCreate` (nur `UserSettings`), danach `AddFuelTypeSettings` (Tabelle `FuelTypeSettings`) | So existiert eine echte ältere Schemaversion, auf der der Upgrade-Test Daten anlegt und die Aktualisierung prüft; Migrationen sind noch nicht ausgeliefert, daher kein Nachteil |
| Plattform-Provider / Pakete | `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 (bringt `SQLitePCLRaw.bundle_e_sqlite3` für iOS/Android/Windows mit), `Microsoft.EntityFrameworkCore.Design` 10.0.10 mit `PrivateAssets=all` | Version passend zum global installierten `dotnet-ef` 10.0.10; lokal im NuGet-Cache vorhanden |
| Generierter Migrationscode und CS1591 | `.editorconfig`-Abschnitt `[src/Tankradar.MAUI/Data/Migrations/*.cs]` mit `dotnet_diagnostic.CS1591.severity = none` | Der Hook `csproj-xmldoc-check.py` verbietet `#pragma warning disable` für XML-Doku-Codes; die Migrationsdateien sind generiert und werden nicht von Hand dokumentiert |

## Programmabläufe

### App-Start und Datenbankinitialisierung

1. `MauiProgram.CreateMauiApp()` registriert `IDatabaseFileProtector`, `IDatabaseInitializer`, `IDbContextFactory<TankradarDbContext>`, `ISettingsService`.
2. `App`-Konstruktor ruft `IDatabaseInitializer.InitializeAsync()` auf (nicht awaited).
3. `DatabaseInitializer.InitializeAsync()` ermittelt `IAppDataPathProvider.GetDataDirectory()`, legt das Verzeichnis an und öffnet über die Factory einen `TankradarDbContext`.
4. `Database.MigrateAsync()` legt beim ersten Start die Datenbank samt `__EFMigrationsHistory` an bzw. wendet ausstehende Migrationen auf eine vorhandene Datenbank an (bestehende Zeilen bleiben erhalten).
5. `IDatabaseFileProtector.Protect(databasePath)` setzt unter iOS das Dateischutzattribut für Verzeichnis, `tankatlas.db`, `-wal`, `-shm` (nach der Migration, wenn die Dateien existieren); sonst keine Aktion.
6. Der abgeschlossene Task wird gespeichert; bei einer Ausnahme wird er verworfen, damit ein späterer Zugriff erneut versucht.

Beteiligte Klassen/Komponenten: `App`, `DatabaseInitializer`, `TankradarDbContext`, `DatabaseFileProtector`, `AppDataPathProvider`.

### Einstellungen laden

1. `SettingsPage` erscheint, `TankradarContentPage.OnAppearing` ruft `SettingsViewModel.OnAppearing()`; dieses startet `LoadAsync()`.
2. `SettingsViewModel.LoadAsync()` setzt `IsBusy`, ruft `ISettingsService.LoadAsync()`.
3. `SettingsService.LoadAsync()` wartet auf `IDatabaseInitializer.InitializeAsync()`, liest `UserSettings` (Id 1) und alle `FuelTypeSettings` (sortiert nach `SortOrder`).
4. Mapping: fehlende Zeile oder ungültiger Text ergibt Standardwert; Spritsorten mit unbekanntem Schlüssel werden ignoriert; fehlende `FuelType`-Werte werden ans Ende angehängt (nicht ausgewählt); sind keine Spritsorten gespeichert, gilt die Standardauswahl; bei null ausgewählten gespeicherten Sorten gilt die Standardauswahl.
5. Das ViewModel befüllt `FuelTypes`, `GpsOptions`, `ViewOptions`, `SortOptions` mit unterdrücktem Speichern (`_isLoading`).
6. Bei einer Ausnahme zeigt das ViewModel `StatusMessage` („Die Einstellungen konnten nicht geladen werden.") und bleibt bei den Standardwerten; es wird nichts gespeichert.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `ISettingsService`, `SettingsService`, `AppSettings`.

### Einstellung ändern und sofort speichern

1. Anwender schaltet einen `Switch`, wählt einen `RadioButton` oder tippt ▲/▼.
2. Das jeweilige Element-ViewModel (`FuelTypeItemViewModel`, `ChoiceOptionViewModel<T>`) meldet die Änderung an `SettingsViewModel`.
3. Spritsorte abwählen: Ist es die letzte ausgewählte Sorte, wird das Abwählen zurückgenommen (`IsSelected` bleibt `true`) und `StatusMessage` „Mindestens eine Spritsorte muss ausgewählt bleiben." gesetzt; es erfolgt keine Speicherung.
4. Verschieben: `FuelTypes.Move(...)` tauscht benachbarte Einträge; `CanMoveUp`/`CanMoveDown` der Zeilen werden aktualisiert (erste Zeile kann nicht nach oben, letzte nicht nach unten).
5. `SettingsViewModel.SaveAsync()` (Serialisierung über `SemaphoreSlim`, letzter Stand gewinnt) baut aus dem Zustand ein `AppSettings` und ruft `ISettingsService.SaveAsync(settings)`; der Task ist über `LastSaveTask` abrufbar.
6. `SettingsService.SaveAsync` validiert (siehe Validierungsregeln), wartet auf die Initialisierung, lädt `UserSettings` (Id 1) und die `FuelTypeSettings` (Load-and-Patch), legt fehlende Zeilen an, patcht per `Update(...)` der Entität (`SortOrder` = Position, `IsSelected`), ein `SaveChangesAsync` (atomar). Unbekannte vorhandene Schlüssel bleiben unverändert.
7. Bei Fehler: `StatusMessage` „Die Einstellung konnte nicht gespeichert werden."; bei Erfolg wird eine vorhandene Meldung gelöscht.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `FuelTypeItemViewModel`, `ChoiceOptionViewModel<TValue>`, `SettingsService`, `UserSettingsEntity`, `FuelTypeSettingEntity`.

### Neustart und App-Update

1. Nach Neustart derselbe Ablauf „App-Start" (die Datei existiert, `MigrateAsync` ist ein No-op) und „Einstellungen laden": zuvor gespeicherte Werte werden angezeigt.
2. Bei einem App-Update mit neuer Migration: `MigrateAsync` führt nur ausstehende Migrationen aus; Daten früherer Schemaversionen bleiben erhalten. Eine Datenbank ohne `__EFMigrationsHistory` (Legacy) kann nicht existieren, da die erste App-Version, die eine Datenbank anlegt, mit Migrationen arbeitet; ein Übergangsschritt ist daher nicht erforderlich.

## Neue Klassen

Alle unter `src/Tankradar.MAUI/`; jede öffentliche Klasse und jedes Member mit XML-Doku (CS1591 ist Fehler).

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Models/FuelType` | Enum | `SuperE5`, `SuperE10`, `Diesel` |
| `Models/GpsUsage` | Enum | `Always`, `WhileInUse`, `Never` |
| `Models/ResultView` | Enum | `List`, `Map` |
| `Models/ResultSortOrder` | Enum | `Price`, `Distance`, `Name` |
| `Models/FuelTypeSelection` | Record | Spritsorte und `IsSelected` |
| `Models/AppSettings` | Record | `FuelTypes` (geordnet), `GpsUsage`, `ResultView`, `ResultSortOrder`; `CreateDefault()`, `Normalize()`, `Validate()` |
| `Data/TankradarDbContext` | DbContext | `DbSet<UserSettingsEntity>`, `DbSet<FuelTypeSettingEntity>`; `DatabaseFileName` = `tankatlas.db`, `GetDatabasePath(string)` |
| `Data/TankradarDbContextFactory` | `IDesignTimeDbContextFactory` | Nur für `dotnet ef`; Datenquelle `:memory:` (legt keine Datei an) |
| `Data/UserSettingsEntity` | Datenmodellklasse | Tabelle `UserSettings`: `Id`, `GpsUsage`, `ResultView`, `ResultSortOrder` (Text); `Update(AppSettings)` |
| `Data/FuelTypeSettingEntity` | Datenmodellklasse | Tabelle `FuelTypeSettings`: `FuelTypeKey` (PK), `SortOrder`, `IsSelected` |
| `Data/Migrations/*` | Migrationen | durch `dotnet ef` erzeugt (siehe Abschnitt Migrationen) |
| `Services/IDatabaseInitializer`, `Services/DatabaseInitializer` | Interface, Klasse | Migrationslauf, Dateischutz, Task-Caching |
| `Services/IDatabaseFileProtector`, `Services/DatabaseFileProtector` | Interface, Klasse | iOS-Dateischutz (`#if IOS`), sonst No-op |
| `Services/ISettingsService`, `Services/SettingsService` | Interface, Klasse | `Task<AppSettings> LoadAsync(CancellationToken)`, `Task SaveAsync(AppSettings, CancellationToken)` |
| `Resources/Texts/SettingsTexts` | Statische Klasse | Zentrale UI-Texte und `GetLabel` je Enum-Wert |
| `ViewModels/FuelTypeItemViewModel` | ViewModel | Zeile: `FuelType`, `DisplayName`, `IsSelected`, `CanMoveUp`, `CanMoveDown`, `MoveUpCommand`, `MoveDownCommand`, `AutomationKey` |
| `ViewModels/ChoiceOptionViewModel<TValue>` | ViewModel | Auswahloption: `Value`, `Label`, `IsSelected`, `AutomationKey` |

## Änderungen an bestehenden Klassen

### `Tankradar.MAUI.csproj` (Projektdatei)

- `<Product>Tankatlas</Product>` in der ersten `PropertyGroup` (Assembly-/Dateiname bleiben, Dateieigenschaften ProductName/FileDescription zeigen den Anzeigenamen).
- PackageReferences `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 und `Microsoft.EntityFrameworkCore.Design` 10.0.10 (`PrivateAssets=all`, `IncludeAssets` mit runtime, build, native, contentfiles, analyzers, buildtransitive).
- Version in der csproj bleibt unverändert (Versionen werden von Skripten/CI vergeben, vgl. 0.1.1/0.1.2).

### `MauiProgram` (Klasse)

- **Geänderte Methoden:** `CreateMauiApp` registriert `IDatabaseFileProtector` (Singleton), `IDbContextFactory<TankradarDbContext>` per `AddDbContextFactory` mit `UseSqlite(Data Source=<GetDatabasePath(IAppDataPathProvider.GetDataDirectory())>)`, `IDatabaseInitializer` (Singleton), `ISettingsService` (Singleton).

### `App` (Klasse)

- **Geänderte Methoden:** Konstruktor erhält zusätzlich `IDatabaseInitializer` und startet `InitializeAsync()` (nicht awaited).

### `SettingsViewModel` (Klasse)

- **Neue Eigenschaften:** `FuelTypes` (`ObservableCollection<FuelTypeItemViewModel>`), `GpsOptions`, `ViewOptions`, `SortOptions` (`IReadOnlyList<ChoiceOptionViewModel<...>>`), `StatusMessage`, `HasStatusMessage`, `LastSaveTask`, Texte aus `SettingsTexts`.
- **Neue Methoden:** `LoadAsync`, privates `SaveAsync`; `OnAppearing` überschrieben (startet `LoadAsync`).
- **Geänderte Methoden:** Konstruktor erhält `ISettingsService` (Titel „Optionen" bleibt).

### `SettingsPage` (XAML-Seite)

- Platzhaltertext entfällt; `ScrollView` mit vier Karten (Überschrift = `headline-md`, Hinweise = `body-sm`, Farben per `AppThemeBinding` mit den Design-System-Schlüsseln `ColorSurface*`, `ColorBorder*`, `ColorTextPrimary*`, `ColorTextSecondary*`, `ColorPrimaryTeal`, Abstände `space-*`). `SettingsPage.Headline` bleibt erhalten (Navigationstest). Bedienelemente mindestens `MinimumTouchTarget` (44) hoch/breit. Lokale Styles in den Seitenressourcen (Karte, Schaltfläche), keine Änderung am Design-System.
- AutomationIds: `Settings.FuelType.<FuelType>.Switch`, `.MoveUp`, `.MoveDown`; `Settings.Gps.<GpsUsage>`; `Settings.View.<ResultView>`; `Settings.Sort.<ResultSortOrder>`; `Settings.StatusMessage`.
- Reihenfolge der Karten: Spritsorten, Standort (GPS), Standardansicht der Suchergebnisse, Standardsortierung. GPS-Hinweis: „Nur bei Nutzung" ist voreingestellt.

### `Platforms/iOS`, `Info.plist`, Entitlements

- Keine Änderung (siehe Designentscheidungen).

### `.editorconfig`, `.gitignore`

- `.editorconfig`: Abschnitt für `Data/Migrations/*.cs` (CS1591 aus). `.gitignore`: `*.db`, `*.db-wal`, `*.db-shm` ergänzen (Schutz vor versehentlichen Datenbankdateien; Hooks blockieren sie ebenfalls).

### `README.md`, `changes.log`, Doku

- README Zeile 13ff.: Satz auf „Entwicklungsschritt 4 abgeschlossen: lokale Datenhaltung (SQLite, EF Core) und Einstellungen (Optionen)" mit Ergänzung der Funktionsliste aktualisieren (Navigation, Design-System, CI/CD bleiben erwähnt); Projektstruktur-Abschnitt um `Data/` ergänzen, falls dort Ordner gelistet sind.
- `changes.log`: neuer Eintrag oben („Lokale Datenhaltung und Einstellungen (Tankradar Schritt 4)", Version 0.1.3).
- Neues Verzeichnis `docs/help/Einstellungen/` mit `index.md`, `beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md` (Aufbau analog `docs/help/Navigation`); `docs/help/index.md` verlinkt es unter „Benutzung der App". `docs/help/Navigation/beschreibung.md` und `ablauf-anwender.md` aktualisieren (Optionen nicht mehr Platzhalter).

## Datenbankmigrationen

Erzeugung ausschließlich per CLI (nie handschriftlich), jeweils nach dem Anlegen/Ändern des Modells:

`dotnet build src/Tankradar.MAUI -f net10.0-windows10.0.19041.0` und danach  
`dotnet ef migrations add <Name> --project src/Tankradar.MAUI --startup-project src/Tankradar.MAUI --framework net10.0-windows10.0.19041.0 --context TankradarDbContext --output-dir Data/Migrations`

Die Design-Time-Factory sorgt dafür, dass das Tooling die MAUI-App nicht starten muss.

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `InitialCreate` | `UserSettings` (`Id` PK, `GpsUsage`, `ResultView`, `ResultSortOrder`: Text, nicht null) | Erste Schemaversion nur mit `UserSettings` (zuerst Entität und Kontext mit nur dieser Tabelle anlegen, Migration erzeugen) |
| `AddFuelTypeSettings` | `FuelTypeSettings` (`FuelTypeKey` PK Text, `SortOrder` int, `IsSelected` bool) | Rein additiv; keine destruktiven Operationen. Danach `Up`/`Down` prüfen (Datensicherheit) |

Begründung zu Regel 6/5 des Skills: Jede Modelländerung hat eine Migration; ein Legacy-Übergang ist nicht nötig, da vor diesem Schritt keine Datenbank existierte. Ein Test (`HasPendingModelChanges`) stellt sicher, dass das Modell und der Migrationsstand nicht auseinanderlaufen.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `AppSettings.FuelTypes` | Jeder `FuelType` höchstens einmal; mindestens eine Sorte ausgewählt | `SaveAsync` wirft `ArgumentException`; UI verhindert das Abwählen der letzten Sorte (Hinweis `StatusMessage`) |
| `AppSettings.FuelTypes` (Normalisierung) | Fehlende `FuelType`-Werte werden unausgewählt ans Ende ergänzt (`Normalize`), bevor gespeichert oder aus der DB gemappt wird | – |
| `GpsUsage`, `ResultView`, `ResultSortOrder` | Wert muss definiert sein (`Enum.IsDefined`) | `ArgumentException` beim Speichern; beim Laden gespeicherte ungültige Texte ergeben den Standardwert |
| Standardwerte | GPS `WhileInUse`, Ansicht `List`, Sortierung `Price`, Spritsorten E5, E10, Diesel (alle ausgewählt) | – |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `<Product>` (csproj) | MSBuild-Eigenschaft | `Tankatlas` | Dateieigenschaften der Windows-EXE zeigen den Anzeigenamen |
| `TankradarDbContext.DatabaseFileName` | Konstante | `tankatlas.db` | Dateiname der Datenbank im Datenverzeichnis aus `IAppDataPathProvider` |
| `appsettings.json` | – | unverändert | Keine Änderung |

## Seiteneffekte und Risiken

- **`ViewModelTests_PageTitles`:** Der Theory-Eintrag `SettingsViewModel` bricht, weil der parameterlose Konstruktor entfällt (siehe Tests).
- **`App`-Konstruktor:** neue Abhängigkeit; Fehler beim Datenbankstart dürfen die App nicht beenden (Task wird nicht awaited, Fehler erscheint in den Optionen).
- **`dotnet ef` im Multi-Target-MAUI-Projekt:** Aufruf nur mit `--framework net10.0-windows10.0.19041.0` und Design-Time-Factory; die Funktion wird im ersten Schritt der Umsetzung verifiziert.
- **Windows-Dateisperre:** `Microsoft.Data.Sqlite` pooled Verbindungen; Tests rufen vor dem Aufräumen `SqliteConnection.ClearAllPools()` auf (Integration, `TestDataContext`-Cleanup).
- **E2E:** `E2ETestBase` wird für den Neustart refaktoriert; bestehende E2E-Tests dürfen sich im Verhalten nicht ändern.
- **iOS-Code:** Unter Windows nicht ausführbar; Absicherung durch iOS-Build in CI und dokumentierte Geräteprüfung.
- **Paketstände / Offline-Restore:** `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10 liegt im lokalen Cache; die Abhängigkeitsprüfung (`scripts/check-vulnerabilities.mjs`) ist nach dem Hinzufügen der Pakete auszuführen.
- **Hooks:** Neue Enums brauchen alle Werte in Tests (siehe Tests); keine `*.db`/`*.sql`-Dateien erzeugen oder committen (Testdatenbanken liegen im Temp-/`TEST_DATA_PATH`-Verzeichnis).
- **Enum-Namen** sind Persistenzvertrag: Umbenennen würde gespeicherte Werte auf Standard zurücksetzen (durch Test abgesichert).

## Umsetzungsreihenfolge

1. **Pakete und Projekteinstellungen**
   - Voraussetzungen: Keine
   - Beschreibung: `<Product>Tankatlas</Product>` setzen; PackageReferences EF Core Sqlite und Design 10.0.10 ergänzen; `.editorconfig`-Abschnitt für Migrationen; `.gitignore` ergänzen; `dotnet restore`/Build prüfen.
2. **Enums und Modelle**
   - Voraussetzungen: Schritt 1
   - Beschreibung: `FuelType`, `GpsUsage`, `ResultView`, `ResultSortOrder`, `FuelTypeSelection`, `AppSettings` (Standardwerte, `Normalize`, `Validate`).
3. **Datenmodell Stufe 1 und Migration `InitialCreate`**
   - Voraussetzungen: Schritt 2
   - Beschreibung: `UserSettingsEntity`, `TankradarDbContext` (nur `UserSettings`), `TankradarDbContextFactory`; Build mit `-f net10.0-windows10.0.19041.0`; `dotnet ef migrations add InitialCreate` (siehe Abschnitt Migrationen).
4. **Datenmodell Stufe 2 und Migration `AddFuelTypeSettings`**
   - Voraussetzungen: Schritt 3
   - Beschreibung: `FuelTypeSettingEntity`, `DbSet` im Kontext, Konfiguration (Schlüssel, Spaltentypen); Build; `dotnet ef migrations add AddFuelTypeSettings`; generierte `Up`/`Down` prüfen.
5. **Dateischutz und Initialisierung**
   - Voraussetzungen: Schritt 4, `IAppDataPathProvider`
   - Beschreibung: `IDatabaseFileProtector`/`DatabaseFileProtector` (`#if IOS`), `IDatabaseInitializer`/`DatabaseInitializer`.
6. **SettingsService**
   - Voraussetzungen: Schritte 2, 4, 5
   - Beschreibung: `ISettingsService`/`SettingsService` mit Mapping, Load-and-Patch (`UserSettingsEntity.Update`), Validierung.
7. **DI und App-Start**
   - Voraussetzungen: Schritte 5, 6
   - Beschreibung: `MauiProgram` und `App` anpassen.
8. **UI-Texte, Element-ViewModels, `SettingsViewModel`**
   - Voraussetzungen: Schritt 6
   - Beschreibung: `SettingsTexts`, `FuelTypeItemViewModel`, `ChoiceOptionViewModel<TValue>`, `SettingsViewModel` (Laden, Ändern, Speichern, Sperre der letzten Sorte).
9. **SettingsPage**
   - Voraussetzungen: Schritt 8
   - Beschreibung: XAML mit Karten, Schaltern, RadioButtons, ▲/▼-Schaltflächen, AutomationIds, 44-px-Zielgrößen, Hell/Dunkel über Design-System-Ressourcen; Seite per Build prüfen.
10. **Unit-Tests und Anpassung bestehender Tests**
    - Voraussetzungen: Schritte 2 bis 8
    - Beschreibung: Siehe Tests; `ViewModelTests_PageTitles` anpassen.
11. **Integrationstests**
    - Voraussetzungen: Schritte 3 bis 7
    - Beschreibung: Siehe Tests (`TestDatabase`-Hilfsklasse, Erstanlage, Upgrade, Persistenz, Modellabgleich).
12. **E2E-Testbasis und E2E-Tests**
    - Voraussetzungen: Schritt 9; Debug-Build der Windows-App
    - Beschreibung: `E2ETestBase` um `RestartApplication()` erweitern; `SettingsE2ETests_*` anlegen; App bauen (`dotnet build src/Tankradar.MAUI -f net10.0-windows10.0.19041.0`), Testlauf.
13. **Dokumentation und README**
    - Voraussetzungen: Schritte 1 bis 12
    - Beschreibung: `docs/help/Einstellungen/*`, `docs/help/index.md`, Navigation-Doku, README-Satz, `changes.log` (Version 0.1.3).
14. **Gesamtprüfung und Zwischenstand**
    - Voraussetzungen: Schritte 1 bis 13
    - Beschreibung: `dotnet format --verify-no-changes`, Build mit Warnungen als Fehler, alle Testsuiten, Hooks (`enum-coverage-check.py --strict`, `csproj-xmldoc-check.py --all`, `translation-check.py --all`), Vulnerability-Check; danach `scripts/create-review-version.ps1 -Version 0.1.3` (Ergebnis `review-versions/0.1.3_<JJJJ-MM-TT>/`, nicht committen); Start des Zwischenstands prüfen.

## Tests

### Neue Tests

Testklassen nach Themen getrennt (Konvention `Klasse_Thema`); alle Enum-Werte kommen in Tests vor (Hook `enum-coverage-check.py`).

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CreateDefault_*` (Standardwerte GPS WhileInUse, List, Price, E5/E10/Diesel in Reihenfolge, alle ausgewählt) | `AppSettingsTests_Defaults` (Unit) | Standardwerte „sicher statt bequem" |
| `Normalize_*`, `Validate_*` (fehlende Sorten angehängt, Duplikate, keine Auswahl, undefinierte Enum-Werte) | `AppSettingsTests_Validation` (Unit) | Validierungsregeln |
| Theory über jeden Wert von `FuelType`, `GpsUsage`, `ResultView`, `ResultSortOrder` mit erwartetem Persistenznamen; `GetLabel` je Wert nicht leer und deutsch (Anzeigenamen „Immer", „Nur bei Nutzung", „Nie", „Liste", „Karte", „Preis", „Entfernung", „Name", „Super E5", „Super E10", „Diesel") | `SettingsEnumTests_Persistence`, `SettingsTextsTests_Labels` (Unit) | Enum-Abdeckung, Persistenzvertrag, zentrale Texte |
| `LoadAsync_WithoutData_ReturnsDefaults`, `SaveThenLoad_RoundTrips` (je Enum-Wert), `Load_InvalidStoredValue_FallsBackToDefault`, `Load_UnknownFuelTypeKey_IsIgnoredAndKept`, `Load_NewFuelTypeMissingInDatabase_AppendedUnselected`, `Save_ZeroSelected_Throws`, `Save_Twice_UpdatesSameRows` | `SettingsServiceTests_LoadSave` (Unit, SQLite In-Memory mit offener Verbindung und echter `MigrateAsync`) | Speichern/Laden, Load-and-Patch, Vorwärtskompatibilität |
| `InitializeAsync_RunsOnce`, `InitializeAsync_AfterFailure_Retries`, `Protect_IsCalledAfterMigration` | `DatabaseInitializerTests_Lifecycle` (Unit, Fake-Factory/Fake-Protector) | Task-Caching, Reihenfolge Migration vor Schutz |
| `Protect_OnWindows_DoesNothingAndDoesNotThrow` (inkl. nicht existierender Pfad) | `DatabaseFileProtectorTests_Platform` (Unit) | Kein Seiteneffekt außerhalb iOS |
| `LoadAsync_PopulatesAllSections`, `LoadAsync_ServiceThrows_ShowsStatusMessageAndKeepsDefaults`, `OnAppearing_StartsLoad` | `SettingsViewModelTests_Loading` (Unit, Fake `ISettingsService`) | Laden und Fehleranzeige |
| `SelectGps/View/Sort_SavesImmediately` (je Wert), `ToggleFuel_SavesImmediately`, `MoveUp/MoveDown_ChangesOrderAndSaves`, `CanMove_FirstAndLast`, `DeselectLastFuel_IsRevertedWithMessageAndNotSaved`, `Save_Fails_ShowsStatusMessage`, `LoadAsync_DoesNotTriggerSave` | `SettingsViewModelTests_Changes` (Unit) | Sofort-Speichern und Regeln |
| `FakeSettingsService`, `InMemoryDbContextFactory`, `FakeDatabaseFileProtector` | `Unit/Support/*` | Testhilfen |
| `Initialize_FirstStart_CreatesDatabaseFileAndSchema` (Datei `tankatlas.db` im `TestDataContext`-Verzeichnis, Tabellen `UserSettings`, `FuelTypeSettings`, History mit beiden Migrationen) | `DatabaseInitializerTests_FirstStart` (Integration) | Anlegen beim ersten Start, Pfad aus `IAppDataPathProvider` |
| `Initialize_FromOlderSchemaVersion_KeepsData` (Datenbank per `IMigrator.MigrateAsync("<InitialCreate-Id>")` auf alten Stand bringen, `UserSettings`-Zeile mit Nicht-Standardwerten per SQL einfügen, danach `InitializeAsync`: Zeile erhalten, `FuelTypeSettings` vorhanden, History vollständig), `Initialize_Twice_IsIdempotent` | `DatabaseInitializerTests_Upgrade` (Integration) | Aktualisierung ohne Datenverlust |
| `Model_HasNoPendingChanges` (`Database.HasPendingModelChanges()` ist false) | `MigrationsTests_ModelSync` (Integration) | Modell und Migrationen konsistent |
| `Save_ThenNewServiceInstance_LoadsSameValues` (neue Factory, `ClearAllPools`, simuliert Neustart), `Save_AllEnumValues_RoundTripThroughFile` | `SettingsServiceTests_RestartPersistence` (Integration) | Neustart-Persistenz mit echter Datei |
| `TestDatabase` (Hilfsklasse: Fake-`IAppDataPathProvider` auf `TestDataContext.DataDirectory`, Factory, Initializer, `ClearAllPools` beim Aufräumen) | `Integration/Support/TestDatabase` | Wiederverwendbare Integrationsumgebung |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ViewModelTests_PageTitles.Constructor_SetsExpectedTitle` | `SettingsViewModel` hat keinen parameterlosen Konstruktor mehr: Theory-Zeile entfernen und separaten Test `SettingsViewModel_Constructor_SetsTitle` mit `FakeSettingsService` ergänzen |
| `TestDataContextTests_Lifecycle` | Falls `TestDataContext.Cleanup` um `SqliteConnection.ClearAllPools()` ergänzt wird, Verhalten unverändert; Test läuft unverändert weiter |
| `AppDataPathProviderTests_*`, `AppConfigurationTests_*`, `BaseViewModelTests_*` | Unverändert |

### E2E-Tests (primärer Funktionsnachweis)

Alle E2E-Tests laufen mit `TEST_DATA_PATH`-Verzeichnis (bereits durch `E2ETestBase`), nie gegen produktive Daten. `E2ETestBase` erhält `protected void RestartApplication()` (App schließen, auf Prozessende warten, mit demselben Datenverzeichnis neu starten, `MainWindow`/`Application` neu setzen); Startlogik wird aus dem Konstruktor in eine private Methode `LaunchApplication()` extrahiert, Diagnose-Verhalten bleibt. Zugriff auf `Switch` über das Toggle-Pattern, `RadioButton` über das SelectionItem-Pattern.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Erster Start: Optionen zeigen Standardwerte (GPS „Nur bei Nutzung", Liste, Preis, E5/E10/Diesel an, in dieser Reihenfolge) | `SettingsE2ETests_Defaults` | Standardwerte „sicher statt bequem"; Datenbank wird beim ersten Start angelegt (Seite lädt aus frischem Datenverzeichnis) | Nur die laufende App belegt Migrationsstart, DI und Anzeige zusammen |
| Pflicht | GPS, Ansicht und Sortierung ändern, App neu starten, Werte bleiben erhalten | `SettingsE2ETests_Persistence` | Änderungen bleiben nach Neustart erhalten; sofortiges Speichern (Neustart ohne weitere Aktion) | Echter Neustartpfad mit echter Datenbankdatei |
| Pflicht | Spritsorte abwählen und Diesel nach oben verschieben, Neustart, Auswahl und Reihenfolge bleiben | `SettingsE2ETests_FuelTypes` | Auswahl und Reihenfolge der Spritsorten, Persistenz | Auf/Ab-Bedienung und Reihenfolge sind nur über die UI nachweisbar |
| Pflicht | Letzte ausgewählte Spritsorte lässt sich nicht abwählen, Hinweis erscheint | `SettingsE2ETests_FuelTypes` | Validierungsregel sichtbar für Anwender | UI-Regel mit Meldung |
| Hoch | Bedienelemente erreichbar und mit AutomationIds auffindbar (alle Spritsorten-, GPS-, Ansichts-, Sortier-Elemente) | `SettingsE2ETests_Defaults` | Optionen gemäß Anforderung vollständig vorhanden (keine Strom-/Ladetyp-Einstellung) | Prüft, dass keine Strom-Elemente existieren |

Betroffene bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `NavigationE2ETests` | Keine Codeänderung; `SettingsPage.Headline` bleibt erhalten; Lauf bestätigt, dass die geänderte Seite navigierbar bleibt |
| `DiagnosticsCaptureE2ETests`, `E2ETestBase` | Basisklasse wird für `RestartApplication` refaktoriert; Verhalten des Standardstarts bleibt, Lauf bestätigt dies |

## Offene Punkte

Keine.
