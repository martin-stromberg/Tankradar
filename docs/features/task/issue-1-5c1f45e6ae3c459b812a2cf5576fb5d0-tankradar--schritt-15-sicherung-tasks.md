# Tasks: Schritt 15 – Lokale Sicherung und Wiederherstellung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `BackupResult` Enum anlegen (`Ok`, `NotFound`, `InvalidFile`, `TooNew`, `Busy`, `Failed`) | Offen | — |
| 2 | Datenmodell | `BackupFormat` (Format-Kennung, Schemaversion 1, Dateinamen und Muster) anlegen | Offen | — |
| 3 | Datenmodell | `BackupLimits` (Einträge, Gesamtgröße, Datenbankgröße, Manifestgröße) anlegen | Offen | — |
| 4 | Datenmodell | `BackupManifest`, `BackupManifestFile`, `BackupManifestCounts` anlegen | Offen | — |
| 5 | Datenmodell | `BackupFileInfo`, `BackupOutcome`, `PickedBackupFile` anlegen | Offen | — |
| 6 | Konfiguration | `TestDataPaths.TestBackupImportEnvironmentVariable` ergänzen | Offen | — |
| 7 | Texte | `BackupTexts` anlegen (Überschrift, Hinweis, Schaltflächen, Rückfragen, Meldungen je `BackupResult`, Datum/Größe) | Offen | — |
| 8 | Texte | `TankbookTexts.FormatDeleteQuestion` um Sicherungshinweis bei Vorgängen erweitern | Offen | — |
| 9 | Logik | `BackupArchiveWriter` anlegen (ZIP, Manifest, SHA-256, `.tmp` und Umbenennen) | Offen | — |
| 10 | Logik | `BackupArchiveValidator`: Eintragsnamen (Zip-Slip, Whitelist, Duplikate) prüfen | Offen | — |
| 11 | Logik | `BackupArchiveValidator`: Limits, Manifest, Prüfsummen prüfen | Offen | — |
| 12 | Logik | `BackupArchiveValidator`: Versionsregeln (Schema, Migrationsstand, Historie) umsetzen | Offen | — |
| 13 | Logik | `BackupArchiveValidator`: Entpacken ins Staging mit Größenzählung und Fotosignaturprüfung | Offen | — |
| 14 | Logik | Staging-Datenbank prüfen und migrieren (`integrity_check`, `MigrateAsync`, `foreign_key_check`) | Offen | — |
| 15 | Logik | `RestoreJournal` anlegen | Offen | — |
| 16 | Logik | `RestoreSwap` anlegen (Umbenennen, Rücksicherung, Rollback, Test-Hook) | Offen | — |
| 17 | Logik | `IRestoreRecovery`/`RestoreRecovery` anlegen | Offen | — |
| 18 | Logik | `DatabaseInitializer` um optionale Wiederaufnahme erweitern | Offen | — |
| 19 | Logik | `IPriceCacheInvalidator` anlegen, `FuelPriceService.InvalidateCaches` implementieren | Offen | — |
| 20 | Logik | `IBackupService` anlegen | Offen | — |
| 21 | Logik | `BackupService.CreateAsync` (Online-Backup, Archiv, Dateischutz, Aufräumen) | Offen | — |
| 22 | Logik | `BackupService.ListAsync` und `InspectAsync` | Offen | — |
| 23 | Logik | `BackupService.RestoreAsync` (Validieren, Staging, Migration, Austausch, Cache-Invalidierung) | Offen | — |
| 24 | Logik | `BackupService.ImportAsync` | Offen | — |
| 25 | Logik | `BackupService.DeleteAsync` und `GetFullPath` mit Pfadschutz | Offen | — |
| 26 | Logik | Sperre gegen parallele Operationen (`Busy`) | Offen | — |
| 27 | Plattform | `IBackupFilePicker`, `TestBackupFilePicker`, `MauiBackupFilePicker`, `BackupFilePickerSelector` anlegen | Offen | — |
| 28 | Plattform | `IBackupSharer`, `TestBackupSharer`, `MauiBackupSharer`, `BackupSharerSelector` anlegen | Offen | — |
| 29 | Plattform | `Info.plist` (iOS/MacCatalyst) prüfen, Ergebnis für ADR festhalten | Offen | — |
| 30 | ViewModel | `BackupItemViewModel` mit Rückfragen anlegen | Offen | — |
| 31 | ViewModel | `BackupViewModel` anlegen (Erstellen, Liste, Wiederherstellen, Löschen, Teilen, Import, `Restored`) | Offen | — |
| 32 | ViewModel | `SettingsViewModel` um `Backup` erweitern und nach `Restored` neu laden | Offen | — |
| 33 | Registrierung | `MauiProgram`: Dienste, Selektoren, ViewModel, `IPriceCacheInvalidator` registrieren | Offen | — |
| 34 | UI | `SettingsPage.xaml`: `TankbookStyles` einbinden und Karte „Sicherung“ mit AutomationIds anlegen | Offen | — |
| 35 | UI | Semantik/Sprachausgabe der Sicherungskarte | Offen | — |
| 36 | UI | `StatisticsTileFont` anlegen | Offen | — |
| 37 | UI | `TankbookKindStatisticsViewModel`: Schriftgrößen-Eigenschaften ergänzen | Offen | — |
| 38 | UI | `TankbookEntriesPage.xaml`: `NoWrap` und gebundene `FontSize` an Kachelzahlen, `NoWrap` an Monatsbetrag | Offen | — |
| 39 | Tests (Unit) | `BackupArchiveValidatorTests_EntryNames` | Offen | — |
| 40 | Tests (Unit) | `BackupArchiveValidatorTests_Manifest` und `_Limits` | Offen | — |
| 41 | Tests (Unit) | `BackupVersionRuleTests` | Offen | — |
| 42 | Tests (Unit) | `BackupTextsTests_Results` und `_Format` | Offen | — |
| 43 | Tests (Unit) | `RestoreRecoveryTests` | Offen | — |
| 44 | Tests (Unit) | `BackupFilePickerSelectorTests_TestMode`, `BackupSharerSelectorTests_TestMode`, `TestBackupFilePickerTests`, `TestBackupSharerTests` | Offen | — |
| 45 | Tests (Unit) | `BackupViewModelTests_Create`, `_List` | Offen | — |
| 46 | Tests (Unit) | `BackupViewModelTests_Restore` | Offen | — |
| 47 | Tests (Unit) | `BackupViewModelTests_Delete` | Offen | — |
| 48 | Tests (Unit) | `BackupViewModelTests_ShareImport` | Offen | — |
| 49 | Tests (Unit) | `SettingsViewModelTests_Backup` | Offen | — |
| 50 | Tests (Unit) | `StatisticsTileFontTests` | Offen | — |
| 51 | Tests (Unit) | `FuelPriceServiceTests_InvalidateCaches` | Offen | — |
| 52 | Tests (Unit) | `TankbookTextsTests_Labels` und `TankbookViewModelTests_Delete` an Löschhinweis anpassen | Offen | — |
| 53 | Testhilfe | `BackupTestArchive` (gültig, zu neu, Zip-Slip, falscher Hash, abgeschnitten) | Offen | — |
| 54 | Tests (Integration) | `BackupServiceTests_Create` | Offen | — |
| 55 | Tests (Integration) | `BackupServiceTests_Completeness` (alle Entitäten, Zeitstempel, Fotos) | Offen | — |
| 56 | Tests (Integration) | `BackupServiceTests_Restore` | Offen | — |
| 57 | Tests (Integration) | `BackupServiceTests_OlderVersion` | Offen | — |
| 58 | Tests (Integration) | `BackupServiceTests_NewerVersion` | Offen | — |
| 59 | Tests (Integration) | `BackupServiceTests_InvalidFiles` | Offen | — |
| 60 | Tests (Integration) | `BackupServiceTests_Atomicity` (Fehlerinjektion, Absturzwiederaufnahme) | Offen | — |
| 61 | Tests (Integration) | `BackupServiceTests_List`, `_Delete`, `_Import` | Offen | — |
| 62 | E2E-Tests | `BackupE2ETestBase` anlegen | Offen | — |
| 63 | E2E-Tests | `BackupE2ETests_Flow` | Offen | — |
| 64 | E2E-Tests | `BackupE2ETests_Photo` | Offen | — |
| 65 | E2E-Tests | `BackupE2ETests_Persistence` | Offen | — |
| 66 | E2E-Tests | `BackupE2ETests_Import` | Offen | — |
| 67 | E2E-Tests | `BackupE2ETests_InvalidFiles` | Offen | — |
| 68 | E2E-Tests | `BackupE2ETests_Share` | Offen | — |
| 69 | E2E-Tests | `BackupE2ETests_List` | Offen | — |
| 70 | E2E-Tests | `TankbookE2ETests_DeleteWithEntries` und `_Flow` um den Sicherungshinweis erweitern | Offen | — |
| 71 | E2E-Tests | `TankbookStatisticsE2ETests_TileNumbers` (360/390 pt, einzeilig) | Offen | — |
| 72 | E2E-Tests | bestehende `SettingsE2ETests_*` auf Auswirkungen der neuen Karte prüfen | Offen | — |
| 73 | Dokumentation | ADR 0011 `docs/adr/0011-local-backup-restore.md` anlegen | Offen | — |
| 74 | Dokumentation | ADR 0010 ergänzen (Verbrauchsverlauf, vorletzter Balken, „Keine Symbole“ präzisiert, Kachelzahlen, Sichtprüfung) | Offen | — |
| 75 | Dokumentation | Hilfe `docs/help/Sicherung/` (4 Dateien) anlegen | Offen | — |
| 76 | Dokumentation | `docs/help/index.md`, `Einstellungen/`, `Tankbuch/` anpassen | Offen | — |
| 77 | Dokumentation | `changes.log` Eintrag Version 0.1.19 | Offen | — |
| 78 | Dokumentation | `README.md`: Stand Schritt 15, Testmodus-Variable, Bekannte Einschränkungen (Sichtprüfung, unverschlüsselte Sicherung) | Offen | — |
| 79 | Abschluss | `scripts/local-ci.ps1` ausführen (Unit, Integration, E2E grün) | Offen | — |
| 80 | Abschluss | Windows-Zwischenstand unter `review-versions/0.1.19_<Datum>/` ablegen (nicht committen) | Offen | — |
