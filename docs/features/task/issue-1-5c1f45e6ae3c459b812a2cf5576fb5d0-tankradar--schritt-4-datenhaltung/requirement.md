# Übersetzte Anforderung: Lokale Datenhaltung und Einstellungen (Tankradar Schritt 4)

Quelle: `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Schritt 4. Zusätzlich: `<Product>Tankatlas</Product>` in der MAUI-Projektdatei und Aktualisierung des veralteten README-Satzes zum Entwicklungsstand.

## Fachliche Zusammenfassung

Die App erhält eine einzige lokale SQLite-Datenbank (Entity Framework Core), die beim ersten Start angelegt und bei App-Updates per Migrationen ohne Datenverlust auf den neuen Schemastand gebracht wird. Unter iOS werden die Datenbankdateien über Data Protection (Dateischutzattribut) geschützt; eine zusätzliche Verschlüsselung entfällt. Als erste Nutzerdaten werden die Einstellungen (Spritsorten mit Auswahl und Reihenfolge, GPS-Nutzung, Standardansicht, Standardsortierung) persistiert, und der bisherige Platzhalter-Bereich „Optionen" wird zur funktionalen Einstellungsseite im vorhandenen Design-System. Änderungen werden sofort gespeichert und überstehen einen Neustart.

## Betroffene Klassen und Komponenten

- Datenmodell (neu, `src/Tankradar.MAUI/Data/` bzw. `Models/`): Datenbankentitäten `UserSettingsEntity`, `FuelTypeSettingEntity`; Domänenmodell `AppSettings`, `FuelTypeSelection`
- Enums (neu): `FuelType` (Super E5, Super E10, Diesel), `GpsUsage` (Immer, Nur bei Nutzung, Nie), `ResultView` (Liste, Karte), `ResultSortOrder` (Preis, Entfernung, Name)
- Logik / Services (neu): `TankradarDbContext`, `TankradarDbContextFactory` (Design-Time), `IDatabaseInitializer` / `DatabaseInitializer`, `DatabaseFileProtector` (iOS Data Protection), `ISettingsService` / `SettingsService`, Migrationen
- Bestehend: `IAppDataPathProvider` (einzige Quelle des Datenbankpfads), `MauiProgram` (DI), `App` (Start der Datenbankinitialisierung), `SettingsViewModel`, `SettingsPage`, `BaseViewModel`
- UI: `SettingsPage.xaml` mit Auswahlbereichen; Hilfs-ViewModels für Spritsorten-Zeilen und Auswahloptionen
- Tests: Unit (Service, ViewModel, Enums), Integration (echte SQLite-Datei, Migration, Neustart), FlaUI-E2E (Einstellungen ändern, App neu starten); Erweiterung `E2ETestBase`
- Projektdatei: `<Product>Tankatlas</Product>`; Doku `docs/help/Einstellungen/`, `README.md`, `changes.log`

## Implementierungsansatz

- Persistenz mit `Microsoft.EntityFrameworkCore.Sqlite` (10.0.x); Schema ausschließlich über `dotnet ef migrations add`, Start-Initialisierung per `MigrateAsync` (kein `EnsureCreated`). Pfad der Datei nur über `IAppDataPathProvider.GetDataDirectory()`, dadurch greift die Testisolation (`TEST_DATA_PATH`).
- Auswahl-/Identifikationsanforderung: Spritsorten, GPS-Modus, Ansicht und Sortierung sind feste Wertemengen und werden in Klartext (Schalter, Auswahlschaltflächen) ausgewählt; keine internen Kennungen für Anwender. Reihenfolge der Spritsorten per Auf/Ab-Schaltflächen.
- Einstellungen als typisiertes Modell hinter `ISettingsService` (Laden/Speichern); Standardwerte „sicher statt bequem": GPS „Nur bei Nutzung", Ansicht Liste, Sortierung Preis, Spritsorten Super E5, Super E10, Diesel.
- Strompreise: keine Einstellung, keine Ladetypen (ADR 0001).
- Hooks: XML-Doku (CS1591 als Fehler), Formatierung, Enum-Testabdeckung (jeder Enum-Wert in Tests), keine `*.db`/`*.sqlite*`/`*.sql`-Dateien im Commit.

## Konfiguration

Einstellungen sind anwendungsweit (ein Gerät, ein Anwender) in der SQLite-Datenbank gespeichert. Keine Einträge in `appsettings.json`.

## Annahmen (durch den Auftraggeber vorgegeben)

- Standard-Spritsorten Super E5, Super E10, Diesel (die Quelle Tankerkönig liefert genau diese).
- Mindestens eine Spritsorte muss gewählt bleiben (Annahme zur Vermeidung leerer Suchen; im Plan als Validierung).

## Offene Fragen

Keine.
