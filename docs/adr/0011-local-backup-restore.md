# ADR 0011: Lokale Sicherung und Wiederherstellung

## Status

Angenommen (2026-10-12)

## Kontext

Schritt 15 ergänzt in den Optionen eine Sicherung der Nutzerdaten: auf Veranlassung des Anwenders erstellen, auflisten,
wiederherstellen, löschen, über die Systemfunktion „Teilen/Speichern“ weitergeben und eine externe Datei einspielen. Die Daten
(SQLite-Datenbank, Belegfotos) liegen ausschließlich auf dem Gerät; die App überträgt nichts selbst. Die Anforderung verlangt,
dass die Wiederherstellung die Daten vollständig ersetzt, bei einem Fehler nichts verändert und ungültige, beschädigte oder zu
neue Dateien verständlich ablehnt. Grundlage sind [ADR 0008](0008-vehicle-management.md) bis [ADR 0010](0010-tankbook-statistics.md).

## Entscheidungen

### Archivformat

Eine Sicherung ist eine ZIP-Datei `tankatlas-backup-<JJJJMMTT-HHmmss>[-n].zip` im Verzeichnis `backups/` des Datenverzeichnisses
(Dateischutz wie die Datenbank). Sie enthält genau drei Arten von Einträgen, alles andere wird abgelehnt:

- `manifest.json` mit `format` (`Tankatlas-Backup`), `schemaVersion` (Formatversion, derzeit 1), `appVersion`, `databaseMigration`
  (letzte angewendete EF-Migration), `createdUtc`, der Dateiliste (Pfad, Größe, SHA-256) und Zählwerten für die Rückfrage,
- `database/tankatlas.db`, ein konsistenter Schnappschuss,
- `receipts/<Kennung>.<Endung>` für jedes gültige Belegfoto (Namensregeln von `ReceiptPhotoStore`).

Die Datenbank wird mit dem SQLite-Online-Backup (`SqliteConnection.BackupDatabase`, Teil von `Microsoft.Data.Sqlite`) in eine
temporäre Datei kopiert, nicht per Dateikopie. So ist der Stand auch bei laufender App und WAL-Modus konsistent. Das Archiv
entsteht als `.tmp` und wird erst am Ende umbenannt; ein Fehler (Platte voll, Zugriff verweigert) hinterlässt keine halbe Datei.

### Umfang: Was nicht gesichert wird

Nicht enthalten sind der Zugangsschlüssel zum Preisdienst (er liegt nicht in der Datenbank, sondern im Build bzw. in der
Umgebung), Kartenkacheln und der Standort (wird nie gespeichert, ADR 0003). Die Datenbank enthält nur die bekannten Tabellen
(Einstellungen, Spritsorten, Stationen, Preisstände, Favoritengruppen und -einträge, Fahrzeuge, Vorgänge); das ist integrationsgetestet.
Preisstände werden mit ihren Zeitstempeln gesichert; nach der Wiederherstellung gelten sie über die vorhandene Alterslogik als
veraltet und werden bei Bedarf neu abgerufen. Dazu verwirft `FuelPriceService` über `IPriceCacheInvalidator` seine
Zwischenspeicher.

### Versionsregeln

- `schemaVersion` größer als die der App: „zu neu“ (`BackupResult.TooNew`).
- `databaseMigration` ist eine Migration der App: erlaubt. Ist sie unbekannt und lexikografisch größer als die letzte bekannte
  (Migrations-IDs tragen einen Zeitstempel und sind ordenbar): „zu neu“; sonst unbekannt und kleiner: ungültig.
- Die tatsächliche Migrationshistorie der enthaltenen Datenbank muss zum Manifest passen.
- Ältere Stände werden im Staging mit `MigrateAsync` auf den aktuellen Stand gebracht (integrationsgetestet mit einer bis
  `AddVehicles` migrierten und befüllten Datenbank).

So kann eine ältere App keine neuere Datenbank „verbiegen“, und eine neuere App liest ältere Sicherungen.

### Atomarität und Wiederaufnahme

