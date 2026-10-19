# Abnahmeprüfung – Entwicklungsschritt 15 (Lokale Sicherung und Wiederherstellung), erneute Prüfung nach Nachbesserungsrunde 1

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Die Nachbesserung wirkt gegen alle Fälle, die die erste Abnahme (`acceptance-schritt-15.1.md`) und die eigenen Tests nennen. Der
Trigger-Angriff und das fremde Schema aus 15.1 werden jetzt schon beim Einspielen abgelehnt. Dasselbe gilt für einen geänderten
Default, einen Ausdrucksindex, eine generierte Spalte, einen Teilindex und Vorgänge zu einem nicht vorhandenen Fahrzeug; Daten
und Liste bleiben dabei unverändert.

Ebenfalls am Zwischenstand belegt:

- Sichern und Wiederherstellen mit einer 0.1.18-Datenbank, Belegfoto bytegleich.
- `VACUUM INTO`: Gelöschte Inhalte sind nicht mehr in der Sicherung lesbar.
- Nach der Wiederherstellung läuft die Datenbank im WAL-Modus.

Die Prüfläufe sind grün: `local-ci` mit 109/109 E2E, Pre-Push, 3 × Backup-E2E-Klassen.

Die Vorgabe „Eine beschädigte oder ungültige Sicherungsdatei führt zu einer verständlichen Fehlermeldung, ohne die vorhandenen
Daten zu verändern“ ist für präparierte Dateien aber weiterhin nicht erfüllt:

- **Abweichung 1 – stiller Datenverlust weiterhin möglich:**
  - Der Schemavergleich blendet alle Objekte aus, deren Name mit dem für SQLite-interne Objekte reservierten Präfix beginnt.
  - Ein so benannter Trigger mit derselben Wirkung wie in 15.1 wurde als „gültige Sicherung“ angenommen und wiederhergestellt.
  - Das nächste in der App angelegte Fahrzeug löschte alle Tank- und Ladevorgänge.
  - Der Trigger gelangt außerdem in jede weitere eigene Sicherung.
- **Abweichung 2 – präparierte Inhalte werden angenommen:**
  - Eine zusätzliche `CHECK`-Einschränkung und ein manipulierter Autoinkrement-Zähler werden angenommen. Danach lässt sich kein
    Fahrzeug mehr anlegen.
  - Ein ungültiger Datumswert legt das Tankbuch lahm.
  - Eine Zeile in der Migrationssperrtabelle lässt das Einspielen endlos und ohne Meldung hängen.
  - Damit ist die Bewertung in ADR 0011 widerlegt, solche Lücken könnten „höchstens eine Wiederherstellung dauerhaft scheitern
    lassen, aber keine Daten löschen“.

**Selbstauskunft bestätigt:**

| Prüfung | Ergebnis |
|---|---|
| Unit | 1272/1272 |
| Integration | 171/171 |
| E2E | 109/109 |
| Zeilenabdeckung | 93,2 % |

## Abweichungen

