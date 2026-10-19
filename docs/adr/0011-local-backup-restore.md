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

Die Datenbank wird mit `VACUUM INTO` in eine temporäre Datei geschrieben, nicht per Dateikopie. So ist der Stand auch bei
laufender App und WAL-Modus konsistent. Anders als das Online-Backup (`BackupDatabase`) kopiert `VACUUM INTO` keine Seiten
unverändert, sondern baut die Datenbank aus den lebenden Zeilen neu auf: Gelöschte Inhalte (zum Beispiel ein gelöschtes Fahrzeug
oder dessen Notizen) bleiben nicht in freigegebenen Seiten lesbar. Das ist wichtig, weil Sicherungen unverschlüsselt geteilt werden
können; ein Test sucht eine gelöschte Zeichenkette in der Sicherung. Das Archiv entsteht als `.tmp` und wird erst am Ende
umbenannt; ein Fehler (Platte voll, Zugriff verweigert) hinterlässt keine halbe Datei.

Die Erstellung prüft das fertige Archiv mit derselben Strukturprüfung wie die Wiederherstellung (Eintrags-, Größen- und
Summengrenzen, zum Beispiel ein Belegfoto über `MaxReceiptPhotoBytes`). Überschreitet eine Sicherung diese Grenzen, wird sie nicht
abgelegt (`Failed`), statt eine Datei zu erzeugen, die später abgelehnt würde.

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
- Ältere Stände werden nicht in der Sicherungsdatenbank migriert, sondern in einer frisch angelegten Referenzdatenbank auf dem
  Migrationsstand der Sicherung neu aufgebaut und danach migriert (siehe „Sicherheit der Eingabedatei“). Integrationsgetestet mit einer
  bis `AddVehicles` und einer bis `AddFavorites` (Schema der Version 0.1.14) migrierten, befüllten Datenbank.

So kann eine ältere App keine neuere Datenbank „verbiegen“, und eine neuere App liest ältere Sicherungen.

### Atomarität und Wiederaufnahme

Alle fehleranfälligen Schritte laufen vor dem ersten Eingriff in die Livedaten: Validierung, Entpacken in
`restore-staging/<Guid>/` (gleiches Volumen), Prüfsummen, Neuaufbau der Datenbank aus einer frisch migrierten Referenz samt `PRAGMA integrity_check`,
`PRAGMA foreign_key_check` und Lesen aller Entitäten (siehe „Sicherheit der Eingabedatei“); die fertige Referenzdatenbank ersetzt im Staging die entpackte Datei. Erst dann tauscht `RestoreSwap` aus, und zwar nur durch Umbenennen: Journal `restore.journal`
schreiben, `ClearAllPools()`, Datenbank samt `-wal`/`-shm` und `receipts/` nach `restore-rollback/` verschieben, Staging-Stand
an die Zielorte verschieben, Dateischutz anwenden, Journal löschen (Commit-Punkt). Bei jeder Ausnahme gibt der Rollback den alten
Stand zurück. Die Datenbank kommt im Journalmodus `DELETE` an (damit keine fremden `-wal`/`-shm` mitkommen); nach dem Commit setzt
`BackupService` einmal `PRAGMA journal_mode=WAL`, sodass die Datenbank wieder im WAL-Modus der Normalkonfiguration läuft (getestet am
Dateikopf). Scheitert das, bleibt die Datenbank funktionsfähig im Modus `DELETE`. Stürzt der Prozess ab, nimmt `DatabaseInitializer` beim nächsten Start über `IRestoreRecovery` den Austausch anhand des
Journals zurück und räumt verwaiste Verzeichnisse auf. Die Fehlerinjektion an jedem Schritt (Hook im Konstruktor von `RestoreSwap`)
und ein simulierter Absturz sind integrationsgetestet.

Robustheit des Austauschs:

- Das Journal wird mit `Flush(true)` auf den Datenträger geschrieben, bevor etwas verschoben wird.
- Existiert beim Start einer Wiederherstellung noch ein Journal (früherer Austausch nicht abgeschlossen und nicht zurückgenommen), nimmt
  `RestoreSwap` zuerst diesen Austausch zurück. Misslingt das, endet die Wiederherstellung mit `Failed`; `restore-rollback/` (die
  einzige Kopie des alten Stands) und das Journal bleiben erhalten und werden nie gelöscht, solange ein Journal besteht.
