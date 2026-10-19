# Abnahmeprüfung – Entwicklungsschritt 15 (Lokale Sicherung und Wiederherstellung), dritte Prüfung nach Nachbesserungsrunde 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Der Neuaufbau der Datenbank aus einer frischen Referenz (`BackupDatabasePreparer`) schließt die beiden blockierenden Abweichungen
aus `acceptance-schritt-15.2.md` praktisch. Alle dort und in `acceptance-schritt-15.1.md` genannten Angriffe wurden am
Zwischenstand `review-versions/0.1.19_2026-10-18` über die Oberfläche (off-screen, UI-Automation, keine Maus-/Tastatureingaben)
erneut nachgestellt. In jedem Fall gilt jetzt:

- **Entweder sauber verworfen:** Die Datei wird angenommen, aber nur die Daten der bekannten Tabellen gelangen in eine neu
  aufgebaute Datenbank. Nach der Wiederherstellung ist das Schema identisch mit dem einer frischen Datenbank (praktisch geprüft:
  keine zusätzlichen Objekte, `integrity_check` = ok, `user_version`/`application_id` = 0, `__EFMigrationsLock` leer,
  `sqlite_sequence` aus den Daten). Die App ist voll funktionsfähig: In jedem Fall ließen sich ein weiteres Fahrzeug und ein
  weiterer Vorgang anlegen, die vorhandenen Vorgänge blieben erhalten.
- **Oder abgelehnt:** mit „Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“
  (bzw. der Hinweismeldung bei zu neuen Dateien), Liste und Bestand unverändert (Fingerabdruck `a927319b0c3d7a7a` über alle Fälle
  stabil).

Nie beobachtet: Hängen, stiller Datenverlust, dauerhaft gesperrte Bedienung.

**Die drei Befunde aus 15.2 im Einzelnen:**

- **Trigger mit reserviertem Präfix (`sqlite_evil`):** angenommen, sauber aufgebaut, Trigger ohne Wirkung. Nach der
  Wiederherstellung wurde ein Fahrzeug angelegt, die drei Vorgänge blieben erhalten (`vehicles`=3, `entries`=4). Der stille
  Datenverlust aus 15.2 Abweichung 1 tritt nicht mehr auf.
- **Zusätzliche `CHECK`-Einschränkung und manipulierter Autoinkrement-Zähler:** angenommen, verworfen; ein neues Fahrzeug ließ
  sich ohne Fehler anlegen (`sqlite_sequence` aus den Daten neu). 15.2 Abweichung 2 behoben.
- **Zeile in `__EFMigrationsLock`:** angenommen, kein Hängen (Einspielen in ~3,5 s abgeschlossen), danach Fahrzeug anlegen und
  neue Sicherung erstellen möglich. Die Quelle wird nur gelesen und nie migriert; die Sperrzeile kann nichts blockieren.

**Weitere praktisch belegte Eigenschaften:**

- Zählwerte der Rückfrage stammen aus der geprüften Datenbank, nicht aus dem Manifest: `counts.zip` (Manifest 42/420/7/9/99/999/5)
  zeigte in der Rückfrage die echten „2 Fahrzeuge, 3 Tank- und Ladevorgänge …“.
- Datenmigrationen älterer Stände laufen auf den kopierten Daten: Eine 0.1.16-Sicherung (Vorgänge im alten Schema) wird auf 7
  Migrationen gehoben; `AddTankbookEntryDetails` füllt `OccurredAt` aus `CreatedUtc` (lokale Zeit: 06:30 UTC → 08:30),
  `OdometerKm` aus dem Startstand des Fahrzeugs (40000/10000) und `Kind` = `Electricity` für das Elektrofahrzeug. Eine
  0.1.14-Sicherung (ohne Fahrzeugtabelle) wird vollständig migriert, Favoriten/Stationen/Preisstände/Einstellungen bleiben
  erhalten.
- Vollständiger Ablauf: Sicherung erstellen (`VACUUM INTO`, Archiv-Datenbank ≙ Ausgangsdaten) → Fahrzeug mit Vorgängen über die
  Oberfläche löschen (Löschrückfrage nennt den Sicherungshinweis) → aus der Liste wiederherstellen → Daten ≙ Sicherung, Belegfoto
  bytegleich, Datenbank im WAL-Modus (Kopfbytes 2/2), kein Staging/Journal/Rücksicherung. „Teilen“ → bytegleiche Kopie in
  `test-shared/`.

