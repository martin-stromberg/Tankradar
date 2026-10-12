# Umsetzungsplan: Schritt 15 – Lokale Sicherung und Wiederherstellung (Tankatlas, Version 0.1.19)

## Übersicht

Im Bereich „Optionen“ entsteht eine Karte „Sicherung“: Der Anwender erstellt auf Veranlassung eine ZIP-Sicherung (Datenbank-Schnappschuss, Belegfotos, Manifest), sieht die vorhandenen Sicherungen mit Datum und Größe, stellt eine ausgewählte Sicherung nach Rückfrage atomar wieder her, löscht Sicherungen nach Rückfrage, gibt sie über die Systemfunktion „Teilen/Speichern“ weiter und spielt eine externe Datei über den Dateiauswahldialog ein. Neu sind der Dienst `BackupService` samt Archiv-Validierung, zwei testbare Plattformdienste (Dateiauswahl, Teilen), `BackupViewModel` und die Oberfläche; angepasst werden die Fahrzeug-Löschrückfrage, der Preisdienst (Zwischenspeicher), der `DatabaseInitializer` (Wiederaufnahme) sowie Dokumentation. Hinzu kommen die Restpunkte aus Abnahme 14 (ADR 0010, Kachelzahlen ohne Umbruch, Vermerk zur Sichtprüfung). Es gibt keine Schemaänderung und keine neue Abhängigkeit; `MtouchInterpreter` und `TrimmerRootAssembly` bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `BackupService` | Service Layer (`IBackupService`) mit Transaction Script je Operation (Erstellen, Prüfen, Wiederherstellen, Importieren, Löschen, Auflisten); Archivformat, Validierung und Austausch in eigenen Klassen (`BackupArchiveWriter`, `BackupArchiveValidator`, `RestoreSwap`) | Entspricht `FavoritesService`/`VehicleService`; kleine, einzeln testbare Klassen statt einer großen. |
| Datenbank-Schnappschuss | SQLite-Online-Backup (`SqliteConnection.BackupDatabase`) in eine temporäre Datei, danach ZIP | Liefert auch bei laufender App und WAL einen konsistenten Stand; Kopieren der Datei wäre unsicher. Bestandteil von `Microsoft.Data.Sqlite`/`e_sqlite3`, keine neue Abhängigkeit. |
| Archivinhalt | Genau drei Arten von Einträgen: `manifest.json`, `database/tankatlas.db`, `receipts/<Guid N>.<Endung>`; Whitelist, alles andere ist ungültig | Beantwortet „keine Geheimnisse, keine Standortdaten“ strukturell (nur Datenbank und Fotos; kein `tiles`, kein Schlüsselspeicher) und ist Basis des Zip-Slip-Schutzes. Die Stationstabelle bleibt (öffentliche Stammdaten, Ziel des Fremdschlüssels der Favoriten); ein Anwenderstandort wird nirgends gespeichert (Inventar). |
| Manifest | JSON (`System.Text.Json`): `format` = `Tankatlas-Backup`, `schemaVersion` (Format, aktuell 1), `appVersion`, `databaseMigration` (letzte Zeile `__EFMigrationsHistory`), `createdUtc`, `files[]` (`path`, `size`, `sha256`), `counts` (Fahrzeuge, Vorgänge, Gruppen, Favoriten, Stationen, Preisstände, Belegfotos) | Schemaversion für das Archivformat und Migrationsstand für die Datenbank sind getrennt; Prüfsummen erkennen beschädigte Archive; Zählwerte dienen der Rückfrage (Anzeige) und dem Vollständigkeitstest. |
| Versionsprüfung | `schemaVersion` > aktuelle → „zu neu“; `databaseMigration` nicht in `GetMigrations()` der App: ist sie lexikografisch größer als die letzte bekannte → „zu neu“, sonst ungültig; ältere Migration → erlaubt (Migration der Staging-Datenbank mit `MigrateAsync`); zusätzlich muss die echte Migrationshistorie der enthaltenen Datenbank zum Manifest passen | Migrations-IDs sind Zeitstempel-präfixiert und damit ordenbar; verhindert, dass eine neuere Datenbank von einer älteren App „verbogen“ wird. |
| Atomarität | Validieren → Staging in `<Daten>/restore-staging/<Guid>/` (gleiches Volumen) → Staging-Datenbank migrieren und prüfen (`PRAGMA integrity_check`, `foreign_key_check`) → Austausch per Umbenennen mit Rücksicherung in `restore-rollback/` und Journal `restore.journal`; bei Fehler Rückgabe der alten Dateien; `DatabaseInitializer` stellt nach Absturz anhand des Journals den alten Stand her (`RestoreRecovery`) | Alle fehleranfälligen Schritte (Entpacken, Prüfen, Migrieren) laufen vor dem ersten Eingriff in die Livedaten; der Austausch besteht nur aus Umbenennungen innerhalb eines Volumens. Commit-Punkt ist das Löschen des Journals. |
| Entpacken | Eigene Kopierschleife je Eintrag (kein `ExtractToDirectory`): Zielpfad = feste Basis + whitelist-geprüfter Name, zusätzlich `Path.GetFullPath`-Präfixprüfung; Zähler gegen deklarierte Größe und `BackupLimits` | Zip-Slip- und Zip-Bomb-Schutz mit Defense in Depth. |
| Zwischenspeicher der Preise | Neues kleines Interface `IPriceCacheInvalidator` (Methode `InvalidateCaches`), von `FuelPriceService` implementiert und als weitere Registrierung desselben Singletons eingebunden; `IFuelPriceService` bleibt unverändert | Bestehende Testdoppel von `IFuelPriceService` bleiben kompilierbar; nach der Wiederherstellung greift die Alterserkennung an den Zeitstempeln der zurückgespielten Preisstände. |
| Plattformdienste | `IBackupFilePicker` (`FilePicker`) und `IBackupSharer` (`Share`) mit je Test- und MAUI-Implementierung und Selector-Klasse nach dem Muster `ReceiptPhotoPickerSelector` | Im Testmodus (`TANKATLAS_TEST_DATA_PATH`) nie ein Systemdialog: `TestBackupFilePicker` liefert die Datei aus der neuen Variablen `TANKATLAS_TEST_BACKUP_IMPORT`, `TestBackupSharer` kopiert nach `<Daten>/test-shared/`. |
| Rückfragen und Bedienung | Inline-Rückfragen im Listeneintrag (Muster `VehicleItemViewModel.ShowDeleteConfirmation`), nicht `DisplayAlert`; Schaltflächen aus `TankbookStyles.xaml` (`TankbookButton`, `TankbookOutlineButton`, `TankbookDangerButton`, 44 pt); Auswahl durch Zeile der Liste (kein Eingeben von Dateinamen); Meldungen zusätzlich über `IScreenReaderAnnouncer` | Einheitliches, per FlaUI testbares Muster; Systemdialoge sind in E2E nicht bedienbar. |
| Teil-ViewModel | `BackupViewModel` wird wie `DataSourceViewModel` optional in `SettingsViewModel` eingebettet (`Backup`, `HasBackup`); nach `Restored` lädt `SettingsViewModel.LoadAsync` neu | Seite „Optionen“ bleibt schlank; vorhandenes Einbettungsmuster. |
| Eintragsschlüssel in der Liste | 1-basierter Index (neueste zuerst) als `AutomationKey`: `Settings.Backup.Item.<i>.*` | Dateinamen enthalten Zeitstempel und sind im Test nicht vorhersagbar. |
| Importablauf | Datei wählen → vollständig prüfen (Struktur, Prüfsummen, Version) → in `backups/` übernehmen → direkt die Rückfrage zur Wiederherstellung dieses Eintrags öffnen | „Einspielen“ im Sinne der Anforderung; ungültige Dateien gelangen nie in die Liste. |
| Liste | `ListAsync` liest je Datei nur das Manifest (Datum, Version); nicht lesbare `.zip` erscheinen mit Dateidatum und Kennzeichnung „nicht lesbar“, damit sie gelöscht werden können; `.tmp`-Reste werden bereinigt | Beschädigte Dateien dürfen die Liste nicht blockieren. |
| Verschlüsselung / Automatik | Keine Verschlüsselung, keine automatischen Sicherungen, keine Mengenbegrenzung | Entscheidung „Schutz lokaler Daten“; Anforderung: ausschließlich auf Veranlassung. Hilfe und Karte weisen auf die Unverschlüsselung geteilter Dateien hin. |
| iOS-Dateischutz | `IDatabaseFileProtector.ProtectFile` für jede neue Sicherung, jedes wiederhergestellte Foto; `Protect` für die wiederhergestellte Datenbank; Staging liegt im geschützten Datenverzeichnis | Belegfotos bleiben unter Dateischutz. |
| `Info.plist` | Geprüft, keine Änderung: `FilePicker` und `Share` laufen außerhalb des Prozesses, kein neuer `Usage`-Schlüssel nötig. `UIFileSharingEnabled`/`LSSupportsOpeningDocumentsInPlace` werden bewusst nicht gesetzt (würden das Datenverzeichnis in „Dateien“ offenlegen). Ergebnis im ADR 0011 | Anforderung verlangt die Prüfung; Datenschutz/Dateischutz bleiben gewahrt. |
| Kachelzahl | Zahl `NoWrap`; Schriftgröße je Textlänge über `StatisticsTileFont.SizeFor(text)` (bis 6 Zeichen 28 pt, 7 Zeichen 24 pt, 8 Zeichen 21 pt, ab 9 Zeichen 18 pt; Feinabstimmung am Messwert), gebunden an Eigenschaften des Statistik-ViewModels; Einheit bleibt in eigenem Label | Löst den Umbruch bei 360–375 pt ohne Änderung des Kachelrasters; rein rechnerische Funktion ist unit-testbar. Die Alternative „Einheit unter die Zahl“ ändert das Layout bereits bei kurzen Zahlen. |
| Sichtprüfung (Restpunkt) | Wird nicht durch die Implementierung ersetzt, sondern als offene Abnahmeprüfung in README („Bekannte Einschränkungen“), ADR 0010/0011 („Folgen“) und `changes.log` vermerkt; die Layout-Prüfung per UIA-Rechtecke ersetzt sie nicht | Die Sichtprüfung erfordert eingeschalteten Bildschirm und Augenschein; der Agent kann sie nicht belegen. |