- [ ] **1. Ein Trigger mit reserviertem Namenspräfix umgeht den Schemavergleich: stiller Datenverlust wie in 15.1.**
  - **Vorgabe:**
    - Projektplan Schritt 15: „Eine beschädigte oder ungültige Sicherungsdatei führt zu einer verständlichen Fehlermeldung,
      ohne die vorhandenen Daten zu verändern.“
    - Kern der Abweichung 1 aus 15.1 war der Trigger.
    - ADR 0011 sagt zu: „jede Abweichung (zusätzlicher Trigger …) führt zu `InvalidFile`“.
    - Die Hilfe (`docs/help/Sicherung/beschreibung.md`) sagt zu: „Dateien mit … zusätzlichen Triggern oder Ansichten werden
      abgelehnt“. Dasselbe steht im `changes.log` 0.1.19.
  - **Ist:** `BackupSchemaDescriber.DescribeAsync` liest `sqlite_master` mit dem Filter `name NOT LIKE 'sqlite\_%'`.
    - Der Filter soll nur die von SQLite selbst angelegten Tabellen und Autoindizes ausblenden. Er blendet aber auch Trigger und
      Views mit diesem Präfix aus.
    - Eine externe Sicherungsdatei kann ein solches Objekt enthalten. SQLite lädt und führt es beim Öffnen normal aus.
    - `integrity_check` und `foreign_key_check` melden nichts.
  - **Nachweis** am Zwischenstand 0.1.19 über „Sicherung aus Datei einspielen“ (Datei mit gültigem Manifest und gültigen SHA-256,
    Ausgangsbestand 2 Fahrzeuge, 3 Vorgänge, 1 Belegfoto):
    1. Meldung „Die Datei ist eine gültige Sicherung und wurde in die Liste übernommen.“, danach Rückfrage.
    2. Nach „Ja, wiederherstellen“ steht der Trigger in der Livedatenbank.
    3. Über die Oberfläche wurde das Fahrzeug „Neu Abnahme“ angelegt. Danach gab es `TankbookEntries` = 0: alle drei Vorgänge
       waren ohne Hinweis gelöscht.
    4. Eine anschließend in der App erstellte Sicherung enthielt den Trigger ebenfalls (`VACUUM INTO` übernimmt ihn), und ihr
       Manifest zählte 0 Vorgänge.
    5. Die Wiederherstellung der vorherigen, sauberen Sicherung stellte den Ausgangszustand wieder her.
  - **Gegenprobe:** Dieselbe Datei mit einem Trigger ohne Präfix wird korrekt abgelehnt.
  - **Umsetzbar:**
    - Trigger und Views ohne Namensfilter vergleichen, also jeden Trigger und jede View ablehnen, den oder die die Referenz
      nicht hat.
    - Bei Tabellen und Indizes nur die bekannten internen Namen ausnehmen: `sqlite_sequence`, `sqlite_stat*` und
      `sqlite_autoindex_*`, Letztere erfasst ohnehin `pragma_index_list`.
    - Einen Integrations- und einen E2E-Fall mit einem so benannten Trigger ergänzen (`BackupTamper` erweitern).
    - Robuster ist der Neuaufbau aus Abweichung 2: Er schließt diese Klasse grundsätzlich aus.

