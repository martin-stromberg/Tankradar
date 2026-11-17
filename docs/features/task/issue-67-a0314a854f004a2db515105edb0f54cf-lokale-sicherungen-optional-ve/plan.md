# Umsetzungsplan: Lokale Sicherungen optional verschlüsseln (Issue #67)

## Übersicht

Die lokale Sicherung (ADR 0011, `BackupService`) wird um einen optionalen Passwortschutz erweitert: Beim Erstellen kann der Anwender ein Passwort vergeben, die Datei wird dann als verschlüsselter Container (unverschlüsselter Header + AES-256-GCM-Chiffretext des gesamten ZIP-Archivs) abgelegt. Die Wiederherstellung und das Einspielen erkennen verschlüsselte Dateien an Magic-Bytes im Header und fordern das Passwort an. Format und Verfahren werden in einem neuen ADR 0019 festgelegt; die gesamte nachgelagerte Prüf- und Austauschpipeline (`BackupArchiveValidator`, `BackupDatabasePreparer`, `RestoreSwap`, `RestoreRecovery`) bleibt unverändert wirksam, weil entschlüsselt wird, bevor sie greift.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine Sicherungsdatei `tankatlas-backup-<Stempel>[-n].zip` in `backups/` des Datenverzeichnisses bzw. eine über `ImportAsync` eingespielte externe Datei. Eine verschlüsselte Sicherung ist derselbe Dateiname/dieselbe Endung, aber ein Binärcontainer: unverschlüsselter Header (Magic, Containerversion, KDF-Parameter, Salt, Nonce-Basis, Chunk-Größe) + verschlüsseltes ZIP als Nutzlast. |
| Auslösende Ereignisse | `CreateCommand` auf der Sicherungskarte (mit optional befüllten Passwortfeldern), `RestoreCommand` an einem Listeneintrag, `ImportCommand` (Dateiauswahl). Erkennung verschlüsselt/unverschlüsselt ohne Zutun des Anwenders über die Magic-Bytes am Dateianfang. |
| Erwartete Wirkung | Mit Passwort erstellte Datei ist ohne Passwort nicht lesbar (Manifest inklusive); Wiederherstellung mit korrektem Passwort läuft mit denselben Zählwert-Rückfrage- und Austauschschritten wie bisher; falsches Passwort, Beschädigung und Manipulation werden kontrolliert mit einer gemeinsamen Meldung abgelehnt und verändern den Datenbestand nicht; unverschlüsselte Sicherungen und der Ablauf ohne Passwort bleiben unverändert. |
| Umgang mit fehlenden Objekten | **Passwort beim Erstellen:** optional; leere Felder → unverschlüsselter Pfad (heutiges Verhalten). **Passwort bei Wiederherstellung/Einspielen:** wird bei verschlüsselten Dateien in der Inline-Rückfrage angefordert (`BackupResult.PasswordRequired` als Dienst-Signal); Abbruch verwirft den Vorgang ohne Änderung. **Entschlüsseltes Zwischenprodukt:** wird je Vorgang als `.tmp`-Arbeitsdatei im Sicherungsverzeichnis erzeugt und im `finally` verworfen (Muster von `tempPath`/`workDirectory` in `BackupService`). **Formatversionskennung:** `EncryptedContainerVersion` im unverschlüsselten Header; höhere Version → `TooNew`. |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine nachträgliche Verschlüsselung vorhandener Sicherungsdateien | Nichtziel im Issue; nur neu erstellte Dateien können verschlüsselt sein | bestätigt (steht als Nichtziel im Issue) |
| 2 | Keine Verwaltung/Wiederherstellung vergessener Passwörter (kein Reset, kein Schlüsselbund) | Nichtziel im Issue; Hinweis im UI-Text, dass das Passwort nicht gespeichert wird | bestätigt (steht als Nichtziel im Issue) |
| 3 | Keine automatische Cloud-Synchronisierung von Sicherungen | Nichtziel im Issue und in ADR 0011 | bestätigt (steht als Nichtziel im Issue) |
| 4 | Keine Verschlüsselungspflicht: unverschlüsseltes Erstellen bleibt Standard | Nichtziel im Issue | bestätigt (steht als Nichtziel im Issue) |
| 5 | Kein Schutz zusätzlicher Datenquellen; der Sicherungsumfang bleibt wie in ADR 0011 | Nichtziel im Issue | bestätigt (steht als Nichtziel im Issue) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Containerformat | Neuer Binärcontainer: unverschlüsselter Header (Magic-Bytes, 16-Bit-Containerversion, KDF-Kennung, Iterationen, Salt, Nonce-Basis, Chunk-Größe) + das vollständige bisherige ZIP als verschlüsselte Nutzlast. Inneres Format (`SchemaVersion` = 1, Manifest, Prüfsummen) unverändert. | Erkennung und Versionsregeln müssen vor der Entschlüsselung greifen (SchemaVersion liegt im verschlüsselten Manifest). Das erprobte ZIP-Innenformat mit seiner gesamten Prüfkette bleibt erhalten; verschlüsselte Dateien sind nach dem Entschlüsseln exakt das bisherige Archiv. Exakte Bytefolge/Headerfelder in ADR 0019. |
| Schlüsselableitung | PBKDF2-HMAC-SHA256 über `Rfc2898DeriveBytes.Pbkdf2`, 32-Byte-Schlüssel, Salt 16 Byte, Iterationen im Header (Startwert 600.000, OWASP-Richtwert) | In der BCL enthalten — kein neues NuGet-Paket (Argon2id würde eine Fremdbibliothek erfordern; das Projekt hat bewusst nur MAUI/EF/SQLite-Pakete). NIST-konform; die Iterationszahl steht im Header und ist später erhöhbar. |
| Ver-/Entschlüsselung | AES-256-GCM, chunked (z. B. 1 MiB je Chunk): Nonce je Chunk aus Basis-Nonce + Zähler, 16-Byte-Tag je Chunk, Header-Bytes + Chunkindex + Letztes-Chunk-Flag als AAD | `AesGcm` ist eine One-Shot-API; Sicherungen dürfen bis 2 GiB (`BackupLimits.MaxTotalBytes`) groß sein und dürfen nicht vollständig im Speicher liegen (Stream-Kopien, ADR 0011). Chunking mit per-Chunk-Authentifizierung bindet Reihenfolge und Vollständigkeit. Details in ADR 0019. |
| Dateiname/Endung | Unverändert `tankatlas-backup-*.zip`; Erkennung ausschließlich über Magic-Bytes (Kollision mit `PK\x03\x04` ausgeschlossen) | `BackupFormat.IsValidFileName`, `GetUniquePath`, ZIP-Filter des `MauiBackupFilePicker`, `IBackupSharer` und das E2E-Hilfsmittel `BackupFiles()` bleiben unverändert. Beantwortet Anforderungsfrage 5. |
| Passwortannahme | Passwort als optionaler Methodenparameter (`string? password = null`) an `CreateAsync`, `InspectAsync`, `RestoreAsync`, `ImportAsync`; **kein** `IBackupPasswordPrompt`-Dienst | Die Passwortabfrage ist reine UI (Inline-`Entry` in den bestehenden Rückfragen — das Muster der Sicherungskarte aus ADR 0011: keine Systemdialoge). Ein Prompt-Interface von Dienst zu UI würde die bisherige Richtung (ViewModel steuert Dienst) umkehren. Tests: Unit/Integration übergeben den Parameter direkt, E2E tippt in das `Entry`-Feld — es ist keine neue Umgebungsvariable nötig. Beantwortet Anforderungsfrage 6. |
| Neue `BackupResult`-Werte | `PasswordRequired` (Dienst signalisiert: Datei verschlüsselt, kein Passwort übergeben) und `WrongPassword` (AEAD-Authentifizierung fehlgeschlagen — falsches Passwort **oder** beschädigte/manipulierte Datei, eine gemeinsame Meldung) | AEAD kann beides technisch nicht unterscheiden; eine kombinierte Meldung ist ehrlich und verständlich. Beantwortet Anforderungsfrage 3. |
| Wiederherstellungs-UX bei verschlüsselten Einträgen | Zweistufige Inline-Rückfrage am Eintrag: Stufe 1 Passwort-`Entry` + Schaltfläche „Prüfen"; nach erfolgreichem `InspectAsync` Stufe 2 die bisherige Rückfrage **mit Zählwerten** + „Ja, wiederherstellen" | Erhält die bestehende Sicherheitswirkung (geprüfte Zählwerte vor dem destruktiven Schritt). Beantwortet Anforderungsfrage 1: Das Passwort wird vor der Inhaltsrückfrage angefordert, der volle Ablauf bleibt erhalten. Gleiches gilt für `InspectAsync` und `ImportAsync`. |
| Passworteingabe beim Erstellen | Zwei Felder (Passwort + Wiederholung) auf der Sicherungskarte; Gleichheit vor dem Dienstaufruf prüfen; keine Mindestlänge/Zusammensetzung | Ein Tippfehler macht die Sicherung unbrauchbar, ohne dass es der Anwender merkt — die Bestätigung ist der übliche Schutz. Verantwortung für Passwortstärke liegt beim Anwender (Hinweistext). Beantwortet Anforderungsfrage 2. |
| Kennzeichnung in der Liste | `BackupFileInfo.IsEncrypted` (aus dem unverschlüsselten Header lesbar, kein Passwort nötig); `FormatItemTitle` hängt „(verschlüsselt)" an, analog „(nicht lesbar)" | Beantwortet Anforderungsfrage 4: ja, Kennzeichnung in der Liste — möglich, weil die Erkennung im Header steht. |
| Teilen verschlüsselter Dateien | Unverändert über `IBackupSharer`; kein zusätzlicher Dialog beim Teilen | Die Datei ist in sich abgeschlossen (Passwort wird ohnehin mündlich/extern ausgetauscht); der Sichtvermerk „(verschlüsselt)" im Eintrag macht den Zustand sichtbar. Beantwortet Anforderungsfrage 7. |
| Krypto-Komponente | Neue statische Klasse `BackupCrypto` (Schlüsselableitung, Chunk-Ver-/Entschlüsselung, Header lesen/schreiben) + Datenmodellklasse `BackupEncryptionHeader`; Benennung endgültig in ADR 0019 | Folgt dem Muster `BackupArchiveWriter`/`BackupArchiveValidator` (statische Helfer, vom Dienst orchestriert). `BackupArchiveValidator` bleibt reine ZIP-Prüfpipeline — Erkennung/Entschlüsselung liegt als vorgelagerter Schritt im `BackupService`. |