Alle fehleranfälligen Schritte laufen vor dem ersten Eingriff in die Livedaten: Validierung, Entpacken in
`restore-staging/<Guid>/` (gleiches Volumen), Prüfsummen, `PRAGMA integrity_check`, Migration der Staging-Datenbank,
`PRAGMA foreign_key_check`. Erst dann tauscht `RestoreSwap` aus, und zwar nur durch Umbenennen: Journal `restore.journal`
schreiben, `ClearAllPools()`, Datenbank samt `-wal`/`-shm` und `receipts/` nach `restore-rollback/` verschieben, Staging-Stand
an die Zielorte verschieben, Dateischutz anwenden, Journal löschen (Commit-Punkt). Bei jeder Ausnahme gibt der Rollback den alten
Stand zurück. Stürzt der Prozess ab, nimmt `DatabaseInitializer` beim nächsten Start über `IRestoreRecovery` den Austausch anhand des
Journals zurück und räumt verwaiste Verzeichnisse auf. Die Fehlerinjektion an jedem Schritt (Hook im Konstruktor von `RestoreSwap`)
und ein simulierter Absturz sind integrationsgetestet.

Robustheit des Austauschs:

- Das Journal wird mit `Flush(true)` auf den Datenträger geschrieben, bevor etwas verschoben wird.
- Existiert beim Start einer Wiederherstellung noch ein Journal (früherer Austausch nicht abgeschlossen und nicht zurückgenommen), nimmt
  `RestoreSwap` zuerst diesen Austausch zurück. Misslingt das, endet die Wiederherstellung mit `Failed`; `restore-rollback/` (die
  einzige Kopie des alten Stands) und das Journal bleiben erhalten und werden nie gelöscht, solange ein Journal besteht.
- `RestoreSwap.Undo` löscht ohne lesbares Journal nie etwas, sondern gibt nur die Rücksicherung zurück; das Journal entsteht vor dem Rollback-Block, ein Schreibfehler löst kein Undo aus. Undo verschiebt über `MoveWithRetry`.
- `RestoreRecovery` wirft bei erfolglosem Zurücknehmen eine `IOException`: Die Initialisierung scheitert (neuer Versuch möglich), `MigrateAsync` läuft nie auf dem Zwischenstand. Journal und Rücksicherung bleiben bestehen,
  und der nächste Start versucht es erneut.
- Die Verschiebungen im Austausch werden bei `IOException`/`UnauthorizedAccessException` bis zu zwanzigmal im Abstand von 150 ms (rund drei Sekunden)
  wiederholt, jeweils nach `ClearAllPools()`.
- Offen und bewusst akzeptiert: Es gibt keine gemeinsame Sperre zwischen `BackupService` und den übrigen Diensten, die über
  `IDbContextFactory` parallel Verbindungen öffnen. Hält eine solche Verbindung die Datei länger als die Wiederholungen, scheitert das
  Verschieben (Windows) und der Rollback gibt den alten Stand zurück (`Failed`); unter iOS kann eine Verbindung auf der verschobenen
  Datei weiterarbeiten. Die Oberfläche sperrt während der Arbeit die Schaltflächen, und der Preisdienst-Zwischenspeicher wird
  verworfen (`IPriceCacheInvalidator`). Weitere Singletons mit Zwischenspeichern wurden nicht gefunden, aber nicht systematisch
  geprüft. Eine App-weite Lesen/Schreiben-Sperre wäre die vollständige Lösung.

Es läuft höchstens eine verändernde Operation gleichzeitig (`SemaphoreSlim`, sonst `Busy`); die Oberfläche sperrt während der
Arbeit alle Schaltflächen.

### Sicherheit der Eingabedatei

Eine externe Datei ist nicht vertrauenswürdig. Geprüft wird in zwei Stufen (Struktur ohne Entpacken, dann Inhalt):