- [ ] **2. Präparierte Inhalte außerhalb des verglichenen Schemas werden angenommen und machen Funktionen unbrauchbar; eine
  Datei lässt das Einspielen ohne Meldung hängen.**
  - **Vorgabe:** wie Abweichung 1. In 15.1 war „Tankbuch meldet ‚Die Fahrzeuge konnten nicht geladen werden.‘“ Teil der
    Abweichung.
  - **Ist:** Nicht geprüft werden:
    - Einschränkungen, die nur im Tabellentext stehen (`CHECK`); das ADR nennt das bewusst,
    - der Inhalt der internen Zähltabelle `sqlite_sequence`,
    - Zeileninhalte,
    - Zeilen in `__EFMigrationsLock`.
  - **Nachweis** am Zwischenstand. Alle Dateien wurden über die Oberfläche eingespielt; jede wurde angenommen („gültige
    Sicherung“), der Bestand blieb bis zur Bestätigung unverändert.

    | Präparierte Datei | Folge nach der Wiederherstellung |
    |---|---|
    | Zusätzliche `CHECK`-Einschränkung an `Vehicles`, die vorhandene Zeilen erfüllen | Jedes neue Fahrzeug scheitert mit „Die Änderung konnte nicht gespeichert werden.“ |
    | Autoinkrement-Zähler von `Vehicles` auf den Höchstwert gesetzt | Jedes neue Fahrzeug scheitert mit „Die Änderung konnte nicht gespeichert werden.“ |
    | Ungültiger Datumswert in `Vehicles.CreatedUtc` | Das Tankbuch meldet dauerhaft „Die Fahrzeuge konnten nicht geladen werden.“ |
    | Zeile in `__EFMigrationsLock` | Schon das Einspielen kommt nie zu Ende, siehe unten. |

    - Die ersten drei Zustände bleiben, bis eine andere Sicherung wiederhergestellt wird. `CHECK` und Zählerstand gehen mit
      `VACUUM INTO` auch in neue eigene Sicherungen über.
    - Zur Zeile in `__EFMigrationsLock`: EF Core wartet in `MigrateAsync` der Staging-Datenbank ohne Zeitlimit auf die Sperre.
      - Es erscheint keine Meldung.
      - Alle Schaltflächen der Karte bleiben gesperrt.
      - Nachgewiesen zweimal: Am 12.10. lief die Instanz noch nach mehr als 9 h, `import-*.tmp` und Staging lagen unverändert
        vor; der `finally`-Block lief also nie. Am 18.10. war nach 3 min kein Ergebnis da.
      - Daten unverändert. Erst ein Neustart räumt Staging und `.tmp` auf.
  - **Bewertung:**
    - Wirkung wie beim fremden Schema in 15.1: Auslöser ist eine Datei von außen, Folge ist eine dauerhaft eingeschränkte App.
    - Die ADR-Aussage „Solche Abweichungen könnten höchstens eine Wiederherstellung dauerhaft scheitern lassen, aber keine Daten
      löschen, weil Trigger erfasst werden“ ist widerlegt: siehe `CHECK` hier und den Trigger in Abweichung 1.
  - **Umsetzbar:**
    - **Neuaufbau statt Vergleich:** Die ohnehin frisch migrierte Referenzdatenbank wird zum Ergebnis.
      1. Staging anhängen und je App-Tabelle `INSERT INTO main.T (Spalten der Referenz) SELECT … FROM staging.T` ausführen,
         ohne `__EFMigrationsLock`; `__EFMigrationsHistory` kommt aus der Referenz.
      2. `sqlite_sequence` aus `MAX(Id)` neu setzen.
      3. Danach `foreign_key_check`.

      So erreichen nur die Schemaobjekte der App die Livedaten: keine fremden Trigger, `CHECK`, `COLLATE` und Pragmawerte,
      auch keine mit reservierten Namen. Das Problem der unterschiedlichen Tabellentexte entfällt.
    - **Materialisierungsprüfung:** Jede Entität einmal über EF lesen (`AsNoTracking().ToListAsync()` je `DbSet`). Ausnahmen wie
      `FormatException` führen zu `InvalidFile`.
    - **Migrationssperre:** Vor `MigrateAsync` die Zeilen aus `__EFMigrationsLock` der privaten Staging-Kopie löschen oder die
      Migration mit Zeitlimit abbrechen; das Ergebnis ist `InvalidFile`.
    - **Tests:** Die Fälle `CHECK`, Zähler, ungültiges Datum und Sperrzeile in `BackupTamper` und
      `BackupServiceTests_Schema` aufnehmen.
    - **Dokumentation:** ADR 0011 (Grenzen des Vergleichs und Bewertung), Hilfe und `changes.log` an den dann zutreffenden Stand
      anpassen.

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-15-sicherung`, HEAD `f57dc45`.
- **Basis:** lokaler Projekt-Basisbranch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `42b4710`. Er ist zugleich
  Merge-Base und entspricht `origin`.
- **Diff gegen die Basis:** 107 Dateien, +8674/−13, 13 Commits.
- **Nachbesserung seit der ersten Abnahme (`e90dc68..f57dc45`):** 16 Dateien, +815/−85.
  - neu: `BackupDatabasePreparer`, `BackupSchemaDescriber`, `TestSupport/BackupTestTampering.cs`
  - Tests: `BackupServiceTests_Schema` (14), `BackupServiceTests_Storage` (3), `RestoreSwapUndoTests` (1),
    `BackupE2ETests_TamperedSchema` (1)
  - angepasst: `BackupService`, `RestoreSwap`, ADR 0011, Hilfe und `changes.log`
- **Zwischenstand:** `review-versions/0.1.19_2026-10-12/`, neu erzeugt am 12.10. um 21:05.
  - `FileVersion` `0.1.19.0`, `ProductVersion` `0.1.0+f57dc45…`; der Code entspricht HEAD.
  - `CHANGELOG.md` enthält den `changes.log`-Eintrag 0.1.19 wortgleich, diesmal ohne BOM; das ist unerheblich.
- **Grundlagen:**
  - `project-plan.md`, Schritt 15 samt Zusatz aus Schritt 14
  - `requirement.md`
  - `acceptance-schritt-15.1.md`
  - ADR 0011
  - Diff gegen die Basis
- **Re-Review:** Die Selbstauskunft „Re-Review ohne Befunde“ ließ sich nicht an Dokumenten prüfen. Im unversionierten
  Feature-Verzeichnis liegen nur Reviews vom 12.10. bis 16:55, also aus der Zeit vor der Nachbesserung.

### Abweichung und Hinweise aus 15.1: Stand

| Punkt aus 15.1 | Stand | Nachweis |
|---|---|---|
| Abweichung 1: fremdes Schema und Trigger | **teilweise** – fremdes Schema, sichtbare Trigger, Views, Tabellen, Spalten und Indizes werden abgelehnt; Trigger mit reserviertem Präfix, `CHECK`, Zählerstand, Zeileninhalte und Sperrzeile nicht | Abweichungen 1 und 2; Tabelle „Präparierte Dateien“ |
| Prüfung schon beim Import | erledigt; abgelehnte Dateien kommen nicht in die Liste, kein Staging-Rest | Integration `Import_TamperedSchema_*`; praktisch 7 Dateien abgelehnt, Liste unverändert |
| WAL nach Wiederherstellung | erledigt | Kopfbytes 18/19 = 2/2 nach Wiederherstellung am Zwischenstand; `journal_mode` = `wal`; Integration `Restore_LeavesDatabaseInWalMode` |
| Gelöschte Inhalte in der Sicherung | erledigt (`VACUUM INTO`) | Vor dem Sichern ein Fahrzeug mit eindeutigem Namen angelegt und gelöscht: Name in der Livedatei vorhanden, im Archiv nirgends; Sicherung 5,2 KB statt 5,5 KB in 15.1 |
| iCloud-Gerätebackup | entschieden: nicht ausgenommen, begründet in ADR 0011 und in der Hilfe genannt | Entspricht der Empfehlung aus 15.1 („oder es in der Hilfe erwähnen“); die App überträgt nichts selbst. Vertretbar. |
| Grenzen nur beim Lesen | erledigt: Erstellung prüft mit `Validate`, Ergebnis `Failed` mit allgemeiner Meldung | Integration `Create_PhotoOverRestoreLimit_FailsWithoutLeavingFiles` |
| Reihenfolge `RestoreSwap.Undo` | erledigt: Hauptdatei zuerst | Unit `RestoreSwapUndoTests` (nur unter Windows wirksam, sonst `return`) |
| Import-Meldung bei unbrauchbarer Datenbank | erledigt | Integration `Import_UnusableDatabase_IsRejectedBeforeEnteringTheList` |
| Zählwerte der Rückfrage aus dem Manifest | offen (bewusst) | siehe „Zählwerte“ unten |
| App-Version „0.1.0.1“ unter Windows | offen | Manifest und Rückfrage am Zwischenstand nennen „Version 0.1.0.1“ |
| Datenschutzhinweis (Rückschlüsse aus Stationen und Preisständen) | offen | Hinweistext unverändert |
| Kachelzahl bei iOS-Textgröße, Sichtprüfung hell | offen | ohne Gerät bzw. ohne Änderung der Systemeinstellung nicht prüfbar |

### Präparierte Dateien

- **Vorgehen:**
  - Jede Datei wurde mit gültigem Manifest und gültigen Prüfsummen aus der am Zwischenstand erstellten Sicherung abgeleitet.
  - Eingespielt wurde über die Oberfläche. Ausgangsbestand: Fingerabdruck `5f091783…` (2 Fahrzeuge, 3 Vorgänge, 1 Foto,
    20 Preisstände).
  - Zwischen den Fällen wurde die saubere Sicherung wiederhergestellt und die eingespielte Datei über „Löschen“ entfernt.
- **Gegenprobe:** Eine Nachbildung von `BackupSchemaDescriber` in Python sagte für jede Datei dasselbe Ergebnis voraus.

| Manipulation | Ergebnis beim Einspielen | Folge |
|---|---|---|
| Trigger wie in 15.1 (`abnahme_evil`) | abgelehnt: „Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“ | Liste und Daten unverändert |
| Fremdes Schema wie in 15.1 (Tabelle gelöscht, Tabelle umbenannt) | abgelehnt | unverändert |
| Default von `TankbookEntries.Kind` geändert | abgelehnt | unverändert |
| Ausdrucksindex statt Spaltenindex | abgelehnt | unverändert |
| Zusätzliche generierte Spalte | abgelehnt | unverändert |
| Eindeutiger Index als Teilindex | abgelehnt | unverändert |
| Vorgang zu nicht vorhandenem Fahrzeug | abgelehnt (`foreign_key_check`) | unverändert |
| Trigger mit reserviertem Präfix | **angenommen** | **Abweichung 1** |
| `CHECK` an `Vehicles` | **angenommen** | **Abweichung 2** |
| Autoinkrement-Zähler `Vehicles` | **angenommen** | **Abweichung 2** |
| Ungültiges Datum | **angenommen** | **Abweichung 2** |
| Zeile in `__EFMigrationsLock` | **keine Meldung, Import hängt** | **Abweichung 2** |
| `COLLATE NOCASE` aus `Vehicles.Name` entfernt, Index mit ausdrücklichem `NOCASE` | angenommen | ohne erkennbare Folge: „golf abnahme“ wird weiterhin als Doppelname erkannt, weil `VehicleService` die Verletzung des eindeutigen Index abbildet |
| Unbekannte Kraftstoffart „Plutonium“ | angenommen | ohne Fehler; das Fahrzeug erscheint als „Benzin“ |
| `user_version` 99, `application_id` 4660, `page_size` 1024 | angenommen | ohne Folge; App und Neuanlage funktionieren, die Werte bleiben in der Livedatenbank (keine Verwendung im Code) |
| Manifest-Zählwerte 42/420/7/9/99/999/5 bei Inhalt 2/3/1/2/2/20/1 | angenommen | Rückfrage nennt „42 Fahrzeuge … 5 Belegfotos“ |

### Bewertung der im ADR genannten Grenzen des Vergleichs

- **`CHECK`:** ausnutzbar. Funktionsstörung, siehe Abweichung 2.
- **`COLLATE`:**
  - Bei indizierten Spalten wird die Kollation über `pragma_index_xinfo` (`coll`) mitverglichen. Ein Weglassen ohne Ausgleich
    im Index fiele auf.
  - Bei nicht indizierten Spalten oder getrennter Spalten- und Indexkollation bleibt sie unerkannt. Die Folge ist praktisch nur
    eine andere Sortierung oder Gleichheitsprüfung in Abfragen; im Test ohne erkennbare Wirkung.
- **Teilindex-`WHERE`, Ausdrucksindex:** Die Referenz hat weder Teil- noch Ausdrucksindizes. Jede Umwandlung ändert deshalb
  `partial` bzw. `cid`/`name` und wird erkannt (praktisch belegt). Ausnutzbar wäre das erst, wenn die App selbst solche Indizes
  bekommt.
- **Generierte Spalten, geänderter Default:** werden über `pragma_table_xinfo` (`hidden`, `dflt_value`) erkannt (praktisch
  belegt).
- **Strengere Prüfung über den Text:**
  - Ein Vergleich des normalisierten SQL-Texts aller Objekte aus `sqlite_master` ist praktisch möglich.
  - Nachweis: Die Datenbank aus 0.1.14 (4 Migrationen), die die App beim Wiederherstellen auf 7 Migrationen gehoben hatte (aus
    15.1), stimmt nach Zusammenfassen der Leerzeichen und Zeilenenden in allen Tabellen- und Indextexten mit einer von 0.1.18
    angelegten Datenbank überein. Dasselbe gilt für drei weitere Datenbanken dieser Abnahmen.
  - Grund: Die Migrationskette ist dieselbe, auch `ALTER TABLE ADD COLUMN` schreibt deterministischen Text.
  - Risiken:
    - Ein späteres EF-Update mit anderer SQL-Erzeugung würde ältere Sicherungen fälschlich ablehnen; Abhilfe sind
      Testdatenbanken älterer Versionen.
    - Unterschiedliche Zeilenenden zwischen iOS und Windows müssen normalisiert werden.
  - Der Textvergleich deckt aber weder Zählerstand noch Zeileninhalte noch die Sperrzeile ab. Der in Abweichung 2 beschriebene
    Neuaufbau aus der Referenz ist deshalb die einfachere und vollständigere Lösung.

### Manipulationen außerhalb des Schemas

- **`user_version`, `application_id`, `page_size`:** unschädlich, im Code nicht verwendet.
- **Fremdschlüssel:** Verletzungen werden per `foreign_key_check` erkannt.
- **Zeileninhalte:**
  - Unbekannte Enum-Zeichenketten fängt die App ab.
  - Ungültige Datums- und Zahlwerte führen zu Ladefehlern (Abweichung 2).
- **`sqlite_sequence` und `__EFMigrationsLock`:** schädlich (Abweichung 2).

### Zählwerte in der Rückfrage

- Die Rückfrage ist die Entscheidungsgrundlage des Anwenders („Die aktuellen Daten werden vollständig ersetzt“). Ihre Zählwerte
  stammen ungeprüft aus dem Manifest.
- Bei einer präparierten Datei kann sie deshalb „0 Fahrzeuge“ bei vorhandenem Inhalt zeigen, oder umgekehrt „42 Fahrzeuge,
  420 Vorgänge“ für eine Datei mit 2 Fahrzeugen.
- Für eigene Sicherungen sind die Werte korrekt (praktisch: 2/3/1/2/2/20/1). Deshalb ist das keine Abweichung.
- Weil Import und Wiederherstellung die Datenbank jetzt ohnehin ins Staging entpacken, ließen sich die Werte mit geringem Aufwand
  dort lesen. Die Fotozahl ergibt sich aus der Dateiliste.
- Empfehlung: entweder so umsetzen oder bei Abweichung zum Manifest ablehnen.

### Bewusst nicht umgesetzte Punkte der Selbstauskunft

| Punkt | Bewertung |
|---|---|
| Nur die Datenbank statt auch der Fotos ins Staging entpacken | vertretbar (Leistung) |
| Kein Referenz-Cache (Referenz je Import und Wiederherstellung neu migriert) | vertretbar; spürbar war es nicht (Import < 1 s) |
| Keine eigene Meldung für ein zu großes Foto beim Erstellen | vertretbar; praktisch nur bei Manipulation des Fotoverzeichnisses erreichbar |
| `return` statt Skip im Windows-Test | Kleinigkeit: Der Test zeigt außerhalb von Windows grün, ohne etwas zu prüfen |
| Zählwerte weiter aus dem Manifest | Hinweis oben |

### Praktische Verifikation am Zwischenstand

- **Start:**
  - Testmodus, `TANKATLAS_TEST_DATA_PATH` im Scratchpad, `TANKATLAS_TEST_WINDOW=offscreen`.
  - `TANKATLAS_TEST_BACKUP_IMPORT` zeigte auf eine austauschbare Datei.
  - Schlüsselvariablen entfernt, ohne sie zu lesen.
  - Bedienung nur über UI-Automation-Muster (`Invoke`, `Toggle`, `SetValue`).
- **Ablauf mit einer 0.1.18-Datenbank:**
  1. 0.1.18 mit leerem Datenverzeichnis gestartet (7 Migrationen bis `AddTankbookEntryDetails`) und per PID beendet.
     - Befüllt mit denselben synthetischen Daten wie in 15.1: Einstellungen, 3 Spritsorten, 2 Stationen, 20 Preisstände, Gruppe
       mit Beschreibung, 2 Favoriten mit Notiz und Priorität, 2 Fahrzeuge, 3 Vorgänge, davon einer mit PNG-Beleg.
     - Zusätzlich ein Fahrzeug mit eindeutigem Namen angelegt und wieder gelöscht.
  2. 0.1.19: „Sicherung erstellen“ → „Sicherung erstellt.“, Eintrag „12.10.2026 21:51 / 5,2 KB“.
     - Archiv: genau `database/tankatlas.db` (Deflate), `receipts/<guid>.png` (unkomprimiert), `manifest.json`.
     - Manifest: Migration `20261011170920_AddTankbookEntryDetails`, Zählwerte 2/3/1/2/2/20/1, alle SHA-256 korrekt.
     - Archiv-Datenbank ≙ Ausgangsdaten (`5f091783…`), Journalmodus `DELETE`.
     - Der gelöschte Name stand nicht im Archiv.
  3. Daten über die Oberfläche geändert: „In der Nähe“ aus; „Golf Abnahme“ samt 2 Vorgängen gelöscht. Die Löschrückfrage nannte
     den Sicherungshinweis, das Foto wurde entfernt.
  4. „Wiederherstellen“: Rückfrage mit Datum, Version und Zählwerten → „Ja, wiederherstellen“ → „Sicherung wiederhergestellt. …“.
     - Schalter wieder an, „Golf Abnahme“ wieder da.
     - Datenbank ≙ Ausgangsdaten, Foto bytegleich, WAL-Modus (2/2).
     - Kein Journal, keine Rücksicherung, kein Staging.
  5. „Teilen“ → bytegleiche Kopie in `test-shared/`.
  6. Neustart: Daten unverändert.
  7. „Löschen“ (bei den eingespielten Dateien): Rückfrage „… Die Sicherungsdatei wird endgültig gelöscht. Deine aktuellen Daten
     bleiben unverändert.“ → „Sicherung gelöscht.“
- **Aufräumen nach dem hängenden Import:** Ein Neustart auf dem Datenverzeichnis entfernte `restore-staging/` und
  `import-*.tmp`. Liste und Daten (`5f091783…`) blieben unverändert.

### Prüfläufe

**`scripts/local-ci.ps1`**

- Vollständig am 12.10. von 21:17 bis 21:27 auf HEAD `f57dc45`, E2E off-screen, Exit-Code 0.
- `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY` wurden per `env -u` entfernt, ohne sie zu
  lesen.
- Seitdem ist der Code unverändert; ein erneuter Lauf nach der Pause war deshalb nicht nötig.
- Vor dem Start lief bis 21:16 der E2E-Lauf eines anderen Projekts (VideoPlayer) auf dem Rechner. Mit dem eigenen Lauf wurde bis
  zu dessen Ende gewartet.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK, 0 Warnungen |
| Unit | 1272/1272 |
| Integration | 171/171 |
| Zeilenabdeckung | 93,2 % (Schwelle 70 %) |
| FlaUI-E2E | **109/109** in 8 min 54 s, beide `DiagnosticsCaptureE2ETests` grün (Bildschirm an); keine Wiederholung nötig |
| iOS-Compile-Prüfung | OK (bekannte Warnung `MAUI1001`) |

**Pre-Push-Prüfungen**

- `.githooks/pre-push` wurde direkt aufgerufen, ohne Push, am 12.10. von 21:14 bis 21:16.
- Eingabe: lokal `f57dc45`, Remote-Stand `42b4710`; Schlüsselvariablen entfernt.

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz | OK |
| `conventional-commits-check.py` (`42b4710..f57dc45`) | OK, 13 Commits |
| `forbidden-patterns-check.py --all --strict` | OK (865 Dateien) |
| `format-code-style-check.py --strict` | OK |
| `csproj-xmldoc-check.py --all` | OK (633 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (629 Dateien) |
| `test-execution-check.py` | OK: Unit 1272/1272, Integration 171/171 |
| Gesamt | Exit-Code 0 |

- Zusätzlich: `conventional-commits-check.py --base main --end HEAD` OK (154 Commits).
- In den Protokollen gibt es keine `apikey=`-Fundstelle.

**Wiederholte E2E-Läufe**

- **Filter:** `BackupE2ETests` (alle 8 Klassen einschließlich `_TamperedSchema`), `TankbookStatisticsE2ETests_TileNumbers`,
  `TankbookE2ETests_DeleteWithEntries`, `TankbookEntriesE2ETests_EditDelete`.
- **Ausführung:** Release, `--no-build`, off-screen.

| Lauf | Ergebnis | Dauer |
|---|---|---|
| 1 | 14/14 | 1 min 36 s |
| 2 | 14/14 | 1 min 39 s |
| 3 | 14/14 | 1 min 37 s |

Während der Läufe gab es Eingaben des Anwenders (Leerlauf 0–46 s); es gab trotzdem keinen Ausfall.

### Kleinigkeiten

- **`BackupE2ETests_TamperedSchema`:** Der Kommentar verspricht „samt Möglichkeit, ein weiteres [Fahrzeug] anzulegen (Trigger
  wirkungslos)“. Der Test legt aber kein weiteres Fahrzeug an.
- **Neuer Fehlerfall bei der Referenz:** Scheitert das Anlegen der Referenzdatenbank selbst (z. B. Speicher voll), lautet das
  Ergebnis „keine gültige Sicherung“ (`DbException` → `InvalidFile`) statt „Aktion konnte nicht ausgeführt werden“. Selten, aber
  irreführend.

### Sonstiges

- **Keine Änderungen am Repository:**
  - Keine Commits, keine Pushes und keine Codeänderungen. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert: keine Datei jünger als der Beginn der Abnahme
    (12.10. 21:09).
- **Testdaten, Werkzeuge und Protokolle** liegen im Scratchpad (`abn15b/`).
- **Eigene Prozesse:** Alle eigenen `Tankradar.MAUI`-Prozesse wurden per PID beendet: 12880, 24816, 39716, 40692. Die Instanz
  26652 mit dem hängenden Import lief nach der Unterbrechung nicht mehr. Am Ende lief kein `Tankradar.MAUI`- und kein
  `testhost`-Prozess.
- **Keine Systemeinstellungen** wurden geändert, und es gab keine Maus- oder Tastatureingaben.
- **Unterbrechung:** Die Abnahme ruhte vom 12.10. gegen 22:05 bis zum 18.10. 06:30 (Nutzungslimit). Die Ergebnisse vor der Pause
  wurden übernommen; die Beobachtung des hängenden Imports wurde am 18.10. mit einer neuen Instanz wiederholt.
- **Testdaten außerhalb des Scratchpads (eigenes Versehen):**
  - Für den Textvergleich wurde kurz der Zwischenstand 0.1.3 gestartet. Er kennt `TANKATLAS_TEST_DATA_PATH` noch nicht und legte
    deshalb `C:\Users\Martin\AppData\Local\User Name\com.softwareschmiede.tankradar.dev\Data\tankatlas.db` an: leere Datenbank mit
    2 Migrationen, angelegt am 12.10. um 22:01:33. Der Ordner bestand vorher nicht.
  - Der Prozess wurde sofort per PID beendet. Der Vergleich stützt sich stattdessen auf die Datenbank aus 0.1.14.
- **Vom Berechtigungssystem abgelehnt:**
  - Das Entfernen dieses Ordners (`Remove-Item … -Recurse`) wurde abgelehnt; der Ordner besteht weiter. Das Löschen ist Sache
    des Anwenders.
  - Eine unmittelbar danach gestellte, nur lesende UI-Abfrage (Zustand der Karte) wurde einmal ebenfalls abgelehnt. Die
    Beobachtung wurde später mit einer neuen Instanz nachgeholt.
  - Weitere Ablehnungen gab es nicht. Keine Ablehnung wurde umgangen.
