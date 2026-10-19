# Abnahmeprüfung – Entwicklungsschritt 16 (Export des Tankbuchs als CSV oder PDF)

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Geprüft wurde der Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-16-export` (HEAD `da52732`) gegen den Projekt-Basisbranch (`39f6d7c`). Der Zwischenstand `review-versions/0.1.21_2026-10-19` ist aus `bdb08af` gebaut; `da52732` löscht nur das Feature-Verzeichnis unter `docs/features/`, der Code ist identisch.

Der Export ist fachlich vollständig umgesetzt und am Zwischenstand praktisch bestätigt:

- **CSV:** Die Datei ist in deutschsprachigen Tabellenkalkulationen korrekt lesbar. Geprüft wurde mit einem LibreOffice-Import (Deutsch, Semikolon, UTF-8, Formelauswertung eingeschaltet):
  - Datum und Uhrzeit sind Datums- und Zeitwerte, Mengen, Preise, Beträge, Kilometer und Verbrauch sind Zahlen.
  - Umlaute, kyrillische Zeichen, Emoji und CJK bleiben erhalten.
  - Semikolon, Anführungszeichen und Zeilenumbruch in Freitext bleiben jeweils in einer Zelle.
  - Es entsteht keine einzige Formelzelle.
  - Einheiten (Liter/kWh) und Art (Kraftstoff/Strom) stehen in eigenen Spalten; der Hinweis „ab Startstand“ ist gesetzt.
- **Zeitraum:**
  - Die Grenzen stimmen minutengenau, auch am Jahreswechsel (`31.12. 23:59` und `01.01. 00:00`).
  - „Letzte 12 Monate“ beginnt am 20.10.2025: `19.10.2025 23:59` ist nicht enthalten, `20.10.2025 00:00` schon.
  - Der Verbrauch wird über die ganze Kette gerechnet; der eigene Zeitraum filtert richtig.
- **PDF:**
  - Die Struktur ist gültig, unabhängig geprüft: eigene Prüfung von xref, Objekten und Längen, Xpdf `pdftotext` ohne Fehlermeldung, Darstellung über den PDF-Import von LibreOffice.
  - Seitenumbruch mit wiederholter Kopfzeile funktioniert; alle 120 Einträge erscheinen genau einmal.
  - Die Kennzahlen sind bei fünf Fahrzeugen (darunter ein Hybrid) identisch mit der Auswertung in der App.
- **Leere Ergebnisse:** Es erscheint eine Meldung, und es entsteht keine Datei.
- **Ablage:** Ausweichname `_2` bei gesperrter Datei, Aufräumen beim nächsten Export.
- **H5 Neuladen nach Wiederherstellung:** Eine geöffnete Vorgangsseite zeigt nach der Wiederherstellung den gesicherten Stand.
- **Version:** 0.1.21.1, im Manifest der Sicherung und in der Rückfrage.

Nicht vollständig erfüllt sind zwei kleine, schnell behebbare Punkte (Details unter „Abweichungen“):

1. **PDF:** Die Zeile „Strecke der berechneten Intervalle“ wird in jedem Bericht mit berechnetem Verbrauch vom Wert überdruckt und ist unleserlich.
2. **Wiederherstellung:** Fehlt das Fahrzeug der geöffneten Vorgangsseite in der Sicherung, schließt die Seite nicht, anders als in ADR 0013 beschrieben. Es bleibt eine Seite mit „Das Fahrzeug gibt es nicht mehr.“ stehen, deren Schaltflächen ohne Rückmeldung nichts tun.

**Prüfläufe (selbst ausgeführt, `FUEL_PRICE_API_KEY` per `env -u` entfernt, nie gelesen):**

| Prüfung | Ergebnis |
|---|---|
| `scripts/local-ci.ps1` vollständig, E2E off-screen (19.10., 20:00–20:14) | Exit 0, alle 15 Schritte OK bzw. wie vorgesehen übersprungen (Windows-Paket). Unit 1487/1487, Integration 254/254, **E2E 123/123** (11 min 27 s), Zeilenabdeckung 93,8 %, Build mit Warnungen als Fehler 0 Warnungen, iOS-Compile OK (nur `MAUI1001`), keine anfälligen Pakete. Workloads vorhanden, keine `Include*Target`-Anpassung nötig. `DiagnosticsCaptureE2ETests` grün, „Screenshot einfarbig“ trat nicht auf. |
| Neue bzw. geänderte E2E-Klassen 3× (off-screen, 19.10., 23:01–23:06) | Alle 8 Klassen (`TankbookExportE2ETests_Accessibility`, `_Csv`, `_Empty`, `_Hybrid`, `_Pdf`, `_Range`, `TankbookEntriesE2ETests_RestoreReload`, `TankbookStatisticsE2ETests_TileNumbers`) mit 10 Tests: **3 × 10/10** (je etwa 1 min 25 s), keine Diagnosedaten. Zusammen mit dem Gesamtlauf liefen sie 4×. |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push (Refs auf stdin, Basis `39f6d7c`, 23:06–23:07) | Exit 0: Commits 9/9, verbotene Muster (970 Dateien), Format, XML-Doku (720), Übersetzung (keine `.resx`), Enums, Platzhalter (716), Build 0 Warnungen, Unit 1487/1487, Integration 254/254. E2E ist im Hook standardmäßig aus. |
| Conventional Commits | 9/9 (`39f6d7c..da52732`, gleich dem Bereich gegenüber `origin/task/…-tankradar`). `main..HEAD` (197) enthält GitHub-Merge-Commits aus dem Basisbranch (Hinweis 9). |

## Abweichungen

### 1. PDF: Kennzahl „Strecke der berechneten Intervalle“ überdruckt (niedrig, sichtbar in jedem Bericht)

- **Befund:** `TankbookPdfReportWriter.WriteLabeled` setzt die Beschriftung fett bei x = 40 pt und den Wert bei x = 160 pt (`LabelWidth` = 120 pt).
  - „Strecke der berechneten Intervalle“ ist in Helvetica-Bold 10 pt 163,4 pt breit und reicht damit bis x ≈ 203 pt.
  - Der Wert („2.900 km“) beginnt auf derselben Grundlinie bei 160 pt; beide Texte überlagern sich auf etwa 43 pt.
  - Belegt im Inhaltsstrom: `40 574.89 Td (Strecke der berechneten Intervalle)` und `160 574.89 Td (2.900 km)`.
  - Sichtbar in der Darstellung durch LibreOffice: „Strecke der berechneten [2.900 km überdruckt]“.
  - Xpdf liest die Zeile als „Strecke der berechneten I2n.9te0r0vaklmle“.
- **Betroffen:** Jeder Bericht, in dem eine Art berechnete Intervalle hat. Bei Hybridfahrzeugen erscheint die Zeile zweimal (Kraftstoff und Strom). Alle anderen Beschriftungen sind kürzer als 120 pt (längste: „Vorgänge im Bericht“ mit 97,8 pt).
- **Umsetzbar:**
  - Beschriftung kürzen (z. B. „Strecke (berechnet)“) oder die Wertspalte nach der breitesten Beschriftung bemessen (`PdfFontMetrics.Measure`); alternativ Beschriftungen auf die Spaltenbreite umbrechen.
  - Unit-Test ergänzen, der für alle Zeilen mit Beschriftung und Wert prüft, dass sich die Textbereiche nicht überlappen. `Measure` gibt es bereits; `PdfTestReader` liefert die Positionen.

### 2. Wiederherstellung ohne das Fahrzeug der geöffneten Vorgangsseite: Seite bleibt als tote Seite stehen (niedrig)

- **Vorgabe:** ADR 0013, Abschnitt „Neuladen nach Wiederherstellung“: „Ist das Fahrzeug nach der Wiederherstellung weg, schließt die Seite wie bisher („Das Fahrzeug gibt es nicht mehr“)“.
- **Ablauf am Zwischenstand:**
  1. Sicherung erstellen.
  2. Neues Fahrzeug „Neu nach Sicherung“ anlegen und dessen Vorgangsseite öffnen.
  3. In den Optionen die Sicherung wiederherstellen.
  4. Zurück in den Reiter Tankbuch wechseln.
- **Beobachtung:**
  - Die Vorgangsseite ist weiterhin offen. Sie zeigt „Das Fahrzeug gibt es nicht mehr.“, dazu „Vorgang erfassen“, „Auswertung“ (leer) und den vollständigen Bereich „Export“.
  - „Exportieren“ und „Vorgang erfassen“ tun nichts, ohne Rückmeldung; auch die Sprachausgabe bekommt keine.
  - Die Seite schließt auch nach 5 s nicht. Erst „Zurück“ führt zur korrekt neu geladenen Fahrzeugliste.
  - Die Optionen-Seite blieb unberührt.
- **Ursache (Code):**
  - `TankbookEntriesViewModel.OnRestored` lädt neu, während der Reiter Optionen aktiv ist.
  - `ShellTankbookNavigator.CloseAsync` ruft `Shell.Current.GoToAsync("..")` auf. Das wirkt auf den Navigationsstapel des *aktiven* Reiters (Optionen, Wurzelseite) und schließt die Vorgangsseite im Reiter Tankbuch nicht.
  - Beim Zurückwechseln greift `OnAppearing` offenbar nicht (ADR 0013 beschreibt das selbst als unzuverlässig).
- **Einordnung:**
  - Gegenüber dem Stand vor Schritt 16 ist das eine Verbesserung: Damals zeigte die Seite die alten Vorgänge eines nicht mehr vorhandenen Fahrzeugs.
  - Daten sind nicht gefährdet.
  - Das in ADR 0013 zugesagte Verhalten tritt aber nicht ein, und die Schaltflächen der Seite sind ohne Funktion.
- **Umsetzbar:**
  - Die Seite über den Navigationsstapel der eigenen Seite bzw. des Tankbuch-Reiters schließen statt über `Shell.Current`. Alternativ das Schließen vormerken und beim nächsten Erscheinen bzw. Reiterwechsel ausführen.
  - Solange `Vehicle` `null` ist, „Vorgang erfassen“ und den Bereich „Export“ ausblenden oder deaktivieren.
  - E2E-Fall „Wiederherstellung ohne das geöffnete Fahrzeug“ ergänzen (bisher deckt `TankbookEntriesE2ETests_RestoreReload` nur ein vorhandenes Fahrzeug ab).
  - ADR 0013 an das tatsächliche Verhalten angleichen.

## Hinweise

### 1. Anforderungstabelle

| Anforderung (Plan Schritt 16 und Zusatz aus 15a) | Umsetzung | Nachweis | Bewertung |
|---|---|---|---|
| Export der Vorgänge eines Fahrzeugs, wahlweise mit Zeitraum | Bereich „Export“ auf der Vorgangsseite; Chips Alle, Dieses Jahr, Letztes Jahr, Letzte 12 Monate, Eigener Zeitraum (Von/Bis) | Unit (`TankbookExportRangeResolverTests`, `…DataBuilderTests_Range`), E2E `_Range`, Zwischenstand (alle Voreinstellungen und eigener Zeitraum) | erfüllt |
| CSV zur Weiterverarbeitung, in deutschsprachigen Tabellenkalkulationen korrekt lesbar (Umlaute, Dezimaltrennzeichen, Einheiten Liter/kWh) | UTF-8 mit BOM, `;`, CRLF, Dezimalkomma ohne Tausendertrenner, `TT.MM.JJJJ` und `HH:mm` getrennt, Einheit und Verbrauchseinheit in eigenen Spalten, Art je Zeile | Unit `TankbookCsvWriterTests_*`, E2E `_Csv`/`_Hybrid`, LibreOffice-Import (Hinweis 2) | erfüllt |
| PDF-Bericht mit Einträgen und Kennzahlen (Ø Verbrauch, Kosten/100 km, Monatskosten) | eigener PDF-1.4-Schreiber, Kennzahlen aus `TankbookStatisticsCalculator` | Unit (Struktur, Inhalt, Seitenumbruch), E2E `_Pdf`, Strukturprüfung und Xpdf, Vergleich mit der App (Hinweis 3) | erfüllt bis auf Abweichung 1 |
| Export nur auf Veranlassung, Übergabe über Teilen/Speichern, keine automatische Weitergabe | `IBackupSharer` (Windows-Dialog, iOS-Teilen-Blatt), Testmodus kopiert nach `test-shared/` | Zwischenstand: Öffnen der Seite und Wahl von Format/Zeitraum erzeugen nichts; Code ohne Netzwerkzugriff | erfüllt (iOS-Teilen-Blatt ungeprüft, Hinweis 6) |
| Tests: Inhalt und Format beider Exporte, Zeitraumfilter, Fahrzeug ohne Einträge, FlaUI-Auslösen | Unit 1487 (Abnahme 15a: 1345), Integration 254 (241, u. a. echte SQLite-Datei), E2E 123 (114; 8 Export-Tests und 1 Neuladen-Test neu) | alle grün, siehe Prüfläufe | erfüllt |
| Windows-Zwischenstand | `review-versions/0.1.21_2026-10-19` (EXE 0.1.21.0, Produktversion `0.1.21+bdb08af`, `AppInfo.Version` 0.1.21.1) | gestartet, Upgrade einer 0.1.20-Datenbank, Exporte | erfüllt |
| H1 Kachelzahlen-Test volle Höhe | `EnsureFullyVisible`, `ReadStableBounds`, Höhe ≥ eine Schriftzeile, oben/unten im Fenster | Code, E2E grün | erfüllt, kleine Restlücke (Hinweis 7) |
| H2 Regressionstest NUL + ADR 0011 | `BackupServiceTests_EmbeddedNul` (CustomName, Name, Marke, Notiz; Wiederherstellen und Einspielen), ADR 0011 nennt die Byteprüfung | Integration grün | erfüllt |
| H3 Hilfetexte | doppelter Satz in Tankstellendetails entfernt; Teilladung in Tabelle und Aufzählung der Tankbuch-Hilfe | Diff | erfüllt |
| H4/H5 Neuladen nach Wiederherstellung | `IRestoreNotifier` (schwache Abonnenten), `TankbookEntriesViewModel`/`TankbookViewModel` laden neu | Unit, E2E `_RestoreReload`, Zwischenstand | erfüllt; Sonderfall ohne Fahrzeug siehe Abweichung 2 |
| H6 Abnahmen ohne `Start-Process` | Hinweis in README und `docs/help/ci-cd/lokaler-pruefung.md` | Diff; diese Abnahme startete ohne ShellExecute | erfüllt |
| Keine neue Abhängigkeit, iOS-Einstellungen unverändert | kein Paket, kein Diff an `*.csproj`/`*.props`/Plattformordnern | Diff (keine neue `PackageReference`), Sicherheitsprüfung ohne Funde | erfüllt |

### 2. CSV im Einzelnen (Zwischenstand, LibreOffice 26.2 headless mit eigenem Profil im Scratchpad)

- **Rohformat (alle Dateien):**
  - BOM vorhanden, nur CRLF (das CRLF in der Notiz steht innerhalb der Anführungszeichen), Datei endet mit CRLF.
  - Jede Zeile hat 15 Felder.
  - Sortierung aufsteigend; bei gleicher Uhrzeit nach Kilometerstand.
- **Import mit Filter `59,34,76,1,,1031,…,Formeln auswerten = an`:**
  - Spalten Datum `Zahl[Datum]`, Uhrzeit `Zahl[Zeit]`.
  - Menge, Preis, Betrag, Kilometerstand, Strecke und Verbrauch sind `Zahl` (z. B. 1,799 → 1.799, 62,97 → 62.97).
  - Alle übrigen Spalten sind `Text`. **0 Formelzellen.**
- **Formelschutz:**
  - Ein Hochkomma erhielten: `=HYPERLINK(…)`, `@Shell Express`, ` +49 …` (mit führendem Leerzeichen), `-5 € Rabatt …` und `\tTab vorne`.
  - Eine Notiz mit geschütztem Leerzeichen vor `=2+3` bleibt ungeschützt, LibreOffice wertet sie aber nicht aus (Text). Das ist unkritisch.
  - Das Hochkomma ist in der Zelle sichtbar (bewusster Kompromiss laut ADR 0013).
- **Einheiten bei Hybrid:** Strom-Zeilen haben `kWh` und `kWh/100 km`, Kraftstoff-Zeilen `Liter` und `L/100 km`. Die Teilladung ist `Teilbetankung = Ja`. Der erste Kraftstoffwert trägt „ab Startstand“, Strom bei Hybrid keinen Startwert (wie 15a).
- **Grenze des Tests:**
  - Mit leerem Zeichensatz-Token wertet der headless-Import von LibreOffice das BOM nicht aus (Umlaute verstümmelt). Im interaktiven Dialog muss „Unicode (UTF-8)“ gewählt sein bzw. erkannt werden.
  - Excel ist auf dem Rechner nicht installiert. Laut Format (BOM, `;` als Listentrennzeichen in de-DE, Dezimalkomma, `TT.MM.JJJJ`) sollte Excel die Datei per Doppelklick korrekt öffnen; geprüft ist das nicht.

### 3. PDF im Einzelnen

- **Strukturprüfung** (eigenes Skript, nur Python-Standardbibliothek), alle 10 exportierten Berichte (1 bis 4 Seiten, darunter einer mit Ausweichnamen):
  - Kopf `%PDF-1.4` mit Binärkommentar; Körper rein ASCII.
  - xref-Einträge im 20-Byte-Format, alle Offsets zeigen auf `n 0 obj`; `/Size` entspricht Objektzahl + 1, `startxref` zeigt auf `xref`.
  - Alle `/Length` stimmen; `/Count` entspricht der Zahl der Kids.
  - Alle Textstellen liegen innerhalb der MediaBox. **0 Probleme.**
- **Unabhängiges Werkzeug:** Xpdf `pdftotext` 4.00 (`/mingw64/bin`, `pypdf` ist nicht installiert) liest alle 10 Dateien ohne Fehlermeldung. Seitenzahl und Text im `-raw`-Modus stimmen.
  - Der `-layout`-Modus ordnet Monatsbeträge um eine Zeile versetzt an. Das ist ein Artefakt der Spaltenerkennung: Die Grundlinien sind identisch, und die Darstellung durch LibreOffice ist korrekt.
- **Darstellung:** Die erste Seite wurde mit LibreOffice (PDF-Import, headless) als PNG gerendert, für Golf, Hybrid und Elektro. Die Gestaltung ist sauber und gut lesbar; einzige Ausnahme ist Abweichung 1.
- **Kennzahlen gegen die App** (UIA-Werte der Auswertung, Zeitraum „Alle“):

| Fahrzeug | App Ø / Kosten je 100 km / Monatskosten Okt. | PDF |
|---|---|---|
| Diesel1 | 6,11 / 10,39 / 178,50 € | identisch |
| Golf (Grenzfälle) | 5,89 / 10,34 / 51,87 € | identisch |
| Hybrid1 Kraftstoff / Strom | 4,29 / 7,54 / 52,77 € · 3,70 / 1,54 / 4,68 € | identisch |
| Elektro1 (nur ab Startstand) | 15,00 / 6,00 / 0,00 €, Herkunft „Ab Startstand – … Akku …“ | identisch |
| Vielfahrer (120 Einträge) | 5,55 / 9,40 / 0,00 € | identisch |

- **Zeiträume im PDF:**
  - „Letztes Jahr“: 4 Vorgänge, Ø 5,96 (Intervalle mit Abschluss 2025, ohne das Intervall ab Startstand), Monatsreihe Januar bis Dezember 2025.
  - „Letzte 12 Monate“: Oktober 2025 nur mit dem Vorgang vom 20.10.
  - Der Hinweis zum Zeitraum erscheint nur mit Zeitraum.
- **Seitenumbruch (Vielfahrer, 4 Seiten):**
  - Die Monatsreihe (41 Monate) läuft auf Seite 2 weiter.
  - Die Vorgangstabelle beginnt auf Seite 2; Kopfzeile auf den Seiten 2 bis 4.
  - 120 Einträge, jeder genau einmal; Fußzeile „Seite x von 4“.
- **Zeichen außerhalb WinAnsi:**
  - Wie in ADR 0013 festgelegt, wird je Zeichen ein `?` gesetzt: „Сеть АЗС Лукойл“ → „???? ??? ??????“, 😀 → „?“, 東京 → „??“.
  - €, Umlaute, ß, „–“, „•“ und „Ø“ werden korrekt dargestellt.
  - Lange Namen werden mit „…“ gekürzt („Autohof Großraum Nürnberg…“).

### 4. Praktische Prüfung am Zwischenstand (Testpfad im Scratchpad, off-screen, ohne `Start-Process`)

- **Start:** Wie die E2E-Tests über `Process.Start` mit `UseShellExecute = false`, dazu `TANKATLAS_TEST_DATA_PATH` und `TANKATLAS_TEST_WINDOW=offscreen`.
  - Bedienung nur über UI-Automation-Muster (Invoke, SelectionItem, Value); keine Maus-, Tastatur- oder Fokusaufrufe.
  - 0.1.20 lief mit einem lokalen Preisdienst-Mock (`127.0.0.1:18765`), 0.1.21 ganz ohne Preisdienst.
- **Upgrade 0.1.20 → 0.1.21:**
  - Ausgangspunkt war die 0.1.20-Datenbank der Abnahme 15a. Sie wurde um ein Grenzfall-Fahrzeug `Golf "GTI": Kombi/Ü` ergänzt: Jahreswechsel, Grenze der 12 Monate, Semikolon, Anführungszeichen, CRLF, Formeln, Emoji, CJK, Kyrillisch, 59 Zeichen langer Name, 425 Zeichen lange Notiz.
  - Weiter ergänzt: „Vielfahrer“ (120 Vorgänge), „Ohne Vorgänge“ und weitere Hybrid-Vorgänge mit Teilladung.
  - Die Ergänzung erfolgte per SQL im Schema 0.1.20, also präpariert.
  - In 0.1.20 dann per Oberfläche:
    - eigener Name „Tanke Müller-Süd am Ring“ für Westfalen/Klaus Heinrich
    - ein Vorgang über diesen Chip (mit Notiz `Notiz; mit "Anführung" und Ümlaut`)
    - eine Sicherung
  - Ergebnis 0.1.21:
    - Start ohne Fehler, keine Migration nötig (letzte bleibt `AddStationCustomName`), `integrity_check` ok, WAL.
    - Startseite „Tanke Müller-Süd am Ring / Klaus Heinrich“.
    - Der Vorgang trägt den gespeicherten Anzeigenamen (Momentaufnahme laut ADR 0012). Ältere Vorgänge an derselben Station heißen weiter „Klaus Heinrich“.
- **Keine automatische Datei:** Vor dem ersten „Exportieren“ gab es weder `exports/` noch `test-shared/`.
- **Exporte:** 20 Exporte (10 CSV, 10 PDF) über alle Formate und Zeiträume sowie 4 leere Fälle.
  - Fahrzeug ohne Vorgänge, „Alle“: „Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.“
  - Mit Zeitraum: „Im gewählten Zeitraum gibt es keine Vorgänge.“ (bekannter offener Punkt, auch bei „Eigener Zeitraum“ ohne jeden Vorgang).
  - Keine Datei in den leeren Fällen.
- **Statusmeldung und Sprachausgabe:**
  - Erfolg: „Export bereit: Tankbuch_Golf_GTI_Kombi_Ü_2026-10-19.csv.“
  - Hinweis an „Exportieren“, z. B. „Fahrzeug „Diesel1“, PDF, Alle Vorgänge“.
- **Ablage und Bereinigung:**
  - In `<Testdaten>/exports` liegt nach jedem Export nur die letzte Datei.
  - Mit gesperrter Vorversion (eigener `FileStream` mit `FileShare.None`) entsteht `Tankbuch_Elektro1_2026-10-19_2.pdf`, und die Meldung nennt diesen Namen.
  - Nach dem Entsperren löscht der nächste Export beide Dateien.
- **Eigener Zeitraum:**
  - Voreinstellung Von 19.09.2026, Bis 19.10.2026.
  - Hybrid: 4 von 6 Vorgängen. Die Strecke des ersten Vorgangs im Zeitraum (20.09.) zählt zum Vorgänger vom 05.09.
- **H5 Neuladen nach Wiederherstellung:**
  - Sicherung (Rückfrage nennt „Version 0.1.21.1“, Manifest `appVersion` „0.1.21.1“).
  - Bei Diesel1 einen Vorgang ergänzen (Ø 6,54), bei geöffneter Vorgangsseite in den Optionen wiederherstellen und zurückwechseln.
  - Ergebnis: Liste mit 4 statt 5 Vorgängen, Ø 6,11, Kosten 10,39 €. ✓
  - Sonderfall ohne Fahrzeug: siehe Abweichung 2. Die Fahrzeugliste war danach korrekt neu geladen (6 Fahrzeuge, ohne „Neu nach Sicherung“).

### 5. Datenschutz

- **Ablauf:** Der Export entsteht nur über „Exportieren“, ohne Netzwerkzugriff (`Services/Export` ohne HTTP).
  - Protokolliert werden nur Ausnahmetypen.
  - Der Hinweis „unverschlüsselt, keine Belegfotos, verlässt das Gerät nur über Teilen/Speichern“ steht im Bereich und in der Hilfe.
- **Tests:**
  - Die Tests nutzen den Testmodus (`test-shared/`) und keine Produktivendpunkte; die E2E-Export-Tests laufen ohne Preisdienst.
  - Kein Schlüssel im Code oder in den Tests.
- **Ablage:**
  - Die Datei liegt im Zwischenspeicher (`FileSystem.CacheDirectory`, iOS `Library/Caches`, nicht im iCloud-Backup) und bleibt bis zum nächsten Export liegen.
  - Wer nur einmal exportiert, behält eine unverschlüsselte Kopie im App-Zwischenspeicher, bis das System ihn leert. Das ist dokumentiert und vertretbar.
  - Härtung: alte Exporte zusätzlich beim App-Start löschen.

### 6. iOS (Interpreter, Trimming, Kodierung)

- **csproj:** `Tankradar.MAUI.csproj` ist unverändert (`MtouchInterpreter=all`, `TrimmerRootAssembly` unverändert); es gibt keine neue Abhängigkeit. Der iOS-Compile in `local-ci` ist grün.
- **PDF-Schreiber:** reines C# ohne Reflection, ohne Quellgeneratoren und ohne native Bibliotheken; er liegt in der Trimmer-Root-Assembly `Tankradar.MAUI`.
- **Kodierung:**
  - Die WinAnsi-Kodierung ist von Hand abgebildet (`PdfTextEncoder`). Ausgegeben wird nur ASCII über `Encoding.Latin1`, die CSV über `UTF8Encoding`; beide liegen in CoreLib.
  - Es wird **kein** `CodePagesEncodingProvider` bzw. Windows-1252 benötigt; unter iOS wäre dieser nicht ohne Weiteres vorhanden.
  - `CultureInfo.GetCultureInfo("de-DE")` nutzt die App schon an 13 Stellen; `InvariantGlobalization` ist nicht gesetzt.
- **Kein erkennbares Risiko** durch Interpreter oder Trimming. Laufzeit beim Vielfahrer-Bericht (60 KB, 4 Seiten): `Truncate` misst per binärer Suche, auch unter dem Interpreter unkritisch.
- **Dateiname unter iOS (niedrig, Härtung):**
  - `Path.GetInvalidFileNameChars()` liefert unter iOS nur `\0` und `/`.
  - Ein Fahrzeug `Golf "GTI": Kombi` ergäbe unter iOS `Tankbuch_Golf_"GTI":_Kombi_…`, unter Windows `Tankbuch_Golf_GTI_Kombi_…`.
  - Beim Speichern in Cloud- oder Windows-Freigaben können `:` und `"` stören.
  - Empfehlung: eine feste, plattformunabhängige Menge ungültiger Zeichen (Windows-Menge) verwenden; dann gilt auch der Unit-Test für iOS.
