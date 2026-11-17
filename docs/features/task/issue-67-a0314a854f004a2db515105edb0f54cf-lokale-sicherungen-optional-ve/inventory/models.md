# Datenmodell: Lokale Sicherung

Bestand der Datenmodellklassen unter `src/Tankradar.MAUI/Models/Backup/` sowie des testseitigen Manipulations-Enums in `src/TestSupport/`. Es existiert **keinerlei** Verschlüsselungskennung, kein Container-Header, kein Passwort- oder KDF-bezogenes Feld.

## `BackupFormat` (statische Klasse)

Datei: `src/Tankradar.MAUI/Models/Backup/BackupFormat.cs`

| Mitglied | Typ | Beschreibung / Zweck |
|----------|-----|----------------------|
| `FormatName` | `const string` = `"Tankatlas-Backup"` | Wert des Manifestfelds `format` |
| `SchemaVersion` | `const int` = `1` | Aktuelle Formatversion des Sicherungsformats (liegt im Manifest, also innerhalb des Archivs) |
| `ManifestEntryName` | `const string` = `"manifest.json"` | Name des Manifesteintrags im ZIP |
| `DatabaseEntryName` | `const string` = `"database/tankatlas.db"` | Name des Datenbankeintrags im ZIP |
| `ReceiptEntryPrefix` | `const string` = `"receipts/"` | Präfix der Belegfotos im ZIP |
| `BackupDirectoryName` | `const string` = `"backups"` | Unterverzeichnis der Sicherungen im Datenverzeichnis |
| `StagingDirectoryName` | `const string` = `"restore-staging"` | Verzeichnis, in das eine Wiederherstellung entpackt |
| `RollbackDirectoryName` | `const string` = `"restore-rollback"` | Zwischenlager des bisherigen Stands beim Austausch |
| `JournalFileName` | `const string` = `"restore.journal"` | Journaldatei eines laufenden Austauschs |
| `TestSharedDirectoryName` | `const string` = `"test-shared"` | Zielverzeichnis der Testweitergabe |
| `FileNamePrefix` | `const string` = `"tankatlas-backup-"` | Anfang des Sicherungsdateinamens |
| `FileExtension` | `const string` = `".zip"` | Endung einer Sicherung |
| `TempExtension` | `const string` = `".tmp"` | Endung unfertiger Dateien |
| `WorkDirectoryPrefix` | `const string` = `".work-"` | Anfang der Namen temporärer Arbeitsverzeichnisse |
| `IsValidFileName(string?)` | `static bool` | Prüft `tankatlas-backup-*.zip` per `GeneratedRegex` (`^tankatlas-backup-[0-9A-Za-z_-]+(\.[0-9A-Za-z_-]+)*\.zip$`), Länge ≤ 100, keine Pfadanteile |

Hinweis: Eine verschlüsselte Datei mit anderem Namensschema oder anderer Endung würde von `IsValidFileName` derzeit abgelehnt (betrifft `GetFullPath`, `ListAsync`, Dateiauswahl-Filter). Es gibt keine Magic-Bytes- oder Versionskennung außerhalb des ZIP-Archivs.

## `BackupManifest`

Datei: `src/Tankradar.MAUI/Models/Backup/BackupManifest.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Format` | `string` (`format`) | Muss `BackupFormat.FormatName` sein |
| `SchemaVersion` | `int` (`schemaVersion`) | Formatversion; Standard `BackupFormat.SchemaVersion` (1) |
| `AppVersion` | `string` (`appVersion`) | Version der erstellenden App |
| `DatabaseMigration` | `string` (`databaseMigration`) | Letzte angewendete EF-Migration der gesicherten Datenbank |
| `CreatedUtc` | `DateTime` (`createdUtc`) | Erstellungszeitpunkt (UTC) |
| `Files` | `List<BackupManifestFile>` (`files`) | Dateiliste (ohne das Manifest selbst) |
| `Counts` | `BackupManifestCounts` (`counts`) | Zählwerte des Inhalts |
| `ToJson()` | `byte[]` | Serialisiert als UTF-8-JSON (eingerückt) |
| `FromJson(Stream)` | `static BackupManifest?` | Deserialisiert; `null` bei ungültigem JSON oder fehlenden `Files`/`Counts` |

Das Manifest liegt vollständig im ZIP-Nutzdatenbereich; bei verschlüsselten Sicherungen wäre `SchemaVersion` vor der Entschlüsselung nicht lesbar (in der Anforderung als zu lösendes Problem benannt).

## `BackupManifestCounts`

Datei: `src/Tankradar.MAUI/Models/Backup/BackupManifestCounts.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Vehicles` | `int` (`vehicles`) | Zahl der Fahrzeuge |
| `Entries` | `int` (`entries`) | Zahl der Tank- und Ladevorgänge |
| `Groups` | `int` (`groups`) | Zahl der Favoritengruppen |
| `Favorites` | `int` (`favorites`) | Zahl der Favoriten |
| `Stations` | `int` (`stations`) | Zahl der lokal bekannten Tankstellen |
| `Prices` | `int` (`prices`) | Zahl der gespeicherten Preisstände |
| `Photos` | `int` (`photos`) | Zahl der Belegfotos |

