# ADR 0013: Export des Tankbuchs als CSV oder PDF

## Status

Angenommen (2026-10-19)

## Kontext

Schritt 16 exportiert die Tank- und Ladevorgänge eines Fahrzeugs auf ausdrückliche Veranlassung des Anwenders: als CSV-Datei für
deutschsprachige Tabellenkalkulationen oder als PDF-Bericht mit den Einträgen und den Kennzahlen (Durchschnittsverbrauch, Kosten pro
100 km, Monatskosten). Grundlagen sind die [Vorgänge](0009-tankbook-entries.md), die [Auswertung](0010-tankbook-statistics.md)
(keine zweite Rechenlogik), die [Sicherung](0011-local-backup-restore.md) (Teilen über die Systemfunktion, Testmodus) und der
[Anzeigename](0012-station-display-name.md). Vorgaben: keine neue Abhängigkeit, wenn es sich vermeiden lässt, sonst nur frei lizenziert
(MIT/Apache/BSD) und lauffähig unter iOS mit Interpreter (`MtouchInterpreter=all`) und Trimming; `MtouchInterpreter` und
`TrimmerRootAssembly` in `Tankradar.MAUI.csproj` bleiben unverändert.

## Entscheidungen

### Auslöser, Fahrzeug, Weitergabe

- Der Bereich **Export** steht auf der Seite „Tank- und Ladevorgänge“ des Fahrzeugs (zwischen Auswertung und Liste). Das Fahrzeug ist
  das der geöffneten Seite, gewählt per Namen (ADR 0009); eine zweite Fahrzeugauswahl gibt es nicht. Eine Datei entsteht nur mit
  **Exportieren**: Das Öffnen der Seite, die Wahl von Format oder Zeitraum und das Neuladen erzeugen nichts.
- Die Datei wird über die vorhandene Schnittstelle `IBackupSharer` (`ShareAsync(Pfad, Titel)`) an die Systemfunktion Teilen/Speichern
  übergeben (`MauiBackupSharer`, `BackupSharerSelector`). Der Name ist sicherungsbezogen, die Schnittstelle aber allgemein; ein Umbenennen
  in `IFileSharer` ist bewusst nicht Teil dieses Schritts (kein Eingriff in bestehende Tests). Im Testmodus (`TANKATLAS_TEST_DATA_PATH`)
  kopiert `TestBackupSharer` die Datei nach `<Testdaten>/test-shared/`; es erscheint kein Dialog. Unter Windows öffnet sich der
  Teilen-Dialog des Systems wie bei Sicherungen, Speichern ist darüber möglich. Der Abbruch des Dialogs ist kein Fehler.
- Keine Netzwerkübertragung, keine automatische Weitergabe, keine Hintergrundexporte. Belegfotos sind nicht Teil des Exports.

### CSV-Format

- UTF-8 **mit BOM** (Umlaute in deutschen Excel- und LibreOffice-Einstellungen), Trennzeichen **Semikolon**, Zeilenende **CRLF**,
  Dezimalkomma **ohne Tausendertrenner** (Zahlen bleiben als Zahlen erkennbar), Datum `TT.MM.JJJJ`, Uhrzeit `HH:mm` in eigener Spalte.
- Spalten: `Datum;Uhrzeit;Tankstelle;Art;Menge;Einheit;Preis pro Einheit (EUR);Gesamtbetrag (EUR);Kilometerstand;Teilbetankung;Strecke (km);Verbrauch;Verbrauchseinheit;Hinweis zum Verbrauch;Notiz`.
  Die Einheit steht in einer eigenen Spalte (`Liter` bzw. `kWh`, Verbrauchseinheit `L/100 km` bzw. `kWh/100 km`), nie als Text in einer
  Zahlenzelle. `Teilbetankung` ist `Ja`/`Nein`, `Hinweis zum Verbrauch` `ab Startstand` (ADR 0009/0010) oder leer. Es gibt keine
  Fahrzeugspalte; das Fahrzeug steht im Dateinamen. Jede Zeile trägt ihre Art, damit Hybridfahrzeuge eindeutig bleiben.
