← [Zurück zur Übersicht](index.md)

# Sicherung — Technischer Ablauf

## Komponenten

| Komponente | Aufgabe |
|-----------|---------|
| `IBackupService` / `BackupService` | Erstellen, Auflisten, Prüfen (`InspectAsync`), Wiederherstellen, Importieren, Löschen, Pfad ermitteln; höchstens eine verändernde Operation gleichzeitig (sonst `Busy`); alle Kernmethoden mit optionalem `password` |
| `BackupArchiveWriter` | schreibt das ZIP (`database/tankatlas.db`, `receipts/*`, zuletzt `manifest.json`) und berechnet SHA-256 |
| `BackupCrypto`, `BackupEncryptionHeader` | optionaler Passwortschutz (ADR 0019): unverschlüsselter Container-Header (Magic `TKBENC01`, Version, PBKDF2-HMAC-SHA256 mit 600 000 Iterationen, Salt, Nonce-Basis, Chunk-Größe) plus AES-256-GCM-verschlüsseltes ZIP in Chunks mit AAD |
| `BackupArchiveValidator` | Namens-Whitelist, Grenzen, Manifest, Versionsregeln, Prüfsummen, Entpacken ins Staging |
| `RestoreSwap` / `RestoreJournal` | Austausch durch Umbenennen mit Rücksicherung und Rollback; Journal `restore.journal` |
| `IRestoreRecovery` / `RestoreRecovery` | nimmt beim Start (im `DatabaseInitializer`) einen unterbrochenen Austausch zurück |
| `IBackupFilePicker`, `IBackupSharer` | Dateiauswahl und Teilen; MAUI-Umsetzung oder Testdoppel (Selector) |
| `IPriceCacheInvalidator` | `FuelPriceService.InvalidateCaches` nach der Wiederherstellung |
| `IRestoreNotifier` / `RestoreNotifier` | Singleton (`WeakEventManager`, `Generation`); einziger Benachrichtigungsweg nach erfolgreicher Wiederherstellung — Abonnenten `SettingsViewModel`, `TankbookViewModel`, `TankbookEntriesViewModel` laden neu |
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
4. Das fertige Archiv wird mit `BackupArchiveValidator.Validate` auf die Grenzen der Wiederherstellung geprüft (sonst `Failed`, keine Datei). Mit Passwort verschlüsselt `BackupCrypto.EncryptAsync` das geprüfte ZIP als Container nach `….zip.tmp`. Dann Umbenennen in `*.zip`, Dateischutz (`ProtectFile`), Aufräumen des Arbeitsverzeichnisses (auch im Fehlerfall).

## Wiederherstellen

Die zweistufige Rückfrage steuert `BackupViewModel` im Eintrag: `ShowRestoreQuestionAsync` zeigt bei `IsEncrypted` zuerst das Passwort-Stadium (`item.ShowPasswordQuestion`, `IsPasswordStage`) statt `InspectAsync`. `UnlockRestoreAsync` (Schaltfläche „Prüfen“) liest `item.RestorePassword` vor `TryBeginOperation()` in eine lokale Variable — das Sperren schließt über `CloseQuestions`/`HideRestoreQuestion` die Rückfrage und leert das Feld —, ruft `InspectAsync` mit dem Passwort auf und zeigt bei `Ok` die Zählwert-Rückfrage; das Passwort wird in `item.RestorePassword` zurückgeschrieben und von `ConfirmRestoreAsync` an `RestoreAsync` weitergereicht. Bei `WrongPassword` oder `PasswordRequired` (auch leere Eingabe) öffnet `ShowPasswordQuestion` die Passwortabfrage erneut, sodass die Eingabe direkt wiederholt werden kann; bei `NotFound` wird die Liste neu geladen.

0. Bei einer Datei mit Magic `TKBENC01` entschlüsselt `DecryptToTempAsync` sie zuerst in eine temporäre Klartext-Datei: Header ohne Klartextmanifest prüfen (`InvalidFile` bei ungültigem Header, `TooNew` bei neuerer Containerversion, `PasswordRequired` ohne Passwort, `WrongPassword` bei Authentifizierungsfehler). Erst danach folgt der gewohnte ZIP-Pfad; die `.tmp`-Datei wird in jedem Fall gelöscht.
1. `BackupArchiveValidator.Validate`: Eintragsnamen (Zip-Slip), Grenzen, Manifest, Dateiliste = Einträge, Versionsregeln → `InvalidFile` oder `TooNew`.
2. Entpacken ins Staging mit eigener Kopierschleife, Größenzählung und SHA-256-Vergleich; Fotos zusätzlich `DetectExtension`.
3. `BackupDatabasePreparer` baut die Datenbank neu auf (ohne Pool, Höchstdauer `BackupLimits.PrepareTimeoutSeconds`, geprüft zwischen den Schritten; das Entpacken steht außerhalb): Die entpackte Datenbank wird nur gelesen (`integrity_check`, Migrationshistorie gegen Manifest). Eine frische Referenzdatenbank wird mit `IMigrator` bis zur Migration des Manifests angelegt; die Zeilen aller App-Tabellen werden mit ausdrücklichen Spaltenlisten parametrisiert hineinkopiert (fehlende Tabelle oder Spalte → `InvalidFile`; Trigger, Views, `CHECK`, Indizes, `sqlite_sequence`, `__EFMigrationsLock` und fremde Tabellen werden nie übernommen). Danach `foreign_key_check`, `MigrateAsync` auf den aktuellen Stand, `integrity_check`, jede Entität einmal über EF gelesen (Datum, Dezimalwerte, Enum-Werte → `InvalidFile`) und gezählt. Die fertige Referenz ersetzt die Staging-Datei. Zeitüberschreitung oder Fehler außerhalb der Datei (Speicher voll) → `Failed`. Die Rückfrage (`InspectAsync`) entpackt nur die Datenbank, führt dieselbe Vorbereitung aus und liefert die Zählwerte daraus.
4. `RestoreSwap.Execute`: Journal, `ClearAllPools()`, alten Stand nach `restore-rollback/`, neuen Stand an die Zielorte, `Protect`/`ProtectFile`, Journal löschen (Commit), Aufräumen. Bei jeder Ausnahme: neue Dateien entfernen, alte zurück, Journal löschen, Ergebnis `Failed`.
5. Nach Erfolg `PRAGMA journal_mode=WAL` auf der neuen Datenbank, dann `IPriceCacheInvalidator.InvalidateCaches()`; `BackupViewModel.ConfirmRestoreAsync` meldet die Wiederherstellung ausschließlich über `IRestoreNotifier.NotifyRestored()` (Ereignis `Restored`). `SettingsViewModel`, `TankbookViewModel` und eine geöffnete `TankbookEntriesViewModel` (Seite der Vorgänge eines Fahrzeugs) laden daraufhin sofort neu; die übrigen Seiten laden in `OnAppearing`. Fehlt das Fahrzeug einer geöffneten Vorgangsseite danach, schließt `ITankbookNavigator.CloseAsync(vehicleId)` genau diese Seite (nie die eines anderen Fahrzeugs).