**Prüfläufe (selbst ausgeführt, HEAD `3e7e54e`, Schlüsselvariablen per `env -u` entfernt, nie gelesen):**

| Prüfung | Ergebnis |
|---|---|
| `local-ci.ps1` vollständig, E2E off-screen | Exit 0 |
| Unit | 1272/1272 |
| Integration | 208/208 |
| FlaUI-E2E | 111/111 (beide `DiagnosticsCaptureE2ETests` grün, Bildschirm an) |
| Zeilenabdeckung | 93,3 % (Schwelle 70 %) |
| Build mit Warnungen als Fehler | 0 Warnungen |
| iOS-Compile-Prüfung | OK (nur `MAUI1001`) |
| Pre-Push-Prüfungen (ohne Push) | Exit 0 |
| Backup-E2E-Klassen 3× | je 16/16 |

## Abweichungen

Keine Abweichung von der Anforderung. Die beiden blockierenden Befunde aus `acceptance-schritt-15.2.md` sind behoben; es wurden
keine neuen gefunden. Die offenen Punkte unten sind wünschenswerte Härtungen bzw. Umgebungsgrenzen, keine Verfehlungen der
Vorgabe.

## Hinweise

### Angriffe aus 15.1/15.2 – Ergebnis am Zwischenstand 0.1.19 (2026-10-18)

Vorgehen: Jede Datei wurde mit gültigem Manifest und gültigen SHA-256 aus einer am Zwischenstand erstellten Sicherung abgeleitet
und über „Sicherung aus Datei einspielen“ eingespielt. Ausgangsbestand Fingerabdruck `a927319b0c3d7a7a` (2 Fahrzeuge, 3 Vorgänge,
1 Foto, 20 Preisstände). Zwischen den Fällen wurde die saubere Sicherung wiederhergestellt und die eingespielte Datei gelöscht.
Nach jedem angenommenen Fall wurde das Schema gegen eine Referenzdatenbank verglichen und ein Fahrzeug samt Vorgang angelegt.

| Manipulation | Ergebnis | Folge |
|---|---|---|
| Trigger (`evil_trigger`) | angenommen, verworfen | Schema = Referenz, App nutzbar, Vorgänge erhalten |
| Trigger mit Präfix `sqlite_` | angenommen, verworfen | wie oben (15.2 Abw. 1 behoben) |
| Zusätzliche `CHECK` an `Vehicles` | angenommen, verworfen | neues Fahrzeug anlegbar (15.2 Abw. 2 behoben) |
| Manipulierter `sqlite_sequence` | angenommen, verworfen | Zähler aus Daten, neues Fahrzeug anlegbar |
| Zeile in `__EFMigrationsLock` | angenommen, verworfen | kein Hängen, App nutzbar, Sicherung erstellbar |
| `COLLATE NOCASE` entfernt | angenommen, verworfen | Schema = Referenz |
| Geänderter Default, Teilindex, Ausdrucksindex, generierte Spalte, Pragmawerte | angenommen, verworfen | Schema = Referenz, Pragmawerte auf Standard |
| Manifest-Zählwerte 42/420/… | angenommen | Rückfrage nennt echte 2/3/… aus der geprüften DB |
| Fremdes Schema, Vorgang ohne Fahrzeug, fehlende Tabelle/Spalte, Doppelname, View statt Tabelle | abgelehnt | Liste und Daten unverändert |
| Ungültiges Datum, unbekannte Kraftstoffart | abgelehnt | unverändert |
| notzip, empty, Zip-Slip (2×), Zip-Bombe, falsche Prüfsumme, Zufallsbytes-DB | abgelehnt | unverändert, keine `evil.txt` |
| zu neue Formatversion/Migration | abgelehnt mit Aktualisierungshinweis | unverändert |
| unbekannte ältere Migration | abgelehnt | unverändert |

Anders als in 15.1/15.2 kommt eine unbrauchbare Datenbank (Zufallsbytes) jetzt gar nicht erst in die Liste, sondern wird schon
beim Einspielen abgelehnt, weil der Neuaufbau bereits beim Import läuft.

### Neue Angriffsflächen des Neuaufbaus

