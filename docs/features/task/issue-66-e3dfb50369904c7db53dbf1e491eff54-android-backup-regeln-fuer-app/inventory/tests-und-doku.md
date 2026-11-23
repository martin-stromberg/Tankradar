# Tests und Dokumentation

## Testprojekte

- `src/Tankradar.Tests.Unit` — xUnit auf `net11.0-windows10.0.19041.0`, Testklassen in `Unit/`, Basisklasse `BaseTest.cs` (IDisposable-Grundgerüst). `GenerateDocumentationFile` + `WarningsAsErrors CS1591` → alle öffentlichen Typen/Member brauchen deutsche XML-Dokumentationskommentare.
- `src/Tankradar.Tests.Integration` — gleiche Grundstruktur, echte SQLite-Dateien (`Integration/`, z. B. `BackupServiceTests_*`, gemeinsame xUnit-Collection `BackupCollection` gegen Parallelitätsprobleme, ADR 0015).
- `src/Tankradar.Tests.E2E` — FlaUI-Tests gegen die laufende Windows-App; Kategorie `OsInterface` (`src/TestSupport/TestCategories.cs:9–16`) → laufen in der nicht blockierenden CI-Lane.
- `src/TestSupport` — geteilte Testhelfer, per `<Compile Include>` in die Testprojekte gelinkt (u. a. `TestCategories`, `FlakyConventionTests`, `BackupTestArchive`).

Kein Android-Testprojekt, kein Emulator-/Device-Test, keine `AndroidManifest`-Validierung im Build.

## Bestehende Manifest-/Plattformdatei-Testkonvention (fertiges Muster für den geforderten Manifest-Test)

- `src/Tankradar.Tests.Unit/Unit/MauiSourceFiles.cs:6–43` — interner Helfer: `RepositoryRoot()` sucht ausgehend von `AppContext.BaseDirectory` aufwärts nach `Tankradar.sln`; `PathOf(...)`/`Read(...)` liefern Dateien unter `src/Tankradar.MAUI`.
- `Unit/PlatformManifestTests_Location.cs` — liest `Info.plist` (iOS/MacCatalyst) und `Package.appxmanifest` (Windows) als Text, `Assert.Contains`/`DoesNotContain`; eigene Kopie der Root-Suche (`:11–22`, älter als `MauiSourceFiles`, funktional identisch).
- `Unit/PlatformManifestTests_Photos.cs` — gleiches Textmuster für Foto-Berechtigungen.
- `Unit/IosProjectFilesTests.cs` — strukturierte Auswertung per `XDocument` (`:11–22`), einschließlich Positiv- **und** Negativlisten („nur diese `NS…UsageDescription`-Schlüssel erlaubt“, `:38–43`).
- `Unit/AppIconResourceTests_Xml.cs` — XML-Auswertung der Icon-Quellen (gleiches Muster).

Ein „Manifest-Test“ für Issue #66 passt damit in `Tankradar.Tests.Unit` (z. B. neue `PlatformManifestTests_Backup.cs` oder `AndroidManifestTests.cs`): Manifest (und ggf. Regel-XML) als Text oder `XDocument` prüfen — `allowBackup`-Attribut, ggf. `dataExtractionRules`/`fullBackupContent`-Verweis und Existenz/Ausschlüsse der Regeldatei. Läuft unter Windows ohne Android-Workload — passend zur CI, die `IncludeAndroidTarget=false` setzt. Namenskonvention `<Thema>Tests_<Aspekt>.cs`, deutsche `<summary>`-Kommentare auf Klasse und Methoden (`CS1591` ist Fehler).

## Dokumentationslandschaft

