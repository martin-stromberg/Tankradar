# Plan-Gegenprüfung

Geprüft: `plan.md` gegen `requirement.md`, `inventory.md` und alle Detaildokumente unter `inventory/` (`models.md`, `logic.md`, `enums.md`, `interfaces.md`, `tests.md`). Zentrale Planbehauptungen wurden gegen den tatsächlichen Code verifiziert (`IBackupService`, `BackupService`, `BackupViewModel`, `BackupItemViewModel`, `BackupTexts`, `BackupFormat`, `SettingsPage.xaml`, Test-csproj-Dateien, `Info.plist`, `FakeBackupService`, `BackupE2ETestBase`).

## Ergebnis

**Status:** Plan lückenhaft

Die fachliche Abdeckung ist sehr hoch: alle sieben offenen Fragen der Anforderung sind entschieden und begründet, alle fünf Scope-Ausschlüsse tragen Begründung und Status `bestätigt`, die Scope-Klärung ist vollständig, die Akzeptanzszenarien enthalten einen vollständigen Happy Path vom Nutzer-Auslöser bis zur sichtbaren Wirkung samt benannten Ausgangszuständen, und die Kern-Nutzerflüsse (verschlüsselt erstellen, kennzeichnen, wiederherstellen, falsches Passwort, verschlüsselte Datei einspielen) sind E2E-verankert. Drei über die UI erreichbare Fälle haben jedoch nur Unit-Nachweis, und eine Reihenfolge-Inkonsistenz im geplanten Passwort-Lebenszyklus ist unbenannt.

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Mit Passwort erstellte Sicherung liegt verschlüsselt vor; Inhalt inkl. Manifest ohne Passwort nicht lesbar | Binärcontainer (Header + AES-256-GCM-Chiffretext des gesamten ZIP), `CreateAsync`-Verschlüsselungszweig, `BackupCrypto`/`BackupEncryptionHeader` | `EncryptDecrypt_Roundtrip_MatchesSource`, `Create_WithPassword_IsEncrypted_AndListMarksIt`, E2E-Happy-Path | Abgedeckt |
| Wiederherstellung mit korrektem Passwort vollständig (Zählwert-Rückfrage und Austausch wie bisher) | Zweistufige Inline-Rückfrage (Passwort-Stadium → `InspectAsync` → Zählwert-Stadium → `RestoreAsync` mit Passwort); Entschlüsselung vor der unveränderten Pipeline | `Restore_EncryptedFullFlow_RestoresAllData`, E2E-Happy-Path | Abgedeckt |
| Falsches Passwort, Beschädigung, Manipulation → kontrollierte Ablehnung, keine Teilimporte | Gemeinsame `WrongPassword`-Meldung (AEAD), `.tmp` im `finally`, `CleanUpLeftovers`; Header defekt → `InvalidFile` | `Decrypt_*_Fails`-Tests, `Restore_WrongPassword_IsRejected_AndKeepsLiveData`, `Restore_TamperedEncrypted_IsRejected`, E2E „falsches Passwort" | Abgedeckt |
| Automatische Erkennung verschlüsselt/unverschlüsselt ohne Zutun des Anwenders | Magic-Bytes am Dateianfang; `ReadInfo` → `IsEncrypted`; Header-Peek in `ImportAsync` vor dem Kopieren | `IsEncrypted_DetectsMagic_NotZip`, `ReadHeader_ValidAndBroken`, `Import_Encrypted_RequiresPassword_ThenAccepts` | Abgedeckt |
| UI zeigt Passworteingabe und verständliche Fehlermeldung; unverschlüsselter Ablauf unverändert | Passwort-`Entry`s auf der Karte und im Rückfrage-Bereich, `IsAwaitingImportPassword`-Bereich, neue `BackupTexts`-Konstanten | `BackupViewModelTests_*`, `BackupTextsTests_*`, E2E-Pflichtszenarien | Abgedeckt |
| Passwort beim Erstellen optional; wird keins angegeben → unverschlüsselte Sicherung | Beide Felder leer → `password = null` → bisheriger Pfad; Hinweis-`Label` auf der Karte | `Create_PasswordMismatch_…`-Gegenprobe, bestehender `CreateChangeRestoreDelete_FullFlow`, empfohlener E2E | Abgedeckt |
| Passwort wird nicht gespeichert/verwaltet; existiert nur pro Vorgang | Felder im `finally` leeren, `RestorePassword`/`ImportPassword` bei Hide/Abbruch leeren, `ZeroMemory` des Schlüssels | `Create_WithPassword_PassesIt_AndClearsFields` | Abgedeckt |
| Passwort bei Wiederherstellung zwingend; ohne Eingabe (Abbruch) wird verworfen, Datenbestand unverändert | `PasswordRequired`-Signal, `CancelRestoreCommand`/`CancelImportPasswordCommand` verwerfen den Vorgang | Akzeptanzszenario 8 + `Cancel_DiscardsPickedFile` (jeweils nur Unit) | Lücke (E2E fehlt) |
| Eindeutige Formatversionskennung außerhalb des verschlüsselten Bereichs; zu neu → kontrolliert abgelehnt | `EncryptedContainerVersion` (16 Bit) im unverschlüsselten Header; höhere Version → `TooNew` | `TooNewContainer_IsTooNew` (Integration); `TooNew`-Anzeige identisch zum bestehenden, E2E-getesteten Pfad | Abgedeckt |
| Bestehende Atomarität (Staging, `RestoreSwap`, `RestoreRecovery`) bleibt gewahrt | Entschlüsselung als vorgelagerter Schritt; Pipeline unverändert; `.tmp`-Reste räumt `CleanUpLeftovers` | Akzeptanzszenario 9 über `Restore_CancelledByCaller_…`-Muster | Abgedeckt |
| Unverschlüsselte Sicherungen und Dateien im bisherigen Format bleiben importierbar/nutzbar | Kein Magic → bisheriger Pfad unverändert; `SchemaVersion` bleibt 1 | Szenario 2, bestehende Integrations-/E2E-Tests | Abgedeckt |
| Scope-Ausschlüsse (keine nachträgliche Verschlüsselung, kein Passwort-Reset, keine Cloud-Sync, keine Pflicht, kein erweiterter Schutzumfang) | Alle fünf als Tabelle mit Begründung und Status `bestätigt` dokumentiert | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] E2E-Test für den Abbruch im Passwort-Stadium der Wiederherstellung fehlt: Die Anforderung „ohne Eingabe (Abbruch) wird die Wiederherstellung verworfen, der Datenbestand bleibt unverändert" ist über die UI erreichbar (`Wiederherstellen` → `Abbrechen` im Passwort-Stadium). Akzeptanzszenario 8 nennt als Nachweis nur `BackupViewModelTests_Restore` (Unit). Unit-Tests dürfen fehlende E2E-Abdeckung für Benutzerflüsse nicht kompensieren.
- [ ] E2E-Test für den Abbruch im Import-Passwortbereich fehlt: Akzeptanzszenario 5 erwartet ausdrücklich „Abbruch im Passwortbereich erzeugt keinen Eintrag und keine Datei"; geplanter Nachweis ist nur `Cancel_DiscardsPickedFile` (Unit). Projektpräzedenz (`ImportWithoutFile_IsCancelledSilently` ist ein bestehender E2E-Test) zeigt, dass Abbruchpfade E2E-abgesichert werden.
- [ ] E2E-Test für den sichtbaren Fehlerfall ungleicher Passwortfelder fehlt: `PasswordsDoNotMatch` ist ein über die UI auslösbarer sichtbarer Fehlerfall (Status ohne Dienstaufruf, keine Datei entstanden); geplant ist nur `Create_PasswordMismatch_ShowsMessage_WithoutService` (Unit). Im selben E2E wie der Happy Path über die Karte mitprüfbar.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Verschlüsselt erstellen → „(verschlüsselt)"-Kennzeichnung → Passwort-Stadium → Zählwert-Rückfrage → wiederherstellen | `BackupE2ETests_Encryption` (Pflicht) | Abgedeckt |
| Falsches Passwort → gemeinsame Meldung, Daten unverändert, erneuter Versuch mit richtigem Passwort | `BackupE2ETests_Encryption` (Pflicht) | Abgedeckt |
| Externe verschlüsselte Datei einspielen (über `test-shared` + `TANKATLAS_TEST_BACKUP_IMPORT`) → Passwortbereich → Listeneintrag → Wiederherstellen | `BackupE2ETests_Encryption` (Pflicht) | Abgedeckt |
| Unverschlüsselter Standardfluss (leere Passwortfelder) unverändert | Bestehender `CreateChangeRestoreDelete_FullFlow` + „empfohlenes" E2E-Szenario | Abgedeckt |
| Abbruch im Passwort-Stadium → Rückfrage zu, Daten unverändert, Passwortfeld geleert | — (nur Unit geplant) | Lücke |
| Abbruch im Import-Passwortbereich → kein Eintrag, keine Datei | — (nur Unit geplant) | Lücke |
| Ungleiche Passwortfelder beim Erstellen → Status `PasswordsDoNotMatch`, keine Datei | — (nur Unit geplant) | Lücke |
| Manipulierte/beschädigte verschlüsselte Datei → gemeinsame `WrongPassword`-Meldung bzw. `InvalidFile` bei defektem Header | `BackupE2ETests_Encryption` „falsches Passwort" deckt denselben AEAD-Ablehnungspfad ab; defekter Header läuft über den bestehenden, E2E-getesteten `InvalidFile`-Weg | Nicht erforderlich mit Begründung (identische Fehlersemantik) |
| Zu neue Containerversion → `TooNew`-Meldung mit Update-Hinweis | Integrationstest `TooNewContainer_IsTooNew`; die `TooNew`-Anzeige ist bereits E2E-getestet (`TooNewBackup_IsRejected_WithUpdateHint`), nur der Auslöser ist neu | Abgedeckt |
| Unterbrechung während Entschlüsselung/Staging → Reste werden aufgeräumt, Daten unverändert | Integrationsmuster `Restore_CancelledByCaller_…` (Abbruch nicht über die UI auslösbar) | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