## Programmabläufe

### Sicherung erstellen

1. Anwender betätigt `Settings.Backup.Create`; `BackupViewModel.CreateCommand` setzt `IsBusy` (sperrt alle Schaltflächen) und ruft `IBackupService.CreateAsync`.
2. `BackupService` wartet auf seine Sperre (`SemaphoreSlim`), initialisiert über `IDatabaseInitializer.InitializeAsync` die Datenbank, legt `backups/` und ein Arbeitsverzeichnis `backups/.work-<Guid>/` an.
3. Die Datenbank wird per `BackupDatabase` in `.work/tankatlas.db` kopiert (Online-Backup); aus der Kopie werden Migrationsstand und Zählwerte gelesen.
4. `BackupArchiveWriter` schreibt `tankatlas-backup-<JJJJMMTT-HHmmss>[-n].zip.tmp`: zuerst `database/tankatlas.db`, dann jedes gültige Foto aus `receipts/` (`ReceiptPhotoStore.IsValidFileName`, Lesen mit `FileShare.ReadWrite`), zuletzt `manifest.json` mit Größen und SHA-256 (während des Schreibens berechnet).
5. Die `.tmp`-Datei wird in `*.zip` umbenannt, `ProtectFile` angewendet, das Arbeitsverzeichnis gelöscht (auch im Fehlerfall im `finally`).
6. Ergebnis `BackupOutcome(Ok, BackupFileInfo)`; `BackupViewModel` lädt die Liste, meldet „Sicherung erstellt.“ (`Settings.Backup.Status`, Sprachausgabe). Fehler (Platte voll, Zugriff verweigert) → `Failed` und Meldung; es bleibt keine halbe Datei zurück.

Beteiligte Klassen: `BackupViewModel`, `IBackupService`/`BackupService`, `BackupArchiveWriter`, `BackupManifest`, `IAppDataPathProvider`, `IDatabaseFileProtector`, `ReceiptPhotoStore`.

### Sicherungen auflisten