- **Ungeprüft** (wie im Auftrag vermerkt):
  - das iOS-Teilen-Blatt, inklusive iPad-Popover ohne `PresentationSourceBounds`, aus Schritt 15 übernommen
  - die Darstellung in Vorschau/Quick Look

### 7. Härtungen (keine Abweichungen)

- **Kachelzahlen-Test (H1), Restlücke:**
  - `EnsureFullyVisible` vergleicht mit dem Fenster, nicht mit dem Sichtbereich des `ScrollViewer`. WinUI beschneidet die UIA-Grenzen auf den sichtbaren Teil; die Prüfung „im Fenster“ ist für ein beschnittenes Element daher immer erfüllt.
  - Eine umgebrochene Zahl, von der zwischen 1,0 und 2,0 Schriftzeilen sichtbar sind, bestünde weiterhin fälschlich.
  - Die Lücke ist gegenüber 15a deutlich kleiner geworden (vorher genügte jede Höhe > 0).
  - Empfehlung: gegen den Sichtbereich des `ScrollViewer` prüfen oder nach einem weiteren kleinen Bildlauf auf unveränderte Höhe prüfen.
- **PDF-Notizen:** Sie werden auf eine Zeile gekürzt (425 Zeichen → bis „Wort15…“). Die CSV enthält den vollen Text. Umbruch auf 2–3 Zeilen wäre leserfreundlicher.
- **Info-Dictionary:** `/Title` wird in WinAnsi-Bytes geschrieben; dort gilt aber PDFDocEncoding. Umlaute sind gleich; €, „–“ und typografische Anführungszeichen im Fahrzeugnamen würden in den Dokumenteigenschaften falsch erscheinen. Abhilfe: UTF-16BE mit BOM.
- **Zeichen außerhalb WinAnsi:**
  - Ein rein kyrillischer oder chinesischer Tankstellenname wird im PDF unlesbar („????“), wie in ADR 0013 festgelegt.
  - Zerlegte Umlaute (NFD) würden zu „u?“; eine Normalisierung auf NFC vor dem Kodieren ist billig.