- `RestoreSwap.Undo` gibt die Hauptdatei vor `-wal`/`-shm` zurück. Bricht ein Undo dazwischen ab, gilt die Hauptdatei beim nächsten Durchlauf nicht mehr als verschoben, und die bereits zurückgegebene `-wal` wird nicht als Rest der neuen Datenbank gelöscht (Test mit gesperrter `-wal`).
- `RestoreSwap.Undo` löscht ohne lesbares Journal nie etwas, sondern gibt nur die Rücksicherung zurück; das Journal entsteht vor dem Rollback-Block, ein Schreibfehler löst kein Undo aus. Undo verschiebt über `MoveWithRetry`.
- `RestoreRecovery` wirft bei erfolglosem Zurücknehmen eine `IOException`: Die Initialisierung scheitert (neuer Versuch möglich), `MigrateAsync` läuft nie auf dem Zwischenstand. Journal und Rücksicherung bleiben bestehen,
  und der nächste Start versucht es erneut.
- Die Verschiebungen im Austausch werden bei `IOException`/`UnauthorizedAccessException` bis zu zwanzigmal im Abstand von 150 ms (rund drei Sekunden)
  wiederholt, jeweils nach `ClearAllPools()`.
- Bekannte Einschränkung: Bricht eine Wiederherstellung nach dem Platzieren neuer Belegfotos ab und ist das Journal unlesbar (und gab es vorher kein Belegverzeichnis), bleiben die neuen Fotos neben dem alten Datenbestand liegen; es geht nichts verloren. Ein dauerhaft fehlschlagendes Zurücknehmen verzögert jeden Dienstaufruf um etwa drei Sekunden je Verschiebung und wird nur protokolliert.
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
  Prüfsummen schützen nur vor Beschädigung, nicht vor Manipulation: Wer eine Datei ändert, rechnet sie neu.