## `BackupManifestFile`

Datei: `src/Tankradar.MAUI/Models/Backup/BackupManifestFile.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Path` | `string` (`path`) | Pfad im Archiv |
| `Size` | `long` (`size`) | Entpackte Größe in Bytes |
| `Sha256` | `string` (`sha256`) | SHA-256-Prüfsumme, hexadezimal klein |

## `BackupFileInfo` (sealed record)

Datei: `src/Tankradar.MAUI/Models/Backup/BackupFileInfo.cs`

| Positionsparameter | Typ | Beschreibung / Zweck |
|--------------------|-----|----------------------|
| `FileName` | `string` | Dateiname im Sicherungsverzeichnis |
| `CreatedUtc` | `DateTime` | Erstellungszeitpunkt laut Manifest; bei nicht lesbaren Dateien das Änderungsdatum |
| `SizeBytes` | `long` | Dateigröße |
| `AppVersion` | `string` | App-Version laut Manifest; leer bei nicht lesbaren Dateien |
| `IsReadable` | `bool` | Ob das Manifest gelesen werden konnte |

Es gibt keine Kennzeichnung „verschlüsselt“. Für verschlüsselte Dateien würde `IsReadable` heute `false` ergeben (Manifest nicht als ZIP lesbar) — Auswirkung auf `ShowRestoreQuestionAsync` (stellt sofort `InvalidFile` dar).

## `BackupOutcome` (sealed record)

Datei: `src/Tankradar.MAUI/Models/Backup/BackupOutcome.cs`

| Positionsparameter | Typ | Beschreibung / Zweck |
|--------------------|-----|----------------------|
| `Result` | `BackupResult` | Ergebnis der Operation |
| `File` | `BackupFileInfo?` | Betroffene Sicherung; `null`, wenn es keine gibt |
| `Manifest` | `BackupManifest?` | Gelesenes Manifest (nur bei Prüfung/`TooNew`) |

## `BackupLimits` (statische Klasse)

Datei: `src/Tankradar.MAUI/Models/Backup/BackupLimits.cs`

| Konstante | Typ | Wert | Zweck |
|-----------|-----|------|-------|
| `BufferSize` | `int` | 81.920 | Puffergröße beim Kopieren/Archivieren |
| `MaxEntries` | `int` | 20.000 | Höchstzahl der Archiveinträge |
| `MaxTotalBytes` | `long` | 2 GiB | Höchstsumme entpackter Größen; auch Import-Größengrenze (`MaxTotalBytes + MaxManifestBytes` in `CopyLimitedAsync`) |
| `MaxDatabaseBytes` | `long` | 1 GiB | Höchstgröße der entpackten Datenbank |
| `MaxManifestBytes` | `long` | 1 MiB | Höchstgröße des Manifests |
| `PrepareTimeoutSeconds` | `int` | 120 | Höchstdauer der Datenbankaufbereitung |

Es existieren keine Grenzen für einen verschlüsselten Container (Header-Größe o. ä.).

## `BackupTextLimits` (statische Klasse)

Datei: `src/Tankradar.MAUI/Models/Backup/BackupTextLimits.cs`

| Mitglied | Typ | Beschreibung / Zweck |
|----------|-----|----------------------|
| `Columns` | `IReadOnlyList<(string Table, string Column, int MaxLength)>` | Obergrenzen je frei befüllbarer Textspalte (100 / 200 / 2.000 / 20.000 Zeichen; `Stations.CustomName` aus `StationNaming.MaxCustomNameLength`); geprüft von `BackupDatabasePreparer` |

## `PickedBackupFile` (sealed class)

Datei: `src/Tankradar.MAUI/Models/Backup/PickedBackupFile.cs`

| Mitglied | Typ | Beschreibung / Zweck |
|----------|-----|----------------------|
| `FileName` | `string` | Name der gewählten Datei |
| `OpenReadAsync()` | `Task<Stream>` | Öffnet den Inhalt; Aufrufer schließt den Strom |

Konstruktor: `PickedBackupFile(string fileName, Func<Task<Stream>> openRead)`.

## `BackupPreparation` (sealed record, Services-Namespace)

Datei: `src/Tankradar.MAUI/Services/Backup/BackupPreparation.cs`

| Positionsparameter | Typ | Beschreibung / Zweck |
|--------------------|-----|----------------------|
| `Result` | `BackupResult` | Ergebnis der Datenbankvorbereitung |
| `Counts` | `BackupManifestCounts?` | Zählwerte aus der geprüften Datenbank (nur bei `Ok`) |

## `RestoreJournal` (Klasse, Services-Namespace)

Datei: `src/Tankradar.MAUI/Services/Backup/RestoreJournal.cs`

| Mitglied | Typ | Beschreibung / Zweck |
|----------|-----|----------------------|
| `HadDatabase` | `bool` (`hadDatabase`) | Vor dem Austausch vorhandene Datenbank |
| `HadReceipts` | `bool` (`hadReceipts`) | Vor dem Austausch vorhandenes Belegverzeichnis |
| `GetPath` / `Write` / `Read` / `Exists` / `Delete` | `static` | Journalverwaltung im Datenverzeichnis (`restore.journal`, atomar über `.tmp` + `Flush(true)`) |