- **CSV:**
  - Die Spalte „Teilbetankung“ gilt auch für Strom-Zeilen; die App nennt das dort „Teilladung“. Möglich wäre „Teilbetankung/Teilladung“.
  - Excel wandelt Freitext wie „24/7“ oder „3.4.“ in Notizen beim Öffnen per Doppelklick in Datumswerte um; das ist eine bekannte CSV-Grenze.
- **E2E `_Empty`:** Der zweite `WaitForExportStatus` nach „PDF“ ist sofort erfüllt, weil die Meldung gleich lautet und alt stehen bleiben könnte. Abhilfe: vorher auf das Verschwinden der Meldung warten oder die Datei-Abwesenheit allein werten.
- **Sprachausgabe:** Der Hinweis an „Exportieren“ nennt bei „Eigener Zeitraum“ die Daten nicht (z. B. „19.09.2026 bis 19.10.2026“).

### 8. Bewusst offene Punkte (laut Selbstauskunft, bestätigt)

- **Leermeldung:** „Noch keine Vorgänge“ hängt an der Zeitraumart „Alle“. Ein Fahrzeug ohne Vorgänge meldet bei anderem Zeitraum „Im gewählten Zeitraum …“. Das ist vertretbar.
- **Testklasse:** `TankbookExportTextsTests` statt `TankbookExportTextsTests_*` laut Plan; inhaltlich gleich und unerheblich.
- **PDF-Betrachter:**
  - Eine Prüfung in einem echten Betrachter fehlt weiter; Ersatz waren LibreOffice-Darstellung und Xpdf.
  - Eine Sichtprüfung in Edge/Acrobat bzw. iOS Quick Look ist vor der Auslieferung empfehlenswert, vor allem nach Behebung von Abweichung 1.
