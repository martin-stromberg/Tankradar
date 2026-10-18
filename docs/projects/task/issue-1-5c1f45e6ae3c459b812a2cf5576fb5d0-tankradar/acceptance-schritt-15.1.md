# Abnahmeprüfung – Entwicklungsschritt 15 (Lokale Sicherung und Wiederherstellung)

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Die Sicherung ist im Kern solide umgesetzt und praktisch belegt:

- **Erstellen:** ZIP mit SQLite-Online-Backup der ganzen Datenbank, Belegfotos und Manifest mit SHA-256. Eine mit 0.1.18
  angelegte und befüllte Datenbank kam mit 0.1.19 in allen zehn Tabellen zeilengleich ins Archiv; das Belegfoto war
  bytegleich, und es gab keine weiteren Einträge.
- **Wiederherstellen:** Nach Änderungen über die Oberfläche (Fahrzeug mit Vorgängen und Foto gelöscht, Einstellungen
  geändert) stellte „Wiederherstellen“ aus der Liste den gesicherten Stand vollständig her: alle Tabellen identisch, Foto
  bytegleich, keine Reste von Staging, Rücksicherung oder Journal.
- **Ältere Sicherung:** Eine Datenbank der Version 0.1.14 (4 Migrationen) ließ sich über „Sicherung aus Datei einspielen“
  importieren. Sie wurde auf 7 Migrationen gehoben, und alle Daten blieben erhalten.
- **Abgelehnte Dateien:** Zu neue, beschädigte, leere, fremde und Zip-Slip-Dateien sowie eine Zip-Bombe mit gefälschten
  Größenangaben lehnt die App mit verständlicher Meldung ab, ohne Daten oder Liste zu verändern.
- **Absturz:** Ein simulierter Absturz mitten im Austausch wird beim nächsten Start vollständig zurückgenommen.
- **iOS-Risiko JSON:** Das Risiko besteht nicht. Das iOS-SDK setzt `JsonSerializerIsReflectionEnabledByDefault=true`, und
  die iOS-Release-`runtimeconfig.json` dieses Laufs enthält den Schalter mit `true` (Details unter „Prüfschwerpunkte“).

Nicht erfüllt ist die Vorgabe „Eine beschädigte oder ungültige Sicherungsdatei führt zu einer verständlichen
Fehlermeldung, ohne die vorhandenen Daten zu verändern“ für präparierte oder fremde Datenbanken (Abweichung 1). Eine
Sicherung mit gültigem Manifest und gültigen Prüfsummen, deren Datenbank ein fremdes Schema oder einen Trigger enthält, wird
angenommen und ersetzt den Bestand. Praktisch nachgewiesen:

- **Fremdes Schema:** Das Tankbuch meldet danach „Die Fahrzeuge konnten nicht geladen werden.“
- **Trigger:** Ein eingeschleuster Trigger löschte beim nächsten Anlegen eines Fahrzeugs in der App stillschweigend alle
  Tank- und Ladevorgänge.

Das war als „bewusst offen“ deklariert. Es widerspricht aber der Anforderung, ADR 0011 („Eine externe Datei ist nicht
vertrauenswürdig“), der Hilfe („streng geprüft … nur erlaubte Inhalte“) und `changes.log` („manipulierte Dateien … werden
abgelehnt“).

**Prüfläufe:**

- **Unit:** 1271/1271 (Selbstauskunft bestätigt).
- **Integration:** 154/154 (Selbstauskunft bestätigt).
- **E2E im `local-ci`:** 104/108. Die vier Fehlschläge entstanden nachweislich durch Eingaben des Anwenders während des
  Laufs (Leerlauf 0–6 s, Fahrzeugname „cheGolf“ statt „Golf“). Einzeln wiederholt waren sie 7/7 grün;
  `DiagnosticsCaptureE2ETests` war diesmal grün, weil der Bildschirm an war.
- **Pre-Push:** grün.
- **Neue und geänderte E2E-Klassen:** 3 × 13/13.

## Abweichungen