- **Neuaufbau statt Schemavergleich** (`BackupDatabasePreparer`): Ein Vergleich des Schemas (Objekte, Spalten, Indizes, Trigger)
  hat sich als unzureichend erwiesen. Er übersah Trigger mit dem für SQLite reservierten Namenspräfix `sqlite_`, Einschränkungen im
  Tabellentext (`CHECK`), den Inhalt von `sqlite_sequence`, ungültige Zeileninhalte und eine Zeile in `__EFMigrationsLock` (EF Core wartet
  dann in `MigrateAsync` ohne Zeitlimit). Deshalb übernimmt die App aus der Sicherung nur noch Daten, nie Schemaobjekte:
  1. Die Sicherungsdatenbank wird nur gelesen (`Mode=ReadOnly`): `integrity_check`, die letzte Zeile von `__EFMigrationsHistory` muss
     zum Manifest passen. Sie wird nie migriert und nie beschrieben; eine Zeile in ihrer Migrationssperre kann nichts blockieren,
     ein Trigger kann nie feuern.
  2. Im Arbeitsverzeichnis entsteht eine **Referenzdatenbank** mit der Migrationskette der App, und zwar bis zur Migration laut Manifest
     (`IMigrator.MigrateAsync(Zielmigration)`). Sie hat damit genau das Schema, das die Sicherung nach Angabe des Manifests haben muss.
     Scheitert ihre Anlage (zum Beispiel Speicher voll), lautet das Ergebnis `Failed`, nicht `InvalidFile`.
  3. Für jede Tabelle der Referenz (ohne `sqlite_*` und `__EFMigrations*`) werden die Zeilen mit ausdrücklichen Spaltenlisten
     (Namen aus der Referenz, in Anführungszeichen) und parametrisierten `INSERT`-Anweisungen kopiert, in einer Transaktion, mit
     abgeschalteten Fremdschlüsseln. Fehlt in der Sicherung eine Tabelle oder eine Spalte der Referenz, oder verletzt eine Zeile eine
     Bedingung der Referenz (`NOT NULL`, eindeutiger Index), ist die Datei ungültig. Zusätzliche Tabellen, Spalten,
     Indizes, Trigger, Views, Kollationen und Einschränkungen der Sicherung werden **nicht übernommen und nicht abgelehnt**: Sie haben
     nach dem Neuaufbau keine Wirkung. Auch `sqlite_sequence` wird nicht übernommen, sondern ergibt sich beim Einfügen der Zeilen mit
     ihren Ids (es gibt dadurch keinen manipulierten Zähler; ein früher gelöschter, höherer Id-Wert kann wiederverwendet werden, was hier unschädlich ist).
  4. `foreign_key_check`; danach Migration der Referenz auf den aktuellen Stand (Datenübernahmen der Migrationen laufen auf den kopierten
     Daten in dieser sauberen Datenbank); `integrity_check` und `foreign_key_check` erneut.
  5. Jede Entität wird einmal über Entity Framework gelesen (`AsNoTracking`, gestreamt): ungültige Datums- und Dezimalwerte
     (`FormatException` u. a.) führen zu `InvalidFile`; Text in Ganzzahl- oder Wahrheitswertspalten liest SQLite ohne Ausnahme als 0 bzw. wahr und wird daher nicht erkannt. Zusätzlich müssen die Enum-Zeichenketten, die die App nur gültig schreibt
     (`Vehicles.FuelKind`, `TankbookEntries.Kind`, `FavoriteEntries.Priority`), definierte Werte sein. **Textlängen** werden nach der letzten Migration
     (alle Spalten vorhanden) mit festen Obergrenzen je frei befüllbarer Textspalte geprüft (`BackupTextLimits`, SQL `length()` in Zeichen):
     eigener Anzeigename 60 (wie in der App), Namen von Tankstellen, Marken, Straßen, Orten, Gruppen, Fahrzeugen und Vorgängen 200,
     Kennungen, Hausnummern, Postleitzahlen und Schlüssel 100, Notizen und Beschreibungen 2.000, `OpeningTimesJson` 20.000. Sie liegen
     großzügig über dem, was die App und die Quelle liefern, aber weit unter Größen, bei denen die Oberfläche versagt (ein präparierter
     eigener Name mit 200.000 Zeichen ließ „Vorgang erfassen“ abstürzen). Überlange Texte ergeben `InvalidFile`; der Bestand bleibt
     unverändert. Kilometerstände und andere Zahlenwerte werden weiterhin nicht gemessen; die Anzeige kürzt lange Texte zusätzlich (ADR 0012).
     Einstellungen mit unbekannten Werten fängt die App beim Lesen selbst ab (Rückfall auf Standardwerte).
  6. Erst die so aufgebaute Referenzdatenbank (Journalmodus `DELETE`, ohne `-wal`/`-shm`) ersetzt im Staging die entpackte Datei und
     wird per `RestoreSwap` (Journal und Rollback) eingespielt. Die **Zählwerte der Rückfrage** (Fahrzeuge, Vorgänge, Gruppen, Favoriten,
     Stationen, Preisstände) werden aus dieser geprüften Datenbank gezählt; die Zahl der Fotos stammt aus der Dateiliste des Manifests,
     deren Dateien beim Wiederherstellen per Prüfsumme geprüft werden. Zählwerte im Manifest werden nie angezeigt.

  Gewählt wurde die Referenz **auf dem alten Migrationsstand** statt die Sicherungsdatenbank selbst zu migrieren: So bleibt die Quelle
  schreibgeschützt, es gibt keine Sperrtabelle der Quelle, auf die EF warten könnte, und fremde Trigger oder Views können bei den
  Datenmigrationen (`UPDATE` in `AddTankbookEntryDetails`) nie feuern. Die Kosten sind eine zweite Migration der Referenz für ältere Stände und ein zusätzlicher Speicherbedarf von etwa der Größe der Datenbank im Staging (es gibt keine Vorabprüfung des freien Platzes; Platzmangel führt zu `Failed`).
  Tabellen und Spalten werden aus der Referenz abgeleitet; neue Migrationen werden daher ohne Änderung am Neuaufbau erfasst (der
  Wächtertest `Create_DatabaseHasOnlyKnownTables` bleibt für den Umfang der Sicherung).
  Dieselbe Vorbereitung läuft schon beim Einspielen einer externen Datei (Entpacken ins Staging, Neuaufbau, Aufräumen), bei der Rückfrage (nur die
  Datenbank wird entpackt) und bei der Wiederherstellung: Eine unbrauchbare Datei kommt gar nicht erst in die Liste, und die Meldung
  „gültige Sicherung“ erscheint erst danach.
  Die gesamte Vorbereitung hat eine Höchstdauer (`BackupLimits.PrepareTimeoutSeconds`, zwei Minuten); danach endet sie mit `Failed`, der Bestand bleibt
  unverändert, Staging und Referenz werden entfernt, und das ViewModel gibt die Schaltflächen im `finally` wieder frei (der Dienst ist mit Zeitlimit 0 getestet, das Verhalten des ViewModels nicht gesondert). Das
  Abbruchsignal wird zwischen den Anweisungen und Zeilen geprüft; eine einzelne SQLite-Anweisung ist nicht unterbrechbar, ihre
  Dauer ist aber durch die Größengrenzen der Datenbank beschränkt.
  Getestet mit Integrations- und E2E-Tests bei gültigem Manifest und gültigen Prüfsummen: Trigger (auch mit Präfix `sqlite_`), View, zusätzlicher `CHECK`,
  manipulierte `sqlite_sequence`, Zeile in `__EFMigrationsLock`, entfernte Kollation, zusätzliche Spalte, Tabelle und Index (alle ohne Wirkung,
  App danach benutzbar, Daten vollständig, Schema gleich dem einer frischen Datenbank); fehlende Tabelle oder Spalte, ungültiges Datum, unbekannte
  Kraftstoffart und Vorgang ohne Fahrzeug (abgelehnt, Bestand unverändert).
  Bekannte Grenze: Daten gelten als Nutzerdaten. Werte, die weder ein Schema noch die Entitäten verletzen, aber unsinnig sind (zum Beispiel ein
  Kilometerstand von 0 oder ein erfundener Tankstellenname), werden übernommen; das betrifft nur den Anwender selbst.
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