- Sortierung chronologisch **aufsteigend** (älteste zuerst, `TankbookEntryOrder.Ascending`), wie in Tabellen üblich.
- **Maskierung:** Felder mit `;`, `"`, CR oder LF stehen in Anführungszeichen, `"` wird verdoppelt.
- **Schutz vor Formelinjektion:** Textfelder (Tankstelle, Notiz), die mit `=`, `+`, `-`, `@`, Tabulator oder CR beginnen, erhalten ein
  vorangestelltes `'`. Der Schutz gilt auch, wenn vor dem Formelzeichen Leerraum steht, den manche Tabellenkalkulationen überspringen. Zahlen (auch negative) werden nie verändert. Die Namen und Notizen stammen vom Anwender oder aus dem
  Preisdienst; eine Tabellenkalkulation würde sie sonst als Formel auswerten.
- Umsetzung: `TankbookCsvWriter` (reine statische Funktion), Formate in `TankbookExportFormatter`.

### PDF-Technik

Gewählt wurde ein **eigener, rein verwalteter PDF-Schreiber ohne neue Abhängigkeit** (`PdfDocumentBuilder`, PDF 1.4, A4 hoch,
Standardschriften Helvetica und Helvetica-Bold ohne Einbettung, `WinAnsiEncoding`, unkomprimierte Inhaltsströme, Info-Dictionary,
Querverweistabelle, Fußzeile „Seite x von y“). Der Bericht (`TankbookPdfReportWriter`) braucht nur Text und Linien: Kopf mit Fahrzeug,
Zeitraum, Erstellungsdatum und Zahl der Vorgänge, Kennzahlen und Monatskosten je Art, danach die Vorgangstabelle mit Seitenumbruch und
wiederholter Kopfzeile. Diagramme werden nicht gezeichnet.

Belege aus dem Bestand: `Microsoft.Maui.Graphics 10.0.20` kann PDFs nur lesen und rendern (`IPdfPage`, `IPdfRenderService`), nicht
schreiben; SkiaSharp ist nicht referenziert (`obj/project.assets.json`); `Tankradar.MAUI.csproj` enthält kein PDF-Paket.

Verworfene Alternativen:

| Alternative | Grund |
|-------------|-------|
| SkiaSharp (`SKDocument.CreatePdf`, MIT) | neue native Abhängigkeit; Lauffähigkeit unter iOS mit Interpreter und Trimming ist nicht belegt und bräuchte voraussichtlich Änderungen an `MtouchInterpreter`/`TrimmerRootAssembly` (untersagt); vergrößert den iOS-Build, der am 60-Minuten-Limit der CI liegt |
| QuestPDF | Community-Lizenz mit Umsatzgrenze, keine freie Lizenz im Sinne MIT/Apache/BSD |
| PdfSharp/MigraDoc (MIT) | Schriftauflösung und Reflection unter dem iOS-Interpreter und Trimming unbelegt |
| Plattform-APIs (`UIGraphicsPdfRenderer`, Android `PdfDocument`) | drei Implementierungen, unter Windows (Testplattform) nicht verfügbar und nicht testbar |
| `Microsoft.Maui.Graphics` | kein PDF-Export |
| HTML-Druck/WebView | Systemdialoge, nicht automatisierbar |

Der eigene Schreiber ist reines C# ohne Reflection, Quellgeneratoren und native Bibliotheken; er liegt in `Tankradar.MAUI`, das bereits
Trimmer-Root ist, und ist damit unter `MtouchInterpreter=all` und Trimming unkritisch. **Ein Bedarf an Änderungen von
`MtouchInterpreter` oder `TrimmerRootAssembly` besteht nicht.** Die Datei ist nicht komprimiert (der Bericht ist klein; die
Struktur bleibt so ohne Fremdwerkzeug prüfbar).