- [ ] Reihenfolge-Konflikt zwischen Passwort-Leerung und Passwort-Lesung nicht aufgelöst: Der Plan erweitert `HideRestoreQuestion` um das Leeren von `RestorePassword` und `IsPasswordStage`. `BackupViewModel.TryBeginOperation` ruft jedoch `CloseQuestions` → `item.HideRestoreQuestion()` auf, und sowohl `UnlockRestoreAsync` (neu, muss wie alle Vorgänge über `TryBeginOperation` sperren) als auch `ConfirmRestoreAsync` (bestehend, `src/Tankradar.MAUI/ViewModels/BackupViewModel.cs:242–249`) beginnen mit `TryBeginOperation`, **bevor** das Passwort gelesen wird. Wörtlich geplant läse `InspectAsync(fileName, password)`/`RestoreAsync(item.FileName, item.RestorePassword)` ein bereits geleertes Feld → `PasswordRequired`/`WrongPassword` trotz korrekter Eingabe. Der Plan muss festlegen, dass das Passwort vor `TryBeginOperation`/`CloseQuestions` in eine lokale Variable bzw. ein ViewModel-Feld übernommen wird (z. B. `_pendingRestorePassword` beim Übergang in Stufe 2).

## Hinweise

- `IosProjectFilesTests.Info_plist_DeclaresExemptEncryptionAndArm64` (`src/Tankradar.Tests.Unit/Unit/IosProjectFilesTests.cs:77–84`) ist ein Wächtertest, der `ITSAppUsesNonExemptEncryption = false` behauptet. Durch die getroffene Entscheidung (`false` beibehalten) bleibt er grün und muss nicht angepasst werden — er ist im Plan zu Recht nicht unter „betroffene Tests" gelistet, sollte aber bei einer späteren Änderung der Entscheidung mitbedacht werden.
- `docs/help/Einstellungen/ablauf-technisch.md:16` („Es gibt keine zusätzliche Verschlüsselung") bezieht sich auf den Datenbank-Dateischutz, nicht auf Sicherungen — technisch weiterhin wahr, im Dokumentationsschritt aber auf Verwechselbarkeit prüfen.
- Der Import-Peek ruft `PickedBackupFile.OpenReadAsync` zweimal auf (Peek + `CopyLimitedAsync`). Für `FakeBackupFilePicker`-Testdaten sicherstellen, dass der übergebene Delegate je Aufruf einen neuen/seekbaren Strom liefert (`TestBackupFilePicker` und `MauiBackupFilePicker` tun das bereits).
- Beim verschlüsselten Import ist `Manifest.CreatedUtc` erst nach der Entschlüsselung lesbar; die Planstelle `File.Move(temp1, finalPath)` sollte die Zeitstempelquelle für `GetUniquePath` benennen (naheliegend: `validation.Manifest.CreatedUtc` der entschlüsselten Datei, wie beim unverschlüsselten Pfad).
- E2E-Detail: `TANKATLAS_TEST_BACKUP_IMPORT` zeigt auf einen festen Pfad, während `TestBackupSharer` den zeitstempelbasierten Dateinamen in `test-shared` ablegt. Der E2E-Schritt sollte konkretisieren, ob der Test die geteilte Datei auf den Env-Pfad kopiert/umbenennt oder die App mit neuer Umgebung neu startet (`RestartApplication`).
- Für das Passwort-Stadium der Eintragsrückfrage sollte die Sprachausgabe analog zum bestehenden Muster (`_announcer.Announce(item.RestoreQuestion)` in `ShowRestoreQuestionAsync`, BackupViewModel.cs:437) explizit benannt werden.