Stürzt der Prozess zwischen Schritt 4 und dem Commit ab, ruft `DatabaseInitializer` beim nächsten Start `RestoreRecovery.RecoverIfNeeded` vor `MigrateAsync` auf: Mit Journal wird der Austausch zurückgenommen; ohne Journal werden nur verwaiste Verzeichnisse gelöscht.

## Importieren, Löschen, Teilen

- **Import:** Der Dienst liest vor dem Kopieren nur den Dateianfang (`BufferHeaderAsync`, höchstens `BackupLimits.MaxEncryptedHeaderBytes`); `ClassifyContainer` meldet `InvalidFile`, `TooNew` oder `PasswordRequired` früh, ohne die Datei zu kopieren. Danach: Kopie nach `backups/import-<Guid>.tmp` (mit Größengrenze), bei verschlüsselten Containern Entschlüsselung wie beim Wiederherstellen in eine zweite `.tmp`-Datei (`decrypt-<Guid>.tmp`), dann Strukturprüfung, Entpacken ins Staging und dieselbe Datenbankprüfung wie beim Wiederherstellen (Prüfsummen, Neuaufbau und Prüfung der Datenbank), Aufräumen des Staging, dann Umbenennen der **verschlüsselten Originaldatei** in `tankatlas-backup-<Zeitstempel des Manifests>.zip` (bei Kollision Zähler; der Zeitstempel stammt aus dem Manifest des entschlüsselten ZIP). Fehler entfernen die `.tmp`-Dateien. Auf ViewModel-Ebene hält `_pendingImport` die gewählte Datei bei `PasswordRequired` oder `WrongPassword`, die Karte fragt das Passwort nach (`IsAwaitingImportPassword`, „Prüfen“ = `ConfirmImportPasswordAsync`; leeres Feld → Status `PasswordRequired`, die Abfrage bleibt offen). Nach erfolgreichem Einspielen öffnet `ShowRestoreQuestionAsync(item, password)` direkt die Zählwert-Rückfrage — das Passwort wird nicht ein zweites Mal abgefragt.
- **Löschen und Teilen:** `GetFullPath` lässt nur Namen nach dem Muster `tankatlas-backup-*.zip` ohne Pfadanteile zu; sonst `NotFound`.

## Ergebnisse

`BackupResult`: `Ok`, `NotFound`, `InvalidFile`, `TooNew`, `Busy`, `Failed`, `PasswordRequired` (verschlüsselt, kein Passwort), `WrongPassword` (falsches Passwort oder Authentifizierungsfehler). Die zugehörigen Meldungen liefert `BackupTexts.GetMessage`.

## Testmodus

Mit `TANKATLAS_TEST_DATA_PATH` ersetzen `TestBackupFilePicker` (Datei aus `TANKATLAS_TEST_BACKUP_IMPORT`, ohne Wert gilt die Auswahl als abgebrochen) und `TestBackupSharer` (Kopie nach `test-shared/`) die Systemdialoge.

## Tests

- Unit: `BackupArchiveValidatorTests_*`, `BackupVersionRuleTests`, `BackupTextsTests_*`, `RestoreRecoveryTests`, `BackupViewModelTests_*` (u. a. `_Encryption`), `BackupCryptoTests`, `SettingsViewModelTests_Backup`, `BackupFilePickerSelectorTests_TestMode`, `BackupSharerSelectorTests_TestMode`, `TestBackupSharerTests`, `FuelPriceServiceTests_InvalidateCaches`, `StatisticsTileFontTests`.
- Integration (echte SQLite-Datei): `BackupServiceTests_Create` (Umfang, Manifest, Vollständigkeit), `_Restore`, `_Versions` (ältere, zu neue, ungültige Dateien), `_Atomicity` (Fehlerinjektion an jedem Schritt, Absturz), `_ListDeleteImport`, `_Encryption` (Container, fehlendes/falsches Passwort, Manipulation, Pipeline unverändert).
- E2E (FlaUI): `BackupE2ETests_Flow`, `_List`, `_Photo`, `_Persistence`, `_Import`, `_InvalidFiles`, `_Share`, `_Encryption`, `TankbookStatisticsE2ETests_TileNumbers`, Erweiterung von `TankbookE2ETests_DeleteWithEntries`.

← [Zurück zur Übersicht](index.md)
