# ADR 0019: Optionale Passwort-Verschlüsselung lokaler Sicherungen

## Status

Angenommen (2026-11-17)

## Kontext

Die lokale Sicherung aus [ADR 0011](0011-local-backup-restore.md) erzeugt unverschlüsselte ZIP-Dateien; eine geteilte oder
gespeicherte Kopie kann jeder auslesen (dort Folgeabschnitt). Issue #67 verlangt einen optionalen Passwortschutz: Der Anwender
kann beim Erstellen ein Passwort vergeben; die Datei ist dann ohne das Passwort nicht lesbar. Die Wiederherstellung und das
Einspielen sollen verschlüsselte Dateien ohne Zutun des Anwenders erkennen und das Passwort anfordern. Unverschlüsseltes
Erstellen bleibt der Standard; bereits vorhandene Sicherungen werden nicht nachträglich verschlüsselt; es gibt keine
Passwortverwaltung und keinen Reset (Nichtziele des Issues).

Da die Verschlüsselung auch das Manifest umfasst, steht die bisherige Versionskennung (`schemaVersion` im Manifest) vor der
Entschlüsselung nicht zur Verfügung. Erkennung (verschlüsselt/unverschlüsselt) und Versionsregel müssen daher außerhalb des
verschlüsselten Nutzdatenbereichs liegen.

## Entscheidungen

### Containerformat

Eine verschlüsselte Sicherung ist ein Binärcontainer: ein unverschlüsselter Header, gefolgt vom bisherigen ZIP-Archiv
(ADR 0011, `schemaVersion` = 1 unverändert) als verschlüsselter Nutzdatenstrom. Nach der Entschlüsselung ist die Datei exakt
das bisherige Archiv; die gesamte Prüfpipeline (`BackupArchiveValidator`, `BackupDatabasePreparer`, `RestoreSwap`,
`RestoreRecovery`) greift unverändert.

Der Header ist ein Feld ohne Trennzeichen, alle Zahlen Little-Endian:

| Offset | Länge | Feld |
|--------|-------|------|
| 0 | 8 | Magic-Bytes `TKBENC01` (ASCII `54 4B 42 45 4E 43 30 31`) |
| 8 | 2 | Containerversion (UInt16), derzeit `1` |
| 10 | 2 | KDF-Kennung (UInt16): `1` = PBKDF2-HMAC-SHA256 |
| 12 | 4 | Iterationen (UInt32), Startwert `600000` |
| 16 | 1 | Saltlänge (UInt8), derzeit `16` |
| 17 | Saltlänge | Salt (zufällig je Datei) |
| — | 1 | Länge der Nonce-Basis (UInt8), derzeit `12` |
| — | Nonce-Länge | Nonce-Basis (zufällig je Datei) |
| — | 4 | Chunk-Größe in Bytes (UInt32), derzeit `1048576` (1 MiB) |

Version 1 belegt damit 50 Bytes. Die ersten zehn Bytes (Magic + Containerversion) sind über alle Containerversionen
invariant: Eine App liest die Version immer an dieser festen Position und kann so auch ein unbekanntes späteres Layout als
„zu neu" (`BackupResult.TooNew`) melden. Beginnt eine Datei nicht mit den Magic-Bytes, gilt sie als unverschlüsseltes ZIP
(die Magic beginnen nicht mit `PK`, es gibt keine Kollision mit einem Archiv). Beim Einlesen werden höchstens
`BackupLimits.MaxEncryptedHeaderBytes` (4 KiB) gelesen.

Feldgrenzen der Version 1 (Verletzung → `InvalidFile`): KDF-Kennung muss `1` sein, Iterationen 1 bis 10.000.000, Saltlänge 8
bis 64, Nonce-Länge genau 12 (AES-GCM), Chunk-Größe 1 KiB bis 64 MiB (die Entschlüsselung reserviert einen Puffer dieser
Größe, die Grenze schützt vor präparierten Werten). Die Containerversion muss kleiner oder gleich der der App sein; größer →
`TooNew`.