1. `BackupViewModel.OnAppearing` (von `SettingsViewModel.OnAppearing` aufgerufen) lädt über `IBackupService.ListAsync`.
2. Je `*.zip` in `backups/` wird das Manifest gelesen (nur Zentralverzeichnis und `manifest.json`); Ergebnis `BackupFileInfo(FileName, CreatedUtc, SizeBytes, AppVersion, IsReadable)`, neueste zuerst; `.tmp` und Fremddateien werden ignoriert, verwaiste `.tmp`/`.work-*` entfernt.
3. `BackupItemViewModel` je Eintrag mit Titel „12.10.2026 13:15“, Größe („1,2 MB“, deutsches Format über `BackupTexts`), Schaltflächen „Wiederherstellen“, „Teilen“, „Löschen“; leere Liste zeigt `Settings.Backup.Empty`.

### Sicherung wiederherstellen

1. Anwender wählt in der Zeile „Wiederherstellen“; `BackupViewModel` schließt andere Rückfragen und liest Zählwerte (`IBackupService.InspectAsync(fileName)`) nur für die Anzeige. Nicht lesbare Dateien zeigen sofort die Fehlermeldung (kein Eingriff).
2. Inline-Rückfrage `…RestoreQuestion` nennt Datum, App-Version, Zählwerte und „Die aktuellen Daten werden vollständig ersetzt. Das lässt sich nicht rückgängig machen. Erstelle vorher eine Sicherung, wenn du den aktuellen Stand behalten möchtest.“; Schaltflächen „Ja, wiederherstellen“ / „Abbrechen“.
3. Bei „Ja“ ruft `BackupViewModel` `IBackupService.RestoreAsync(fileName)`; `IsBusy` sperrt die Oberfläche.
4. `BackupService`:
   1. `BackupArchiveValidator.ValidateAsync`: Datei öffnen, Eintragsnamen gegen die Whitelist prüfen (Zip-Slip: Trenner, `..`, Laufwerk, absolute Pfade, Duplikate, Verzeichniseinträge), Anzahl und deklarierte Größen gegen `BackupLimits`, Manifest lesen und prüfen (Format, `schemaVersion`, Dateiliste deckungsgleich mit den Einträgen), Versionsprüfung; Fehler → `InvalidFile` bzw. `TooNew`, keine Dateisystemänderung außerhalb `restore-staging`.
   2. Entpacken in `restore-staging/<Guid>/` mit Größenzählung und SHA-256-Vergleich; Fotos zusätzlich `DetectExtension` und `MaxReceiptPhotoBytes`.
   3. Staging-Datenbank öffnen (ohne Pool), `PRAGMA integrity_check` und Migrationshistorie gegen Manifest prüfen, mit `MigrateAsync` auf den aktuellen Stand bringen, `foreign_key_check`.
   4. `RestoreSwap.Execute`: Journal schreiben, `SqliteConnection.ClearAllPools()`, Datenbank samt `-wal`/`-shm` und `receipts/` nach `restore-rollback/` verschieben, Staging-Datenbank und Staging-Fotos an die Zielorte verschieben, `Protect`/`ProtectFile` anwenden, Journal löschen, `restore-rollback/` und Staging löschen. Bei jeder Ausnahme in diesem Schritt: neue Dateien entfernen, alte zurückverschieben, Journal löschen, Ergebnis `Failed`.
   5. Nach Erfolg `IPriceCacheInvalidator.InvalidateCaches()`.
5. `BackupViewModel` meldet das Ergebnis (`Settings.Backup.Status`), löst `Restored` aus; `SettingsViewModel` lädt die Einstellungen neu; die übrigen Seiten laden in `OnAppearing`. Preisstände mit altem Zeitstempel gelten durch die vorhandene Alterslogik als veraltet und werden bei Bedarf über die API aktualisiert (kein neuer Code).
6. Fehlermeldungen (zentral in `BackupTexts`): ungültig/beschädigt („Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“), zu neu („Diese Sicherung stammt aus einer neueren Version von Tankatlas (…). Bitte aktualisiere die App. Deine Daten wurden nicht verändert.“), nicht gefunden, allgemeiner Fehler („… Deine Daten wurden nicht verändert.“; im Rollback-Fall identisch, da Rollback den alten Stand wiederherstellt).

Beteiligte Klassen: `BackupViewModel`, `BackupItemViewModel`, `BackupService`, `BackupArchiveValidator`, `RestoreSwap`, `RestoreJournal`, `FuelPriceService`, `SettingsViewModel`.

### Wiederaufnahme nach unterbrochener Wiederherstellung

1. `DatabaseInitializer.RunAsync` ruft vor `MigrateAsync` `IRestoreRecovery.RecoverIfNeeded(dataDirectory)` auf (optionaler Konstruktorparameter, damit bestehende Tests unverändert bleiben).
2. Existiert `restore.journal`: neu platzierte Dateien entfernen, Inhalt von `restore-rollback/` zurückverschieben, Journal löschen; danach regulärer Start. Ohne Journal werden nur verwaiste `restore-staging/` und `restore-rollback/` gelöscht.

### Sicherung löschen

1. „Löschen“ öffnet die Inline-Rückfrage `…DeleteQuestion` (Datum, „Die Sicherungsdatei wird endgültig gelöscht. Deine aktuellen Daten bleiben unverändert.“).
2. „Ja, löschen“ → `IBackupService.DeleteAsync(fileName)`: Dateiname wird gegen das Muster `tankatlas-backup-*.zip` und `Path.GetFileName`-Gleichheit geprüft (keine Pfade), Datei im `backups/` gelöscht; fehlende Datei zählt als `NotFound` und aktualisiert die Liste.

### Sicherung teilen/speichern

1. „Teilen“ → `IBackupSharer.ShareAsync(fullPath, title)`; MAUI: `Share.Default.RequestAsync(ShareFileRequest)` (iOS: Teilen-Blatt mit „In Dateien sichern“; Windows: Freigabe-Dialog), Test: Kopie nach `<Daten>/test-shared/`.
2. `BackupService.GetFullPath(fileName)` liefert den geprüften Pfad; keine Netzübertragung durch die App selbst. Fehler → Meldung „Die Sicherung konnte nicht weitergegeben werden.“

### Sicherung aus Datei einspielen