- **Whitelist der Eintragsnamen** (Zip-Slip): nur `manifest.json`, `database/tankatlas.db`, `receipts/<gültiger Fotoname>`; keine
  Duplikate (ohne Beachtung der Groß-/Kleinschreibung), keine Verzeichniseinträge, kein `\`, `..`, `:` oder führendes `/`.
- **Eigene Kopierschleife** statt `ExtractToDirectory`: Zielpfad = feste Basis plus geprüfter Name, zusätzlich
  Präfixprüfung nach `Path.GetFullPath`.
- **Grenzen gegen Zip-Bomben** (`BackupLimits`): höchstens 20.000 Einträge, 2 GiB Gesamtgröße, 1 GiB Datenbank, 1 MiB Manifest,
  Foto höchstens `MaxReceiptPhotoBytes`; die tatsächlich gelesenen Bytes dürfen die deklarierte Größe nicht überschreiten.
- **Prüfsummen** (SHA-256) aller Dateien und die Fotosignatur (`DetectExtension`) beim Import und beim Entpacken.
- **Dateinamen** für Löschen, Teilen und Wiederherstellen müssen dem Muster `tankatlas-backup-*.zip` entsprechen und dürfen
  keine Pfadanteile haben; sonst gilt die Sicherung als nicht gefunden.

Fehler werden ohne Pfade und Inhalte protokolliert (nur der Ausnahmetyp).

### Plattformdienste und Testmodus

Dateiauswahl (`IBackupFilePicker`) und Weitergabe (`IBackupSharer`) sind Schnittstellen. Produktiv öffnet `MauiBackupFilePicker`
den Dateidialog (ZIP-Typen je Plattform) und `MauiBackupSharer` die Systemfunktion „Teilen“ (iOS: Teilen-Blatt mit „In Dateien
sichern“, Windows: Freigabe-Dialog). Im Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) ersetzen `TestBackupFilePicker` (liefert die Datei aus
`TANKATLAS_TEST_BACKUP_IMPORT`; ohne Wert gilt die Auswahl als abgebrochen) und `TestBackupSharer` (kopiert nach
`<Daten>/test-shared/`) die Dialoge, damit die E2E-Tests nicht an Systemdialogen hängen. Die Auswahl trifft ein Selector; die
Plattformklassen werden im Testmodus nie erzeugt.

`Info.plist` wurde geprüft: Weder die Dateiauswahl noch das Teilen-Blatt brauchen einen zusätzlichen Eintrag (kein
Fotozugriff, kein Zugriff außerhalb des App-Containers ohne Systemdialog). `MtouchInterpreter` und `TrimmerRootAssembly` bleiben
unverändert; `System.IO.Compression` und `System.Text.Json` gehören zur Laufzeit, es gibt keine neue Abhängigkeit.

### Löschrückfrage für Fahrzeuge

Hat ein Fahrzeug Vorgänge, ergänzt die Löschrückfrage den Satz „Erstelle vorher in den Optionen eine Sicherung, wenn du die
Vorgänge behalten möchtest.“ Das verbindet den Bereich Tankbuch mit der neuen Funktion, ohne die Löschlogik zu ändern.

## Abweichungen vom Designentwurf

Für die Karte „Sicherung“ gibt es keinen Designentwurf. Sie verwendet die vorhandenen Bausteine der Optionen (Karte
`SettingsCard`, Überschrift, Schaltflächenstile aus `TankbookStyles`) und die Inline-Rückfragen des Tankbuchs (keine
Systemdialoge). Es gibt keine eigene Seite und kein neues Symbol.

## Folgen

- **Sicherungen sind unverschlüsselt.** Sie enthalten Fahrdaten und Belege. Die Datei im Datenverzeichnis hat den Dateischutz
  der App; eine geteilte oder gespeicherte Kopie liegt außerhalb davon. Die Oberfläche und die Hilfe weisen darauf hin. Eine
  Verschlüsselung mit Passwort wäre eine eigene Entscheidung (Schlüsselverwaltung, Verlustrisiko).
- **Keine automatische Sicherung** und keine Cloud-Anbindung; beides ist nicht Teil der Anforderung.
- **Ein Wechsel des Datenformats** (neue Tabellen, neue Migrationen) bleibt über die Migrationsregeln abgedeckt; ändert sich das
  Archivformat selbst, ist `schemaVersion` zu erhöhen.
- **Das iOS-Verhalten** (Teilen-Blatt, Dateiauswahl) wird nur kompiliert und über die Testdoppel abgesichert; eine Sichtprüfung auf
  einem Gerät bleibt Teil der Abnahme.
- **Datenmenge:** Dauer und Größe wachsen mit Datenbank und Fotos. Verarbeitet wird mit Stream-Kopien (80-KiB-Puffer), nichts wird
  vollständig in den Speicher geladen.