## Programmabläufe

### Sicherung mit Passwort erstellen

1. Anwender füllt `CreatePassword` und `CreatePasswordRepeat` auf der Karte (optional) und löst `CreateCommand` aus.
2. `BackupViewModel.CreateAsync`: bei ungleichen Einträgen → Status `PasswordsDoNotMatch`, kein Dienstaufruf; beide leer → `password = null`.
3. `BackupService.CreateAsync(password)`: wie bisher `_gate`, `InitializeAsync`, `.work-<Guid>`-Verzeichnis, `CreateSnapshot` (VACUUM INTO).
4. `BackupArchiveWriter.WriteAsync` schreibt das ZIP in das Arbeitsverzeichnis (unverändert); Selbstprüfung `BackupArchiveValidator.Validate` wie bisher.
5. Mit Passwort: `BackupCrypto.EncryptAsync(zipPath, tempPath, password)` schreibt Header (Magic, `EncryptedContainerVersion`, KDF-Parameter, Salt, Nonce-Basis, Chunk-Größe) + chunked-AES-256-GCM-Strom nach `finalPath + .tmp`. Ohne Passwort: ZIP direkt nach `tempPath` (bisheriger Pfad).
6. `File.Move(tempPath, finalPath)`, `ProtectFile`, `BackupOutcome(Ok, ReadInfo)` — `ReadInfo` erkennt den Header und liefert `IsEncrypted = true`.
7. `finally`: `tempPath` und Arbeitsverzeichnis entfernen; ViewModel leert die Passworteigenschaften nach dem Vorgang.

Beteiligte Klassen/Komponenten: `BackupViewModel`, `BackupService`, `BackupArchiveWriter`, `BackupArchiveValidator`, `BackupCrypto`, `BackupEncryptionHeader`, `BackupFormat`, `BackupLimits`.

### Verschlüsselte Sicherung wiederherstellen (Listeneintrag)

