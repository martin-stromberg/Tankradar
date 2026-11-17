# Übersetzte Anforderung – Issue #67: Lokale Sicherungen optional verschlüsseln

## Fachliche Zusammenfassung

Die lokale Sicherung (ADR 0011, `BackupService`) wird um einen optionalen Passwortschutz erweitert: Beim Erstellen einer Sicherung kann der Anwender ein Passwort vergeben; die Datei wird dann verschlüsselt gespeichert und ist nur mit diesem Passwort wiederherstellbar. Die Wiederherstellung erkennt automatisch, ob eine Sicherung verschlüsselt ist, und fordert in diesem Fall die Passworteingabe an. Unverschlüsselte Sicherungen bleiben das Standardverhalten, und Sicherungen im bisherigen Format bleiben weiterhin importierbar.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine Sicherungsdatei (`tankatlas-backup-*.zip` im Verzeichnis `backups/` des Datenverzeichnisses bzw. eine über `ImportAsync` eingespielte externe Datei). Eine Sicherung umfasst Manifest, Datenbankinhalte und Belegfotos; die Verschlüsselung schützt diesen gesamten Inhalt.
- **Auslösende Ereignisse:**
  - Nutzeraktion „Sicherung erstellen" (`CreateCommand` in `BackupViewModel`) mit optionaler Passworteingabe im Backup-Dialog bzw. auf der Sicherungskarte.
  - Nutzeraktion „Wiederherstellen" an einem Listeneintrag sowie „Sicherung aus Datei einspielen" — das System erkennt ohne Zutun des Anwenders, ob die Datei verschlüsselt ist, und fordert bei verschlüsselten Sicherungen das Passwort an.
- **Erwartete Wirkung:**
  - Eine mit Passwort erstellte Sicherung liegt verschlüsselt vor; ihr Inhalt (einschließlich Manifest) ist ohne das Passwort nicht lesbar.
  - Eine verschlüsselte Sicherung kann mit dem korrekten Passwort vollständig wiederhergestellt werden; mit falschem Passwort, beschädigt oder manipuliert wird sie kontrolliert abgelehnt, ohne dass teilweise importierte Daten zurückbleiben (bestehende Atomarität über Staging, `RestoreSwap` und `RestoreRecovery` bleibt gewahrt).
  - Die UI zeigt bei verschlüsselten Dateien eine Passworteingabe und bei falscher Eingabe eine verständliche Fehlermeldung; für unverschlüsselte Sicherungen ändert sich am Ablauf nichts.
- **Umgang mit fehlenden Objekten:**
  - **Passwort beim Erstellen:** optional; wird keins angegeben, wird die Sicherung wie bisher unverschlüsselt erzeugt. Das Passwort wird nicht gespeichert und nicht verwaltet — es existiert nur für diesen einen Vorgang.
  - **Passwort bei der Wiederherstellung:** bei verschlüsselten Sicherungen ist es zwingend anzufordern; ohne Eingabe (Abbruch) wird die Wiederherstellung verworfen, der Datenbestand bleibt unverändert.
  - **Formatversionskennung:** Das bisherige `BackupManifest.SchemaVersion` liegt im verschlüsselten Teil und ist daher vor der Entschlüsselung nicht lesbar. Für die automatische Erkennung (verschlüsselt/unverschlüsselt, zu neu/lesbar) ist eine eindeutige Versionskennung außerhalb des verschlüsselten Nutzdatenbereichs erforderlich — Umsetzung im ADR festzulegen.
- **Scope-Ausschlüsse:**
  - Keine nachträgliche Verschlüsselung bereits vorhandener Sicherungsdateien.
  - Keine Verwaltung oder Wiederherstellung vergessener Passwörter (kein Passwort-Reset, kein Schlüsselbund).
  - Keine automatische Cloud-Synchronisierung von Sicherungen.
  - Keine Verschlüsselungspflicht: unverschlüsseltes Erstellen bleibt Standardverhalten.
  - Kein Schutz zusätzlicher Datenquellen — der Umfang der Sicherung (was gesichert wird und was nicht) bleibt wie in ADR 0011 festgelegt.

## Betroffene Klassen und Komponenten