- **iOS-Teilen-Blatt:** ungeprüft.

### 9. Sonstiges

- **Conventional Commits:**
  - `main..HEAD` meldet 11 GitHub-Merge-Commits aus dem Basisbranch („Merge pull request #9…#11“, „Merge branch 'staging' …“). Sie stammen nicht aus Schritt 16.
  - Ein Push auf den vorhandenen Remote-Projektbranch prüft nur `39f6d7c..HEAD` (9/9 OK).
  - Würde der Schritt-Branch neu auf das Remote gepusht, nähme der Hook `main` als Basis und bräche ab. Das ist bestehendes Verhalten, nicht Teil dieses Schritts.
- **Repository:**
  - Keine Commits, keine Pushes, kein Code geändert; angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` sind unverändert.
  - `TestResults/`, `coverage-report/` und `e2e-diagnostics/` erzeugen `local-ci` und die Testläufe selbst (gitignoriert).
  - Das unversionierte `docs/features/` bestand schon vor der Abnahme.
- **Unterbrechung:** Die Abnahme war vom 19.10., etwa 20:21, bis 22:51 wegen eines Nutzungslimits unterbrochen.
  - Danach liefen keine eigenen Prozesse mehr: App 23596, Mock und Skripte waren beendet; es war nichts zu beenden.
  - Bereits abgeschlossene Läufe (`local-ci`, Exporte) wurden nicht wiederholt. Nur die vier leeren Fälle, deren Ausgabe verloren war, und die Statistikabfrage wurden nachgeholt.
- **Eigene Prozesse, per PID beendet:** App 35984 (0.1.20), App 24472 (0.1.21). Die erste 0.1.21-Instanz 23596 und der Mock waren nach der Unterbrechung nicht mehr aktiv. LibreOffice (headless) beendete sich nach jeder Umwandlung selbst.
  - Die testhost-Prozesse der E2E-Läufe endeten von selbst.
  - Am Ende lief kein eigener `Tankradar.MAUI`-, `testhost`-, Mock- oder `soffice`-Prozess mehr.
- **Keine Systemeinstellungen geändert.**
  - Keine Maus- oder Tastatureingaben, keine Vordergrund-Überwachung.
  - LibreOffice lief headless mit eigenem Benutzerprofil im Scratchpad; das Profil des Anwenders wurde nicht berührt.
- **Vom Berechtigungssystem abgelehnt:** ein Aufruf mit `rm -rf` auf ein noch nicht vorhandenes Scratchpad-Verzeichnis. Er wurde ohne Löschen wiederholt (Kopie in ein neues Verzeichnis); dieselbe Löschung wurde nicht anders versucht.
- **Testdaten, Exporte, Darstellungen und Skripte** liegen im Scratchpad unter `abn16/` (Daten `d20`, `d21`, Exporte `out/`, Texte `txt/`, Bilder `render/`) und `abn16-tools/` (`pdfcheck.py`, `csvcheck.py`, `seed20.py`, UIA-Skripte).