1. `ListAsync` → `ReadInfo`: Magic-Bytes erkannt → `BackupFileInfo` mit `IsEncrypted = true`, `IsReadable = true`, `CreatedUtc` = Dateidatum, `AppVersion` = leer. Titel zeigt „(verschlüsselt)". Magic erkannt, aber Header defekt → `IsReadable = false`.
2. `RestoreCommand` → `BeginRestoreAsync` → `ShowRestoreQuestionAsync`: `IsReadable == false` weiterhin sofort `InvalidFile`; neu: `IsEncrypted == true` → `item.ShowPasswordQuestion(...)` (Rückfrage-Bereich im Passwort-Stadium, `IsPasswordStage = true`; `ShowPasswordQuestion` trägt den Rückfragetext in `item.RestoreQuestion`), kein `InspectAsync`-Aufruf ohne Passwort; danach `_announcer.Announce(item.RestoreQuestion)` — Sprachausgabe analog zum Zählwert-Stadium (`BackupViewModel.cs:437`).
3. Anwender gibt das Passwort in `item.RestorePassword` ein und löst `UnlockRestoreCommand` aus → `UnlockRestoreAsync(item)`. **Verbindliche Reihenfolge:** `UnlockRestoreAsync` liest `item.RestorePassword` **zuerst in eine lokale Variable** und ruft **erst danach** `TryBeginOperation()` auf — das Sperren schließt über `CloseQuestions` → `item.HideRestoreQuestion()` (`BackupViewModel.cs:454–466`, `474–481`) die Rückfrage und leert `RestorePassword`; ein Lesen nach dem Sperren läfe ins Leere. Dann `InspectAsync(item.FileName, password)`:
   - `WrongPassword` → Status `GetMessage(WrongPassword)` („Passwort falsch oder beschädigt"), Rückfrage bleibt geschlossen (konsistent zum bisherigen Fehlerverhalten), das lokale Passwort wird verworfen, Datenbestand unverändert.
   - `Ok` → `item.ShowRestoreQuestion(...)` mit den Zählwerten aus der geprüften Datenbank (Stufe 2, `IsPasswordStage = false`), danach `item.RestorePassword = password` zurückschreiben — das Item bleibt alleiniger Träger des Rückfrage-Zustands, `HideRestoreQuestion` leert es beim nächsten Schließen/Abbruch wieder; `_announcer.Announce(item.RestoreQuestion)`.
4. `ConfirmRestoreCommand` → `ConfirmRestoreAsync`: **gleiche Reihenfolge** — zuerst `item.RestorePassword` in eine lokale Variable lesen, dann `TryBeginOperation()`, dann `RestoreAsync(item.FileName, password)`: Formaterkennung → `BackupCrypto.DecryptAsync` in `.tmp`-Arbeitsdatei → bestehende Pipeline `Validate` → `StageAsync` → `RestoreSwap.Execute` → `RestoreWalMode` → Cache-Verwerfung → `Restored`/`NotifyRestored`.
5. Abbruch (`CancelRestoreCommand`) oder `HideRestoreQuestion`: leert `RestorePassword` und schließt die Rückfrage — keine Änderung.

Beteiligte Klassen/Komponenten: `BackupItemViewModel`, `BackupViewModel`, `BackupService`, `BackupCrypto`, `BackupArchiveValidator`, `BackupDatabasePreparer`, `RestoreSwap`, `RestoreNotifier`, `BackupTexts`, `SettingsPage.xaml`.

### Externe verschlüsselte Datei einspielen

1. `ImportCommand` → `ImportAsync`: `_picker.PickAsync` → `BackupService.ImportAsync(file, null)`.
2. Dienst liest begrenzt den Dateianfang (`≤ BackupLimits.MaxEncryptedHeaderBytes`):
   - kein Magic → bisheriger Pfad (kopieren, `Validate`, `StageAsync`, verschieben).
   - Magic + kein Passwort → `BackupOutcome(PasswordRequired)`, **ohne** die Datei zu kopieren oder anzulegen.
3. ViewModel: `PasswordRequired` → `IsAwaitingImportPassword = true` mit Passwort-`Entry` und „Einspielen"/„Abbrechen" auf Kartenebene; die `PickedBackupFile` bleibt im ViewModel-Feld (`_pendingImport`).
4. „Einspielen" → `ImportAsync(file, password)`: `CopyLimitedAsync` → temp1 (verschlüsselt), `DecryptAsync` → temp2 (ZIP), `Validate`/`StageAsync` auf temp2, `finalPath = GetUniquePath(directory, validation.Manifest.CreatedUtc)` — die Zeitstempelquelle ist das Manifest der **entschlüsselten** Datei (Parität zum unverschlüsselten Pfad, `BackupService.cs:299`); `File.Move(temp1, finalPath)` — die **verschlüsselte** Originaldatei wandert in `backups/`. `OpenReadAsync` der `PickedBackupFile` wird erneut aufgerufen (Peek und Kopie sind zwei getrennte Strom-Öffnungen); der übergebene Delegate muss daher **je Aufruf einen neuen/seekbaren Strom** liefern — `TestBackupFilePicker` und `MauiBackupFilePicker` tun das bereits; Fake-Testdaten sind ebenso aufzubauen (Muster `new PickedBackupFile(name, () => Task.FromResult<Stream>(new MemoryStream(bytes)))`, wie in `BackupViewModelTests_ShareImport`).
5. `WrongPassword`/`InvalidFile` → Status, temporäre Dateien weg, kein Listeneintrag. `Ok` → Liste neu laden, Eintrag suchen, Stufe-1-Passwort-Rückfrage wie beim Listeneintrag (bekanntes Verfahren), Status `Imported`.

Beteiligte Klassen/Komponenten: `BackupViewModel`, `IBackupFilePicker`, `PickedBackupFile`, `BackupService`, `BackupCrypto`, `BackupArchiveValidator`.

### Formaterkennung und Entschlüsselung im Dienst (gemeinsamer Vorschritt)

1. `InspectAsync`, `RestoreAsync`, `ImportAsync` rufen nach `GetFullPath`/Kopie zuerst `BackupCrypto.ReadHeader`/`IsEncrypted` auf.
2. Unverschlüsselt → bisheriger Ablauf unverändert (übergebene Passwörter werden ignoriert).
3. Verschlüsselt: Headerversion > `EncryptedContainerVersion` → `TooNew`; Header defekt → `InvalidFile`; kein Passwort → `PasswordRequired`.
4. Passwort vorhanden → `Pbkdf2` → `DecryptAsync` in `.tmp`-Datei; `CryptographicException`/Authentifizierungsfehler → `WrongPassword`; danach die bestehende `Validate`/`StageAsync`-Pipeline auf der entschlüsselten Datei; `.tmp` im `finally` entfernen.

Beteiligte Klassen/Komponenten: `BackupService`, `BackupCrypto`, `BackupEncryptionHeader`, `BackupFormat`, `BackupLimits`, `BackupFileCleanup`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `BackupCrypto` | statische Klasse (`Services/Backup`) | Schlüsselableitung (PBKDF2-HMAC-SHA256), chunked AES-256-GCM-Ver-/Entschlüsselung als Strom, Header schreiben/lesen/validieren, `IsEncrypted`-Erkennung anhand der Magic-Bytes; Schlüssel nach Ableitung mit `CryptographicOperations.ZeroMemory` verwerfen |
| `BackupEncryptionHeader` | Datenmodellklasse (`Models/Backup`) | Geparster unverschlüsselter Container-Header: Containerversion, KDF-Kennung, Iterationen, Salt, Nonce-Basis, Chunk-Größe; serialisiert/deserialisiert mit festen Feldgrenzen |
| `BackupTamper`-Erweiterung ist nicht nötig; stattdessen: `BackupEncryptedTestArchive` | Test-Hilfsklasse (`src/TestSupport/`, nur in Unit/Integration eingebunden) | Erzeugt/manipuliert verschlüsselte Container über den Produktivcode `BackupCrypto` (roundtrip-fähige Testdateien, gezielt defekte Tags/Header/Chunks) — eigene Datei, weil `BackupTestArchive` auch im E2E-Projekt ohne `Tankradar.MAUI`-Referenz kompiliert wird |

## Änderungen an bestehenden Klassen

### `BackupFormat` (statische Klasse)

- **Neue Mitglieder:** `EncryptedMagic` (`byte[]`/`ReadOnlySpan<byte>`, Magic-Bytes am Dateianfang, darf nicht mit `PK` beginnen), `EncryptedContainerVersion` (`ushort` = 1), `EncryptedChunkSize` (`int`, z. B. 1 MiB), `EncryptedKeyBytes`/`EncryptedSaltBytes`/`EncryptedNonceBytes`/`EncryptedTagBytes` (Längen der Krypto-Parameter), `Pbkdf2Iterations` (Startwert 600.000)
- **`IsValidFileName`/`FileExtension`/`FileNamePrefix`:** unverändert (`tankatlas-backup-*.zip` bleibt)
- **`SchemaVersion`:** bleibt 1 — das innere Manifestformat ändert sich nicht

### `BackupLimits` (statische Klasse)

- **Neue Konstante:** `MaxEncryptedHeaderBytes` (`int`, z. B. 4 KiB) — Höchstlänge beim Header-Einlesen

### `BackupResult` (Enum)

- **Neue Werte:** `PasswordRequired` — die Sicherung ist verschlüsselt und es wurde kein Passwort übergeben; `WrongPassword` — Entschlüsselung/Authentifizierung fehlgeschlagen (falsches Passwort oder beschädigte/manipulierte Datei)

### `BackupFileInfo` (sealed record)

- **Neuer Positionsparameter:** `IsEncrypted` (`bool`, anfügen als letzter Parameter) — Kennzeichnung für Liste und Ablaufsteuerung; alle Konstruktionsstellen (`BackupService.ReadInfo`, `FakeBackupService`, Tests) anpassen

### `IBackupService` (Interface)

- **Geänderte Signaturen** (Passwort optional, `null` = unverschlüsselt/nicht angegeben):
  - `CreateAsync(string? password = null, CancellationToken = default)`
  - `InspectAsync(string fileName, string? password = null, CancellationToken = default)`
  - `RestoreAsync(string fileName, string? password = null, CancellationToken = default)`
  - `ImportAsync(PickedBackupFile file, string? password = null, CancellationToken = default)`
- `ListAsync`, `DeleteAsync`, `GetFullPath` unverändert

### `BackupService` (Klasse)

- **`CreateAsync`:** optionaler `password`-Parameter; bei `IsNullOrEmpty` der bisherige Pfad, sonst ZIP im Arbeitsverzeichnis → Selbstprüfung → `BackupCrypto.EncryptAsync` nach `tempPath`
- **`InspectAsync`/`RestoreAsync`:** Formaterkennung vor `Validate`; bei verschlüsselt: `PasswordRequired` (nur `InspectAsync` — das Dienst-Outcome trägt das Signal; `RestoreAsync` liefert `BackupResult.PasswordRequired`), Header-Versionsregel (`TooNew`), `DecryptAsync` in `.tmp`-Arbeitsdatei, Fehler → `WrongPassword`, danach bestehende Pipeline auf der entschlüsselten Datei
- **`ImportAsync`:** Header-Peek vor `CopyLimitedAsync` (frühes `PasswordRequired` ohne Kopie); mit Passwort zwei temporäre Dateien (verschlüsselte Kopie + entschlüsseltes ZIP); `GetUniquePath` erhält `validation.Manifest.CreatedUtc` des entschlüsselten ZIP; in die Liste wandert die verschlüsselte Datei; Peek und Kopie öffnen die `PickedBackupFile` zweimal (siehe Programmablauf)
- **`ReadInfo`:** liest den Dateianfang; bei Magic → Header parsen → `IsEncrypted = true`, `IsReadable = true` (Header gültig) bzw. `false` (Header defekt), `CreatedUtc` aus Dateidatum, `AppVersion` leer
- **Neue private Hilfsmethode:** `DecryptToTempAsync(string sourcePath, string? password, string workFile)` o. ä. — kapselt Erkennung, Versionsregel, Ableitung und Strom-Entschlüsselung für `Inspect`/`Restore`/`Import`

### `BackupViewModel` (Klasse)

- **Neue Eigenschaften:** `CreatePassword`/`CreatePasswordRepeat` (`string`, zwei-Wege-gebunden, werden nach jedem Erstellversuch geleert), `IsAwaitingImportPassword` (`bool`), `ImportPassword` (`string`), `ImportPasswordText` (Rückfragetext der Karte)
- **Neue Befehle:** `ConfirmImportPasswordCommand`, `CancelImportPasswordCommand` (Einspielen mit/ohne Passwort fortsetzen bzw. verwerfen; beide `IsNotBusy`-gesperrt)
- **`CreateAsync`:** Passwortgleichheit prüfen (`PasswordsDoNotMatch`-Status, kein Dienstaufruf); `_service.CreateAsync(password)`; Passwortfelder im `finally` leeren
- **`ShowRestoreQuestionAsync`:** nach dem `IsReadable`-Check neu `IsEncrypted`-Zweig → `item.ShowPasswordQuestion(...)` statt `InspectAsync`; `_announcer.Announce(item.RestoreQuestion)` gilt für beide Stadien (Passwort- und Zählwert-Rückfrage)
- **Neue private Methode:** `UnlockRestoreAsync(BackupItemViewModel)` — Passwort-Stadium der Wiederherstellungs-Rückfrage; **liest `item.RestorePassword` zwingend vor `TryBeginOperation()` in eine lokale Variable** (das Sperren schließt über `CloseQuestions` → `HideRestoreQuestion` die Rückfrage und leert das Feld, `BackupViewModel.cs:454–466`/`474–481`); `InspectAsync(item.FileName, password)` → bei `Ok` Zählwert-Rückfrage (Stufe 2) und Rückschreiben des Passworts in `item.RestorePassword`, bei `WrongPassword` Statusmeldung mit verworfenem Passwort
- **`ConfirmRestoreAsync`:** liest `item.RestorePassword` ebenfalls vor `TryBeginOperation()` in eine lokale Variable und ruft `_service.RestoreAsync(item.FileName, password)` — bei unverschlüsselten Einträgen ist der Wert leer und wird als `null` weitergereicht
- **`ImportAsync`:** bei `PasswordRequired` `_pendingImport` halten und `IsAwaitingImportPassword` setzen; `ConfirmImportPasswordAsync` wiederholt `ImportAsync(file, ImportPassword)`; `CancelImportPassword` verwirft die Auswahl; `_pendingImport` und `ImportPassword` nach Abschluss/Abbruch leeren

### `BackupItemViewModel` (Klasse)

- **Neue Eigenschaften:** `RestorePassword` (`string`, zwei-Wege-gebunden an das Passwort-`Entry`), `IsPasswordStage` (`bool` — Rückfrage zeigt Passwortfeld statt Zählwerten), `UnlockText` (Schaltfläche „Prüfen" aus `BackupTexts`)
- **Neue Methoden:** `ShowPasswordQuestion(string question)` (setzt `RestoreQuestion = question`, `IsPasswordStage = true`, `IsConfirmingRestore = true`), `HideRestoreQuestion` erweitert (leert `RestorePassword` und `IsPasswordStage`)
- **`ShowRestoreQuestion`:** setzt `IsPasswordStage = false` (Zählwert-Stadium)
- **Neuer Konstruktor-Delegate:** `unlockRestore` (`Action<BackupItemViewModel>`) → `UnlockRestoreCommand`; `RefreshCommandStates` um den neuen Befehl ergänzen

### `SettingsPage.xaml` (Sicherungskarte)

- **Neue Elemente auf der Karte:** zwei `Entry` (`IsPassword = true`) für Passwort/Wiederholung mit `Placeholder` aus `BackupTexts`, AutomationIds `Settings.Backup.Password` bzw. `Settings.Backup.PasswordRepeat` (vor der Schaltfläche „Sicherung erstellen"), plus Hinweis-`Label`, dass leere Felder eine unverschlüsselte Sicherung ergeben
- **Neuer Inline-Bereich „Passwort zum Einspielen":** sichtbar bei `IsAwaitingImportPassword` — `Label` (`ImportPasswordText`), `Entry` (`Settings.Backup.ImportPassword`), Schaltflächen `Settings.Backup.ImportPasswordConfirm`/`.ImportPasswordCancel`
- **Rückfrage-Bereich des Eintrags:** `Entry` (`IsPassword = true`, `Settings.Backup.Item.{0}.RestorePassword`, `Placeholder` aus `BackupTexts`) und Schaltfläche „Prüfen" (`Settings.Backup.Item.{0}.RestoreUnlock`), beide sichtbar bei `IsPasswordStage`; Zählwert-Text und „Ja, wiederherstellen" weiterhin bei `IsConfirmingRestore` ohne Passwort-Stadium
- Muster: Inline-Rückfragen der bestehenden Karte (`IsVisible`-Layouts, `TankbookButton`/`TankbookOutlineButton`, `SemanticProperties.Hint`) — keine Systemdialoge

### `BackupTexts` (statische Klasse)

- **Geändert:** `Hint` — Satz zur Verschlüsselung neu formulieren („...ist die Datei nur verschlüsselt, wenn du beim Erstellen ein Passwort vergibst; das Passwort wird nicht gespeichert und ist nicht wiederherstellbar.")
- **Neue Konstanten:** `Encrypted` („verschlüsselt"), `PasswordPlaceholder`, `PasswordRepeatPlaceholder`, `PasswordsDoNotMatch`, `PasswordQuestion` (Passwort-Stadium der Wiederherstellungs-Rückfrage, z. B. „Diese Sicherung ist verschlüsselt. Gib das Passwort ein, um ihren Inhalt zu prüfen."), `ImportPasswordQuestion`, `WrongPassword` („Das Passwort ist falsch oder die Datei ist beschädigt. Deine Daten wurden nicht verändert."), `PasswordRequired` (Meldung des `GetMessage`-Werts), `UnlockButton` („Prüfen"), `PasswordHint` (Leerlassen-Hinweis auf der Karte)
- **`GetMessage`:** `PasswordRequired`/`WrongPassword` abbilden
- **`FormatItemTitle`:** bei `IsEncrypted` „(verschlüsselt)" anhängen; `FormatRestorePasswordQuestion(createdUtc)` o. ä. für das Passwort-Stadium

### `MauiProgram` (DI)

- Unverändert: `BackupCrypto` ist statisch, das Passwort ist Aufrufparameter — es gibt keine neue Registrierung und keinen neuen Testmodus-Selektor.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `CreatePassword`/`CreatePasswordRepeat` | Beide Felder müssen übereinstimmen; beide leer → unverschlüsselte Sicherung | Status `PasswordsDoNotMatch`, kein Dienstaufruf |
| Passwort (Dienst) | `IsNullOrEmpty` → unverschlüsselter Pfad; sonst verbatim (kein Kürzen, keine Normalisierung) | — |
| Container-Header | Genau `EncryptedMagic` am Anfang, sonst ZIP-Pfad; Header ≤ `MaxEncryptedHeaderBytes`; `ContainerVersion ≤ EncryptedContainerVersion`; KDF-Kennung bekannt; Iterationen/Saltlänge/Chunk-Größe in dokumentierten Grenzen | Ungültiger Header → `InvalidFile`; zu neue Version → `TooNew` |
| Chiffretext | GCM-Tag je Chunk authentisch; Länge konsistent (letztes Chunk-Flag) | `CryptographicException` → `WrongPassword` |
| `RestorePassword`/`ImportPassword` | Bei verschlüsselter Datei erforderlich; leere Eingabe → `PasswordRequired`-Signal bzw. abgebrochene Rückfrage | UI bleibt im Passwort-Stadium; ohne Passwort kein Dienstaufruf mit leerem Wert nötig |

## Konfigurationsänderungen

Keine Anwendungseinstellung und keine `appsettings`-Änderung. KDF-Parameter (Iterationszahl, Salt-/Nonce-/Schlüssel-/Tag-Längen, Chunk-Größe) und die Containerversion sind Konstanten in `BackupFormat` bzw. werden pro Datei im Header festgehalten — nicht nutzerkonfigurierbar (laut Anforderung).

## Seiteneffekte und Risiken

- **`IBackupService`-Signaturen:** Alle Implementierungen und Aufrufer betroffen — `FakeBackupService` muss die neuen Parameter aufnehmen und kann sie aufzeichnen; bestehende Aufrufe bleiben durch `= null`-Defaults kompilierfähig.
- **`BackupFileInfo`-Record:** Neuer Positionsparameter `IsEncrypted` bricht alle Konstruktionsstellen (`ReadInfo`, `FakeBackupService`, Testdaten) — Anpassung nötig.
- **`BackupResult`-Enum:** `BackupTexts.GetMessage` und `GetMessage_AllResults_HaveText` müssen die neuen Werte abdecken.
- **`BackupTexts.Hint`:** Der geänderte Wortlaut bricht `BackupTextsTests_Results.Hint_NamesScopeAndEncryption` — Test an den neuen Text anpassen.
- **Leistung:** PBKDF2 mit ~600.000 Iterationen kostet je Vorgang grob einige hundert Millisekunden (mobil ggf. mehr) — einmalig pro Vorgang, akzeptabel; `InspectAsync` auf verschlüsselten Dateien entschlüsselt zusätzlich die komplette Datei vor dem Zählen.
- **Klartext-Zwischendatei:** Das entschlüsselte ZIP liegt kurzzeitig als `.tmp`-Datei im App-Datenverzeichnis (gleicher Schutz wie Staging/`restore-staging`, in dem ohnehin die Klartext-Datenbank landet) und wird im `finally` entfernt — Reste räumt `CleanUpLeftovers` beim nächsten `ListAsync` auf; im ADR festzuhalten.
- **iOS-Export-Compliance:** Die App nutzt künftig eigene (wenn auch betriebssystemimplementierte) Verschlüsselung für Dateien — `ITSAppUsesNonExemptEncryption` bleibt `false` (Entscheidung vom 2026-11-17); die Begründung in `docs/help/Datenschutz/beschreibung.md` (Abschnitt „Export-Compliance") ist zu aktualisieren.
- **Abwärtskompatibilität:** Ältere App-Versionen lesen verschlüsselte Dateien als `InvalidFile` (kein `PK`-Anfang) — erwartbares Verhalten, wird in der Hilfe erwähnt. Neue App liest alte unverschlüsselte Dateien unverändert (Format-Pfad unverändert).
- **E2E-Projekt ohne `Tankradar.MAUI`-Referenz:** Verschlüsselte Testdateien können im E2E-Testcode nicht über Produktivcode erzeugt werden; die App erzeugt sie selbst über die UI (Passwortfelder + Teilen in `test-shared`).
- **`IosProjectFilesTests.Info_plist_DeclaresExemptEncryptionAndArm64`** (`src/Tankradar.Tests.Unit/Unit/IosProjectFilesTests.cs:77–84`): Wächtertest, der `ITSAppUsesNonExemptEncryption = false` behauptet — bleibt durch die getroffene Entscheidung grün und wird nicht angepasst; bei einer späteren Änderung der Entscheidung mitbedenken.
- **`Info.plist` (MacCatalyst):** unverändert (`false`, Entscheidung vom 2026-11-17); der kommentierte Eintrag kann ergänzend erläutert werden.
- **`docs/help/Einstellungen/ablauf-technisch.md:16`:** „Es gibt keine zusätzliche Verschlüsselung" bezieht sich auf den Datenbank-Dateischutz (`IDatabaseFileProtector`), nicht auf Sicherungen — technisch weiterhin wahr, im Dokumentationsschritt aber so präzisieren, dass keine Verwechselung mit dem Sicherungsschutz entsteht.

## Umsetzungsreihenfolge

1. **ADR 0019 anlegen** (`docs/adr/0019-backup-encryption.md`)
   - Voraussetzungen: keine
   - Beschreibung: Containerformat (Header-Bytefolge, Chunk-Schema, AAD), Verfahren (PBKDF2/AES-256-GCM-Parameter), Versionsregeln, Dateinamensentscheidung, Fehlersemantik (`PasswordRequired`/`WrongPassword`), Klartext-Zwischendatei und Export-Compliance-Festlegung dokumentieren; Verweis in ADR 0011 (Folgeabschnitt „Sicherungen sind unverschlüsselt") ergänzen.

2. **Format-Konstanten und Ergebniswerte**
   - Voraussetzungen: keine
   - Beschreibung: `BackupFormat` um `EncryptedMagic`, `EncryptedContainerVersion`, `EncryptedChunkSize` und Krypto-Längen/`Pbkdf2Iterations` erweitern; `BackupLimits.MaxEncryptedHeaderBytes`; `BackupResult` um `PasswordRequired`, `WrongPassword`; `BackupFileInfo` um `IsEncrypted` (alle Konstruktionsstellen inkl. `FakeBackupService` nachziehen).

3. **`BackupEncryptionHeader` und `BackupCrypto` implementieren**
   - Voraussetzungen: Schritt 2 (Konstanten); BCL-Typen `Rfc2898DeriveBytes.Pbkdf2`, `AesGcm`, `RandomNumberGenerator` — vorhanden, kein NuGet nötig
   - Beschreibung: Header (de-)serialisieren mit Grenzen; `IsEncrypted`/`ReadHeader` für Erkennung; `DeriveKey`; `EncryptAsync`/`DecryptAsync` chunked mit AAD-Bindung; Schlüsselvernichtung.

4. **`BackupService`-Passwortpfade**
   - Voraussetzungen: Schritte 2–3
   - Beschreibung: `IBackupService`-Signaturen; `CreateAsync` (Verschlüsselungszweig), `ReadInfo` (Header-Erkennung), gemeinsamer Entschlüsselungs-Vorschritt für `InspectAsync`/`RestoreAsync`/`ImportAsync` inkl. `PasswordRequired`/`WrongPassword`/`TooNew`; `ImportAsync` Peek + zwei Temp-Dateien.

5. **UI-Texte**
   - Voraussetzungen: Schritt 2 (`BackupResult`-Werte)
   - Beschreibung: `BackupTexts` — `Hint` umformulieren, neue Konstanten, `GetMessage` erweitern, `FormatItemTitle` („verschlüsselt") und Passwort-Rückfragetext.

6. **ViewModels**
   - Voraussetzungen: Schritte 4–5
   - Beschreibung: `BackupItemViewModel` (Passwort-Stadium, `RestorePassword`, `UnlockRestoreCommand`), `BackupViewModel` (Passwortfelder beim Erstellen inkl. Gleichheitsprüfung, zweistufige Wiederherstellung, `IsAwaitingImportPassword`-Ablauf mit `_pendingImport`).

7. **`SettingsPage.xaml`**
   - Voraussetzungen: Schritt 6 (gebundene Eigenschaften/Befehle)
   - Beschreibung: Passwort-`Entry`s auf der Karte, Passwort-Stadium in der Eintrags-Rückfrage, Import-Passwortbereich — jeweils mit AutomationIds und `SemanticProperties` nach dem bestehenden Kartenmuster.

8. **`FakeBackupService` und Unit-Test-Anpassungen**
   - Voraussetzungen: Schritte 2, 4, 6 (Signaturen und ViewModel-Verhalten)
   - Beschreibung: Fake um Passwortparameter/-aufzeichnung erweitern; betroffene Bestandstests anpassen (siehe Tests).

9. **Unit-/Integrationstests neu**
   - Voraussetzungen: Schritte 3–8; `BackupEncryptedTestArchive` (TestSupport-Datei, nur in Unit/Integration-csproj als `Compile Include` einbinden)
   - Beschreibung: `BackupCrypto`-Tests, Validator-/Format-Tests, `BackupServiceTests_Encryption` über `BackupTestEnvironment`, `BackupViewModelTests`-Erweiterungen, `BackupTextsTests`-Anpassungen.

10. **E2E-Tests**
    - Voraussetzungen: Schritt 7 (AutomationIds), Schritt 8–9 grüne Unit-/Integrationstests; bestehende E2E-Basis `BackupE2ETestBase` mit `test-shared`-Weitergabe und `RestartApplication(IReadOnlyDictionary<string,string>)` (`E2ETestBase.cs:332`)
    - Beschreibung: `BackupE2ETests_Encryption` — Erstellen mit Passwort über die UI (inkl. Fehlerfall ungleicher Passwortfelder), Listenkennzeichnung, Wiederherstellen mit richtigem/falschem Passwort, Abbruch im Passwort-Stadium, Einspielen einer verschlüsselten Datei samt Abbruch im Import-Passwortbereich. Mechanik des Einspielens: Die App erzeugt die verschlüsselte Datei selbst (Passwortfelder + „Teilen" → `test-shared`); der Test kopiert die geteilte Datei auf einen festen Pfad (`NewExternalPath(".zip")`) und startet die App mit `RestartApplication(ImportEnvironment(path))` neu — `TANKATLAS_TEST_BACKUP_IMPORT` wird beim App-Start einmal gelesen (Muster `ImportExternalFile_AddsToListAndRestores`).

11. **Dokumentation und iOS-Angaben**
    - Voraussetzungen: Schritt 1 (ADR); Export-Compliance-Entscheidung liegt vor (`false`, 2026-11-17)
    - Beschreibung: `README` („Bekannte Einschränkungen"), `docs/help/index.md`, `docs/help/Sicherung/*` (alle vier Dateien), `docs/help/Datenschutz/beschreibung.md` + `ablauf-technisch.md` (Schutzniveau, Export-Compliance-Begründung), `docs/help/Einstellungen/ablauf-technisch.md` (Satz „Es gibt keine zusätzliche Verschlüsselung" auf den Datenbank-Dateischutz präzisieren — keine Aussage über Sicherungen), `Info.plist` unverändert (`false`; `IosProjectFilesTests` bleibt grün), ADR-0011-Folgeverweis.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `EncryptDecrypt_Roundtrip_MatchesSource` | `BackupCryptoTests` (Unit) | Verschlüsseln → Entschlüsseln liefert das Ausgangs-ZIP bytegleich, über Chunk-Grenzen hinweg |
| `Decrypt_WrongPassword_Fails` / `Decrypt_TamperedCiphertextTagHeader_Fails` / `Decrypt_Truncated_Fails` | `BackupCryptoTests` | Authentifizierungsfehler bei falschem Passwort, Flip in Tag/Chunk/Header-AAD, abgeschnittener Strom |
| `ReadHeader_ValidAndBroken` / `IsEncrypted_DetectsMagic_NotZip` | `BackupCryptoTests` | Header-Parsing, Grenzen (`MaxEncryptedHeaderBytes`), Versionsregel, keine `PK`-Kollision |
| `CreateEncrypted` + Manipulationshelfer | `BackupEncryptedTestArchive` (TestSupport, Unit/Integration) | Erzeugt echte verschlüsselte Container via `BackupCrypto`; defekte Tags/Header/Chunks für Ablehnungstests |
| `Create_WithPassword_IsEncrypted_AndListMarksIt` | `BackupServiceTests_Encryption` (Integration, `BackupTestEnvironment`) | Datei trägt Magic, ist kein ZIP, `ListAsync` liefert `IsEncrypted` und „(verschlüsselt)"-fähige Info |
| `Restore_EncryptedFullFlow_RestoresAllData` | `BackupServiceTests_Encryption` | Mit korrektem Passwort kompletter Inhalt inkl. Fotos und Zählwerten wie unverschlüsselt |
| `Restore_WrongPassword_IsRejected_AndKeepsLiveData` / `Inspect_WrongPassword` | `BackupServiceTests_Encryption` | `WrongPassword`, Datenbestand unverändert, keine Reste |
| `Restore_NoPassword_RequiresPassword` / `Import_Encrypted_RequiresPassword_ThenAccepts` | `BackupServiceTests_Encryption` | `PasswordRequired`-Signal; Zwei-Schritt-Import (Peek ohne Kopie, danach volle Pipeline) |
| `Restore_TamperedEncrypted_IsRejected` / `TooNewContainer_IsTooNew` | `BackupServiceTests_Encryption` | Manipulierte Container und zu neue Containerversion sauber abgelehnt |
| `Create_WithPassword_PassesIt_AndClearsFields` / `Create_PasswordMismatch_ShowsMessage_WithoutService` | `BackupViewModelTests_Create` (Unit) | Parameterübergabe, Felder leeren, Gleichheitsprüfung |
| `Restore_Encrypted_ShowsPasswordStage_ThenCounts` / `Unlock_WrongPassword_ShowsMessage` / `Confirm_PassesPassword` | `BackupViewModelTests_Restore` | Zweistufige Rückfrage, Fehlermeldung, Passwort an `RestoreAsync` |
| `Import_Encrypted_AsksPassword_ThenImports` / `Cancel_DiscardsPickedFile` | `BackupViewModelTests_ShareImport` | `PasswordRequired` → Kartenbereich → erneuter `ImportAsync` mit Passwort |
| `FormatItemTitle_EncryptedSuffix` / `GetMessage_NewResults` / `Hint_NamesScopeAndEncryption` (angepasst) | `BackupTextsTests_*` | Kennzeichnung, Meldungstexte, neuer Hinweiswortlaut |
| `BackupFileInfo`/`FakeBackupService`-Konstruktionen | alle VM-Tests | `IsEncrypted`-Parameter in Testdaten |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `BackupTextsTests_Results.Hint_NamesScopeAndEncryption` | `Hint` behauptet derzeit pauschal „nicht verschlüsselt" — neuer Wortlaut für optionale Verschlüsselung |
| `FakeBackupService` (Hilfsklasse) | `IBackupService`-Signaturen mit Passwortparameter; Aufzeichnung der Passwörter für neue Prüfungen |
| `BackupViewModelTestBase` und alle `BackupViewModelTests_*` | Konstruktor-Delegate `unlockRestore` am `BackupItemViewModel`; neue Rückfrage-Stadien; betroffene Theorien um `PasswordRequired`/`WrongPassword` ergänzen |
| `BackupServiceTests_ListDeleteImport.List_OrdersNewestFirst_MarksUnreadable_…` | `BackupFileInfo`-Record mit `IsEncrypted`; verschlüsselte Datei mit defektem Header zählt als nicht lesbar |
| `BackupArchiveValidatorTests_*` | Erkennungs-Hilfen am Dateianfang (falls dort platziert, sonst `BackupCryptoTests`); `IsValidFileName`-Erwartungen bleiben |
| `BackupE2ETestBase` | Hilfsmethoden für Passworteingabe (`EnterPassword`, `WaitForPasswordStage`) und `test-shared`-Quelldatei |
| `IosProjectFilesTests.Info_plist_DeclaresExemptEncryptionAndArm64` | **Keine Anpassung** — Wächtertest auf `ITSAppUsesNonExemptEncryption = false`; bleibt durch die Entscheidung `false` (2026-11-17) grün; nur bei einer späteren Änderung der Entscheidung anzupassen |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Happy Path: Sicherung mit Passwort erstellen → Kennzeichnung in Liste → Wiederherstellen über Passwort-Rückfrage mit Zählwerten → Daten ersetzt | `BackupE2ETests_Encryption` | Verschlüsseltes Erstellen und Wiederherstellen funktionieren als kompletter Nutzerfluss | Gesamter UI-Fluss (zwei `Entry`-Felder, zweistufige Inline-Rückfrage, `Entry` mit `IsPassword`) nur über die Oberfläche erreichbar |
| Pflicht | Falsches Passwort: Meldung „falsch oder beschädigt", Daten unverändert, erneuter Versuch mit richtigem Passwort erfolgreich | `BackupE2ETests_Encryption` | Kontrollierte Ablehnung mit verständlicher Meldung | Fehlertext und Wiederholbarkeit über die UI sichtbar |
| Pflicht | Externe verschlüsselte Datei einspielen: Datei (vorher per „Teilen" in `test-shared` erzeugt, vom Test auf einen externen Pfad kopiert, App per `RestartApplication(ImportEnvironment(path))` neu gestartet) via `TANKATLAS_TEST_BACKUP_IMPORT` → Passwortbereich der Karte → Listeneintrag → Wiederherstellen | `BackupE2ETests_Encryption` | Einspielen erkennt Verschlüsselung automatisch und fordert das Passwort an | Zwei-Schritt-Fluss mit `PickedBackupFile` und Karten-Passwortbereich; `TANKATLAS_TEST_BACKUP_IMPORT` wird beim App-Start einmal gelesen — Mechanik wie `ImportExternalFile_AddsToListAndRestores` |
| Pflicht | Abbruch im Passwort-Stadium: verschlüsselte Sicherung → „Wiederherstellen" → Passwort-Stadium sichtbar (`ItemId(1, "RestorePassword")`) → „Abbrechen" (`ItemId(1, "RestoreCancel")`) → Rückfrage zu, Datenbestand unverändert, keine Änderung an Liste/Datei | `BackupE2ETests_Encryption` | Ohne Eingabe (Abbruch) wird die Wiederherstellung verworfen, der Datenbestand bleibt unverändert | Abbruchpfad ist über die UI erreichbar; nur Unit-Nachweis reicht nicht |
| Pflicht | Abbruch im Import-Passwortbereich: verschlüsselte externe Datei (Mechanik wie oben) → „Einspielen" → Passwortbereich sichtbar → „Abbrechen" (`Settings.Backup.ImportPasswordCancel`) → kein Listeneintrag, keine Datei in `backups/`, keine Fehlermeldung | `BackupE2ETests_Encryption` | Abbruch im Passwortbereich erzeugt keinen Eintrag und keine Datei | Projektpräzedenz `ImportWithoutFile_IsCancelledSilently`: Abbruchpfade werden E2E-abgesichert |
| Pflicht | Ungleiche Passwortfelder beim Erstellen: verschiedene Eingaben in `Settings.Backup.Password`/`.PasswordRepeat` → „Sicherung erstellen" → Status `PasswordsDoNotMatch`, keine Datei entstanden (kann als Vorschalt-Schritt im Happy-Path-Test laufen) | `BackupE2ETests_Encryption` | Verständliche Fehlermeldung bei ungleicher Eingabe; kein Dienstaufruf | Über die Karte auslösbarer sichtbarer Fehlerfall |
| Empfohlen | Unverschlüsselter Fluss bleibt unverändert (leere Passwortfelder → bisherige Erstellung und Rückfrage) | `BackupE2ETests_Encryption` oder Erweiterung `_Flow` | Standard bleibt unverschlüsselt | Regression des Standardpfads über die geänderte Karte |

Bestehende E2E-Tests anzupassen: `BackupE2ETests_Flow.CreateChangeRestoreDelete_FullFlow` und `_Import`-Tests laufen mit leeren Passwortfeldern unverändert weiter — nur prüfen, dass die neuen `Entry`-Elemente die Bedienung (Fokus/Tab) nicht stören; keine funktionale Anpassung erwartet. `BackupFiles()`/`WaitForBackupFileCount` zählen `*.zip` unverändert (Endung bleibt).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path verschlüsselt | Anwender tippt „Sicherung erstellen" mit ausgefüllten Passwortfeldern | Sicherungskarte sichtbar; `backups/` existiert ggf. noch nicht; Passwort nirgends gespeichert | Passwort + Wiederholung eingeben → Erstellen → Eintrag erscheint mit „(verschlüsselt)" → „Wiederherstellen" → Passwort-Stadium → „Prüfen" → Zählwert-Rückfrage → „Ja, wiederherstellen" | Neue `tankatlas-backup-*.zip` ohne `PK`-Anfang; nach Bestätigung `Restored`-Status, Daten auf Sicherungsstand, Benachrichtigung an geöffnete Seiten | `BackupE2ETests_Encryption` + `BackupServiceTests_Encryption` |
| 2 | Unverschlüsselt bleibt Standard | „Sicherung erstellen" mit leeren Passwortfeldern | wie #1 | Erstellen → Eintrag ohne Kennzeichnung → bisherige Rückfrage direkt mit Zählwerten | Verhalten wie vor dem Feature | bestehender `CreateChangeRestoreDelete_FullFlow` |
| 3 | Passwortfelder ungleich | „Sicherung erstellen" mit verschiedenen Eingaben | wie #1 | Erstellen | Status `PasswordsDoNotMatch`; keine Datei entstanden, kein Dienstaufruf | `BackupE2ETests_Encryption`, `BackupViewModelTests_Create` |
| 4 | Falsches Passwort | „Prüfen" im Passwort-Stadium einer verschlüsselten Sicherung | verschlüsselte Sicherung in der Liste | Passwort eingeben → „Prüfen" | Status „Passwort falsch oder beschädigt"; Rückfrage geschlossen; Datenbestand unverändert; Wiederholung mit richtigem Passwort möglich | Integration + E2E |
| 5 | Verschlüsselte Datei einspielen | „Sicherung aus Datei einspielen" mit verschlüsselter Datei | externe Datei; keine Listeneinträge nötig | Datei wählen → Karte zeigt Passwortbereich → Passwort + „Einspielen" → Eintrag „(verschlüsselt)" → Rückfrage wie #1 | Eintrag in Liste; Abbruch im Passwortbereich erzeugt keinen Eintrag und keine Datei | `BackupE2ETests_Encryption` (Einspielen **und** Abbruch im Passwortbereich), `BackupServiceTests_Encryption` (`Import_Encrypted_*`), `Cancel_DiscardsPickedFile` (Unit) |
| 6 | Beschädigte/manipulierte verschlüsselte Datei | Wiederherstellen/Einspielen einer Datei mit gekippten Chiffre- oder Header-Bytes | präparierte Datei | Passwort eingeben (falls Header lesbar) → Prüfung | Gemeinsame Meldung `WrongPassword` bzw. `InvalidFile` bei defektem Header; Datenbestand unverändert, keine temporären Reste | `BackupServiceTests_Encryption` |
| 7 | Zu neue Containerversion | Eintrag einer Sicherung mit höherer `EncryptedContainerVersion` | Datei aus neuerer App-Version | „Wiederherstellen" → Passwort-Stadium → „Prüfen" | `TooNew`-Meldung mit Update-Hinweis; keine Änderung | `BackupServiceTests_Encryption` |
| 8 | Abbruch im Passwort-Stadium | „Abbrechen" statt Passworteingabe | verschlüsselte Sicherung in der Liste | „Wiederherstellen" → „Abbrechen" | Rückfrage zu, nichts geändert, Passwortfeld geleert | `BackupE2ETests_Encryption`, `BackupViewModelTests_Restore` |
| 9 | Unterbrechung während Entschlüsselung/Staging | Absturz/Abbruch des Vorgangs | verschlüsselte Sicherung | Vorgang bricht vor dem Austausch ab | `.tmp`-/Arbeitsreste werden beim nächsten `ListAsync` aufgeräumt; Datenbestand unverändert (Atomarität aus ADR 0011 unberührt) | `Restore_CancelledByCaller_…`-Muster erweitert |

## Offene Punkte

Keine — der zuvor offene Punkt wurde am 2026-11-17 entschieden:

| # | Offener Punkt | Entscheidung |
|---|---------------|--------------|
| 1 | **`ITSAppUsesNonExemptEncryption` (iOS `Info.plist`):** Bisher `false` mit der Begründung „ausschließlich Standard-Verschlüsselung des Betriebssystems". Mit der optionalen Backup-Verschlüsselung nutzt die App erstmals eigene Dateiverschlüsselung — allerdings ausschließlich über betriebssystemimplementierte Standardverfahren (.NET `AesGcm`/`Pbkdf2` laufen auf iOS über Apple-Crypto). Ob die Erklärung `false` bleiben darf oder auf `true` umzustellen ist, ist eine rechtliche/Release-Entscheidung des App-Inhabers. | **Entschieden (2026-11-17, Empfehlung übernommen):** `false` beibehalten. Begründung: ausschließlich betriebssystemimplementierte Standardverfahren (PBKDF2, AES-256-GCM, keine proprietären Algorithmen); der Quelltext ist öffentlich publiziert (Public Repo, mögliche EAR-Freistellung für publizierten Source); `true` würde einen `ITSEncryptionExportComplianceCode` erfordern und kann Einreichungen bis zur Dokumentfreigabe blockieren. Der Export-Compliance-Abschnitt in `docs/help/Datenschutz/beschreibung.md` wird neu begründet; die endgültige Erklärung verifiziert der Inhaber bei der nächsten App-Store-Einreichung (steht bereits so in der Doku). |
