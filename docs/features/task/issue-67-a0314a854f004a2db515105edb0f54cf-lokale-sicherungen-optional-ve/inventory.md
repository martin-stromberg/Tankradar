# Bestandsaufnahme: Lokale Sicherungen optional verschlüsseln (Issue #67)

Analysiert wurde der bestehende Backup-/Restore-Bereich (`src/Tankradar.MAUI/Services/Backup/`, `src/Tankradar.MAUI/Models/Backup/`, ViewModels, `SettingsPage.xaml`, Testprojekte, ADR 0011) bezogen auf die Anforderung, lokale Sicherungen optional per Passwort zu verschlüsseln (`requirement.md`).

## Zusammenfassung

- **Vorhanden:** Ein vollständig implementierter unverschlüsselter Sicherungsfluss (ADR 0011): ZIP-Archiv `tankatlas-backup-*.zip` mit `manifest.json`, `database/tankatlas.db` und `receipts/*`; Erstellung per `VACUUM INTO`-Schnappschuss; zweistufige Prüfpipeline (`BackupArchiveValidator`: Whitelist/Grenzen/Manifest/Versionsregeln + SHA-256 und Fotosignatur beim Entpacken); Datenbank-Neuaufbau über `BackupDatabasePreparer`; atomarer Austausch per `RestoreSwap`/`RestoreJournal`/`RestoreRecovery`; UI über `BackupViewModel`/`BackupItemViewModel` auf der Karte „Sicherung" der `SettingsPage` mit Inline-Rückfragen.
- **Nicht vorhanden (komplette Lücke):**
  - Keinerlei Verschlüsselung: keine Schlüsselableitung (kein Argon2/PBKDF2), keine AEAD-Ver-/Entschlüsselung; `System.Security.Cryptography` wird nur für SHA-256-Prüfsummen genutzt. Kein NuGet-Paket für Kryptografie in `Tankradar.MAUI.csproj`.
  - Keine Format-/Versionskennung außerhalb des ZIP: `SchemaVersion` (aktuell `1`) steht ausschließlich im Manifest innerhalb des Archivs; es gibt keine Magic-Bytes und keinen unverschlüsselten Container-Header. Eine verschlüsselte Datei wäre heute weder als ZIP lesbar (`InvalidFile`) noch als verschlüsselt erkennbar.
  - Keine Passwortannahme in `IBackupService`/`BackupService` (`CreateAsync`, `InspectAsync`, `RestoreAsync`, `ImportAsync` haben keinen Passwortparameter) und kein „Passwort erforderlich"/„falsches Passwort" in `BackupResult` (nur `Ok`, `NotFound`, `InvalidFile`, `TooNew`, `Busy`, `Failed`).
  - Keine Abstraktion für die Passwortabfrage (kein Pendant zu `IBackupFilePicker`/`IBackupSharer` samt Test-/Fake-Implementierung).
  - Keine Passwort-UI: `SettingsPage.xaml` hat kein Passwort-`Entry`; `BackupItemViewModel` hat kein Passwortfeld in den Rückfragen; `BackupTexts` hat keine Passwort-Texte. `BackupTexts.Hint` sagt aktuell ausdrücklich „ist die Datei nicht verschlüsselt" (abgesichert durch `BackupTextsTests_Results.Hint_NamesScopeAndEncryption`).
  - Keine „verschlüsselt"-Kennzeichnung: `BackupFileInfo` kennt nur `IsReadable`; `ListAsync` liest das Manifest via ZIP — eine verschlüsselte Datei erschiene heute als „nicht lesbar", und `BackupViewModel.ShowRestoreQuestionAsync` bricht bei `IsReadable == false` sofort mit `InvalidFile` ab, bevor je eine Passwortabfrage möglich wäre.
  - `BackupFormat.IsValidFileName` akzeptiert nur `tankatlas-backup-*.zip` (max. 100 Zeichen, keine Pfadanteile) — ein eigenes Namensschema/eigene Endung verschlüsselter Dateien erforderte eine Anpassung (betrifft auch den ZIP-Dateitypfilter des `MauiBackupFilePicker` und `BackupFiles()` im E2E-Basistest, der `*.zip` zählt).
- **Zu beachtende Berührungspunkte:**
  - Die Passwortabfrage muss vor `InspectAsync` greifen können, denn die Zählwerte der Wiederherstellungs-Rückfrage kommen aus der geprüften Datenbank, die bei verschlüsselten Dateien erst nach Entschlüsselung entpackt werden kann.
  - `Info.plist` (iOS) deklariert `ITSAppUsesNonExemptEncryption = false`; die Datenschutz-Hilfe begründet das mit „ausschließlich Standard-Verschlüsselung des Betriebssystems". Eigene Verschlüsselung (AES-256-GCM/Argon2id) macht diese Angabe prüfungsbedürftig.
  - Der Testmodus für E2E-Tests kennt bisher nur `TANKATLAS_TEST_BACKUP_IMPORT` (Test-Dateiauswahl); für Passwortflüsse existiert kein Übergabeweg.
  - Die Dokumentation (`README` „Bekannte Einschränkungen", `docs/help/Sicherung/*`, `docs/help/Datenschutz/*`, ADR-0011-Folgeabschnitt) beschreibt Sicherungen durchgängig als unverschlüsselt.
- **Test-Ausgangszustand:** Alle drei Suiten sind auf dem dokumentierten Stand vollständig grün — Unit 2097/2097, Integration 298/298, E2E (FlaUI) 177/177 bestanden, keine Fehlschläge, keine übersprungenen Tests. Nachweis und Details in [inventory/tests.md](inventory/tests.md) samt TRX-Reports und Konsolenlogs unter `inventory/test-results/`.

## Details

- [Datenmodell](inventory/models.md) — `BackupFormat`, `BackupManifest`, `BackupManifestCounts`, `BackupManifestFile`, `BackupFileInfo`, `BackupOutcome`, `BackupLimits`, `BackupTextLimits`, `PickedBackupFile`, `BackupPreparation`, `RestoreJournal`
- [Logik](inventory/logic.md) — `BackupService`, `BackupArchiveValidator`, `BackupArchiveWriter`, `BackupDatabasePreparer`, `RestoreSwap`/`RestoreJournal`/`RestoreRecovery`, `RestoreNotifier`, Picker/Sharer-Selektoren und Test-/Maui-Implementierungen, `BackupViewModel`, `BackupItemViewModel`, `SettingsViewModel`, `SettingsPage.xaml`, `MauiProgram`, `DatabaseInitializer`, `BackupTexts`
- [Enums](inventory/enums.md) — `BackupResult`, `BackupTamper` (Testhilfe)
- [Interfaces](inventory/interfaces.md) — `IBackupService`, `IBackupFilePicker`, `IBackupSharer`, `IRestoreNotifier`, `IRestoreRecovery` und angrenzende Verträge
- [Tests](inventory/tests.md) — Ausgangszustand, Testklassen und Hilfsmethoden