Weitere Festlegungen:

- **Schriftbreiten:** `PdfFontMetrics` enthält die Breiten der Adobe-Core-14-Metriken Helvetica und Helvetica-Bold für die Zeichen 32 bis
  126; Akzentbuchstaben (Latin-1) übernehmen die Breite ihres Grundbuchstabens, Sonderzeichen (`ß`, `€`, Anführungszeichen, Striche u. a.)
  sind einzeln hinterlegt. Die Breiten dienen nur der Ausrichtung, dem Kürzen und dem Umbruch; sie sind an Stichproben getestet.
- **Zeichen außerhalb von WinAnsi** (kyrillische Buchstaben, Emoji u. a. in Namen und Notizen) werden je Zeichen durch `?` ersetzt,
  Klammern und Rückstrich maskiert, Zeichen ab 128 als Oktal-Escape geschrieben, sodass die Datei rein aus ASCII besteht. Umlaute, `ß` und `€` sind enthalten.
- **Grundschriften nicht eingebettet:** Helvetica ist in jedem gängigen Betrachter verfügbar (PDF-Grundschriften).
- Prüfung der Struktur im Test: Kopf `%PDF-1.4`, Objekte, Offsets der Querverweistabelle, `startxref`, `%%EOF`, Seitenbaum, Länge der
  Inhaltsströme; ein Testleser (`PdfTestReader`) liest Seiten und Textstellen ohne Fremdpaket.

### Zeitraum

- Auswahlchips **Alle Vorgänge** (Standard), **Dieses Jahr**, **Letztes Jahr**, **Letzte 12 Monate**, **Eigener Zeitraum**; nur bei
  Letzterem erscheinen zwei Datumsfelder (**Von**, **Bis**; beide Tage einschließlich, `Bis` höchstens heute, `Von` höchstens `Bis`).
  Die Voreinstellungen löst `TankbookExportRangeResolver` mit `TimeProvider` auf; sie sind ohne Datumseingabe bedienbar und testbar.
  Der Filter gilt für lokale Wanduhrzeit wie ADR 0009: `Von` ab 00:00, `Bis` bis 23:59:59,999. „Letzte 12 Monate“ sind heute minus
  zwölf Monate plus ein Tag bis heute.
- Ein ungültiger Zeitraum (Start nach Ende, Ende in der Zukunft) wird am Feld gemeldet; es entsteht keine Datei. Die Prüfung liegt an einer Stelle (`TankbookExportRangeResolver.Validate`, Fehler `TankbookExportRangeError`) und wird von der Oberfläche (genaue Meldung) und vom Dienst (Ergebnis `InvalidRange`) verwendet.

### Kennzahlen im Zeitraum

Die Verbrauchswerte stammen aus der **vollständigen** Kette der Vorgänge (`ConsumptionCalculator` über `GetEntriesAsync`); erst danach
wird auf den Zeitraum gefiltert. Ein Vorgang am Anfang des Zeitraums behält damit seine Strecke zum Vorgänger davor. Die Kennzahlen
berechnet `TankbookStatisticsCalculator.Calculate` aus den gefilterten Vorgängen, `today` ist das Zeitraumende (ohne Zeitraum heute), damit
die Monatsreihe nicht über den Zeitraum hinaus läuft. Sie umfassen daher die Intervalle, deren Abschluss im Zeitraum liegt. Ohne
Zeitraum sind sie identisch mit der Auswertung der Seite (Gleichheitstest); es gibt keine zweite Rechenlogik (ADR 0010). Tankstellennamen
sind der gespeicherte Anzeigename der Vorgänge (Momentaufnahme, ADR 0009/0012), keine nachträgliche Auflösung über die Kennung.

### Ablage und Bereinigung

