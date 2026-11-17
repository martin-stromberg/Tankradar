# Logik: Sicherungsdienst, Prüfpipeline, Wiederherstellung und UI-Ablauf

Bestand der Logikklassen unter `src/Tankradar.MAUI/Services/Backup/` sowie der zugehörigen ViewModels und Registrierungen. Es existiert **keine** kryptografische Komponente (keine Schlüsselableitung, keine Ver-/Entschlüsselung); `System.Security.Cryptography` wird ausschließlich für SHA-256-Prüfsummen (`IncrementalHash` in `BackupArchiveValidator` und `BackupArchiveWriter`) genutzt.

## `BackupService` (sealed, `IBackupService`)

Datei: `src/Tankradar.MAUI/Services/Backup/BackupService.cs`

Konstruktor-Abhängigkeiten: `IDbContextFactory<TankradarDbContext>`, `IAppDataPathProvider`, `IDatabaseFileProtector`, `IDatabaseInitializer`, `TimeProvider`, `ILogger<BackupService>`, optional `IPriceCacheInvalidator`, `Action<string>? swapHook` (Fehlerinjektion für Tests), `TimeSpan? prepareTimeout`. Serielle Sperre über `_gate` (`SemaphoreSlim`, `Wait(0)` → `Busy`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateAsync(CancellationToken)` | public | Initialisiert DB, legt `.work-<Guid>`-Arbeitsverzeichnis an, erstellt DB-Schnappschuss per `VACUUM INTO` (Journalmodus `DELETE`), schreibt Archiv über `BackupArchiveWriter.WriteAsync` in `*.tmp`, prüft es selbst mit `BackupArchiveValidator.Validate` (verwirft bei Nicht-`Ok` → `Failed`), benennt um auf `tankatlas-backup-<JJJJMMTT-HHmmss>[-n].zip`, setzt `ProtectFile`. **Kein Passwortparameter.** |
| `ListAsync(CancellationToken)` | public | Listet Dateien mit gültigem Namen (`IsValidFileName`), räumt `*.tmp`/`.work-*`-Reste, liest je Datei das Manifest (`ReadInfo` → `IsReadable`), sortiert neueste zuerst. |
| `InspectAsync(string, CancellationToken)` | public | `GetFullPath`-Prüfung (`NotFound`), `Validate`, dann `StageAsync` mit `databaseOnly: true`; Zählwerte der Rückfrage stammen aus der geprüften Referenzdatenbank (`BackupDatabasePreparer`), Fotos aus der Manifest-Dateiliste. Gibt `BackupOutcome` mit `File` und `Manifest`. |
| `RestoreAsync(string, CancellationToken)` | public | `GetFullPath`, `Validate`, `StageAsync` (vollständig), `RestoreSwap.Execute`, `RestoreWalMode`, `IPriceCacheInvalidator.InvalidateCaches`. Gibt `BackupResult`. |
| `ImportAsync(PickedBackupFile, CancellationToken)` | public | Kopiert externe Datei begrenzt (`CopyLimitedAsync`, >`MaxTotalBytes + MaxManifestBytes` → `InvalidFile`), `Validate`, `StageAsync` (volle Datenbankprüfung vor Listeneintrag), verschiebt nach `tankatlas-backup-<createdUtc>[-n].zip`, `ProtectFile`. |
| `DeleteAsync(string, CancellationToken)` | public | `GetFullPath`, `File.Delete`; `NotFound` bei ungültigem Namen/fehlender Datei. |
| `GetFullPath(string)` | public | Liefert Pfad nur bei `IsValidFileName` und vorhandener Datei, sonst `null`. |
| `GetBackupDirectory()` | private | `<Daten>/backups`. |
| `GetKnownMigrationsAsync()` | private | `context.Database.GetMigrations()` für die Versionsregeln. |
| `GetUniquePath(string, DateTime)` | private | Namensvergabe `tankatlas-backup-<Stempel>[-n].zip` mit Zähler bei Kollision. |
| `ReadInfo(string)` | private | `BackupFileInfo` aus Manifest bzw. Dateidatum (`IsReadable`). |
| `CleanUpLeftovers(string)` | private | Entfernt `*.tmp` und `.work-*`. |
| `ReadHeaderInfo(SqliteConnection)` | private | Migration + Zählwerte der Schnappschuss-DB (`CountTables`: Vehicles, TankbookEntries, FavoriteGroups, FavoriteEntries, Stations, PriceEntries). |
| `CreateSnapshot(string, string)` | private | `VACUUM INTO`, `PRAGMA journal_mode=DELETE`, `HeaderInfo` → `BackupManifest`. |
| `StageAsync(string, BackupManifest, string, bool, CancellationToken)` | private | Hintergrundthread: `BackupArchiveValidator.ExtractAsync` + `BackupDatabasePreparer.PrepareAsync` (Referenz-`workDirectory` `reference-<Guid>`). |
| `RestoreWalMode(string)` | private | Setzt `PRAGMA journal_mode=WAL` nach dem Austausch. |
| `CopyLimitedAsync(PickedBackupFile, string, CancellationToken)` | private | Stream-Kopie der Importdatei mit Größengrenze. |
| `HeaderInfo` (private class) | – | `Migration`, `Counts`. |

Abonnierte Events: keine. Publizierte Events: keine (Benachrichtigung erfolgt im ViewModel über `IRestoreNotifier`).

## `BackupArchiveValidator` (static)

Datei: `src/Tankradar.MAUI/Services/Backup/BackupArchiveValidator.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsValidEntryName(string?)` | public | Whitelist: `manifest.json`, `database/tankatlas.db`, `receipts/<gültiger Fotoname>` (Zip-Slip-Schutz). |
| `AreEntryNamesValid(IEnumerable<string>)` | public | Zusätzlich: keine Duplikate (ordinal-ignore-case), Manifest und Datenbank je genau einmal. |
| `CheckVersion(BackupManifest, IReadOnlyList<string>)` | public | Formatkennung, `SchemaVersion` (<1 `InvalidFile`, >App `TooNew`), `databaseMigration` bekannt → `Ok`, unbekannt größer → `TooNew`, sonst `InvalidFile`. |
| `ReadManifest(string)` | public | Nur Manifest lesen (`null` bei unlesbarer Datei); Basis von `BackupService.ReadInfo`. |
| `Validate(string, IReadOnlyList<string>)` | public | Strukturprüfung ohne Entpacken: Dateilänge, Eintragszahl, Manifest, `CheckVersion`, `CheckStructure` (Grenzen je Eintragstyp, Gesamtgröße, Manifest-Dateiliste deckungsgleich mit Einträgen, SHA-256-Länge 64). |
| `VerifyContentAsync(string, BackupManifest, CancellationToken)` | public | Liest alle Einträge und prüft Größe/SHA-256 gegen das Manifest, ohne zu speichern. |
| `ExtractAsync(string, BackupManifest, string, bool, CancellationToken)` | public | Entpackt geprüfte Einträge ins Staging (`databaseOnly` optional), Präfixprüfung der Zielpfade, kumulierte Größengrenze, `CopyVerifiedAsync` (Größe + SHA-256), Fotosignatur per `ReceiptPhotoStore.DetectExtension`. |
| `ReadManifest(ZipArchive)` | private | Manifest-Eintrag begrenzt lesen (≤ `MaxManifestBytes`). |
| `CheckStructure(ZipArchive, BackupManifest)` | private | Namen, Einzel-/Summengrenzen, Dateiliste↔Einträge-Abgleich. |
| `GetTargetPath(string, string)` | private | Zielpfad im Staging nur für Datenbank und gültige Fotonamen. |
| `HasMatchingSignature(string)` | private | Fotosignatur (jpg/heic/heif/Sonstige) gegen Endung. |
| `CopyVerifiedAsync(ZipArchiveEntry, BackupManifestFile, Stream, CancellationToken)` | private | Kopie mit laufender SHA-256- und Größenprüfung. |

Erkennung „verschlüsselt vs. unverschlüsselt“: nicht vorhanden — `ZipFile.OpenRead` auf einer nicht-ZIP-Datei scheitert mit `InvalidDataException` → `InvalidFile` (bzw. `null` aus `ReadManifest` → `IsReadable = false` in der Liste).

## `BackupArchiveWriter` (static)

Datei: `src/Tankradar.MAUI/Services/Backup/BackupArchiveWriter.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `WriteAsync(string zipPath, string snapshotDatabasePath, string receiptsDirectory, BackupManifest header, CancellationToken)` | public | Schreibt ZIP: `database/tankatlas.db` (Optimal), `receipts/*` (NoCompression, nur gültige Fotonamen, `Counts.Photos++`), zuletzt `manifest.json` mit Dateiliste (Größe + SHA-256 je Eintrag). **Nur unverschlüsselter Schreibpfad.** |
| `AddFileAsync(...)` | private | Eintragskopie mit SHA-256-Berechnung → `BackupManifestFile`. |

## `BackupDatabasePreparer` (static)

Datei: `src/Tankradar.MAUI/Services/Backup/BackupDatabasePreparer.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `PrepareAsync(string databasePath, BackupManifest manifest, string workDirectory, TimeSpan timeout, CancellationToken)` | public | Höchstdauer per linked CTS; `SourceFileException`/Lese-/Formatfehler → `InvalidFile`, Umgebungsfehler (z. B. Speicher voll) und Timeout → `Failed`; räumt `workDirectory` im `finally` auf. |
| `BuildAsync(...)` | private | Sicherungs-DB nur lesend (`Mode=ReadOnly`): `integrity_check` + Migrationsstand = Manifest; frische Referenzdatenbank auf Migrationsstand der Sicherung (`IMigrator.MigrateAsync(Ziel)`), zeilenweiser Kopie mit expliziten Spaltenlisten in einer Transaktion (FK aus), `foreign_key_check`, Migration auf aktuellen Stand, `integrity_check`/`foreign_key_check` erneut, jede Entität via EF gelesen (`AsNoTracking`, gestreamt), Textlängen je `BackupTextLimits` (Zeichen + BLOB-Länge), Enum-Werte (`FuelKind`, `Kind`, `Priority`) geprüft. Ergebnis ersetzt die entpackte DB im Staging (Journalmodus `DELETE`). |

## `RestoreSwap` (sealed)

Datei: `src/Tankradar.MAUI/Services/Backup/RestoreSwap.cs`

| Mitglied | Sichtbarkeit | Kurzbeschreibung |
|----------|-------------|------------------|
| `StepJournal` … `StepBeforeCommit` | public const | Schrittnamen für den `afterStep`-Hook (Fehlerinjektion): `journal`, `database-moved`, `receipts-moved`, `database-placed`, `receipts-placed`, `protected`, `before-commit`. |
| `Execute(string stagingDirectory)` | public | Atomarer Austausch nur per Umbenennen: ggf. offenes Journal vorher per `Undo` zurücknehmen; `restore-rollback/` anlegen; Journal schreiben; alte DB (`-wal`/`-shm`) und `receipts/` weg-, neue heranschieben (`MoveWithRetry`: bis 20×, 150 ms, jeweils `ClearAllPools()`); `Protect`/`ProtectFile`; Journal löschen (Commit). Bei Ausnahme: `TryUndo` + Wurf. |
| `Undo(string dataDirectory)` | public static | Gibt Rollback-Bestand zurück (Hauptdatei vor `-wal`/`-shm`); ohne lesbares Journal nur Rückgabe, nie Löschen von nicht Ersetztem. |
| `TryUndo` | private static | `Undo` mit `IOException`/`UnauthorizedAccessException`-Schutz. |
| `MoveWithRetry(Action)` | private static | Wiederholte Verschiebung mit Pool-Leerung. |
| `Step(string)` | private | Ruft `_afterStep`. |

## `RestoreRecovery` (sealed, `IRestoreRecovery`)

Datei: `src/Tankradar.MAUI/Services/Backup/RestoreRecovery.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RecoverIfNeeded(string)` | public | Bei vorhandenem Journal `RestoreSwap.Undo`; bei Scheitern `IOException` (Initialisierung scheitert, Zwischenstand bleibt erhalten). Räumt Staging und (ohne Journal) Rollback auf. Wird von `DatabaseInitializer.RunAsync` vor der Migration aufgerufen (`src/Tankradar.MAUI/Services/DatabaseInitializer.cs:57`). |

## `RestoreNotifier` (sealed, `IRestoreNotifier`)

Datei: `src/Tankradar.MAUI/Services/Backup/RestoreNotifier.cs`

| Mitglied | Sichtbarkeit | Kurzbeschreibung |
|----------|-------------|------------------|
| `Restored` | public event | `WeakEventManager`-basiert. |
| `Generation` | public int | Zählt gemeldete Wiederherstellungen (`Interlocked.Increment`). |
| `NotifyRestored()` | public | Löst `Restored` aus. Aufgerufen von `BackupViewModel.ConfirmRestoreAsync`. Abonnenten: `SettingsViewModel.OnRestored` (`LoadAsync`), `TankbookViewModel.OnRestored` (`LastTask = LoadAsync()`), `TankbookEntriesViewModel.OnRestored` (`_selectionStore.Clear()` + `LoadAsync`). |

## `BackupFileCleanup` (static)

Datei: `src/Tankradar.MAUI/Services/Backup/BackupFileCleanup.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DeleteFile(string?)` | public | Stilles Löschen (`IOException`/`UnauthorizedAccessException` verschluckt). |
| `DeleteDirectory(string?, bool onlyIfEmpty)` | public | Rekursiv, still; optional nur bei leerem Verzeichnis. |

## `BackupFilePickerSelector` / `BackupSharerSelector` (static)

Dateien: `src/Tankradar.MAUI/Services/Backup/BackupFilePickerSelector.cs`, `BackupSharerSelector.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Create(Func<string,string?>, Func<IBackupFilePicker>)` | public | Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) → `TestBackupFilePicker` (Datei aus `TANKATLAS_TEST_BACKUP_IMPORT`); sonst Plattform. Plattformklassen werden im Testmodus nie erzeugt. |
| `Create(Func<string,string?>, Func<IBackupSharer>)` | public | Testmodus → `TestBackupSharer` (`<Daten>/test-shared`); sonst Plattform. |

## `TestBackupFilePicker` / `TestBackupSharer` (sealed)

Dateien: `TestBackupFilePicker.cs`, `TestBackupSharer.cs`

- `TestBackupFilePicker.PickAsync`: liefert `PickedBackupFile` der übergebenen Datei oder `null` (Abbruch, auch bei fehlender Datei).
- `TestBackupSharer.ShareAsync`: wirft `FileNotFoundException` bei fehlender Quelle, sonst Kopie ins Zielverzeichnis (überschreibt).

## `MauiBackupFilePicker` / `MauiBackupSharer` (sealed)

Dateien: `MauiBackupFilePicker.cs`, `MauiBackupSharer.cs`

- `MauiBackupFilePicker`: `FilePicker.Default` mit ZIP-Dateityp je Plattform (WinUI `.zip`, iOS/MacCatalyst `public.zip-archive`, Android `application/zip`), Titel `BackupTexts.PickerTitle`.
- `MauiBackupSharer`: `Share.Default.RequestAsync` mit `ShareFile`.

## `BackupViewModel`

Datei: `src/Tankradar.MAUI/ViewModels/BackupViewModel.cs`

| Mitglied | Sichtbarkeit | Kurzbeschreibung |
|----------|-------------|------------------|
| `CreateAsync()` | public | `TryBeginOperation` → `_service.CreateAsync()` → Liste neu laden, `Created`/`Busy`/`CreateFailed`; `finally EndOperation`. **Keine Passworteingabe.** |
| `BeginRestoreAsync(BackupItemViewModel)` | public | `ShowRestoreQuestionAsync`. |
| `ConfirmRestoreAsync(BackupItemViewModel)` | public | `_service.RestoreAsync` → `Restored`, Liste neu, `_restoreNotifier.NotifyRestored()`; Fehler via `BackupTexts.GetMessage`. |
| `BeginDelete` / `ConfirmDeleteAsync` | public | Löschrückfrage bzw. `DeleteAsync`. |
| `ShareAsync(BackupItemViewModel)` | public | `GetFullPath` → `_sharer.ShareAsync(path, BackupTexts.Heading)`; `NotFound` lädt Liste neu. |
| `ImportAsync()` | public | `_picker.PickAsync` → `_service.ImportAsync` → Liste neu, Eintrag suchen, `ShowRestoreQuestionAsync`, Status `Imported`. |
| `ShowRestoreQuestionAsync(BackupItemViewModel)` | private | **Wichtig für die Anforderung:** bricht bei `!item.Info.IsReadable` sofort mit `InvalidFile` ab — eine verschlüsselte Datei ohne Header-Lesbarkeit käme hier gar nicht zur Passwortabfrage. Sonst `InspectAsync` → `FormatRestoreQuestion` mit Zählwerten. |
| `LoadAsync`, `OnAppearing` | public | Liste laden (`ListAsync`), Fehler → `ListFailed`. |
| `TryBeginOperation`/`EndOperation`/`CloseQuestions`/`RefreshCommandStates`/`ApplyList`/`SetStatus` | private | Sperrlogik (`IsBusy`), Rückfragen schließen, `Command.ChangeCanExecute`, Liste → `BackupItemViewModel`s, Status + Sprachausgabe. |

Abhängigkeiten: `IBackupService`, `IBackupFilePicker`, `IBackupSharer`, `IRestoreNotifier`, `IScreenReaderAnnouncer`, `ILogger<BackupViewModel>`. Befehle: `CreateCommand`, `ImportCommand` (jeweils `IsNotBusy`); Eintragsbefehle werden über Delegates an `BackupItemViewModel` verdrahtet. **Es existiert keine Passwortabfrage-Abstraktion** (analog zu `IBackupFilePicker`/`IBackupSharer` nicht vorhanden).

## `BackupItemViewModel` (sealed)

Datei: `src/Tankradar.MAUI/ViewModels/BackupItemViewModel.cs`

- Pro Eintrag: `Info` (`BackupFileInfo`), `Title` (`FormatItemTitle`), `SizeText`, `AutomationKey` (1-basiert), `RestoreHint`/`ShareHint`/`DeleteHint`.
- Rückfragen: `IsConfirmingRestore`/`RestoreQuestion` (`ShowRestoreQuestion`/`HideRestoreQuestion`), `IsConfirmingDelete`/`DeleteQuestion` (`ShowDeleteQuestion`/`HideDeleteQuestion`).
- Befehle: `RestoreCommand`, `ConfirmRestoreCommand`, `CancelRestoreCommand`, `ShareCommand`, `DeleteCommand`, `ConfirmDeleteCommand`, `CancelDeleteCommand`; `RefreshCommandStates`.
- Texte alle aus `BackupTexts` (kein Literal). **Kein Passwortfeld, keine „verschlüsselt“-Anzeige.**

## `SettingsViewModel` (nur Backup-Anteil)

Datei: `src/Tankradar.MAUI/ViewModels/SettingsViewModel.cs`

- Konstruktor-Parameter `BackupViewModel? backup = null` (Zeile 38); `Backup`-Eigenschaft (84), `HasBackup => Backup is not null` (89); `OnAppearing` ruft `Backup?.OnAppearing()` (222); abonniert `IRestoreNotifier.Restored` → `OnRestored` → `LoadAsync` (46, 276–279).

## `SettingsPage.xaml` (Sicherungskarte)

Datei: `src/Tankradar.MAUI/Views/SettingsPage.xaml` (Zeilen 259–361)

- Karte `SettingsCard` sichtbar bei `HasBackup`, `BindingContext = Backup` (`x:DataType vm:BackupViewModel`).
- AutomationIds: `Settings.Backup.Heading/Hint/Create/Import/Status/Empty`, Einträge `Settings.Backup.Item.{n}.Title/Size/Restore/Share/Delete` sowie Inline-Rückfragen `RestoreQuestion/RestoreConfirm/RestoreCancel` und `DeleteQuestion/DeleteConfirm/DeleteCancel`.
- **Kein Passwort-`Entry`/`Editor` vorhanden**; Rückfragen sind Inline-Layouts (keine Systemdialoge).

## `MauiProgram` (DI-Registrierung)

Datei: `src/Tankradar.MAUI/MauiProgram.cs`

- `AddSingleton<IBackupService, BackupService>()` (86), `IBackupFilePicker` via `BackupFilePickerSelector` → `MauiBackupFilePicker` (87–89), `IBackupSharer` via `BackupSharerSelector` → `MauiBackupSharer` (90–92), `AddSingleton<IRestoreNotifier, RestoreNotifier>()` (85), `AddTransient<BackupViewModel>()` (118).

## `DatabaseInitializer` (relevanter Ausschnitt)

Datei: `src/Tankradar.MAUI/Services/DatabaseInitializer.cs`

- Optionaler `IRestoreRecovery`-Parameter; `RunAsync` ruft `_recovery?.RecoverIfNeeded(dataDirectory)` vor `MigrateAsync` auf (Zeile 57). Produktiv wird `RestoreRecovery` injiziert.

## `BackupTexts` (static, `Resources/Texts`)

Datei: `src/Tankradar.MAUI/Resources/Texts/BackupTexts.cs`

- Konstanten u. a.: `PickerTitle`, `Heading`, `Hint` (enthält explizit „…ist die Datei nicht verschlüsselt.“ — Zeile 29), `CreateButton`, `ImportButton`, `RestoreButton`, `ShareButton`, `DeleteButton`, `ConfirmRestore`, `ConfirmDelete`, `Cancel`, `Empty`, `Unreadable`, `Created`, `Restored`, `Deleted`, `CreateFailed`, `ListFailed`, `ShareFailed`, `PickFailed`, `NotFound`, `Busy`, `InvalidFile`, `Failed`, `Imported`.
- Methoden: `GetMessage(BackupResult, string? appVersion)` (Abbildung aller `BackupResult`-Werte), `FormatTooNew`, `FormatDate`, `FormatSize`, `FormatItemTitle` (hängt `(nicht lesbar)` an), `FormatRestoreQuestion`, `FormatDeleteQuestion`, `FormatCounts`, `FormatRestoreHint`, `FormatShareHint`, `FormatDeleteHint`.
- Keine Texte für Passworteingabe/-fehler vorhanden.