- **Datenmodell (`src/Tankradar.MAUI/Models/Backup/`):**
  - `BackupFormat` — Erweiterung um Kennungen des verschlüsselten Formats (z. B. Magic-Bytes/Container-Header mit Versionsfeld); `SchemaVersion` ggf. erhöhen.
  - `BackupManifest`/`BackupManifestFile` — unverändert im Nutzdatenbereich, aber bei verschlüsselten Sicherungen selbst Bestandteil der Verschlüsselung; ggf. neue Metadatenklasse für KDF-Parameter (Salt, Iterationen/Parameter) im unverschlüsselten Header (Benennung im ADR).
  - `BackupResult` — voraussichtlich neue Werte (z. B. „Passwort erforderlich", „falsches Passwort"; Benennung im ADR).
  - `BackupOutcome`, `BackupFileInfo` — ggf. Kennzeichnung „verschlüsselt" für die Listenanzeige.
- **Logik/Services (`src/Tankradar.MAUI/Services/Backup/`):**
  - `BackupService` — `CreateAsync` um optionalen Passwortparameter; `InspectAsync`, `RestoreAsync`, `ImportAsync` um Passwortannahme/-anforderung.
  - `BackupArchiveWriter` — Schreibpfad für verschlüsselte Archive (Verschlüsselung sämtlicher Nutzdaten).
  - `BackupArchiveValidator` — Erkennung des Formattyps vor der Strukturprüfung; Entschlüsselung als vorgelagerter Schritt der bestehenden Whitelist-/Grenzen-/Prüfsummen-Prüfung.
  - `BackupLimits` — ggf. Grenzen für den verschlüsselten Container (Header-Größe, Gesamtgröße).
  - Neu: Kryptografische Komponente für Schlüsselableitung und Ver-/Entschlüsselung (Klasse/Interface im ADR zu benennen; Issue schlägt Argon2id oder PBKDF2 und AES-256-GCM vor).
- **Interfaces:**
  - `IBackupService` — Signaturerweiterungen (Passwortparameter, Erkennungsergebnis).
  - Voraussichtlich neu: Abstraktion der Passwortabfrage für UI-Entkopplung und Testmodus (analog `IBackupFilePicker`/`IBackupSharer` mit Test-Implementierungen), Benennung im ADR.