- [ ] **1. Präparierte oder fremde Datenbank in einer Sicherung wird nicht abgelehnt (Schema und Trigger ungeprüft).**
  - **Vorgabe:**
    - Projektplan Schritt 15: „Eine beschädigte oder ungültige Sicherungsdatei führt zu einer verständlichen
      Fehlermeldung, ohne die vorhandenen Daten zu verändern.“
    - ADR 0011 behandelt externe Dateien ausdrücklich als nicht vertrauenswürdig.
    - Hilfe (`docs/help/Sicherung/beschreibung.md`) und `changes.log` 0.1.19 sagen zu, dass manipulierte Dateien abgelehnt
      werden.
  - **Ist:** `BackupService.PrepareDatabaseAsync` prüft nur:
    - `integrity_check`,
    - die letzte Zeile von `__EFMigrationsHistory` gegen das Manifest,
    - `MigrateAsync`,
    - `foreign_key_check`.

    Ob Tabellen, Spalten, Indizes, Trigger und Views dem Modell der App entsprechen, wird nicht geprüft. Die Prüfsummen
    schützen nur vor Beschädigung, nicht vor Manipulation, denn wer die Datei ändert, rechnet sie neu.
  - **Nachweis** am Zwischenstand 0.1.19 über „Sicherung aus Datei einspielen“: Präparierte Archive mit gültigem Manifest
    und gültigen SHA-256, erzeugt mit `tools/make15.py` im Scratchpad. Ausgangsdaten: 2 Fahrzeuge und 3 Vorgänge.
    - **`trigger.zip`:** Die Datenbank enthält zusätzlich
      `CREATE TRIGGER abnahme_evil AFTER INSERT ON Vehicles BEGIN DELETE FROM TankbookEntries; END`.
      - Import und Wiederherstellung: „Sicherung wiederhergestellt …“. Der Trigger steht danach in der Live-Datenbank.
      - Anschließend wurde über die Oberfläche das Fahrzeug „Neu Abnahme“ angelegt. Danach gab es
        `TankbookEntries` = 0; alle drei Vorgänge waren ohne Hinweis gelöscht.
    - **`foreign-schema.zip`:** `TankbookEntries` gelöscht und `Vehicles` umbenannt, Migrationshistorie unverändert.
      - Import und Wiederherstellung: „Sicherung wiederhergestellt …“. Der bisherige Bestand war ersetzt.
      - Danach meldet das Tankbuch „Die Fahrzeuge konnten nicht geladen werden.“
      - Erst die Wiederherstellung einer anderen, gültigen Sicherung machte die App wieder nutzbar.
  - **Bewertung:**
    - Das Code-Review hatte den Punkt als „Niedrig“ eingestuft (Vorbefund 5), die Selbstauskunft als „bewusst offen“.
    - Die praktischen Folgen sind aber stiller Datenverlust bzw. eine unbrauchbare App. Auslöser ist eine Datei, die der
      Anwender von außen einspielt, also genau der Fall, den die Anforderung und ADR 0011 absichern.
  - **Umsetzbar:**
    - **Schemavergleich:** Nach `MigrateAsync` in `PrepareDatabaseAsync` das Schema der Staging-Datenbank mit einer
      Referenz vergleichen. Die Referenz ist eine leere, im Temp-Bereich mit derselben `MigrateAsync`-Kette erzeugte
      Datenbank. Verglichen werden die Mengen aus `sqlite_master` (`type`, `name`, `tbl_name`, `sql`), alternativ je Tabelle
      `pragma_table_info`, `pragma_index_list` und `pragma_foreign_key_list`.
    - **Ablehnen:** Jede Abweichung führt zu `BackupResult.InvalidFile`, insbesondere zusätzliche oder fehlende Tabellen und
      Spalten sowie jeder Trigger und jede View, die die Referenz nicht hat.
    - **Tests:** Integrationstests mit Trigger, View, fehlender Tabelle und zusätzlicher Spalte (`BackupTestArchive` um eine
      Datenbank-Variante erweitern). Erwartet wird jeweils `InvalidFile`, und die Livedaten bleiben bytegleich. Den Trigger-
      oder Schema-Fall zusätzlich in `BackupE2ETests_InvalidFiles` aufnehmen.
    - **Empfohlen:** Dieselbe Prüfung schon beim Import ausführen (Entpacken in Staging und Vorbereitung), damit eine solche
      Datei gar nicht erst als „gültige Sicherung“ in die Liste kommt (siehe Hinweis „Import-Meldung“).
    - **Dokumentation:** ADR 0011 (Abschnitt „Sicherheit der Eingabedatei“ und den Punkt „bewusst offen“ streichen), Hilfe
      und `changes.log` an den dann zutreffenden Stand anpassen.

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-15-sicherung`, HEAD `e90dc68`.
- **Basis:** lokaler Projekt-Basisbranch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `42b4710`. Er ist
  zugleich Merge-Base und entspricht dem Stand auf `origin`.
- **Diff:** 100 Dateien, +7944/−13, 11 Commits (`57cb36a` … `e90dc68`).
  - `e90dc68` entfernt nur das Feature-Verzeichnis.
  - `git diff 4e28674 e90dc68 -- src` ist leer.
- **Grundlagen:**
  - `project-plan.md`: Schritt 15 mit „Zusatz aus der Abnahme von Schritt 14“, Vorgehensentscheidungen „Schutz lokaler
    Daten“, „Datenhaltung“, „Oberflächentests als Auslieferungs-Gate“, „Keine Strompreise“.
  - `requirement.md` (Abschnitte 2.1 „Price Cache“/„Settings“, 4.6 „Backup- und Restore-Logik“, 4.7 E2E-Szenario
    „Backup erstellen und wiederherstellen“, Standort-Datenschutz).
  - Entscheidungstabelle des Umsetzungsplans (`git show 57cb36a:…/plan.md`, inhaltsgleich mit dem unversionierten
    `docs/features/…--schritt-15-sicherung/plan.md`).
  - Die Code-Reviews `review-code.1.md`, `review-code.2.md` und `review-code.md` (unversioniert).
  - ADR 0010 und 0011, `acceptance-schritt-14.md` (Format, Restpunkte).

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Sicherung in „Optionen“ erstellen und wiederherstellen | Karte „Sicherung“ in `SettingsPage` zwischen „Sortierung“ und „Datenquelle“. `BackupViewModel` ist eingebettet in `SettingsViewModel`, `BackupService` ist Singleton. | E2E `BackupE2ETests_Flow`; Zwischenstand, Aufnahme `settings-backup-one-390.png` |
| Alle Nutzerdaten (Favoriten, Gruppen mit Notiz/Priorität, Fahrzeuge, Tankbuch mit Fotos, Einstellungen) und alle Preisstände mit Zeitstempel | `SqliteConnection.BackupDatabase` kopiert die ganze Datenbank, also auch künftige Tabellen. Hinzu kommen alle Dateien in `receipts/` mit gültigem Namen. Alle Einstellungen liegen in der Datenbank; `Preferences` wird nicht genutzt. | Integration `Completeness_AllEntitiesTimestampsAndPhotoBytes_SurviveRestore`, `Create_DatabaseHasOnlyKnownTables` (Wächter: ein neuer Tabellenname lässt ihn scheitern). Praktisch: Archiv-Datenbank ≙ Ausgangsdatenbank in allen 10 Tabellen (Fingerabdruck `1f8542e6…`) |
| Nach Wiederherstellung vollständig verfügbar, Historie erhalten | Austausch der Datenbank samt Fotoverzeichnis; `SettingsViewModel` lädt neu, die übrigen Seiten laden bei `OnAppearing` | Praktisch: Wiederhergestellt ≙ gesichert (alle Tabellen, Foto-SHA-256 `e06f9db7…`), auch nach Neustart; E2E `_Photo`, `_Persistence` |
| Veraltete Preisstände an Zeitstempeln erkennen, bei Bedarf aktualisieren | `RetrievedUtc` bleibt erhalten. `IPriceCacheInvalidator.InvalidateCaches` leert Such- und Aktualisierungs-Zwischenspeicher, danach greift die bestehende Alterslogik (ab 60 min veraltet). | Unit `FuelPriceServiceTests_InvalidateCaches`; Integration (Aufruf nur bei Erfolg, `CacheInvalidations`) |
| Sicherungen älterer App-Versionen einspielbar | Migration der Staging-Datenbank mit `MigrateAsync`; Versionsregeln über Migrations-ID | Integration `Restore_OlderMigration_IsMigratedAndKeepsData` (`AddVehicles`). Praktisch: Datenbank aus 0.1.14 (`AddFavorites`) → 7 Migrationen, Gruppen, Favoriten, Stationen, 20 Preisstände und Einstellungen erhalten, `ShowNearby` = Standard |
| Lokale Ablage, Liste mit Datum und Größe | `<Daten>/backups/tankatlas-backup-<UTC>.zip`, neueste zuerst, „12.10.2026 17:39“ (Ortszeit), „5,5 KB“; nicht lesbare Dateien „(nicht lesbar)“ | E2E `_List`; Integration `List_*`; UIA-Dump am Zwischenstand |
| Auswahl zur Wiederherstellung, Löschen nach Rückfrage | Inline-Rückfragen im Eintrag (Datum, Version, Zählwerte, „vollständig ersetzt … nicht rückgängig“) | E2E `_Flow` (mit Abbruch), `_List` (ältere Sicherung gewählt); Aufnahme `settings-restore-question-390.png` |
| Teilen/Speichern über das Betriebssystem | `MauiBackupSharer` (`Share.Default.RequestAsync`), im Testmodus `TestBackupSharer` → `test-shared/` | E2E `_Share`; praktisch: Datei in `test-shared/` |
| Externe Datei einspielen | `MauiBackupFilePicker` (`.zip`, `public.zip-archive`), Import mit vollständiger Struktur- und Prüfsummenprüfung, danach sofort Rückfrage | E2E `_Import`; praktisch 15 präparierte Dateien (siehe unten) |
| Rückfrage vor dem Überschreiben | ja, inline; „Abbrechen“ ändert nichts | E2E `_Flow` |
| Beschädigte/ungültige Datei: Meldung, Daten unverändert | strukturell, Prüfsummen, Version, `integrity_check`: ja; Schema/Trigger: nein | siehe Abweichung 1 und Tabelle „Präparierte Dateien“ |
| Keine API-Zugangsdaten in der Sicherung | Schlüssel liegt in Credential Locker/Keychain bzw. im Build, nie in der Datenbank; das Archiv enthält nur `manifest.json`, `database/tankatlas.db`, `receipts/*` | Integration `Create_ContainsOnlyManifestDatabaseAndPhotos_…`; praktisch: Eintragsliste |
| Nur auf Veranlassung, keine automatische Übertragung | keine Automatik, kein Netzwerkcode im Diff (kein `HttpClient`, keine URL) | Durchsicht; zu iCloud-Gerätebackup siehe „Kleinigkeiten“ |
| Löschrückfrage Fahrzeug mit Vorgängen nennt Sicherung | `TankbookTexts.BackupAdvice` bei ≥ 1 Vorgang | Unit `TankbookTextsTests_Labels`; E2E `TankbookE2ETests_DeleteWithEntries`; am Zwischenstand gelesen: „… Erstelle vorher in den Optionen eine Sicherung, wenn du die Vorgänge behalten möchtest.“ |
| Tests: Vollständigkeit, Zeitstempel, ungültige Dateien, ältere Version, Liste, Löschen; FlaUI-Ablauf „erstellen – ändern – aus Liste wiederherstellen – löschen“ | +145 Unit-Tests (1126 → 1271), +32 Integrationstests (122 → 154), +10 E2E-Tests (98 → 108) in 7 neuen Backup-Klassen und `TankbookStatisticsE2ETests_TileNumbers` | siehe „Praktische Verifikation“ |
| Windows-Zwischenstand | `review-versions/0.1.19_2026-10-12/` | siehe unten |
| iOS-Dateischutz für Fotos | `ProtectFile` für jede neue Sicherung und jedes wiederhergestellte Foto, `Protect` für die wiederhergestellte Datenbank; Staging im geschützten Datenverzeichnis | Code; nur kompiliert (iOS-Compile-Prüfung OK) |
| Keine Schemaänderung, keine neue Abhängigkeit | keine Migration; App-`csproj` unverändert (nur die drei Testprojekte binden `TestSupport/BackupTestArchive.cs` ein); `Info.plist` unverändert | `git diff --name-only` |

### Restpunkte aus Abnahme 14

| Restpunkt | Stand | Nachweis |
|---|---|---|
| ADR 0010: Verbrauchsverlauf nicht in der Monatskarte, keine Abstufung; Begründung „keine Symbole“ präzisieren | erledigt | Diff ADR 0010: Symbole wären laut ADR 0004 möglich, Verzicht begründet; eigener Punkt „Verbrauchsverlauf nicht auf der Fläche der Monatskarte und ohne Abstufung“ |
| Kachelzahlen mit vier Stellen nicht umbrechen | erledigt | `StatisticsTileFont` (28/24/21/18 pt nach Länge) plus `NoWrap`. UIA am Zwischenstand bei 360 px: „9.989,00“ ist 25 pt hoch, in Abnahme 14 waren es 65 pt. Aufnahmen `extrem-360-1.png`/`extrem-390-1.png`: einzeilig, Einheit „€“ daneben. E2E `_TileNumbers` 4 × grün. |
| Sichtprüfung hell/dunkel, 360/390 pt bei eingeschaltetem Bildschirm | **dunkel nachgeholt, hell offen** | Bildschirm war zu Beginn an. Aufnahmen per `PrintWindow` (off-screen): „Extrem“ 360/390, „Hybrid viel“ 360, Karte „Sicherung“ und Rückfrage 390. Ohne Befund: Kacheln, Monatskarte, Balken samt Rand-Beschriftungen vollständig, keine Überlappung. Hell wäre nur über die Systemeinstellung `AppsUseLightTheme` möglich gewesen; das war für diese Abnahme untersagt (die App hat keine eigene Umschaltung). Empfehlung: einmal hell ansehen. |

### Prüfschwerpunkte

#### 1. iOS: JSON-Serialisierung per Reflection

- **Neuer Code:** `BackupManifest.ToJson/FromJson` und `RestoreJournal.Write/Read` nutzen
  `JsonSerializer.SerializeToUtf8Bytes`/`Deserialize<T>` ohne `JsonSerializerContext`.
- **Bestehender Code verfährt genauso** (kein Source-Generator-Kontext im Projekt):
  - `PriceRepository` serialisiert `OpeningTimes` in `OpeningTimesJson` und liest es zurück.
  - `HttpTileSource` serialisiert `TileMetadata`.
  - Der Tankerkönig-Client, `NominatimGeocodingService` und `AppConfiguration` nutzen `JsonDocument`.
- **Belege, dass Reflection unter iOS aktiv ist:**
  - `Microsoft.iOS.Sdk.net10.0_26.5/…/Xamarin.Shared.Sdk.props:93` setzt
    `JsonSerializerIsReflectionEnabledByDefault=true`, sofern nicht anders gesetzt (Kommentar dort: dotnet/macios#18057).
    Die Bedingung hängt weder an `PublishTrimmed` noch an der RID.
  - Die Auswertung der Eigenschaften für `net10.0-ios`, Release, `ios-arm64` ergibt
    `JsonSerializerIsReflectionEnabledByDefault=true`, `TrimMode=partial`, `MtouchInterpreter=all`, `PublishAot` leer.
  - Die im `local-ci`-Lauf erzeugte `bin/Release/net10.0-ios/iossimulator-arm64/Tankradar.MAUI.runtimeconfig.json` (18:02)
    enthält `"System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault": true`.
- **Trimming:**
  - `TrimMode=partial` kürzt nur als trimmbar gekennzeichnete Framework-Assemblys.
  - `Tankradar.MAUI` ist zusätzlich `TrimmerRootAssembly`. Eigenschaften und `[JsonPropertyName]` von `BackupManifest`,
    `BackupManifestFile`, `BackupManifestCounts` und `RestoreJournal` bleiben damit erhalten.
- **Ergebnis:** Eine `InvalidOperationException` „Reflection-based serialization has been disabled“ ist nicht zu erwarten.
  Keine Abweichung.
  - Weitere Laufzeitmechanismen im neuen Code sind unkritisch:
    - `GeneratedRegex` wird von einem Source-Generator erzeugt.
    - `ZipFile` und `IncrementalHash` (SHA-256) gehören zur Laufzeit.
    - `SqliteConnection.BackupDatabase` ist eine native e_sqlite3-API; die Assembly ist als Wurzel eingetragen.
    - EF-Migrationen laufen wie bisher im Initializer.
  - Ein Lauf auf Gerät oder Simulator war ohne Mac nicht möglich.
  - Optional zur Absicherung gegen künftige NativeAOT-Umstellungen: ein gemeinsamer `JsonSerializerContext` für
    `BackupManifest`, `RestoreJournal`, `TileMetadata` und `List<OpeningTimeEntry>`.

#### 2. Datensicherheit

**Atomarität und Rollback je Phase** (Commit-Punkt ist das Löschen von `restore.journal`):

| Abbruch nach … | Zustand | Verhalten beim nächsten Start bzw. im Fehlerfall | Nachweis |
|---|---|---|---|
| Validierung, Entpacken, Migration der Staging-Datenbank | Livedaten unberührt, Reste in `restore-staging/` | `RestoreRecovery` löscht Staging; ohne Journal kein Undo | Code; praktisch (Staging-Rest in `c-data` entfernt) |
| Journal geschrieben, noch nichts verschoben | Journal `HadDatabase/HadReceipts = true` | Undo verschiebt nichts und löscht nichts | Unit `Recover_JournalBeforeAnyMove_KeepsOriginals` |
| `-wal`/`-shm` verschoben, Hauptdatei noch nicht | Hauptdatei am Platz | Undo gibt `-wal`/`-shm` zurück | Code |
| Hauptdatei verschoben, neue noch nicht platziert | — | Undo gibt die Hauptdatei zurück | Integration `Restore_FailureAtStep_…(database-moved)` |
| Neue Datenbank platziert, alte Fotos in der Rücksicherung | — | Undo löscht die neue Datenbank und gibt alte Datenbank und Fotos zurück | Integration `Crash_DuringSwap_IsRecoveredAtNextStart`; **praktisch** am Zwischenstand: Zustand von Hand erzeugt (`tools/crashstate.py`), Start → Fingerabdruck `1f8542e6…` und Foto zurück, Journal, Rücksicherung und Staging entfernt |
| Fotos platziert, Schutz gesetzt, unmittelbar vor dem Commit | — | Undo vollständig | Integration `Restore_FailureAtStep_…` (7 Schritte, bytegleich) |
| Commit erfolgt, Rücksicherung noch vorhanden | Journal fehlt | Rücksicherung wird gelöscht, kein Undo | Code |
| Undo scheitert dauerhaft | Journal und Rücksicherung bleiben erhalten | Die Initialisierung wirft; `MigrateAsync` läuft nie auf dem Zwischenstand | Integration `Recovery_WhenUndoFails_ThrowsAndKeepsRollback`, `Restore_WhenUndoFails_KeepsRollbackAndJournal` |

- **`-wal`/`-shm`:**
  - Vor jeder Verschiebung leert `ClearAllPools()` die Pools. Erst danach wird geprüft, ob die Dateien existieren; die
    letzte Verbindung checkpointet und entfernt sie dabei.
  - Eine `-wal`, die nicht verschoben werden kann, löst einen Rollback aus. Neben der neuen Datenbank bleibt also nie eine
    fremde `-wal` liegen.
  - Die neue Datenbank kommt ohne `-wal` an (Staging im Modus `DELETE`).
  - Abgesichert durch Integration `Restore_WithPooledConnectionHoldingWal_Succeeds`, `Restore_DatabaseBrieflyLocked_SucceedsByRetry`.
- **Wiederholung statt Sperre** (bewusst offen):
  - **Windows:** Eine offene Verbindung eines anderen Dienstes lässt das Umbenennen scheitern. Nach 20 × 150 ms folgt der
    Rollback, Ergebnis „Aktion konnte nicht ausgeführt werden …“. Kein Datenverlust.
  - **iOS (POSIX):** Das Umbenennen gelingt trotz offener Verbindung, die Wiederholung greift dort nie. Eine gerade aktive
    Verbindung schreibt in die verschobene alte Datei. Ihr Ergebnis geht mit der Rücksicherung verloren; das ist vertretbar,
    weil der Anwender das Ersetzen bestätigt hat. Die neue Datenbank teilt keine `-wal`/`-shm` mit dem alten Inode, weil sie
    im Modus `DELETE` ankommt. Eine Beschädigung der neuen Datenbank ist daher nicht erkennbar.
  - **Wahrscheinlichkeit:** Kein Timer schreibt in die Datenbank. Die Preisabrufe der Favoriten melden sich bei
    `OnDisappearing` ab und brechen ab, der Austausch dauert Millisekunden.
  - **Bewertung:** Für 1.0 akzeptabel. Robuster wären eine app-weite Lese-/Schreibsperre oder ein erzwungener Neustart nach
    der Wiederherstellung.
- **Vollständigkeit:**
  - Die ganze Datenbank wird per Online-Backup kopiert, künftige Tabellen also automatisch mit. Der Wächtertest erzwingt
    beim nächsten neuen Tabellennamen eine bewusste Prüfung auf Schlüssel- und Standortdaten.
  - Weitere Nutzerdaten außerhalb von Datenbank und `receipts/` gibt es nicht. Im Datenverzeichnis liegen nur
    `tankatlas.db`, `receipts/` und `backups/`; Kacheln liegen im Cache-Verzeichnis.
- **Prüfsummen:**
  - SHA-256 je Datei wird beim Schreiben berechnet und beim Import (`VerifyContentAsync`) sowie beim Entpacken geprüft.
  - `badsha.zip` wurde abgelehnt.
  - Die Prüfsummen erkennen nur Beschädigung, keine Manipulation (siehe Abweichung 1).
- **Zip-Slip:**
  - Eintragsnamen werden nie als Pfad verwendet. Es gibt eine Whitelist (`manifest.json`, `database/tankatlas.db`,
    `receipts/<Guid N>.<Endung>`) und Duplikate werden ohne Beachtung der Groß-/Kleinschreibung abgelehnt.
  - Das Ziel entsteht aus festen Namen; zusätzlich prüft `GetFullPath` das Präfix.
  - Praktisch abgelehnt: `../evil.txt` und `receipts/../../…png`. Im ganzen Scratchpad entstand keine Datei `evil.txt`.
- **Größenlimits (Zip-Bombe):**
  - Geprüft werden Einträge ≤ 20.000, deklarierte Größen (Datenbank ≤ 1 GiB, Foto ≤ 20 MiB, Manifest ≤ 1 MiB,
    Summe ≤ 2 GiB) und tatsächlich gelesene Bytes ≤ deklarierte.
  - Beim Import wird die Datei auf 2 GiB + 1 MiB begrenzt kopiert.
  - Praktisch: `bomb-lying.zip` (64 MiB Nullen, Größe in lokalem und zentralem Header auf 98 KB gefälscht) wurde sofort
    abgelehnt. Es blieben keine großen Temporärdateien zurück.
- **Beschädigte und fremde Dateien:** Text als `.zip`, leere Datei, fehlerhafte Prüfsumme, unbekannte ältere Migration und
  eine Datenbank aus Zufallsbytes (`integrity_check`) wurden abgelehnt. Fremdes Schema und Trigger wurden angenommen
  (Abweichung 1).

#### 3. Datenschutz

- **Keine Schlüssel:**
  - Der Tankerkönig-Schlüssel liegt im Credential Locker bzw. in der Keychain oder im Build, nie in der Datenbank.
  - `forbidden-patterns-check.py --all --strict` ist grün (858 Dateien).
  - Die Protokolle dieser Abnahme (`local-ci`, Pre-Push) enthalten keine `apikey=`-Fundstelle.
- **Keine Standortdaten:**
  - Tabellen: `UserSettings` (GPS-Nutzung, Ansicht, Sortierung, „In der Nähe“), `FuelTypeSettings`, `Stations` (Stammdaten
    der Quelle mit Lat/Lon der Tankstelle), `PriceEntries` (Station, Sorte, Preis, `RetrievedUtc`), `FavoriteGroups`,
    `FavoriteEntries`, `Vehicles`, `TankbookEntries`, `__EFMigrationsHistory`, `__EFMigrationsLock`.
  - Keine Tabelle speichert Suchanfragen, Adresseingaben oder Anwenderpositionen. Der Suchcache mit gerundeten Koordinaten
    (`FuelPriceService._searchCache`) liegt nur im Arbeitsspeicher und wird bei der Wiederherstellung geleert.
  - Indirekt lässt sich aus den gespeicherten Stationen und den Zeitstempeln der Preisstände ableiten, in welchen Gegenden
    wann gesucht wurde (siehe „Kleinigkeiten“).
- **Weitergabe nur auf Nutzeraktion:** „Teilen“ öffnet die Systemfunktion. Die App selbst überträgt nichts; der Diff
  enthält keinen Netzwerkcode.
- **Protokolle:** Es wird nur der Ausnahmetyp protokolliert, keine Pfade und keine Inhalte. Die Ausnahme in
  `RestoreRecovery` enthält keinen Pfad.

### Bewusst offene Punkte der Selbstauskunft

| Punkt | Bewertung |
|---|---|
| Staging-Schema nur über Migrations-ID geprüft | **Abweichung 1** (praktisch: Datenverlust durch Trigger, unbrauchbares Tankbuch durch fremdes Schema) |
| Keine gemeinsame Sperre, nur Wiederholung 20 × 150 ms | Für 1.0 vertretbar; Bewertung siehe „Datensicherheit“ (Windows: Rollback, iOS: Wiederholung wirkungslos, aber ohne Beschädigung der neuen Datenbank) |
| `BackupService` groß | Qualitätsbefund ohne Einfluss auf die Erfüllung (515 Zeilen; Aufteilung wie im Review empfohlen) |
| `swapHook` im Produktionskonstruktor | Ohne Wirkung im Betrieb: MS-DI setzt den optionalen Parameter auf `null`. Unschöne Testnaht in der öffentlichen API, kein Mangel. |
| JSON per Reflection unter iOS nicht geprüft | Geprüft, kein Risiko (siehe Prüfschwerpunkt 1) |
| Einmaliger FlaUI-Fehler in `TankbookEntriesE2ETests_EditDelete` (`PropertyNotSupportedException`) | In dieser Abnahme 4 × grün (`local-ci` und 3 Wiederholungen); nicht reproduziert |

### Praktische Verifikation

**`scripts/local-ci.ps1`**

- Vollständig von 17:51 bis 18:02 (2026-10-12), E2E off-screen.
- `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY` wurden per `env -u` entfernt, ohne sie
  zu lesen.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests, Workflow-Validierung (7 Workflows), iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK, 0 Warnungen |
| Unit | 1271/1271 |
| Integration | 154/154 |
| Zeilenabdeckung | 93,1 % (Schwelle 70 %) |
| FlaUI-E2E | **104/108** in 9 min 45 s. Gescheitert: `OfflineE2ETests_Tankbook`, `TankbookEntriesE2ETests_Partial`, `FavoritesE2ETests_Flow`, `BackupE2ETests_InvalidFiles.TooNewBackup_IsRejected_WithUpdateHint`. Beide `DiagnosticsCaptureE2ETests` waren grün (Bildschirm an). |
| iOS-Compile-Prüfung (`net10.0-ios`) | OK (bekannte Warnung `MAUI1001`) |
| Gesamt | Exit-Code 1 allein wegen der vier E2E-Fehlschläge |

**Ursache der vier E2E-Fehlschläge: Eingaben des Anwenders während des Laufs**

- **Leerlauf seit der letzten Eingabe:** um 17:53–17:56 0 s, um 17:59 5 s. Um 17:37 waren es noch 697 s.
- **Diagnosedaten** (`e2e-diagnostics/`):
  - `TankbookEntriesE2ETests_Partial`: Das Fahrzeug heißt „cheGolf“ statt „Golf“. Getippte Zeichen sind also im
    off-screen laufenden Testfenster gelandet.
  - `OfflineE2ETests_Tankbook` und `BackupE2ETests_InvalidFiles.TooNew…`: Die App steht beim Anlegen des Fahrzeugs
    unerwartet auf „Favoriten“.
  - `FavoritesE2ETests_Flow`: Gruppenname nicht übernommen.
  - Alle vier scheitern in der Eingabe- bzw. Navigationsphase, nicht an der geprüften Funktion.
- **Einzelwiederholung** um 18:04 (Leerlauf 219–295 s): `FavoritesE2ETests_Flow|OfflineE2ETests_Tankbook|`
  `TankbookEntriesE2ETests_Partial|BackupE2ETests_InvalidFiles` → **7/7 grün** (1 min 3 s).
- **Bewertung:** Umgebungseffekt, kein Fehler des Schritts. Die Selbstauskunft „E2E 107/108“ ist plausibel; der Ausfall von
  `DiagnosticsCapture` bei ausgeschaltetem Bildschirm trat hier nicht auf.
- **Hinweis:** Off-screen schützt nicht vor Tastatureingaben, wenn das Testfenster den Fokus hat. E2E-Läufe brauchen also
  weiterhin eine ungestörte Sitzung.

**Pre-Push-Prüfungen**

- **Vorgehen:** `.githooks/pre-push` direkt aufgerufen, ohne Push.
  - Eingabe wie bei einem Push nach `refs/heads/task/…-tankradar`: lokal `e90dc68`, Remote-Stand `42b4710`.
  - Schlüsselvariablen wie oben entfernt.
  - Ausführung 18:05–18:06.

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz | OK |
| `conventional-commits-check.py` (`42b4710..e90dc68`) | OK, 11 Commits |
| `forbidden-patterns-check.py --all --strict` | OK (858 Dateien) |
| `format-code-style-check.py --strict` | OK |
| `csproj-xmldoc-check.py --all` | OK (626 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (622 Dateien) |
| `test-execution-check.py` (Build Debug, Unit, Integration; E2E im Hook standardmäßig aus) | OK: Unit 1271/1271, Integration 154/154 |
| Gesamt | Exit-Code 0, „Pre-Push-Prüfungen erfolgreich abgeschlossen.“ |

Zusätzlich meldet `conventional-commits-check.py --base main --end HEAD` OK (152 Commits).

**Wiederholte E2E-Läufe (Flakiness)**

- **Filter:** `BackupE2ETests|TankbookStatisticsE2ETests_TileNumbers|TankbookE2ETests_DeleteWithEntries|`
  `TankbookEntriesE2ETests_EditDelete`. Das sind alle neuen bzw. geänderten Klassen plus die geforderte `_EditDelete`,
  zusammen 13 Tests.
- **Ausführung:** Release, `--no-build`, off-screen, nacheinander, ohne Eingaben des Anwenders (Leerlauf 399–663 s).

| Lauf | Ergebnis | Dauer |
|---|---|---|
| 1 | 13/13 | 1 min 24 s |
| 2 | 13/13 | 1 min 24 s |
| 3 | 13/13 | 1 min 27 s |

Mit `local-ci` lief jede dieser Klassen viermal; einziger Ausfall war der beschriebene Umgebungseffekt in
`_InvalidFiles.TooNew…`.

### Zwischenstand `review-versions/0.1.19_2026-10-12/`

- **Herkunft:**
  - erzeugt um 17:26, per `.gitignore` ausgeschlossen
  - `FileVersion` `0.1.19.0`, `ProductVersion` `0.1.0+4e286740…`
  - Der Code entspricht HEAD.
- **`CHANGELOG.md`:** UTF-8 mit BOM, wortgleich mit dem `changes.log`-Eintrag 0.1.19.
- **Start:**
  - Testmodus mit `TANKATLAS_TEST_DATA_PATH` im Scratchpad und `TANKATLAS_TEST_WINDOW=offscreen`, ohne
    Preis-Endpunkt; Schlüsselvariablen entfernt.
  - Für die Importprüfungen zusätzlich `TANKATLAS_TEST_BACKUP_IMPORT` auf eine austauschbare Datei.
  - Bedienung nur über UI-Automation-Muster (`Invoke`, `Toggle`, `SetValue`), keine Maus- oder Tastatureingaben.
- **Ablauf mit einer 0.1.18-Datenbank:**
  1. 0.1.18 mit leerem Datenverzeichnis gestartet (7 Migrationen bis `AddTankbookEntryDetails`) und geschlossen.
  2. Synthetische Daten per SQL (`tools/seed15.py`): Einstellungen, 3 Spritsorten, 2 Stationen, 20 Preisstände, Gruppe
     mit Beschreibung, 2 Favoriten mit Notiz/Priorität, 2 Fahrzeuge, 3 Vorgänge, davon einer mit PNG-Beleg.
  3. 0.1.19 gestartet, „Sicherung erstellen“ → „Sicherung erstellt.“, Eintrag „12.10.2026 17:39 / 5,5 KB“.
     - Archiv: genau `database/tankatlas.db` (Deflate), `receipts/<guid>.png` (unkomprimiert), `manifest.json`.
     - Manifest: `format`, `schemaVersion` 1, `databaseMigration` `20261011170920_AddTankbookEntryDetails`,
       Zählwerte 2/3/1/2/2/20/1, alle SHA-256 korrekt.
     - Archiv-Datenbank ≙ Ausgangsdaten in allen Tabellen.
  4. Daten über die Oberfläche geändert:
     - „In der Nähe“ aus und Sortierung „Preis“.
     - „Golf Abnahme“ samt 2 Vorgängen gelöscht. Die Löschrückfrage nannte den Sicherungshinweis; das Foto wurde entfernt.
  5. „Wiederherstellen“ → Rückfrage mit Zählwerten → „Ja, wiederherstellen“ → „Sicherung wiederhergestellt. …“.
     - Schalter wieder an, „Golf Abnahme“ wieder da.
     - Datenbank ≙ Ausgangsdaten, Foto bytegleich.
     - Kein Journal, keine Rücksicherung, kein Staging.
  6. „Teilen“ → Kopie in `test-shared/`.
  7. Neustart → Daten unverändert.
- **Ältere Sicherung:**
  - 0.1.14 angelegt (4 Migrationen bis `AddFavorites`) und befüllt.
  - Archiv mit Manifest von Hand erzeugt und über „Sicherung aus Datei einspielen“ importiert → „Die Datei ist eine gültige
    Sicherung …“ → Rückfrage → wiederhergestellt.
  - 7 Migrationen; Gruppen, Favoriten, Stationen, Preisstände und Einstellungen unverändert; `ShowNearby` = 0.
  - Das alte Fotoverzeichnis wurde wie erwartet ersetzt (die Sicherung hatte keine Fotos).

**Präparierte Dateien** (Import über die Oberfläche; Ausgangszustand jeweils Fingerabdruck `1f8542e6…`):

| Datei | Meldung | Liste | Daten |
|---|---|---|---|
| `notzip.zip` (Text) | „keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“ | unverändert | unverändert |
| `empty.zip` (0 Byte) | dito | unverändert | unverändert |
| `zipslip-dotdot.zip` (`../evil.txt` im Manifest) | dito | unverändert | unverändert, keine `evil.txt` |
| `zipslip-receipts.zip` (`receipts/../../…png`) | dito | unverändert | unverändert |
| `bomb-lying.zip` (64 MiB, Größe gefälscht) | dito | unverändert | unverändert |
| `badsha.zip` | dito | unverändert | unverändert |
| `toonew-schema.zip` (`schemaVersion` 99) | „… aus einer neueren Version von Tankatlas (abnahme). Bitte aktualisiere die App. …“ | unverändert | unverändert |
| `toonew-migration.zip` (`20991231…`) | dito | unverändert | unverändert |
| `unknown-older-migration.zip` (`20200101…`) | „keine gültige …“ | unverändert | unverändert |
| `garbage-db.zip` (Zufallsbytes, Prüfsummen stimmen) | Import: „gültige Sicherung“; Wiederherstellen: „keine gültige …“ | **aufgenommen** | unverändert |
| `older.zip` (0.1.14) | wiederhergestellt | aufgenommen | migriert, korrekt |
| `trigger.zip` | wiederhergestellt | aufgenommen | **Trigger aktiv; nach „Fahrzeug anlegen“ alle Vorgänge gelöscht** (Abweichung 1) |
| `foreign-schema.zip` | wiederhergestellt | aufgenommen | **Bestand ersetzt, Tankbuch „konnten nicht geladen werden“** (Abweichung 1) |

Danach stellte die Wiederherstellung der regulären Sicherung den Ausgangszustand wieder her (Fingerabdruck `1f8542e6…`).

- **Absturz-Wiederaufnahme:**
  - Zustand „nach `database-placed`“ von Hand erzeugt: Journal, alte Datenbank und Fotos in `restore-rollback/`, neue
    Datenbank (0.1.14-Stand) am Platz, Staging-Rest.
  - Start von 0.1.19: Tankbuch zeigt „Golf Abnahme“ und „Zoe Abnahme“, Datenbank ≙ Ausgangsstand, Foto zurück, Journal,
    Rücksicherung und Staging entfernt.
- **Aufräumen:** Die eigenen Prozesse (PIDs 35780, 9628, 32612, 38736, 11080, 31132, 39612) wurden per PID beendet.
  Danach lief kein `Tankradar.MAUI`- und kein `testhost`-Prozess mehr.

### Kleinigkeiten und Empfehlungen (ohne Einfluss auf die Erfüllung)

- **Datenbank läuft nach einer Wiederherstellung dauerhaft ohne WAL:**
  - `PrepareDatabaseAsync` setzt in der Staging-Datenbank `journal_mode=DELETE`; EF Core setzt WAL nur beim Anlegen.
  - Nachweis: Kopfbytes 18/19 der wiederhergestellten Datenbank sind 1/1, bei einer regulären Installation 2/2.
  - Funktional unkritisch, aber eine dauerhafte Abweichung von der Normalkonfiguration: Leser blockieren Schreiber, und
    jeder Schreibvorgang braucht mehr Synchronisierungen.
  - Empfehlung: Nach dem Austausch einmal `PRAGMA journal_mode=WAL` ausführen.
- **Gelöschte Inhalte bleiben in der Sicherungsdatei lesbar:**
  - Das Online-Backup kopiert Seiten unverändert, also auch freigegebene Bereiche.
  - Nachweis: Fahrzeug „Golf Abnahme“ samt Vorgängen in der App gelöscht, danach gesichert. Die Archiv-Datenbank enthält
    keine Zeile mehr, aber noch die Zeichenketten „Golf Abnahme“, „Erster Vollvorgang“ (Notiz) und den Fotodateinamen.
  - Relevant, wenn eine Sicherung geteilt wird (unverschlüsselt).
  - Empfehlung: Den Schnappschuss mit `VACUUM INTO` statt `BackupDatabase` erzeugen; das ist ebenfalls konsistent und
    kompakt. Alternativ `PRAGMA secure_delete=ON`.
- **iCloud-Gerätebackup:**
  - `backups/` liegt unter `FileSystem.AppDataDirectory` (iOS: `Library`). Ist das iCloud-Backup des Geräts aktiv,
    überträgt iOS die ZIP-Dateien dorthin, ebenso wie bisher schon die Datenbank.
  - Das ist keine Übertragung durch die App. Wörtlich genommen kollidiert es aber mit „nicht automatisch … an eine Cloud“.
  - Empfehlung: `NSURLIsExcludedFromBackupKey` auf `backups/` setzen (spart zudem doppelten Platz) oder es in der Hilfe
    erwähnen.
- **Import-Meldung und Zählwerte:**
  - Der Import prüft nur Struktur und Prüfsummen. Eine Datei mit unbrauchbarer Datenbank kommt mit „Die Datei ist eine
    gültige Sicherung …“ in die Liste und scheitert erst beim Wiederherstellen.
  - Die Zählwerte der Rückfrage stammen ungeprüft aus dem Manifest; bei präparierten Dateien zeigte sie „0 Fahrzeuge“ trotz
    Inhalt.
  - Mit der empfohlenen Prüfung beim Import aus Abweichung 1 erledigt sich beides; die Zählwerte ließen sich dabei aus der
    Staging-Datenbank lesen.
- **App-Version unter Windows „0.1.0.1“:**
  - Manifest und Rückfrage nennen am Zwischenstand „Version 0.1.0.1“. `AppInfo.VersionString` setzt sich aus
    `ApplicationDisplayVersion` und `ApplicationVersion` der `csproj` zusammen; `create-review-version.ps1` setzt nur
    `-p:Version`.
  - Unter iOS setzt die CI `ApplicationDisplayVersion` richtig.
  - Empfehlung: Für Windows-Stände `ApplicationDisplayVersion` mitgeben.
- **Grenzen nur beim Lesen:** Die Erstellung prüft die Grenzen nicht, die die Wiederherstellung verlangt (Datenbank 1 GiB,
  Summe 2 GiB, 20.000 Einträge). Fotos bis 20 MiB werden unverkleinert gespeichert. Eine sehr große Sicherung würde
  erstellt und später als ungültig abgelehnt; das ist bei realistischen Datenmengen fern, aber eine Warnung beim Erstellen
  wäre folgerichtig.
- **Reihenfolge in `RestoreSwap.Undo`:**
  - Undo gibt erst `-wal`/`-shm` und dann die Hauptdatei zurück. Bricht ein Undo genau dazwischen ab und lag eine `-wal`
    in der Rücksicherung, löscht der nächste Undo (`removeNewDatabase`) die bereits zurückgegebene `-wal`.
  - Das ist ein Doppelfehler, praktisch kaum erreichbar.
  - Empfehlung: Die Hauptdatei zuerst zurückgeben, oder nur Dateien löschen, die es in der Rücksicherung nicht gibt.
- **Kachelzahl und iOS-Textgröße:** Die Zahl ist `NoWrap` mit fester Stufung nach Zeichenzahl. Bei großer
  Systemschriftgröße (Dynamic Type) könnte eine lange Zahl abgeschnitten statt umbrochen werden. Unter Windows nicht
  prüfbar; bei der Gerätesichtung ansehen.
- **Datenschutzhinweis präzisieren:** „Nicht enthalten … dein Standort“ ist zutreffend. Gespeicherte Stationen und
  Preisstände mit Abrufzeit lassen aber Rückschlüsse zu, in welchen Gegenden wann gesucht wurde. Empfehlung: in Hinweis und
  Hilfe kurz erwähnen, weil geteilte Sicherungen unverschlüsselt sind.
- **Feature-Verzeichnis:** `docs/features/…--schritt-15-sicherung/` (Plan, Reviews, Inventur) und die älteren
  Verzeichnisse liegen unversioniert im Arbeitsverzeichnis. Das Löschen ist Sache des Anwenders.

### Sonstiges

- **Keine Änderungen am Repository:**
  - Keine Commits, keine Pushes und keine Codeänderungen. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert: keine Datei jünger als der Beginn der Abnahme.
    Die Zwischenstände 0.1.14, 0.1.18 und 0.1.19 wurden nur ausgeführt; ihre Daten lagen im Scratchpad (`abn15/`).
  - Testergebnisse liegen in den ausgeschlossenen Ordnern `TestResults/`, `coverage-report/` und `e2e-diagnostics/`.
  - Im Scratchpad (`abn15/`) liegen Werkzeuge, Protokolle, präparierte Archive und Aufnahmen.
- **Keine Systemeinstellungen** (Energie, Erscheinungsbild, Registry) wurden geändert, und es gab keine Maus- oder
  Tastatureingaben. Die Sichtprüfung im hellen Erscheinungsbild entfiel deshalb.
- **Vom Berechtigungssystem abgelehnt** wurden zwei Shell-Befehle:
  - **Anlegen und Ausführen eines Bash-Skripts** (`chmod +x`, `bash imports.sh`): Stattdessen wurde dieselbe Bedienfolge
    als PowerShell-Skript im Scratchpad geschrieben und ausgeführt.
  - **Ein Befehl mit `rm -rf`** zum Vorbereiten des Absturz-Zustands: Stattdessen erzeugt `tools/crashstate.py` den Zustand
    in einem neuen Verzeichnis, ohne etwas zu löschen.

  Weitere Ablehnungen gab es nicht.