### Ablage unter iOS und iCloud-Gerätebackup

Das Verzeichnis `backups/` liegt unter `FileSystem.AppDataDirectory` (iOS: `Library`) und wird damit wie die Datenbank vom
Gerätebackup des Systems (iCloud oder Computer) erfasst. Das ist keine Übertragung durch die App, sondern eine Einstellung des
Anwenders am Gerät. Entscheidung: **Sicherungen werden nicht vom Gerätebackup ausgenommen.** Sie sind ein zusätzlicher Schutz (die
Datenbank steckt ohnehin im Gerätebackup, die Sicherungen sind nicht sensibler), und ein Ausschluss über
`NSUrl.SetResource(ExcludedFromBackup)` würde Sicherungen bei einem Gerätewechsel ohne Not verlieren. Die Hilfe nennt das.

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
  Archivformat selbst, ist `schemaVersion` zu erhöhen. Beispiel Schritt 15a: Die neue Spalte `Stations.CustomName` (eigener
  Anzeigename, [ADR 0012](0012-station-display-name.md)) steckt automatisch in neuen Sicherungen; der Neuaufbau kopiert die
  Spalten der Referenzdatenbank auf dem Migrationsstand der Sicherung, ältere Sicherungen ohne die Spalte werden eingespielt und
  migriert (Integrationstests `BackupServiceTests_CustomName`).
- **Fehlerart bei Dateifehlern (Schritt 15a):** Ein Fehler beim Lesen der Sicherungsdatenbank selbst (Öffnen, Integritätsprüfung,
  Lesen der Tabellen, auch Ein-/Ausgabefehler einer abgeschnittenen Datei) gilt als ungültige Datei (`InvalidFile`). Als
  vorübergehender Fehler der Umgebung (`Failed`) gelten nur Fehler der Referenzdatenbank und des Dateisystems, z. B. voller
  Speicher.
- **Datenschutz:** Die gespeicherten Tankstellen und Preisstände mit Abrufzeit sowie die eigenen Anzeigenamen stehen in der
  Sicherung. Daraus lassen sich Gegenden und Zeiten ableiten, in denen gesucht wurde. Weil geteilte Sicherungen unverschlüsselt
  sind, nennt die Hilfe das ausdrücklich.
- **Das iOS-Verhalten** (Teilen-Blatt, Dateiauswahl) wird nur kompiliert und über die Testdoppel abgesichert; eine Sichtprüfung auf
  einem Gerät bleibt Teil der Abnahme.
- **Datenmenge:** Dauer und Größe wachsen mit Datenbank und Fotos. Verarbeitet wird mit Stream-Kopien (80-KiB-Puffer), nichts wird
  vollständig in den Speicher geladen.