### Schlüsselableitung und Verfahren

- **KDF:** PBKDF2-HMAC-SHA256 über `Rfc2898DeriveBytes.Pbkdf2`, Salt 16 Byte (zufällig je Datei), Iterationen aus dem Header
  (Startwert 600.000, OWASP-Richtwert), 32-Byte-Schlüssel. Die Iterationszahl steht im Header und kann später erhöht werden,
  ohne das Format zu brechen. Argon2id wurde nicht gewählt, weil es eine Fremdbibliothek bräuchte; das Projekt hält bewusst
  nur MAUI/EF/SQLite-Pakete, PBKDF2 gehört zur BCL.
- **AEAD:** AES-256-GCM, gestückelt in Klartext-Chunks der Header-Größe (1 MiB). Die `AesGcm`-API ist eine Einmallösung;
  Sicherungen dürfen bis 2 GiB groß sein und werden als Strom verarbeitet (ADR 0011). Chunking mit Authentifikation je Chunk
  bindet Reihenfolge und Vollständigkeit.
- **Nonce je Chunk:** 12-Byte-Nonce = Nonce-Basis, deren Bytes 4–11 mit dem 8-Byte-Little-Endian-Wert des 0-basierten
  Chunkindex XOR-verknüpft werden.
- **AAD je Chunk:** die Header-Bytes ‖ Chunkindex (8 Byte, Little-Endian) ‖ Letztes-Chunk-Kennzeichen (1 Byte, `0x01` beim
  letzten Chunk). Damit ist der Chiffretext an den Header (Parameter, Salt, Nonce-Basis) und jeder Chunk an Position und
  Endmarkierung gebunden; Vertauschen, Wiederholen oder Kürzen scheitert an der Authentifikation.
- **Layout des Chiffretexts:** je Chunk `Chiffretext ‖ 16-Byte-Tag`. Nicht-letzte Chunks haben genau die Chunk-Größe; der
  letzte Chunk hat 0 bis Chunk-Größe Bytes und trägt das Letztes-Chunk-Kennzeichen in seiner AAD (es gibt immer mindestens
  einen Chunk, auch bei leerem Klartext).
- Das Passwort wird niemals gespeichert; der abgeleitete Schlüssel wird nach dem Vorgang mit
  `CryptographicOperations.ZeroMemory` verworfen.

### Dateiname, Erkennung und Ablauf

- Dateiname und Endung bleiben `tankatlas-backup-*.zip`: `IsValidFileName`, die Dateiauswahl (ZIP-Filter), „Teilen" und die
  Liste brauchen keine Anpassung. Die Erkennung erfolgt ausschließlich über die Magic-Bytes am Dateianfang.
- Das Passwort ist ein optionaler Parameter (`string? password = null`) von `CreateAsync`, `InspectAsync`, `RestoreAsync`
  und `ImportAsync` des `IBackupService`; es gibt keinen Abfragedienst — die Passwortabfrage ist reine Oberfläche
  (Inline-Eingabefelder auf der Karte „Sicherung", kein Systemdialog, Muster aus ADR 0011).
- `ListAsync` kennzeichnet verschlüsselte Dateien über den unverschlüsselten Header (`BackupFileInfo.IsEncrypted`);
  die Liste zeigt „(verschlüsselt)". Ein erkannter, aber defekter Header gilt als nicht lesbar.
- **Erstellen:** Zwei Passwortfelder (Eingabe + Wiederholung); bei Ungleichheit entsteht nichts
  (`PasswordsDoNotMatch`-Status), bei leeren Feldern die bisherige unverschlüsselte Datei. Mit Passwort wird das ZIP im
  Arbeitsverzeichnis geprüft (Selbstprüfung wie bisher) und danach als Container verschlüsselt.
- **Wiederherstellen eines verschlüsselten Eintrags:** zweistufige Inline-Rückfrage — erst Passwort mit „Prüfen" (der Dienst
  liest dabei die Datei vollständig ein, entschlüsselt und zählt wie bisher aus der geprüften Datenbank), dann die
  gewohnte Rückfrage mit Zählwerten und „Ja, wiederherstellen". Falsches oder fehlendes Passwort lässt die Abfrage offen;
  die Eingabe kann direkt wiederholt werden.