Der Dienst (`TankbookExportService`) schreibt nach `<Zwischenspeicher>/exports/Tankbuch_<Fahrzeug>_<JJJJ-MM-TT>.csv|pdf`
(`TankbookExportFileNamer`: Fahrzeugname ohne ungültige Zeichen, höchstens 40 Zeichen, sonst „Fahrzeug“). Vor jedem Export werden alte Dateien im Verzeichnis
gelöscht; nach der Übergabe bleibt die Datei bis zum nächsten Export liegen. Sie wird bewusst nicht sofort gelöscht, weil `Share.Default.RequestAsync` auf
manchen Plattformen (Android, möglicherweise auch der Windows-Dialog) zurückkehrt, bevor die Zielanwendung die Datei liest. Das System darf den Zwischenspeicher
ohnehin bereinigen. Eine angefangene Datei wird bei Fehler und bei Abbruch sofort gelöscht (`Discard`). Fehler werden ohne Namen und Werte protokolliert (nur der Ausnahmetyp), eine angefangene Datei wird gelöscht.

### Leeres Ergebnis

Für ein Fahrzeug ohne Vorgänge und für einen Zeitraum ohne Vorgänge entsteht **keine Datei**. Der Bereich meldet „Für dieses Fahrzeug gibt es
noch keine Vorgänge zum Exportieren.“ bzw. „Im gewählten Zeitraum gibt es keine Vorgänge.“. Die Schaltfläche bleibt bedienbar, damit die
Rückmeldung prüfbar und für die Sprachausgabe erreichbar ist (Statusmeldung, wird angesagt).

### Datenschutz

Exporte enthalten Fahrdaten, Tankstellen und Notizen **unverschlüsselt** und verlassen das Gerät nur, wenn der Anwender sie über Teilen/Speichern
weitergibt (vgl. ADR 0011). Der Bereich und die Hilfe sagen das; Belegfotos sind nicht enthalten.

### Neuladen nach Wiederherstellung (H5, aus Schritt 15a)

`IRestoreNotifier` (Singleton, schwache Abonnenten über `WeakEventManager`) meldet eine erfolgreiche Wiederherstellung;
`BackupViewModel` löst es aus, `TankbookEntriesViewModel` und `TankbookViewModel` laden daraufhin neu (`LoadAsync`). `OnAppearing`
genügt nach einem Wechsel des Reiters nicht zuverlässig. Ist das Fahrzeug nach der Wiederherstellung weg, schließt die Seite wie bisher
(„Das Fahrzeug gibt es nicht mehr“); die Rückmeldung des Exports wird zurückgesetzt.

## Abweichungen vom Designentwurf

Für den Bereich **Export** gibt es keinen Entwurf. Er verwendet Karte, Auswahlchips (`FuelKindChip`), Datumsfelder und Schaltflächen der
Seite (ADR 0009, 0010).

## Folgen

- Der Export hat keine Schemaänderung und keine neue Abhängigkeit; `Tankradar.MAUI.csproj` bleibt unverändert.
- Tests: Unit (Schreiber, PDF-Struktur und -Inhalt, Zeitraum, Dienst, ViewModel), Integration (echte SQLite-Datei) und FlaUI-E2E (CSV, PDF,
  Zeitraum einschließlich Kalenderauswahl über `SelectionItemPattern`, leeres Ergebnis, Hybridfahrzeug, Barrierefreiheit, Neuladen nach
  Wiederherstellung). Die E2E-Tests lesen die Datei aus `test-shared/` und verwenden keinen Tastatur-, Maus- oder Fokusaufruf.
- Die Sichtprüfung des PDF-Berichts in einem Betrachter und das Teilen-Blatt unter iOS bleiben Teil der Abnahme (nur kompiliert und über
  Testdoppel abgesichert, wie ADR 0011).
- Eine spätere Umbenennung von `IBackupSharer` in eine allgemeine Dateiweitergabe ist möglich, sobald ein weiterer Anwendungsfall
  (z. B. Schritt 17) sie rechtfertigt.