- **Pfad zu Belegfotos in DB-Spalten (Path traversal):** Eine Sicherung mit `ReceiptPhotoFileName = ..\..\backups\evil.png` in
  einem Vorgang wurde angenommen (der Wert ist reine Dateidatenzeile). Eine Kanariendatei außerhalb des `receipts/`-Ordners
  (`backups/evil.png`) überlebte Wiederherstellung **und** die Aufräumung nicht referenzierter Fotos (`DeleteUnreferenced` beim
  Öffnen des Tankbuchs) unverändert; der `receipts/`-Ordner blieb intakt. Grund: Jede Pfadbildung geht durch
  `ReceiptPhotoStore.IsValidFileName` (Guid + erlaubte Endung); ein manipulierter Wert ergibt `null`, es wird nie eine Datei
  außerhalb des Foto-Ordners angefasst. Keine Abweichung.
- **Quelle im WAL-Modus:** Eine Sicherungs-Datenbank im Journalmodus WAL (Kopfbytes 18/19 = 2, Daten in die Hauptdatei
  gecheckpointet, kein `-wal` im Archiv – `-wal`/`-shm`-Einträge lehnt die Whitelist ohnehin ab) wird schreibgeschützt gelesen und
  sauber wiederhergestellt. Der in Review N1 vermutete Fehlerpfad (Quelle lässt sich read-only nicht öffnen → `CANTOPEN`/`IOERR` →
  fälschlich `Failed`) tritt für eine gecheckpointete WAL-Quelle nicht ein.
- **Gefälschter Migrationsstand:** zu alter/neuer/unbekannter Stand wird über die Versionsregeln abgefangen (abgelehnt bzw. „zu
  neu“); ein Manifeststand, der nicht zur tatsächlichen `__EFMigrationsHistory` der Quelle passt, führt zu `InvalidFile`.

### Offene Punkte (Härtungen bzw. Umgebungsgrenzen, keine Abweichung)

- **Weiches Zeitlimit (Review M1/N6):** Das Limit von zwei Minuten greift zwischen den Anweisungen; eine einzelne SQLite-Anweisung
  und das Entpacken laufen zu Ende. ADR 0011, Hilfe und `changes.log` sind nach Runde 2 ehrlich darauf abgeschwächt. Eine
  Nachbildung in Python fand keine einzelne, nur lesende Anweisung, die unverhältnismäßig lange läuft; die Quelle wird nie
  migriert, die Dauer ist durch die Größengrenzen beschränkt. Vertretbar.
- **`Failed` statt `InvalidFile` bei Quellfehlern (Review N1):** nur eine unpräzise Meldung, keine Datenwirkung; für
  App-erzeugte Sicherungen (Journalmodus `DELETE`) ohne Belang.
- **App-Version „0.1.0.1“ unter Windows:** Manifest und Rückfrage nennen am Windows-Zwischenstand „Version 0.1.0.1“, weil
  `create-review-version.ps1` nur `-p:Version` setzt. Unter iOS setzt die CI `ApplicationDisplayVersion` korrekt. Kosmetisch,
  unverändert gegenüber 15.1.
- **Datenschutzhinweis:** Der Hinweis „Nicht enthalten … dein Standort“ stimmt. Aus gespeicherten Stationen und Preisständen mit
  Abrufzeit lassen sich dennoch Gegenden ableiten, in denen gesucht wurde; ein Halbsatz dazu in Hinweis/Hilfe wäre folgerichtig,
  da geteilte Sicherungen unverschlüsselt sind.
- **Sichtprüfung hell/dunkel und iOS-Gerät:** ohne Änderung der Systemeinstellung bzw. ohne Mac nicht prüfbar (für diese Abnahme
  untersagt). Bleibt offener Abnahmepunkt wie in 15.1.

### Anforderung, Datenschutz, iOS

- **Datenschutz:** Das Archiv enthält genau `manifest.json`, `database/tankatlas.db` und `receipts/*`. Kein Zugangsschlüssel (er
  liegt nicht in der Datenbank), keine Standortdaten des Anwenders (nur Stammdaten der Stationen); keine schlüsselartigen
  Zeichenketten im Archiv. `forbidden-patterns-check --all --strict` grün, keine `apikey=`-Fundstelle in den Protokollen.