1. „Sicherung aus Datei einspielen“ (`Settings.Backup.Import`) → `IBackupFilePicker.PickAsync` (MAUI `FilePicker` mit ZIP-Typen je Plattform; Test: Datei aus `TANKATLAS_TEST_BACKUP_IMPORT`, ohne Datei = abgebrochen).
2. Abbruch → keine Meldung. Sonst `IBackupService.ImportAsync(PickedBackupFile)`: Kopie in `backups/import-<Guid>.tmp`, vollständige Validierung (wie Wiederherstellung Schritt 4.1 inklusive Prüfsummen), bei Erfolg Umbenennen in `tankatlas-backup-<Zeitstempel des Manifests>.zip` (bei Kollision Zähler), `ProtectFile`; bei Fehler `.tmp` löschen und `InvalidFile`/`TooNew`.
3. Erfolg: Liste neu laden und für den neuen Eintrag direkt die Rückfrage zur Wiederherstellung öffnen (Ablauf „Sicherung wiederherstellen“ ab Schritt 2).

### Löschrückfrage für Fahrzeuge mit Vorgängen

`TankbookTexts.FormatDeleteQuestion` ergänzt bei `entryCount >= 1` den Satz „Erstelle vorher in den Optionen eine Sicherung, wenn du die Vorgänge behalten möchtest.“ (Ein- und Mehrzahl). Ohne Vorgänge bleibt der Text unverändert.

### Kachelzahl ohne Umbruch

