# Tasks: Lokale Sicherungen optional verschlüsseln (Issue #67)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Dokumentation | ADR `docs/adr/0019-backup-encryption.md` anlegen (Containerformat, KDF-/AEAD-Parameter, Versionsregeln, Dateinamen, Fehlersemantik, Export-Compliance) und in ADR 0011 verweisen | Offen | — |
| 2 | Datenmodell | `BackupFormat` um `EncryptedMagic`, `EncryptedContainerVersion`, `EncryptedChunkSize` und Krypto-Längen/`Pbkdf2Iterations` erweitern | Offen | — |
| 3 | Datenmodell | `BackupLimits.MaxEncryptedHeaderBytes` ergänzen | Offen | — |
| 4 | Datenmodell | `BackupResult` um `PasswordRequired` und `WrongPassword` erweitern | Offen | — |
| 5 | Datenmodell | `BackupFileInfo` um `IsEncrypted` erweitern (alle Konstruktionsstellen anpassen) | Offen | — |
| 6 | Datenmodell | `BackupEncryptionHeader` (Datenmodellklasse für den unverschlüsselten Container-Header) anlegen | Offen | — |
| 7 | Logik | `BackupCrypto` anlegen: PBKDF2-Ableitung, chunked AES-256-GCM `EncryptAsync`/`DecryptAsync`, `ReadHeader`/`IsEncrypted`, Header-Schreiben/-Prüfung, Schlüsselvernichtung | Offen | — |
| 8 | Logik | `IBackupService`-Signaturen um optionalen `password`-Parameter erweitern (`CreateAsync`, `InspectAsync`, `RestoreAsync`, `ImportAsync`) | Offen | — |
| 9 | Logik | `BackupService.CreateAsync`: Verschlüsselungszweig (ZIP im Arbeitsverzeichnis → Selbstprüfung → `EncryptAsync` → `.tmp` → verschieben) | Offen | — |
| 10 | Logik | `BackupService`: gemeinsamer Entschlüsselungs-Vorschritt für `InspectAsync`/`RestoreAsync`/`ImportAsync` (Erkennung, `TooNew`, `PasswordRequired`, `WrongPassword`, `.tmp`-Aufräumen) | Offen | — |
| 11 | Logik | `BackupService.ReadInfo`: Header-Erkennung (`IsEncrypted`, `IsReadable` bei gültigem/defektem Header) | Offen | — |
| 12 | Logik | `BackupService.ImportAsync`: Header-Peek vor `CopyLimitedAsync` und Zwei-Temp-Datei-Ablauf bei verschlüsselten Dateien; `GetUniquePath` aus `validation.Manifest.CreatedUtc` des entschlüsselten ZIP; doppeltes `OpenReadAsync` (Peek + Kopie) beachten | Offen | — |
| 13 | UI-Texte | `BackupTexts.Hint` für optionale Verschlüsselung umformulieren | Offen | — |
| 14 | UI-Texte | `BackupTexts`: neue Konstanten (`Encrypted`, `PasswordPlaceholder`, `PasswordRepeatPlaceholder`, `PasswordsDoNotMatch`, `PasswordQuestion`, `ImportPasswordQuestion`, `WrongPassword`, `PasswordRequired`, `UnlockButton`, `PasswordHint`) | Offen | — |
| 15 | UI-Texte | `BackupTexts.GetMessage` um `PasswordRequired`/`WrongPassword` und `FormatItemTitle` um „(verschlüsselt)" erweitern; Passwort-Rückfragetext | Offen | — |
| 16 | UI | `BackupItemViewModel`: `RestorePassword`, `IsPasswordStage`, `ShowPasswordQuestion`, `UnlockRestoreCommand`, Passwort-Leerung beim Ausblenden | Offen | — |
| 17 | UI | `BackupViewModel`: `CreatePassword`/`CreatePasswordRepeat` mit Gleichheitsprüfung und Leerung nach dem Vorgang | Offen | — |
| 18 | UI | `BackupViewModel`: zweistufige Wiederherstellung (`IsEncrypted`-Zweig, `UnlockRestoreAsync`, `ConfirmRestoreAsync` mit Passwort) — Passwort zwingend **vor** `TryBeginOperation()`/`CloseQuestions` in lokale Variable lesen, bei Stufe 2 zurück in `item.RestorePassword` schreiben; `_announcer.Announce` in beiden Rückfrage-Stadien | Offen | — |
| 19 | UI | `BackupViewModel`: `IsAwaitingImportPassword`-Ablauf mit `_pendingImport`, `ConfirmImportPasswordCommand`, `CancelImportPasswordCommand` | Offen | — |
| 20 | UI | `SettingsPage.xaml`: Passwort-`Entry`s auf der Sicherungskarte (`Settings.Backup.Password`/`.PasswordRepeat`) inkl. Hinweis | Offen | — |
| 21 | UI | `SettingsPage.xaml`: Passwort-Stadium in der Eintrags-Rückfrage (`RestorePassword`-`Entry`, „Prüfen"-Schaltfläche) | Offen | — |
| 22 | UI | `SettingsPage.xaml`: Import-Passwortbereich (`Settings.Backup.ImportPassword`/`.ImportPasswordConfirm`/`.ImportPasswordCancel`) | Offen | — |
| 23 | Validierung | Passwort-Gleichheitsprüfung und Header-/Versionsvalidierungsregeln umsetzen (in Schritten 17/7 enthalten, hier als Prüfaufgabe) | Offen | — |
| 24 | Tests | `BackupEncryptedTestArchive` (TestSupport) anlegen und nur in Unit-/Integration-csproj einbinden | Offen | — |
| 25 | Tests | `FakeBackupService` um Passwortparameter/-aufzeichnung erweitern | Offen | — |
| 26 | Tests | `BackupCryptoTests` (Roundtrip, falsches Passwort, manipulierter/verkürzter Strom, Header-Parsing, Magic-Erkennung) | Offen | — |
| 27 | Tests | `BackupServiceTests_Encryption` (Integration): Erstellen/Kennzeichnen, voller Restore, `PasswordRequired`, `WrongPassword`, manipulierte Container, `TooNew`, Zwei-Schritt-Import | Offen | — |
| 28 | Tests | `BackupViewModelTests_*`: Erstellen mit Passwort, Passwort-Stadium, `Unlock`-Fluss, Import-Passwortbereich | Offen | — |
| 29 | Tests | `BackupTextsTests_*` und `BackupViewModelTestBase` an geänderte Texte/Signaturen anpassen (`Hint_NamesScopeAndEncryption` u. a.) | Offen | — |
| 30 | E2E-Tests | `BackupE2ETestBase` um Passwort-Hilfsmethoden (`EnterPassword` o. ä.) erweitern | Offen | — |
| 31 | E2E-Tests | `BackupE2ETests_Encryption`: Happy Path (erstellen mit Passwort → Liste → wiederherstellen), ungleiche Passwortfelder (`PasswordsDoNotMatch`), falsches Passwort, Abbruch im Passwort-Stadium | Offen | — |
| 32 | E2E-Tests | `BackupE2ETests_Encryption`: verschlüsseltes Einspielen und Abbruch im Import-Passwortbereich — Datei über „Teilen" in `test-shared` erzeugen, auf externen Pfad kopieren, App mit `RestartApplication(ImportEnvironment(path))` neu starten (`TANKATLAS_TEST_BACKUP_IMPORT` wird beim App-Start gelesen) | Offen | — |
| 33 | Dokumentation | `README` („Bekannte Einschränkungen") und `docs/help/index.md` für optionale Verschlüsselung anpassen | Offen | — |
| 34 | Dokumentation | `docs/help/Sicherung/` (index, beschreibung, ablauf-anwender, ablauf-technisch) um Passwortflüsse und Schutzniveau ergänzen | Offen | — |
| 35 | Dokumentation | `docs/help/Datenschutz/beschreibung.md` + `ablauf-technisch.md`: Schutzniveau und Export-Compliance-Begründung aktualisieren; `docs/help/Einstellungen/ablauf-technisch.md`: Satz „Es gibt keine zusätzliche Verschlüsselung" auf den Datenbank-Dateischutz präzisieren (keine Aussage über Sicherungen) | Offen | — |
| 36 | Konfiguration | `Info.plist` (iOS, ggf. MacCatalyst-Kommentar): `ITSAppUsesNonExemptEncryption` unverändert `false` (Entscheidung 2026-11-17), kommentierten Eintrag ggf. erläutern; Wächtertest `IosProjectFilesTests` bleibt grün | Offen | — |