- **iOS / Abhängigkeiten:** `Tankradar.MAUI.csproj` und `Info.plist` sind gegenüber dem Basisbranch unverändert; keine neue
  `PackageReference` in irgendeinem Projekt (die Testprojekte binden nur die gemeinsamen `TestSupport`-Dateien per `Compile Link`
  ein). Die JSON-per-Reflection-Bewertung aus 15.1 gilt unverändert (iOS-SDK setzt `JsonSerializerIsReflectionEnabledByDefault`;
  `Tankradar.MAUI` ist `TrimmerRootAssembly`).
- **Restpunkte aus Abnahme 14:** Kachelzahlen einzeilig (`StatisticsTileFont`), ADR 0010 zu Symbolen und Verbrauchsverlauf ergänzt
  – unverändert gegenüber 15.1/15.2 übernommen.

### Geprüfter Stand und Prüfläufe

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-15-sicherung`, HEAD `3e7e54e`.
- **Basis:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `42b4710` (Merge-Base, entspricht `origin`).
- **Nachbesserung seit 15.2 (`f57dc45..HEAD`):** Neuaufbau in `BackupDatabasePreparer`, `BackupSchemaDescriber` entfernt, neue
  Tests (`BackupServiceTests_Rebuild`, erweiterte `_Schema`, `BackupTestTampering`, `BackupE2ETests_TamperedSchema`), ADR 0011,
  Hilfe, `changes.log`.
- **Zwischenstand:** `review-versions/0.1.19_2026-10-18/`, `FileVersion` 0.1.19.0, `ProductVersion` `0.1.0+3e7e54e…`; entspricht
  HEAD. `CHANGELOG.md` deckt sich mit `changes.log` 0.1.19.
- **Code-Reviews:** `review-code.r2-1.md` (2 mittel, 5 niedrig) und `review-code.r2-2.md` (nur niedrig) im Feature-Verzeichnis;
  M1/N2/N3/N4 behoben, Rest als niedrig bzw. dokumentiert.
- **`local-ci.ps1`** vollständig (07:42–07:52), E2E off-screen, Exit 0. Unit 1272/1272, Integration 208/208, E2E 111/111,
  Abdeckung 93,3 %, iOS-Compile OK, 0 Build-Warnungen. Beide `DiagnosticsCaptureE2ETests` grün (Bildschirm an; „Screenshot
  einfarbig“ trat nicht auf).
- **Pre-Push** (`.githooks/pre-push`, ohne Push): Exit 0. Conventional Commits 17 Commits (`42b4710..HEAD`), zusätzlich 158
  Commits (`main..HEAD`); `forbidden-patterns`, `format`, `csproj-xmldoc`, `translation`, `enum-coverage`, `no-notimplemented`
  und `test-execution` (Unit 1272, Integration 208) OK.
- **Wiederholte E2E-Läufe:** `BackupE2ETests` (8 Klassen), `TankbookStatisticsE2ETests_TileNumbers`,
  `TankbookE2ETests_DeleteWithEntries`, `TankbookEntriesE2ETests_EditDelete` – 3× je 16/16 (Leerlauf 110–703 s, keine Störung).

### Sonstiges

- **Keine Änderungen am Repository:** keine Commits, keine Pushes, kein Code geändert. Angelegt wurde nur dieser Bericht.
  `review-versions/`, `drafts/` und `design-draft/` blieben unverändert (keine Datei jünger als der Beginn der Abnahme).
- **Testdaten** liegen im Scratchpad (`abn15c/`); alte Zwischenstände (0.1.14, 0.1.16, 0.1.18) wurden nur mit gesetztem
  `TANKATLAS_TEST_DATA_PATH` im Scratchpad gestartet, nichts landete im echten AppData.
- **Eigene Prozesse** wurden per PID beendet (0.1.18: 41484; 0.1.14: 40776; 0.1.16: 8916; 0.1.19: 39084); am Ende lief kein
  `Tankradar.MAUI`- und kein `testhost`-Prozess. Fremde `dotnet`-Prozesse anderer Projekte blieben unberührt.
- **Keine Systemeinstellungen** geändert; keine Maus- oder Tastatureingaben (nur UI-Automation).
- **Vom Berechtigungssystem abgelehnt:** ein zusammengesetzter Shell-Befehl (Neuanlegen der alten Testverzeichnisse) wurde einmal
  abgelehnt; stattdessen in Einzelschritte zerlegt. Keine Ablehnung wurde umgangen. `FUEL_PRICE_API_KEY` wurde nie gelesen oder
  ausgegeben.