`TankbookKindStatisticsViewModel` liefert zusätzlich `AverageValueFontSize` und `CostPer100KmValueFontSize` über `StatisticsTileFont.SizeFor(text)`; in `TankbookEntriesPage.xaml` erhalten die beiden Zahl-Labels `LineBreakMode="NoWrap"` und die gebundene `FontSize`; das Label der Monatskarte erhält nur `NoWrap`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IBackupService` | Interface | Erstellen, Auflisten, Prüfen (`InspectAsync`), Wiederherstellen, Importieren, Löschen, Pfad ermitteln |
| `BackupService` | Klasse | Umsetzung, Sperre gegen Parallelität, Pfade `backups/`, `restore-staging/`, `restore-rollback/` |
| `BackupArchiveWriter` | Klasse | schreibt ZIP und Manifest, berechnet SHA-256 |
| `BackupArchiveValidator` | Klasse | Namens-, Größen-, Manifest-, Prüfsummen- und Versionsprüfung, Entpacken in das Staging |
| `RestoreSwap` | Klasse | Austausch mit Rücksicherung und Rollback; Fehlerinjektion für Tests über optionalen Hook-Parameter |
| `RestoreJournal` / `IRestoreRecovery` / `RestoreRecovery` | Klasse / Interface / Klasse | Journal und Wiederaufnahme beim Start |
| `BackupManifest`, `BackupManifestFile`, `BackupManifestCounts` | Datenmodellklassen | Manifestinhalt (Serialisierung `System.Text.Json`) |
| `BackupFileInfo` | Datenmodellklasse (Record) | Listeneintrag |
| `PickedBackupFile` | Datenmodellklasse (Record) | gewählte externe Datei (Name, Stream-Fabrik) |
| `BackupOutcome` | Datenmodellklasse (Record) | Ergebnis plus optional Datei |
| `BackupResult` | Enum | `Ok`, `NotFound`, `InvalidFile`, `TooNew`, `Busy`, `Failed` |
| `BackupFormat`, `BackupLimits` | Konstantenklassen | Format-Kennung, Schemaversion, Dateinamen und Muster, Grenzen (Einträge, Gesamtgröße, Datenbankgröße, Manifestgröße) |
| `IBackupFilePicker`, `MauiBackupFilePicker`, `TestBackupFilePicker`, `BackupFilePickerSelector` | Interface / Klassen | Dateiauswahl (Plattform/Test) |
| `IBackupSharer`, `MauiBackupSharer`, `TestBackupSharer`, `BackupSharerSelector` | Interface / Klassen | Teilen (Plattform/Test) |
| `IPriceCacheInvalidator` | Interface | `InvalidateCaches` |
| `BackupViewModel` | ViewModel | Liste, Befehle, Busy-Zustand, Statusmeldung, Ereignis `Restored` |
| `BackupItemViewModel` | ViewModel | Zeile mit Rückfragen (Wiederherstellen/Löschen), `AutomationKey` |
| `BackupTexts` | Textklasse (`Resources/Texts`) | alle Texte, Formatierung von Datum und Größe |
| `StatisticsTileFont` | statische Klasse | Schriftgröße je Textlänge |
| `BackupTestArchive` (Testprojekt/TestSupport) | Testhilfe | erzeugt gültige, zu neue und ungültige Archive (Zip-Slip, falsche Prüfsumme, abgeschnitten) |

## Änderungen an bestehenden Klassen

### `DatabaseInitializer` (Klasse)
- **Geänderte Methoden:** Konstruktor mit optionalem `IRestoreRecovery? recovery = null`; `RunAsync` ruft `RecoverIfNeeded` vor `MigrateAsync` auf.

### `FuelPriceService` (Klasse)
- **Neue Methoden:** `InvalidateCaches` — leert `_searchCache` und `_refreshed`; implementiert `IPriceCacheInvalidator`.

### `SettingsViewModel` (ViewModel)
- **Neue Eigenschaften:** `Backup` (`BackupViewModel?`), `HasBackup`.
- **Geänderte Methoden:** Konstruktor mit optionalem `BackupViewModel? backup = null`, abonniert `Backup.Restored` und ruft `LoadAsync`; `OnAppearing` ruft `Backup?.OnAppearing()`.

### `SettingsPage.xaml` (View)
- `TankbookStyles` ist nur in `TankbookPage`/`TankbookEntriesPage` eingebunden (`<resources:TankbookStyles />` in den Seitenressourcen); `SettingsPage` bindet dieselbe Ressource ein, damit die Schaltflächenstile wiederverwendet werden.
- Neue Karte „Sicherung“ zwischen „Sortierung“ und „Datenquelle“ im Stil der vorhandenen Karten: Überschrift, Hinweistext (Umfang, ohne API-Zugangsdaten, nur lokal, keine automatische Übertragung, geteilte Dateien unverschlüsselt), Schaltflächen `Settings.Backup.Create` und `Settings.Backup.Import`, Meldung `Settings.Backup.Status`, Leerhinweis `Settings.Backup.Empty`, Liste (`BindableLayout`) mit `Settings.Backup.Item.<i>.Title/Size/Restore/Share/Delete/RestoreQuestion/RestoreConfirm/RestoreCancel/DeleteQuestion/DeleteConfirm/DeleteCancel`; `SemanticProperties` für Überschrift (Ebene) und Schaltflächen mit Datum im Namen.

### `MauiProgram` (Klasse)
- DI: `IBackupService`, `IRestoreRecovery`, `IBackupFilePicker` und `IBackupSharer` (über Selector), `FuelPriceService` zusätzlich als `IPriceCacheInvalidator`, `BackupViewModel` transient, `BackupService` mit Fabrik der Datenbankkontexte.

### `TestDataPaths` (statische Klasse)
- **Neue Konstante:** `TestBackupImportEnvironmentVariable` = `TANKATLAS_TEST_BACKUP_IMPORT`.

### `TankbookTexts` (statische Klasse)
- **Geänderte Methoden:** `FormatDeleteQuestion` ergänzt den Sicherungshinweis bei Vorgängen.

### `TankbookKindStatisticsViewModel` / `TankbookEntriesPage.xaml`
- **Neue Eigenschaften:** `AverageValueFontSize`, `CostPer100KmValueFontSize` (`double`).
- XAML: `NoWrap` und gebundene `FontSize` an den Zahl-Labels der beiden Kacheln, `NoWrap` am Betrag der Monatskarte.

### `ReceiptPhotoStore` (Klasse)
- Keine funktionale Änderung; `IsValidFileName`, `DetectExtension` und `MaxReceiptPhotoBytes` werden vom Validator wiederverwendet.

### Dokumentation
- `docs/adr/0010-tankbook-statistics.md`: Abschnitt „Bewusste Abweichungen“ um den Verbrauchsverlauf (steht nicht in der Monatskarte auf eigener Fläche; Balken nicht abgestuft, letzter hervorgehoben) und eine präzisierte Begründung „Keine Symbole“ ergänzt (Symbole wären laut ADR 0004 als Vektorpfade möglich; verzichtet wird, weil die Information in den Beschriftungen steckt und die Kacheln dichter wären); Folgen: Kachelzahlen mit Schriftanpassung, Sichtprüfung vermerkt.
- Neu `docs/adr/0011-local-backup-restore.md`: Archivformat, Manifest, Versionsregeln, Atomarität, Sicherheit (Zip-Slip, Limits), Umfang (keine Geheimnisse/Standortdaten), Testmodus, Info.plist-Prüfung, Abweichung vom Designentwurf (keine Vorlage), Folgen (unverschlüsselt, Sichtprüfung offen).
- Neu `docs/help/Sicherung/` (`index.md`, `beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md`); `docs/help/index.md`, `docs/help/Einstellungen/*` und `docs/help/Tankbuch/*` (Löschhinweis, Kachelzahlen) angepasst.
- `README.md`: Stand Schritt 15, Testmodus-Variable, Bekannte Einschränkungen (Sichtprüfung, Sicherung unverschlüsselt); `changes.log`: Eintrag „Lokale Sicherung und Wiederherstellung (Tankradar Schritt 15, Version 0.1.19)“.

## Datenbankmigrationen

Keine. Die Datenbank wird unverändert gesichert und beim Einspielen älterer Stände mit den vorhandenen Migrationen (`MigrateAsync`) angehoben. `MigrationsTests_ModelSync` bleibt unberührt.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Datei | muss ein lesbares ZIP sein; Dateigröße > 0 | `InvalidFile` |
| Eintragsnamen | nur `manifest.json`, `database/tankatlas.db`, `receipts/<ReceiptPhotoStore.IsValidFileName>`; keine Duplikate (ohne Beachtung der Groß-/Kleinschreibung), keine Verzeichniseinträge, kein `\`, `..`, `:` oder führendes `/` | `InvalidFile` (Zip-Slip) |
| Grenzen | höchstens `BackupLimits.MaxEntries`; Summe deklarierter Größen ≤ `MaxTotalBytes`; Datenbank ≤ `MaxDatabaseBytes`; Manifest ≤ `MaxManifestBytes`; Foto ≤ `MaxReceiptPhotoBytes`; tatsächlich gelesene Bytes ≤ deklarierte Größe | `InvalidFile` |
| Manifest | `format` = `Tankatlas-Backup`; `schemaVersion` ≥ 1; Dateiliste = Eintragsliste; Größe und SHA-256 je Datei stimmen | `InvalidFile` |
| Version | `schemaVersion` ≤ aktuelle; `databaseMigration` ≤ letzte Migration der App und bekannt; Historie der enthaltenen Datenbank = Manifest | `TooNew` bzw. `InvalidFile` |
| Datenbank | SQLite-Datei, `integrity_check` = ok, `foreign_key_check` leer (nach Migration) | `InvalidFile` |
| Fotos | Signatur per `DetectExtension` passt zur Endung | `InvalidFile` |
| Dateiname für Löschen/Teilen/Wiederherstellen | Muster `tankatlas-backup-*.zip`, keine Pfadanteile, Datei liegt in `backups/` | `NotFound` |
| Parallelität | höchstens eine Sicherungsoperation gleichzeitig | `Busy` |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `TANKATLAS_TEST_BACKUP_IMPORT` | Umgebungsvariable (nur Testmodus) | nicht gesetzt (Auswahl gilt als abgebrochen) | Pfad der Datei, die `TestBackupFilePicker` liefert |
| `BackupLimits` | Konstanten | `MaxEntries` 20 000, `MaxTotalBytes` 2 GiB, `MaxDatabaseBytes` 1 GiB, `MaxManifestBytes` 1 MiB | Schutz vor Zip-Bomben |
| `BackupFormat.SchemaVersion` | Konstante | 1 | Version des Sicherungsformats |

`Tankradar.MAUI.csproj` und `Info.plist` bleiben unverändert (`MtouchInterpreter`, `TrimmerRootAssembly` unberührt; `System.IO.Compression` und `System.Text.Json` gehören zur Laufzeit).

## Seiteneffekte und Risiken

- **Laufende Datenbankzugriffe beim Austausch:** Dienste nutzen kurzlebige Kontexte aus der Factory mit Verbindungspool. Mitigation: Oberfläche gesperrt (`IsBusy`), `ClearAllPools()` vor dem Austausch, Austausch nur aus Umbenennungen; hintergrundlaufende Preisabrufe können in diesem Sekundenbruchteil fehlschlagen und werden von den bestehenden Fehlerpfaden abgefangen. Test: Austausch mit nicht freigegebener Verbindung (Pooling) in Integrationstest.
- **Windows und geöffnete Dateien:** Das Umbenennen scheitert, wenn eine Datei noch geöffnet ist (Foto im Bildschirm); Rollback stellt den Ausgangszustand her und meldet allgemeinen Fehler.
- **Veralteter Arbeitsspeicherzustand:** `FuelPriceService`-Caches werden verworfen; ViewModels laden bei `OnAppearing`. `TankbookEntriesPage` liegt in einer Navigation und ist während der Wiederherstellung nicht sichtbar.
- **Datenmenge:** Foto- und Datenbankgröße wirken auf Sicherungsdauer; Verarbeitung asynchron mit Stream-Kopien (80-KiB-Puffer), keine Vollladung in den Speicher.
- **Rückfragetext Fahrzeuge:** Texte werden von Unit- und E2E-Tests geprüft (siehe Tests); Sprachausgabe der Bestätigungsschaltfläche bleibt unverändert.
- **Git-Hooks:** XML-Dokumentation für alle Member, Texte zentral in `BackupTexts`, Enum-Testabdeckung (jeder Wert von `BackupResult` in einem Test), keine Platzhalter-Implementierungen, Formatierung (`dotnet format`), Secrets-Prüfung; Testklassen werden nach Themen klein gehalten (`test-class-structure`).
- **E2E-Stabilität:** Neue Seite wird länger; UIA findet Elemente außerhalb des sichtbaren Bereichs, Schaltflächen werden wie bisher über `Invoke` bedient, nicht per Mausklick. Alle neuen E2E-Tests laufen im Off-Screen-Modus.
- **Sicherungen enthalten Fahrdaten und Belege unverschlüsselt:** dokumentiert (Hilfe, ADR); geteilte Dateien liegen außerhalb des Dateischutzes der App.
- **iOS-Pfad ohne Gerät nicht prüfbar:** Share-/Picker-Implementierungen werden nur kompiliert (CI-iOS-Build); Verhalten wird durch die Test-Doppel-Abläufe und Code-Review abgesichert.

## Umsetzungsreihenfolge

1. **Konstanten, Enum und Modelle**
   - Voraussetzungen: Keine.
   - Beschreibung: `BackupFormat`, `BackupLimits`, `BackupResult`, `BackupManifest*`, `BackupFileInfo`, `BackupOutcome`, `PickedBackupFile`, `TestDataPaths.TestBackupImportEnvironmentVariable`.
2. **Texte**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `BackupTexts` (Überschrift, Hinweise, Schaltflächen, Rückfragen, Meldungen, Formatierung Datum/Größe, Sprachausgabe-Beschreibungen); `TankbookTexts.FormatDeleteQuestion` ergänzen.
3. **Archiv schreiben und prüfen**
   - Voraussetzungen: Schritte 1–2; `ReceiptPhotoStore.IsValidFileName/DetectExtension` (vorhanden).
   - Beschreibung: `BackupArchiveWriter`, `BackupArchiveValidator` (Whitelist, Limits, Manifest, Prüfsummen, Versionsregeln, Entpacken ins Staging).
4. **Austausch und Wiederaufnahme**
   - Voraussetzungen: Schritt 3.
   - Beschreibung: `RestoreJournal`, `RestoreSwap` (mit Test-Hook), `IRestoreRecovery`/`RestoreRecovery`; `DatabaseInitializer` erweitern.
5. **Preisdienst**
   - Voraussetzungen: Keine.
   - Beschreibung: `IPriceCacheInvalidator`, `FuelPriceService.InvalidateCaches`.
6. **BackupService**
   - Voraussetzungen: Schritte 3–5; `IDbContextFactory<TankradarDbContext>`, `IAppDataPathProvider`, `IDatabaseFileProtector`, `IDatabaseInitializer` (vorhanden).
   - Beschreibung: `IBackupService`/`BackupService` mit Erstellen, Auflisten, Prüfen, Wiederherstellen, Importieren, Löschen, Pfad; Sperre gegen Parallelität.
7. **Plattformdienste**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `IBackupFilePicker`, `MauiBackupFilePicker` (Dateitypen je Plattform), `TestBackupFilePicker`, `BackupFilePickerSelector`; `IBackupSharer`, `MauiBackupSharer`, `TestBackupSharer`, `BackupSharerSelector`; `Info.plist` prüfen, Ergebnis für ADR notieren.
8. **ViewModels und Registrierung**
   - Voraussetzungen: Schritte 2, 6, 7.
   - Beschreibung: `BackupItemViewModel`, `BackupViewModel`, `SettingsViewModel`-Anbindung, DI in `MauiProgram`.
9. **Oberfläche**
   - Voraussetzungen: Schritt 8.
   - Beschreibung: Karte „Sicherung“ in `SettingsPage.xaml` mit AutomationIds und Semantik, Stile aus `TankbookStyles.xaml`.
10. **Kachelzahl**
    - Voraussetzungen: Keine.
    - Beschreibung: `StatisticsTileFont`, ViewModel-Eigenschaften, XAML (`NoWrap`, `FontSize`).
11. **Unit- und Integrationstests**
    - Voraussetzungen: Schritte 1–10.
    - Beschreibung: laut Abschnitt Tests; `BackupTestArchive` zuerst anlegen (Testhilfe).
12. **E2E-Tests**
    - Voraussetzungen: Schritte 8–10 und Testhilfen aus 11.
    - Beschreibung: `BackupE2ETestBase` und `BackupE2ETests_*`; vorhandene E2E-Tests anpassen.
13. **Dokumentation**
    - Voraussetzungen: Schritte 1–12.
    - Beschreibung: ADR 0011, ADR 0010 (Zusatzpunkte), Hilfe `Sicherung`, Anpassung `Einstellungen`/`Tankbuch`/Index, README, `changes.log` 0.1.19.
14. **Gesamtprüfung und Zwischenstand**
    - Voraussetzungen: Schritte 1–13.
    - Beschreibung: `scripts/local-ci.ps1` (Unit, Integration, E2E grün), anschließend `scripts/create-review-version.ps1` für `review-versions/0.1.19_<Datum>/` (nicht committen); Sichtprüfung als offene Abnahmeprüfung vermerkt (siehe Designentscheidungen).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Namen: gültige Namen; `../x`, `..\x`, `/abs`, `C:\x`, `receipts/../database/tankatlas.db`, Duplikate (Groß/Klein), Verzeichniseintrag, unbekannter Eintrag, ungültiger Fotoname | `BackupArchiveValidatorTests_EntryNames` (Unit) | Zip-Slip-Schutz, Whitelist |
| Manifest fehlt/leer/kein JSON/falsches Format/Schemaversion 0 und 99; Dateiliste weicht ab; Größe, Hash falsch; Limits überschritten; deklarierte Größe < tatsächliche | `BackupArchiveValidatorTests_Manifest`, `_Limits` (Unit) | Ungültige und manipulierte Dateien, Zip-Bomb |
| Migration älter/gleich/neuer/unbekannt; Schemaversion neuer | `BackupVersionRuleTests` (Unit) | `TooNew`/`InvalidFile`/erlaubt |
| Alle Werte von `BackupResult` | `BackupTextsTests_Results` (Unit) | zentrale, verständliche Texte je Ergebnis, Enum-Abdeckung |
| Datum, Größe (B/KB/MB), Rückfrage- und Statustexte, Zählwerte Ein-/Mehrzahl | `BackupTextsTests_Format` (Unit) | Formatierung |
| Journal schreiben/lesen/löschen; Wiederaufnahme mit/ohne Journal | `RestoreRecoveryTests` (Unit mit Temp-Verzeichnis) | Rollback beim Start |
| Selector im Testmodus/ohne Testmodus | `BackupFilePickerSelectorTests_TestMode`, `BackupSharerSelectorTests_TestMode` (Unit) | Plattformklasse im Testmodus nie erzeugt |
| Datei geliefert/fehlt/abgebrochen | `TestBackupFilePickerTests` (Unit) | Testauswahl ohne Dialog |
| Kopie nach `test-shared`, fehlende Quelle | `TestBackupSharerTests` (Unit) | Testweitergabe |
| Erstellen: Zustände, Busy, Fehler; Liste lädt; leere Liste; Reihenfolge; Schlüssel 1-basiert | `BackupViewModelTests_Create`, `_List` (Unit, Fake-`IBackupService`) | ViewModel-Logik |
| Wiederherstellen: Rückfrage mit Zählwerten, Abbrechen ohne Aufruf, Bestätigen ruft Dienst, Ergebnisse je `BackupResult`, `Restored` ausgelöst nur bei `Ok`, Sprachausgabe | `BackupViewModelTests_Restore` (Unit) | Rückfrage vor dem Überschreiben, Meldungen |
| Löschen: Rückfrage, Abbrechen, Bestätigen, `NotFound` aktualisiert Liste; nur eine Rückfrage gleichzeitig | `BackupViewModelTests_Delete` (Unit) | Löschen nach Rückfrage |
| Teilen: Aufruf mit Pfad, Fehlermeldung; Import: Abbruch, `InvalidFile`, `TooNew`, Erfolg öffnet Rückfrage | `BackupViewModelTests_ShareImport` (Unit) | Teilen/Import |
| Einbettung, Neuladen nach `Restored`, `HasBackup` | `SettingsViewModelTests_Backup` (Unit) | Anbindung an Optionen |
| Schriftgröße je Länge (1–12 Zeichen), Monotonie | `StatisticsTileFontTests` (Unit) | Kachelzahl |
| `InvalidateCaches` leert Such- und Aktualisierungs-Cache (erneuter Abruf erfolgt) | `FuelPriceServiceTests_InvalidateCaches` (Unit) | Zwischenspeicher |
| Erstellen: Archiv enthält exakt Manifest, Datenbank, Fotos; Manifest-Werte (Version, Migration, Zählwerte, Prüfsummen); keine weiteren Einträge; kein Schlüssel-/Standortmaterial (Archiv enthält nur Whitelist, Datenbank enthält keine Tabellen außer den bekannten); kein `.tmp`-Rest; `ProtectFile` für die Datei | `BackupServiceTests_Create` (Integration, echte SQLite-Datei) | Umfang, Manifest, Dateischutz |
| Vollständigkeit: alle Entitäten befüllen (Einstellungen, Spritsorten, Gruppen mit Notiz/Priorität, Favoriten, Fahrzeuge, Vorgänge mit Foto, Stationen, Preisstände mit `RetrievedUtc`), sichern, in neuem Datenverzeichnis wiederherstellen, alle Werte, Zeitstempel und Foto-Bytes identisch | `BackupServiceTests_Completeness` (Integration) | Vollständigkeit, Zeitstempel, historische Daten |
| Wiederherstellung überschreibt geänderte Daten und ersetzt Fotos; Pool wird freigegeben; `Protect`/`ProtectFile` aufgerufen; Cache-Invalidierung | `BackupServiceTests_Restore` (Integration) | Ablauf |
| Sicherung mit älterem Migrationsstand (Datenbank bis `AddVehicles` migriert und befüllt): wird eingespielt, auf aktuellen Stand migriert, Daten erhalten | `BackupServiceTests_OlderVersion` (Integration) | ältere App-Version |
| Neuere Migration/Schemaversion → `TooNew`, Daten und Fotos bytegleich unverändert, Staging bereinigt | `BackupServiceTests_NewerVersion` (Integration) | zu neue Sicherung |
| Ungültige Dateien: kein ZIP, leer, abgeschnitten, ohne Manifest, falscher Hash, beschädigte Datenbank, Zip-Slip-Eintrag, Fotosignatur falsch → `InvalidFile`, Livedaten bytegleich unverändert | `BackupServiceTests_InvalidFiles` (Integration) | verständliche Fehler ohne Datenverlust |
| Fehlerinjektion an jedem Austauschschritt (Hook wirft): alter Stand vollständig wiederhergestellt, kein Journal, kein Staging; simulierter Absturz (Journal bleibt) → `RestoreRecovery` stellt alten Stand her | `BackupServiceTests_Atomicity` (Integration) | Atomarität/Rollback |
| Liste: Reihenfolge, Größe, Datum aus Manifest, unlesbare Datei gekennzeichnet, `.tmp` ignoriert/bereinigt | `BackupServiceTests_List` (Integration) | Liste |
| Löschen: Datei weg; Pfad-Eingaben (`..\x`, absolute Pfade) → `NotFound`, nichts außerhalb gelöscht; unbekannt → `NotFound` | `BackupServiceTests_Delete` (Integration) | Löschen, Pfadschutz |
| Import: gültig (Kopie in der Liste), ungültig/zu neu (nicht in der Liste, `.tmp` entfernt), Name-Kollision | `BackupServiceTests_Import` (Integration) | Import |
| `BackupTestArchive` (gültig/zu neu/Zip-Slip/falscher Hash/abgeschnitten) | Testhilfe (`src/TestSupport` oder Testprojekt) | Archive erzeugen |
| `TankbookE2EDriver`/`SettingsE2ETestBase`-Erweiterungen für Sicherung (`OpenBackupCard`, `PressBackup(item, action)`, `WaitForBackupStatus`) | `BackupE2ETestBase` | Bedienung per UIA (`Invoke`, `SetFieldValue` nur falls Texteingabe nötig) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookTextsTests_Labels` | Wortlaut von `FormatDeleteQuestion` bei Vorgängen (Sicherungshinweis, Ein-/Mehrzahl) |
| `TankbookViewModelTests_Delete` | erwartete `DeleteQuestion` bei Vorgängen |
| `DatabaseInitializerTests_FirstStart`, `_Upgrade` | Konstruktor mit optionalem Parameter (nur prüfen, sollte unverändert laufen) |
| `TankbookKindStatisticsViewModelTests_*` | neue Eigenschaften; vorhandene Werte unverändert |
| Tests, die `SettingsViewModel` erzeugen | optionaler Parameter, keine Änderung erwartet |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Fahrzeug „Golf“ anlegen, Sicherung erstellen (Eintrag mit Datum und Größe erscheint), Daten ändern (Fahrzeug „Opel“ anlegen, Option „In der Nähe“ umschalten), „Wiederherstellen“ → Rückfrage mit Zählwerten → Abbrechen: Daten unverändert → erneut bestätigen: Meldung, Tankbuch zeigt „Golf“ ohne „Opel“, Option zurückgesetzt; Sicherung löschen → Rückfrage → Abbrechen → bestätigen: Liste leer | `BackupE2ETests_Flow` | Ablauf „erstellen – ändern – aus der Liste wiederherstellen – löschen“, Rückfragen | Zusammenspiel von UI, Dienst und echter Datenbank im laufenden Prozess; Unit-Tests sehen den Dateiaustausch unter geöffneter Anwendung nicht |
| Pflicht | Vorgang mit Belegfoto (`TANKATLAS_TEST_RECEIPT_PHOTO`) erfassen, sichern, Vorgang löschen (Foto verschwindet in `receipts/`), wiederherstellen → Vorgang und Foto (Bytes gleich) sind zurück, Verbrauch/Auswertung zeigen wieder die Werte | `BackupE2ETests_Photo` | Belegfotos und Tankbuch in der Sicherung, historische Daten für Auswertung | Datei- und Datenbankstand müssen im echten Prozess zusammenpassen |
| Pflicht | Sicherung per Neustart (`RestartApplication`) weiterhin in der Liste; wiederhergestellte Daten bleiben nach Neustart bestehen | `BackupE2ETests_Persistence` | lokale Ablage, atomarer Austausch | Dateizustand nach echtem Prozessneustart |
| Pflicht | Externe Datei einspielen: Sicherung erstellen, Datei aus `backups/` in ein Testverzeichnis kopieren, in-App löschen, Neustart mit `TANKATLAS_TEST_BACKUP_IMPORT` → „Aus Datei einspielen“ → Rückfrage → Daten zurück; Eintrag steht in der Liste | `BackupE2ETests_Import` | Import externer Datei ohne Systemdialog | Nachweis, dass der Testpfad den Dialog ersetzt und die Kette Import–Prüfung–Wiederherstellung trägt |
| Pflicht | Beschädigte Datei (Text als `.zip`) einspielen: verständliche Meldung, Liste und Daten unverändert; Zip-Slip-Archiv: dieselbe Meldung; zu neue Sicherung (Schemaversion 99): Meldung „neuere Version“ | `BackupE2ETests_InvalidFiles` | Fehlermeldung ohne Datenveränderung, Ablehnung zu neuer Sicherung | sichtbare Fehlermeldung und unveränderte Daten im laufenden Prozess |
| Pflicht | „Teilen“ im Testmodus: Datei erscheint in `<Daten>/test-shared/`, kein Dialog | `BackupE2ETests_Share` | Teilen/Speichern | Nachweis, dass kein blockierender Dialog entsteht |
| Pflicht | Fahrzeug mit Vorgängen löschen: Rückfrage nennt die Sicherung (und das Mitlöschen) | `TankbookE2ETests_DeleteWithEntries` (erweitert) | Zusatz zur Löschrückfrage | sichtbarer Text und Bedienbarkeit |
| Pflicht | Kachelzahl mit vier Stellen (z. B. „9.989,00“) bei 360 pt Fensterbreite (Größenänderung per UIA-`TransformPattern`) einzeilig: Höhe des Zahl-Labels ≤ 1,6 × Schriftzeile, Label innerhalb der Kachel; bei 390 pt ebenso | `TankbookStatisticsE2ETests_TileNumbers` | Kachelzahlen nicht umbrechen | Layoutverhalten nur im echten Rendering prüfbar; Fallback, falls Größenänderung im Off-Screen-Modus nicht unterstützt wird: Prüfung bei Standardbreite mit der längsten Zahl |
| Pflicht | Zweite Sicherung: Reihenfolge (neueste zuerst), Größe angezeigt; „Wiederherstellen“ der älteren wählt die richtige Datei (Auswahl aus der Liste) | `BackupE2ETests_List` | Auswahl aus der Liste | Auswahl in der Oberfläche |

Bestehende E2E-Tests, die angepasst werden müssen:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookE2ETests_DeleteWithEntries`, `TankbookE2ETests_Flow` | Löschrückfrage um den Sicherungshinweis erweitert (Textprüfungen bleiben `Contains`; ergänzen um den Hinweis) |
| `SettingsE2ETests_Defaults` und weitere `SettingsE2ETests_*` | Seite enthält neue Karte; Prüfung auf Reihenfolge/Anzahl der Elemente nachsehen, ansonsten unverändert |
| `TankbookStatisticsE2ETests_Flow` | nur falls Kachel-Label-Eigenschaften (Größe) verglichen werden; sonst unverändert |

## Offene Punkte

Keine.