- **UI:**
  - `BackupViewModel` — Passworteingabe beim Erstellen; Passwortanforderung im Wiederherstellungs-/Importablauf; neue Statusmeldungen.
  - `BackupItemViewModel` — ggf. Anzeige „verschlüsselt" je Eintrag und Passwortfeld in der Rückfrage.
  - `SettingsPage.xaml` — Eingabefeld(er) für das Passwort auf der Sicherungskarte.
  - `BackupTexts` — neue Texte (Feldbeschriftungen, Hinweise, Fehlermeldung falsches Passwort) und Anpassung des bestehenden Hinweises `Hint` („…ist die Datei nicht verschlüsselt"), der dann nur noch für unverschlüsselte Sicherungen gilt.
- **Tests:**
  - Unit: neue/angepasste `BackupArchiveValidatorTests_*`, `BackupViewModelTests_*`, `BackupTextsTests_*`, `FakeBackupService`/`FakeBackupPlatform`.
  - Integration: neue `BackupServiceTests_*` (Erstellen/Wiederherstellen mit Passwort, falsches Passwort, beschädigte/manipulierte verschlüsselte Dateien, Abwärtskompatibilität) über `BackupTestEnvironment`.
  - E2E: neue `BackupE2ETests_*` (Passwort-Flüsse) sowie Anpassung des Testmodus für die Passwortübergabe.
- **Dokumentation:** `README`, `docs/help/Sicherung/*`, `docs/help/Datenschutz/*` (Schutzniveau korrekt beschreiben: unverschlüsselte Sicherungen bieten keinen Schutz vor dem Auslesen; verschlüsselte nur mit dem Passwort wiederherstellbar). Neuer ADR für das Format und das Verfahren.

## Implementierungsansatz

- Die konkrete Format- und Verfahrensentscheidung wird laut Issue in einer separaten ADR festgelegt; genannt sind Schlüsselableitung per Argon2id oder PBKDF2, Verschlüsselung per AES-256-GCM und Formatversionierung für zukünftige Erweiterungen.
- Da die Verschlüsselung auch das Manifest umfasst, ist ein unverschlüsselter Container-Header nötig, der mindestens die Erkennung (verschlüsselt ja/nein) und eine Versionskennung trägt. Annahme: Das bestehende ZIP-Format bleibt als innerer Nutzdatencontainer erhalten und wird als Ganzes verschlüsselt; das verschlüsselte Format ist damit kein direkt lesbares ZIP mehr — zu entscheiden im ADR.
- Erweiterungspunkte: `CreateAsync` verzweigt nach Passwortvorgabe in den verschlüsselnden Schreibpfad; `InspectAsync`/`RestoreAsync`/`ImportAsync` führen vor der bestehenden `BackupArchiveValidator`-Pipeline eine Formaterkennung und ggf. Entschlüsselung in das bestehende Staging durch. Die gesamte nachgelagerte Absicherung (Whitelist, `BackupLimits`, SHA-256, `BackupDatabasePreparer`, `RestoreSwap`, `RestoreRecovery`, `IRestoreNotifier`) bleibt unverändert wirksam; bei AES-256-GCM wird die Integritätsprüfung teilweise durch die Authentifizierung des Verfahrens ersetzt (laut Issue zulässig).
- Fehlerbehandlung: Authentifizierungs-/Entschlüsselungsfehler werden auf kontrollierte `BackupResult`-Werte abgebildet; das ViewModel zeigt verständliche Meldungen und lässt den Bestand unverändert (`EndOperation`/`finally`-Freigabe der Schaltflächen wie gehabt). Ob „falsches Passwort" und „manipuliert/beschädigt" unterscheidbar gemeldet werden, ist eine offene Frage (bei AEAD technisch nicht unterscheidbar).
- Passwörter werden nirgends persistiert; sie werden ausschließlich pro Vorgang übergeben und nach der Schlüsselableitung verworfen.

## Konfiguration

- Keine Anwendungseinstellung und kein persistenter Zustand: Das Passwort wird pro Sicherung beim Erstellen angegeben (Ebene „pro Vorgang/pro Sicherungsdatei").
- KDF-Parameter (z. B. Iterationen, Speicher) und Verfahrensversion werden nicht nutzerkonfigurierbar, sondern in der Formatkennung der Datei bzw. ihrem Header festgehalten.

## Offene Fragen

1. **Zeitpunkt der Passwortabfrage bei verschlüsselten Sicherungen:** Die Zählwerte der Wiederherstellungs-Rückfrage stammen aus dem Manifest — das bei verschlüsselten Dateien erst nach Eingabe des Passworts lesbar ist. Wird das Passwort vor der Inhaltsrückfrage angefordert (voller Ablauf wie bisher) oder zeigt die Rückfrage bei verschlüsselten Sicherungen keine Zählwerte? Gilt dasselbe für `ImportAsync` und `InspectAsync`?
2. **Passworteingabe beim Erstellen:** Einmalige Eingabe oder Bestätigungsfeld (zweimalige Eingabe gegen Tippfehler)? Mindestanforderungen an Länge/Zusammensetzung — oder bewusst keine (Verantwortung beim Anwender)?
3. **Fehlermeldung:** Soll die UI „falsches Passwort" von „beschädigt/manipuliert" unterscheiden, obwohl AEAD-Authentifizierungsfehler beides nicht auseinanderhalten können (z. B. gemeinsame Meldung „Passwort falsch oder Datei beschädigt")?
4. **Listenanzeige:** Sollen verschlüsselte Sicherungen in der Liste (`BackupFileInfo`, `BackupItemViewModel`) als solche gekennzeichnet werden? `ListAsync` kann das nur ohne Entschlüsselung, wenn die Kennzeichnung im Dateinamen oder im unverschlüsselten Header steht.
5. **Dateiname und Endung:** Behalten verschlüsselte Sicherungen das Muster `tankatlas-backup-*.zip` und die Endung `.zip` (Relevanz für `IsValidFileName`, `IBackupFilePicker`-Filter und „Teilen"), oder bekommen sie eine eigene Endung/ein eigenes Namensschema?
6. **Testmodus:** Wie wird das Passwort in E2E-/Integrationstests übergeben (z. B. Umgebungsvariable analog `TANKATLAS_TEST_BACKUP_IMPORT`, Erweiterung von `TestBackupFilePicker`/`FakeBackupPlatform`)?
7. **Teilen:** Unveränderte Weitergabe der Datei per `IBackupSharer` auch für verschlüsselte Sicherungen — ja/nein; soll die UI beim Teilen einer verschlüsselten Datei einen Hinweis ergänzen?