- **Einspielen:** Der Dienst liest zuerst nur den Dateianfang; bei erkannter Verschlüsselung ohne Passwort antwortet er mit
  `PasswordRequired`, ohne die Datei zu kopieren. Die Karte blendet dann einen Passwortbereich ein; „Prüfen" wiederholt
  den Vorgang mit dem Passwort (leeres Feld → erneute Aufforderung, die Abfrage bleibt offen). Das eingegebene Passwort
  gilt für die folgende Wiederherstellungs-Rückfrage mit — sie zeigt die Zählwerte ohne zweite Passworteingabe. Die
  verschlüsselte Originaldatei wandert unverändert in `backups/`; der Zeitstempel des Namens stammt aus dem Manifest des
  entschlüsselten ZIP (Parität zum unverschlüsselten Pfad).
- **Fehlersemantik:** `PasswordRequired` (verschlüsselt, kein Passwort übergeben) und `WrongPassword` (AEAD-Authentifikation
  oder Längenkonsistenz fehlgeschlagen — falsches Passwort **oder** beschädigte/manipulierte Datei; AEAD kann beides nicht
  unterscheiden, daher eine gemeinsame Meldung). Beide verändern den Datenbestand nicht.

### Klartext-Zwischendatei

Zum Prüfen und Wiederherstellen wird der Container je Vorgang in eine `.tmp`-Arbeitsdatei im Sicherungsverzeichnis
entschlüsselt und im `finally` verworfen (Muster der bisherigen `tempPath`-/`workDirectory`-Dateien); Reste räumt
`CleanUpLeftovers` beim nächsten `ListAsync` auf. Der Schutz entspricht dem Staging (`restore-staging`), in dem ohnehin die
Klartext-Datenbank liegt.

### Export-Compliance (iOS)

`ITSAppUsesNonExemptEncryption` bleibt `false` (Entscheidung vom 2026-11-17): Die App nutzt betriebssystemimplementierte
Kryptografie (`AesGcm`/`Rfc2898DeriveBytes` aus .NET) für die eigene Dateiverschlüsselung. Das fällt unter die
Freistellung für auf dem Gerät implementierte Standard-Verfahren der Export Administration Regulations; die Begründung
steht in der Datenschutz-Hilfe. Der Wächtertest `IosProjectFilesTests.Info_plist_DeclaresExemptEncryptionAndArm64`
bleibt unverändert und muss bei einer späteren Änderung dieser Entscheidung mitbedacht werden.

## Abweichungen vom Designentwurf

Keine (kein Designentwurf; die Oberfläche verwendet die Bausteine aus ADR 0011).

## Folgen

- **Der Hinweis der Karte und die Hilfe** werden angepasst: Sicherungen sind nur verschlüsselt, wenn beim Erstellen ein
  Passwort vergeben wurde; das Passwort wird nicht gespeichert und ist nicht wiederherstellbar.
- **Ältere App-Versionen** lesen verschlüsselte Dateien als `InvalidFile` (kein `PK`-Anfang) — erwartbares Verhalten; neue
  App-Versionen lesen unverändert ältere unverschlüsselte Dateien.
- **Leistung:** PBKDF2 mit ~600.000 Iterationen kostet je Vorgang grob einige hundert Millisekunden (mobil ggf. mehr) —
  einmalig pro Vorgang, akzeptabel. `InspectAsync` entschlüsselt bei verschlüsselten Dateien zusätzlich die komplette Datei
  vor dem Zählen.
- **Der Folgeabschnitt von ADR 0011** („Sicherungen sind unverschlüsselt") gilt für Dateien ohne Passwort und verweist
  auf diesen ADR.
