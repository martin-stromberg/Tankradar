← [Zurück zur Übersicht](index.md)

# Sicherung — Technischer Ablauf

## Komponenten

| Komponente | Aufgabe |
|-----------|---------|
| `IBackupService` / `BackupService` | Erstellen, Auflisten, Prüfen (`InspectAsync`), Wiederherstellen, Importieren, Löschen, Pfad ermitteln; höchstens eine verändernde Operation gleichzeitig (sonst `Busy`) |
| `BackupArchiveWriter` | schreibt das ZIP (`database/tankatlas.db`, `receipts/*`, zuletzt `manifest.json`) und berechnet SHA-256 |
| `BackupArchiveValidator` | Namens-Whitelist, Grenzen, Manifest, Versionsregeln, Prüfsummen, Entpacken ins Staging |
| `RestoreSwap` / `RestoreJournal` | Austausch durch Umbenennen mit Rücksicherung und Rollback; Journal `restore.journal` |
| `IRestoreRecovery` / `RestoreRecovery` | nimmt beim Start (im `DatabaseInitializer`) einen unterbrochenen Austausch zurück |
| `IBackupFilePicker`, `IBackupSharer` | Dateiauswahl und Teilen; MAUI-Umsetzung oder Testdoppel (Selector) |
| `IPriceCacheInvalidator` | `FuelPriceService.InvalidateCaches` nach der Wiederherstellung |
| `BackupViewModel`, `BackupItemViewModel` | Liste, Befehle, Rückfragen, Status; in `SettingsViewModel` eingebettet (`Backup`, `HasBackup`) |
| `BackupTexts`, `BackupFormat`, `BackupLimits` | alle Texte, Dateinamen und Format, Grenzen |

## Verzeichnisse im Datenverzeichnis

- `backups/` — die Sicherungen (`tankatlas-backup-<JJJJMMTT-HHmmss>[-n].zip`); `.tmp`-Dateien und `.work-*` sind Arbeitsstand und werden beim Auflisten entfernt.
- `restore-staging/<Guid>/` — entpackter Stand während der Wiederherstellung.
- `restore-rollback/` — bisheriger Stand bis zum Commit.
- `restore.journal` — vorhanden, solange ein Austausch läuft.
- `test-shared/` — nur im Testmodus: Ziel von „Teilen“.

## Erstellen

1. `BackupService` wartet auf seine Sperre, initialisiert die Datenbank und legt `backups/` und ein Arbeitsverzeichnis an.
2. `VACUUM INTO` schreibt die Datenbank konsistent und ohne gelöschte Inhalte in eine temporäre Datei (auch bei WAL); aus der Kopie werden Migrationsstand und Zählwerte gelesen.
3. `BackupArchiveWriter` schreibt `….zip.tmp`: Datenbank, jedes gültige Foto aus `receipts/`, zuletzt das Manifest.
4. Das fertige Archiv wird mit `BackupArchiveValidator.Validate` auf die Grenzen der Wiederherstellung geprüft (sonst `Failed`, keine Datei). Dann Umbenennen in `*.zip`, Dateischutz (`ProtectFile`), Aufräumen des Arbeitsverzeichnisses (auch im Fehlerfall).

## Wiederherstellen

1. `BackupArchiveValidator.Validate`: Eintragsnamen (Zip-Slip), Grenzen, Manifest, Dateiliste = Einträge, Versionsregeln → `InvalidFile` oder `TooNew`.
2. Entpacken ins Staging mit eigener Kopierschleife, Größenzählung und SHA-256-Vergleich; Fotos zusätzlich `DetectExtension`.
3. `BackupDatabasePreparer` prüft die Staging-Datenbank (ohne Pool): `integrity_check`, Migrationshistorie gegen Manifest, `MigrateAsync`, Schemavergleich mit einer frisch migrierten Referenzdatenbank (`BackupSchemaDescriber`; Abweichung → `InvalidFile`), `foreign_key_check`.
4. `RestoreSwap.Execute`: Journal, `ClearAllPools()`, alten Stand nach `restore-rollback/`, neuen Stand an die Zielorte, `Protect`/`ProtectFile`, Journal löschen (Commit), Aufräumen. Bei jeder Ausnahme: neue Dateien entfernen, alte zurück, Journal löschen, Ergebnis `Failed`.
5. Nach Erfolg `PRAGMA journal_mode=WAL` auf der neuen Datenbank, dann `IPriceCacheInvalidator.InvalidateCaches()`; `BackupViewModel` löst `Restored` aus, `SettingsViewModel` lädt die Einstellungen neu, die übrigen Seiten laden in `OnAppearing`.

Stürzt der Prozess zwischen Schritt 4 und dem Commit ab, ruft `DatabaseInitializer` beim nächsten Start `RestoreRecovery.RecoverIfNeeded` vor `MigrateAsync` auf: Mit Journal wird der Austausch zurückgenommen; ohne Journal werden nur verwaiste Verzeichnisse gelöscht.

## Importieren, Löschen, Teilen

- **Import:** Kopie nach `backups/import-<Guid>.tmp` (mit Größengrenze), Strukturprüfung, Entpacken ins Staging und dieselbe Datenbankprüfung wie beim Wiederherstellen (Prüfsummen, Migration, Schemavergleich), Aufräumen des Staging, dann Umbenennen in `tankatlas-backup-<Zeitstempel des Manifests>.zip` (bei Kollision Zähler). Fehler entfernen die `.tmp`-Datei.
- **Löschen und Teilen:** `GetFullPath` lässt nur Namen nach dem Muster `tankatlas-backup-*.zip` ohne Pfadanteile zu; sonst `NotFound`.

## Ergebnisse

`BackupResult`: `Ok`, `NotFound`, `InvalidFile`, `TooNew`, `Busy`, `Failed`. Die zugehörigen Meldungen liefert `BackupTexts.GetMessage`.

## Testmodus

Mit `TANKATLAS_TEST_DATA_PATH` ersetzen `TestBackupFilePicker` (Datei aus `TANKATLAS_TEST_BACKUP_IMPORT`, ohne Wert gilt die Auswahl als abgebrochen) und `TestBackupSharer` (Kopie nach `test-shared/`) die Systemdialoge.

## Tests

- Unit: `BackupArchiveValidatorTests_*`, `BackupVersionRuleTests`, `BackupTextsTests_*`, `RestoreRecoveryTests`, `BackupViewModelTests_*`, `SettingsViewModelTests_Backup`, `BackupFilePickerSelectorTests_TestMode`, `BackupSharerSelectorTests_TestMode`, `TestBackupSharerTests`, `FuelPriceServiceTests_InvalidateCaches`, `StatisticsTileFontTests`.
- Integration (echte SQLite-Datei): `BackupServiceTests_Create` (Umfang, Manifest, Vollständigkeit), `_Restore`, `_Versions` (ältere, zu neue, ungültige Dateien), `_Atomicity` (Fehlerinjektion an jedem Schritt, Absturz), `_ListDeleteImport`.
- E2E (FlaUI): `BackupE2ETests_Flow`, `_List`, `_Photo`, `_Persistence`, `_Import`, `_InvalidFiles`, `_Share`, `TankbookStatisticsE2ETests_TileNumbers`, Erweiterung von `TankbookE2ETests_DeleteWithEntries`.

← [Zurück zur Übersicht](index.md)
