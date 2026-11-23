# Lokale Datenablage und Android-Backup-Umfang

## Verzeichnisse

Alle App-Daten laufen über `IAppDataPathProvider` (`src/Tankradar.MAUI/Services/AppDataPathProvider.cs`): `GetDataDirectory()` liefert im Testmodus `TANKATLAS_TEST_DATA_PATH`, sonst `FileSystem.AppDataDirectory` (Zeilen 19–20, 38–45); `GetCacheDirectory()` entsprechend `FileSystem.CacheDirectory` (Zeilen 48–55). Unter Android entspricht `AppDataDirectory` dem internen `files/`-Verzeichnis des App-Sandkastens (Backup-pflichtig bei `allowBackup=true`), `CacheDirectory` dem `cache/`-Verzeichnis (nie Teil von Auto-Backup).

## Inhalt des Datenverzeichnisses (`files/` — von Auto-Backup erfasst)

| Eintrag | Herkunft / Konstante | Inhalt |
|---------|----------------------|--------|
| `tankatlas.db` | `TankradarDbContext.DatabaseFileName` (`Data/TankradarDbContext.cs:16`), Pfad `GetDatabasePath` (Zeilen 72–75); Verbindung in `MauiProgram.cs:63–67`, Migration in `DatabaseInitializer.cs:55–64` | SQLite-Datenbank (EF Core, WAL-Modus → zusätzlich `tankatlas.db-wal`, `tankatlas.db-shm`): Einstellungen, Spritsorten, Stationen, Preisstände, Favoriten, Fahrzeuge, Tank-/Ladevorgänge |
| `receipts/` | `ReceiptPhotoStore.DirectoryName` (`Services/Tankbook/ReceiptPhotoStore.cs:15`), Aufbau `:240` | Belegfotos `<Guid-ohne-Bindestriche>.<.jpg|.png|.heic|.heif>` (Namensregel `IsValidFileName`, Zeilen 161–177) |
| `backups/` | `BackupFormat.BackupDirectoryName` (`Models/Backup/BackupFormat.cs:38`), Pfad `BackupService.GetBackupDirectory` (`Services/Backup/BackupService.cs:425–428`) | Eigene Sicherungen `tankatlas-backup-<JJJJMMTT-HHmmss>[-n].zip` (Präfix `:63`, Endung `.zip` `:68`, Namensprüfung `IsValidFileName` `:130–136`); optional verschlüsselter Container `TKBENC01` (ADR 0019), sonst **unverschlüsselt** |
| `backups/*.tmp`, `backups/.work-*/` | `BackupFormat.TempExtension` `:73`, `WorkDirectoryPrefix` `:78` | unfertige Sicherungs-/Arbeitsstände; beim Auflisten entsorgt (`BackupFileCleanup`) |
| `restore-staging/<Guid>/` | `BackupFormat.StagingDirectoryName` `:43`; `BackupService.cs:209, 273, 355` | entpackter Stand während Prüfung/Wiederherstellung |
| `restore-rollback/` | `BackupFormat.RollbackDirectoryName` `:48`; `RestoreSwap.cs:84, 171` | einzige Rücksicherung des alten Stands bis zum Commit |
| `restore.journal` (+ `.tmp`) | `BackupFormat.JournalFileName` `:53`; `RestoreJournal.cs:30–33, 44–51, 86–90` | markiert einen laufenden Austausch; beim Start zurückgenommen durch `RestoreRecovery` aus `DatabaseInitializer` (`DatabaseInitializer.cs:57`; `RestoreRecovery.cs:33–36`) |
| `test-shared/` | `BackupFormat.TestSharedDirectoryName` `:58` | nur Testmodus: Ziel des „Teilen“-Testdoppels |
| `tiles/` | `MauiProgram.cs:292` (`LegacyTileCache.DeleteInBackground`) | Altbestand des Karten-Caches im Datenverzeichnis, wird beim Start im Hintergrund gelöscht |

## Inhalt des Cache-Verzeichnisses (`cache/` — nie Auto-Backup)

| Eintrag | Konstante | Inhalt |
|---------|-----------|--------|
| `tiles/` | `MauiProgram.cs:296` | HTTP-Kartenkacheln |
| `exports/` | `TankbookExportService.DirectoryName` (`Services/Export/TankbookExportService.cs:17`), Pfad `:159` | CSV-/PDF-Exporte des Tankbuchs |
| `diagnostics/` | `DiagnosticsPackageService.DirectoryName` (`Services/Diagnostics/DiagnosticsPackageService.cs:36`) | Diagnosepakete (`tankatlas-diagnose-*.zip`) |

## `shared_prefs/` (von Auto-Backup erfasst, nicht Teil von `files/`)

| Datei bzw. Bereich | Code | Schlüssel |
|--------------------|------|-----------|
| `Preferences.Default` (MAUI-Standard-SharedPreferences; im Testmodus eigener Scope über `GetTestPreferencesScope`, `MauiProgram.cs:75–78`) | `PreferencesVehicleSelectionStore` (`Services/Tankbook/PreferencesVehicleSelectionStore.cs:13`) | `tankbook.selectedVehicleId` — gemerktes Fahrzeug, unkritisch (kein Geheimnis) |
| `SecureStorage.Default` (MAUI legt unter Android eine Keystore-verschlüsselte SharedPreferences-Datei `<ApplicationId>.microsoft.maui.essentials.preferences` an) | `SecureStorageApiKeyStore` (`Services/Pricing/SecureStorageApiKeyStore.cs:11, 16, 21`) | `tankatlas.price-api-key` (Tankerkönig), `tankatlas.routing-api-key` (OpenRouteService), `tankatlas.routing-enabled` (Schalter „Routensuche aktivieren“) |

## Konsequenz für die Backup-Entscheidung

Bei `allowBackup="true"` ohne Regeln würden mitgenommen: Datenbank (inkl. WAL/SHM), alle Belegfotos, **alle eigenen Sicherungen** (unverschlüsselt, wenn kein Passwort vergeben wurde — ADR 0019 lässt das zu), temporäre Restore-Arbeitsstände (potenziell halbe Datenbestände in `restore-staging/`/`restore-rollback/` sowie das Journal, das beim nächsten Start einen Austausch zurückrollen würde), `test-shared/` sowie die SharedPreferences-Dateien. Besonders bemerkenswert: Die eigenen Sicherungen können vollständige Datenbankabbilder samt Fotos enthalten und würden so gedoppelt (original + Archiv) in ein Cloud-Backup gegeben; die `SecureStorage`-Datei würde mitwandern, ist aber ohne den gerätegebundenen Keystore-Schlüssel nicht lesbar (siehe [securestorage-restore.md](securestorage-restore.md)).