| Stelle | Bezug zu Issue #66 |
|--------|---------------------|
| `docs/adr/0011-local-backup-restore.md` | Eigene Sicherung/Wiederherstellung; Abschnitt „Ablage unter iOS und iCloud-Gerätebackup“ (`:187–192`) entscheidet bewusst, eigene Sicherungen **nicht** vom iOS-Gerätebackup auszunehmen — Präzedenz, wie System-Backup-Fragen hier dokumentiert werden; ein Android-Gegenstück fehlt |
| `docs/adr/0015-ios-delivery-and-privacy.md` | SecureStorage-/Keychain-Zugriffsklasse, „gerätelokal“-Formulierung (`:19–20`) |
| `docs/adr/0019-backup-encryption.md` | optionaler Passwortschutz eigener Sicherungen (`TKBENC01`, AES-256-GCM) — relevant, weil unverschlüsselte Sicherungen in `backups/` liegen |
| `docs/adr/0016-maui-version-and-photo-permissions.md` | Kamera-/Foto-Berechtigungen (iOS/MacCatalyst); Android-Berechtigungen nicht Thema |
| `docs/adr/` | Nummern 0001–0021 belegt; nächste freie Nummer **0022** (falls die Entscheidung als ADR dokumentiert wird) |
| `docs/help/Sicherung/` | `beschreibung.md` (Abschnitt „Datenschutz und Sicherheit“ nennt nur das iOS-Gerätebackup), `ablauf-technisch.md:21–27` (Verzeichnisliste), `ablauf-anwender.md` — Android-Hinweise wären hier ergänzbar |
| `docs/help/Datenschutz/` | `beschreibung.md` („Schlüssel in der Keychain“: iOS/Windows; Android fehlt), `ablauf-technisch.md:11–12` (Protector-/SecureStorage-Tabelle) |
| `docs/help/index.md` | Index der Hilfebereiche; neue Seiten müssten verlinkt werden |
| `README.md:147` | „Android/MacCatalyst … nicht im Fokus“ — Rahmen für die Entscheidungstiefe |
| `changes.log` | Eintragsmuster `## YYYY-MM-DD — Titel (Issue #N)` mit Detailaufzählung |
| `docs/RELEASE_NOTES.md` | zweisprachig (EN „What's New/Important Notes“, DE gespiegelt); Einträge nur bei anwendersichtbarer Änderung — eine reine Manifest-/Backup-Entscheidung könnte hier entfallen oder unter „Wichtige Hinweise“ laufen |

## Offene Entscheidungspunkte, die die Bestandsaufnahme informiert

1. **Option 1 (`allowBackup="false"`)** ist mit den vorhandenen Strukturen eine Ein-Zeilen-Änderung und passt zur Rolle der eigenen Sicherung als einziger, anwendergesteuerter Backup-Weg (ADR 0011/0019) sowie zu „Android nicht im Fokus“. Sie löst zugleich das SecureStorage-Problem (nichts wandert mit) und verhindert das Mitgeben unverschlüsselter Sicherungen.
2. **Option 2 (Regeln)** ist aufwendiger (neue `Resources/xml/`-Dateien, Doppelpflege `<data-extraction-rules>` + `<full-backup-content>`, Exclude-Listen müssen mit `BackupFormat`-/`ReceiptPhotoStore`-Konstanten synchron gehalten werden) und müsste für die `SecureStorage`-Datei die `sharedpref`-Domain ausschließen. Nutzen: System-Restore von z. B. Einstellungen — die aber ohnehin komplett in `tankatlas.db` liegen und über die eigene Sicherung abgedeckt sind; einziger zusätzlicher Gewinn wäre `tankbook.selectedVehicleId` in `Preferences` (unkritisch).
3. Für beide Optionen bleibt der Manifest-Test plattformunabhängig ein Windows-Unit-Test nach dem `IosProjectFilesTests`-Muster; ein echter Android-Restore-Test hat keine Infrastruktur → das Akzeptanzkriterium „getestet oder dokumentiert“ fällt praktisch auf „dokumentiert“ (Datenschutz-Hilfe/ADR, siehe [securestorage-restore.md](securestorage-restore.md)).
