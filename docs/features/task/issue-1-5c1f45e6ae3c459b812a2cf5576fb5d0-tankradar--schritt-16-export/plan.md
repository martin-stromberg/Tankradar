# Umsetzungsplan: Schritt 16 – Export des Tankbuchs als CSV oder PDF

## Übersicht

Auf der Seite „Tank- und Ladevorgänge“ erhält jedes Fahrzeug einen Bereich **Export**: Format (CSV oder PDF) und Zeitraum wählen, Schaltfläche **Exportieren**. Ein neuer `ITankbookExportService` filtert die Vorgänge, berechnet die Kennzahlen mit den vorhandenen Rechnern (ADR 0010), erzeugt die Datei im Zwischenspeicher und übergibt sie über den vorhandenen Teilen-Mechanismus (`IBackupSharer`, im Testmodus Kopie nach `<TANKATLAS_TEST_DATA_PATH>/test-shared`). Das PDF wird von einem eigenen, rein verwalteten PDF-Schreiber ohne neue Abhängigkeit erzeugt. Zusätzlich werden die Härtungen H1 bis H6 aus der Abnahme von Schritt 15a umgesetzt; die Version wird 0.1.21. Keine Schemaänderung, keine Änderung an `Tankradar.MAUI.csproj`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| PDF-Technik | Eigener PDF-Schreiber (`PdfDocumentBuilder`, PDF 1.4, Standardschriften Helvetica und Helvetica-Bold, `WinAnsiEncoding`, A4 hoch, unkomprimierte Content-Streams, Seitenzahlen „Seite x von y“). Kein Zeichnen von Diagrammen. | Beleg im Bestand ([Logik](inventory/logic.md)): `Microsoft.Maui.Graphics 10.0.20` kann PDFs nur lesen/rendern; SkiaSharp ist nicht referenziert. Reines verwaltetes C# ohne Reflection, Generatoren und native Bibliotheken ist unter `MtouchInterpreter=all` und Trimming sicher, vergrößert Build und Paket nicht und braucht keine Lizenzprüfung. Der Bericht (Tabellen und Kennzahlen) braucht nur Text und Linien. Die Standardschriften sind in jedem PDF-Betrachter vorhanden (PDF-Grundschriften, nicht eingebettet). |
| PDF-Alternativen (ADR 0013) | Verworfen: **SkiaSharp** (`SKDocument.CreatePdf`, MIT): neue native Abhängigkeit, auf iOS ohne Beleg, braucht voraussichtlich Änderungen an `MtouchInterpreter`/`TrimmerRootAssembly` (nicht erlaubt), vergrößert den 60-Minuten-iOS-Build. **QuestPDF**: Community-Lizenz mit Umsatzgrenze, keine freie Lizenz im Sinne MIT/Apache/BSD. **PdfSharp/MigraDoc** (MIT): Schriftauflösung und Reflection unter iOS-Interpreter/Trimming unbelegt. **Plattform-APIs** (`UIGraphicsPdfRenderer` iOS, `PdfDocument` Android, unter Windows keine): drei Implementierungen, unter Windows nicht testbar. **`Microsoft.Maui.Graphics`**: kein PDF-Export. **HTML-Druck/WebView**: Systemdialoge, nicht automatisierbar. | Vorgabe: ohne neue Abhängigkeit, sonst frei lizenziert und iOS-tauglich. |
| Bedarf an iOS-Einstellungen | `MtouchInterpreter` und `TrimmerRootAssembly` bleiben unverändert; der Plan braucht keine Änderung. Der eigene Schreiber liegt in `Tankradar.MAUI`, das bereits Trimmer-Root ist. | Vorgabe; wird im ADR festgehalten. Meldung an den Auftraggeber: Bedarf besteht nicht. |
| Schriftbreiten | Statische Breitentabelle der Helvetica-Grundschrift (`PdfFontMetrics`, Zeichen 32 bis 255 in WinAnsi, Hell und Fett) für Ausrichtung, Spaltenkürzung und Zeilenumbruch. | Rechtsbündige Zahlen und feste Spalten brauchen Textbreiten; die Adobe-Core-14-Metriken sind frei verwendbar. Prüfung per Unit-Test an Stichproben. |
| Zeichen außerhalb WinAnsi (PDF) | Ersatz durch „?“; Klammern und Backslash werden maskiert. Umlaute, ß und € sind in WinAnsi enthalten. | Nutzereingaben (Notiz, Name) können beliebige Zeichen enthalten; der Bericht darf nicht scheitern. |
| CSV-Format (ADR 0013) | UTF-8 mit BOM, Trennzeichen Semikolon, Zeilenende CRLF, Dezimalkomma ohne Tausendertrenner, Datum `TT.MM.JJJJ`, Uhrzeit `HH:mm` in eigener Spalte. Spalten: `Datum;Uhrzeit;Tankstelle;Art;Menge;Einheit;Preis pro Einheit (EUR);Gesamtbetrag (EUR);Kilometerstand;Teilbetankung;Strecke (km);Verbrauch;Verbrauchseinheit;Hinweis zum Verbrauch;Notiz`. Einheit ist `Liter` bzw. `kWh`, Verbrauchseinheit `L/100 km` bzw. `kWh/100 km`, Teilbetankung `Ja`/`Nein`, Hinweis `ab Startstand` oder leer. Felder mit `;`, `"`, CR oder LF werden in Anführungszeichen gesetzt (`"` verdoppelt). Textfelder (Tankstelle, Notiz), die mit `=`, `+`, `-`, `@`, Tabulator oder CR beginnen, erhalten ein vorangestelltes `'` (Schutz vor Formeln in Tabellenkalkulationen). Sortierung chronologisch aufsteigend (älteste zuerst). Keine Fahrzeugspalte (Fahrzeug steht im Dateinamen). | Deutsche Excel- und LibreOffice-Einstellungen erwarten Semikolon und Dezimalkomma; der BOM erzwingt UTF-8 (Umlaute). Reine Zahlen ohne Tausendertrenner bleiben als Zahlen erkennbar. Eigene Einheitenspalte erfüllt „Einheiten Liter bzw. kWh“ ohne Text in Zahlenzellen. Aufsteigend ist für Tabellen üblich; das Schema bleibt mit Hybridfahrzeugen (zwei Arten) eindeutig, weil jede Zeile ihre Art trägt. |
| Zeitraum | Auswahlchips **Alle Vorgänge** (Standard), **Dieses Jahr**, **Letztes Jahr**, **Letzte 12 Monate**, **Eigener Zeitraum**; nur bei „Eigener Zeitraum“ erscheinen zwei `DatePicker` (**Von**, **Bis**; beide Tage einschließlich, `Bis` höchstens heute, `Von` höchstens `Bis`). Auflösung der Voreinstellungen in der reinen Klasse `TankbookExportRangeResolver` mit `TimeProvider`. Grenzen: `Von` ab 00:00, `Bis` bis 23:59:59,999 der lokalen Wanduhrzeit (wie ADR 0009). | Wiederverwendung des Chip-Musters der Seite (`FuelKindChip`, `IChoiceOption`, wie Art und Bekannte Tankstellen); Voreinstellungen sind per UIA ohne Datumseingabe bedienbar und testbar; freie Auswahl bleibt über `DatePicker` (wie das Formular der Vorgänge) möglich. Keine Eingabe interner Kennungen. |
| Kennzahlen im Zeitraum | `ConsumptionCalculator` rechnet über die **vollständige** Kette (`GetEntriesAsync`), danach wird auf den Zeitraum gefiltert; `TankbookStatisticsCalculator.Calculate` erhält die gefilterten `TankbookEntryOverview`, `today` ist das Zeitraumende (ohne Zeitraum: heute), damit die Monatsreihe nicht über den Zeitraum hinaus läuft. Kein zweiter Rechenweg. | Vorgabe (ADR 0010: keine zweite Rechenlogik). Intervalle behalten ihre Strecke auch über die Zeitraumgrenze hinweg (ein Vorgang am Zeitraumanfang hat seinen Verbrauch aus dem Vorgänger davor); das ist im ADR und in der Hilfe benannt. Ohne Zeitraum stimmen die PDF-Kennzahlen mit der Auswertung der Seite überein (Gleichheitstest). |
| Tankstellennamen im Export | Exportiert wird `TankbookEntry.StationName` (Anzeigename als Momentaufnahme nach ADR 0009/0012); es erfolgt keine nachträgliche Auflösung über `StationId`. | Der Export zeigt dieselben Namen wie die Liste der Vorgänge; eine spätere Umbenennung ändert gespeicherte Vorgänge bewusst nicht (ADR 0012). |
| Übergabe | Wiederverwendung von `IBackupSharer` (`ShareAsync(path, title)`), `MauiBackupSharer` und `BackupSharerSelector` unverändert (Namen sicherungsbezogen, Schnittstelle allgemein). Dokumentation im ADR 0013; ein Umbenennen in `IFileSharer` bleibt außerhalb dieses Schritts. Testmodus: `TestBackupSharer` kopiert nach `<TANKATLAS_TEST_DATA_PATH>/test-shared`. | Keine Duplikation, kein Eingriff in bestehende Tests. Windows nutzt den System-Teilen-Dialog wie bei Sicherungen; Speichern ist darüber möglich. |
| Dateiablage | Der Dienst schreibt nach `<GetCacheDirectory()>/exports/Tankbuch_<Fahrzeug>_<JJJJ-MM-TT>.csv|pdf`. Vor jedem Export werden alte Dateien im Verzeichnis gelöscht; nach der Übergabe löscht der ViewModel die Datei (`Discard`). Dateiname: Fahrzeugname ohne ungültige Zeichen, höchstens 40 Zeichen, sonst „Fahrzeug“. | Keine dauerhaft liegenbleibenden Exportdaten (Datenschutz, vgl. ADR 0011 zur Unverschlüsselung); Cache darf das System bereinigen. |
| Leeres Ergebnis | Kein Dateiaufbau; Status `NoEntries` mit Text „Im gewählten Zeitraum gibt es keine Vorgänge.“ (ohne Vorgänge insgesamt: „Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.“). Die Schaltfläche bleibt bedienbar, damit die Rückmeldung prüfbar und für Sprachausgabe erreichbar ist. | Verständliche Rückmeldung statt einer leeren Datei; ein Weg im Code für Fahrzeug ohne Einträge und leeren Zeitraum. |
| Erneuerung nach Wiederherstellung (H5) | Neuer Singleton `IRestoreNotifier` (Ereignis `Restored` über `WeakEventManager`, Zähler `Generation`); `BackupViewModel` löst es nach erfolgreicher Wiederherstellung aus (neben dem bisherigen `Restored`-Ereignis), `TankbookEntriesViewModel` und `TankbookViewModel` abonnieren schwach und rufen `LoadAsync` auf. | `OnAppearing()` genügt nach Tabwechsel nicht zuverlässig (Befund aus der Abnahme); schwache Abonnements vermeiden Lecks bei Transient-ViewModels (Muster `ChartGraphicsView`, ADR 0010). Ist das Fahrzeug nach der Wiederherstellung weg, schließt `LoadAsync` die Seite bereits (`VehicleNotFound`). |
| Export-ViewModel | `TankbookExportViewModel` (Transient) wird wie `TankbookStatisticsViewModel` in `TankbookEntriesViewModel` injiziert und von dessen `LoadAsync` mit dem Fahrzeug versorgt. | Folgt dem bestehenden Zuschnitt der Seite. |

## Programmabläufe

### Export auslösen (Happy Path)

1. Anwender öffnet über die Fahrzeugkarte die Seite „Tank- und Ladevorgänge“; `TankbookEntriesViewModel.LoadAsync` setzt über `Export.SetVehicle(vehicle)` das Fahrzeug. Es entsteht noch keine Datei.
2. Der Anwender wählt Format (Chips `TankbookEntries.Export.Format.csv|pdf`, Standard CSV) und Zeitraum (Chips `…Export.Range.all|thisYear|lastYear|last12Months|custom`; bei `custom` erscheinen `…Export.From` und `…Export.To`).
3. **Exportieren** (`TankbookEntries.Export.Start`) startet `TankbookExportViewModel.ExportAsync`: Doppelauslösung wird gesperrt (`IsBusy`), Status zurückgesetzt, Zeitraum mit `TankbookExportRangeResolver` aufgelöst, bei ungültigem Eigenzeitraum Fehlertext am Feld statt Export.
4. `ITankbookExportService.CreateAsync(vehicleId, request)`: Fahrzeug laden (`IVehicleService`), alle Vorgänge laden (`ITankbookEntryService.GetEntriesAsync`), mit `TankbookExportDataBuilder` filtern, aufsteigend ordnen (`TankbookEntryOrder.Ascending`), Kennzahlen mit `TankbookStatisticsCalculator.Calculate` berechnen.
5. Je nach Format `TankbookCsvWriter` oder `TankbookPdfReportWriter` in eine Datei unter `<Cache>/exports/` schreiben (vorher alte Exporte löschen); Ergebnis `TankbookExportResult` (`Status`, `FilePath`, `FileName`, `EntryCount`).
6. Der ViewModel ruft `IBackupSharer.ShareAsync(filePath, TankbookExportTexts.ShareTitle)` auf; abschließend `Discard(filePath)` und Statusmeldung „Export bereit: <Dateiname>.“ (auch angesagt über `IScreenReaderAnnouncer`).
7. Im Testmodus (`TANKATLAS_TEST_DATA_PATH`) liefert `BackupSharerSelector` den `TestBackupSharer`; die Datei liegt danach in `<Testdaten>/test-shared/`.

Beteiligte Klassen: `TankbookExportViewModel`, `TankbookExportService`, `TankbookExportDataBuilder`, `TankbookExportRangeResolver`, `TankbookCsvWriter`, `TankbookPdfReportWriter`, `PdfDocumentBuilder`, `IBackupSharer`.

### Fehler- und Sonderfälle

1. Fahrzeug nicht (mehr) vorhanden: `TankbookExportStatus.VehicleNotFound`, Text „Das Fahrzeug wurde nicht gefunden.“, Seite schließt wie bisher über `LoadAsync`.
2. Keine Vorgänge im Zeitraum bzw. Fahrzeug ohne Vorgänge: `NoEntries`, keine Datei, keine Teilen-Anfrage.
3. Ungültiger Zeitraum (`Von` nach `Bis`, `Bis` in der Zukunft): `InvalidRange`, Text am Feld, kein Export.
4. Fehler beim Schreiben/Teilen: Ausnahmen (außer `OperationCanceledException`) werden abgefangen, nur der Ausnahmetyp wird protokolliert, Text „Der Export konnte nicht erstellt werden.“ bzw. „… nicht weitergegeben werden.“; angefangene Datei wird gelöscht.
5. Abbruch des System-Teilen-Dialogs durch den Anwender ist kein Fehler.

### Zeitraumauflösung

1. `All`: `Von`/`Bis` leer. `ThisYear`: 01.01. des laufenden Jahres bis heute. `LastYear`: 01.01. bis 31.12. des Vorjahres. `Last12Months`: heute minus 12 Monate plus 1 Tag bis heute. `Custom`: Datumsfelder.
2. Filter: `OccurredAt >= Von.Date` und `OccurredAt < Bis.Date.AddDays(1)`.

### Wiederherstellung und offene Vorgangsseite (H5)

1. `BackupViewModel.ConfirmRestoreAsync` meldet Erfolg und ruft `IRestoreNotifier.NotifyRestored()` auf.
2. `TankbookEntriesViewModel` und `TankbookViewModel` laden daraufhin neu (`LoadAsync`); der Export-Bereich übernimmt das Fahrzeug neu und setzt eine alte Statusmeldung zurück.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TankbookExportFormat` | Enum (`Csv`, `Pdf`) | Wahl des Formats. |
| `TankbookExportRangeKind` | Enum (`All`, `ThisYear`, `LastYear`, `Last12Months`, `Custom`) | Art des Zeitraums. |
| `TankbookExportStatus` | Enum (`Exported`, `NoEntries`, `VehicleNotFound`, `InvalidRange`, `Failed`) | Ergebnis des Dienstes. |
| `TankbookExportRequest` | Datenmodell (Record) | `Format`, `From`, `To`. |
| `TankbookExportResult` | Datenmodell (Record) | `Status`, `FilePath`, `FileName`, `EntryCount`. |
| `TankbookExportData` | Datenmodell (Record) | Fahrzeug, gefilterte `TankbookEntryOverview` (aufsteigend), `TankbookStatistics`, Zeitraum, Erstellzeitpunkt. |
| `TankbookExportDataBuilder` | statische Klasse (Transaction Script) | Filtern und Kennzahlen mit den vorhandenen Rechnern. |
| `TankbookExportRangeResolver` | statische Klasse | Voreinstellung zu Von/Bis (`TimeProvider`). |
| `TankbookExportFormatter` | statische Klasse | de-DE-Formate für CSV (ohne Tausendertrenner) und PDF (mit Tausendertrenner), Einheiten, Verbrauchshinweis; nutzt vorhandene Formatierer von `TankbookStatisticsTexts`/`TankbookTexts` wo möglich. |
| `TankbookCsvWriter` | statische Klasse | Erzeugt die CSV-Bytes (BOM, Semikolon, Maskierung, Formelschutz). |
| `TankbookPdfReportWriter` | Klasse | Layout des Berichts (Kopf, Kennzahlen je Art, Monatskosten, Vorgangstabelle, Fußzeile) auf Basis von `PdfDocumentBuilder`. |
| `PdfDocumentBuilder` | Klasse | Minimaler PDF-1.4-Schreiber: Seiten, Text, Linien, Info-Dictionary, xref, Seitenzahlen. |
| `PdfFontMetrics` | statische Klasse | Breitentabellen Helvetica/Helvetica-Bold, Textbreite, Kürzen, Umbruch. |
| `PdfTextEncoder` | statische Klasse | Unicode zu WinAnsi, Ersatzzeichen, Maskierung. |
| `TankbookExportFileNamer` | statische Klasse | Dateiname aus Fahrzeug, Datum, Endung. |
| `ITankbookExportService` | Interface | `CreateAsync(long vehicleId, TankbookExportRequest, CancellationToken)`, `Discard(string path)`. |
| `TankbookExportService` | Dienst (Singleton) | Orchestriert Laden, Filtern, Erzeugen, Ablage und Bereinigung. |
| `TankbookExportViewModel` | ViewModel (Transient) | Chips, Datumsfelder, `ExportCommand`, Status, Sperre, Teilen. |
| `TankbookExportTexts` | Textklasse (`Resources/Texts`) | Alle deutschen Texte des Bereichs und des Berichts. |
| `IRestoreNotifier`, `RestoreNotifier` | Interface, Singleton | Benachrichtigung nach Wiederherstellung (H5). |

## Änderungen an bestehenden Klassen

### `TankbookEntriesViewModel` (ViewModel)
- **Neue Eigenschaften:** `Export` (`TankbookExportViewModel`) – Bereich Export.
- **Geänderte Methoden:** Konstruktor (neue Abhängigkeiten `TankbookExportViewModel`, `IRestoreNotifier`); `LoadAsync` übergibt das Fahrzeug an `Export.SetVehicle`.
- **Neue Event-Handler:** `IRestoreNotifier.Restored` (schwach) → `LoadAsync`.

### `TankbookViewModel` (ViewModel)
- **Neue Event-Handler:** `IRestoreNotifier.Restored` (schwach) → Fahrzeugliste neu laden (H5).

### `BackupViewModel` (ViewModel)
- **Geänderte Methoden:** Konstruktor (`IRestoreNotifier`); nach erfolgreicher Wiederherstellung `NotifyRestored()`.

### `TankbookEntriesPage` (XAML)
- Neuer Abschnitt „Export“ als Karte (`TankbookCard`) zwischen Auswertung und Liste (Überschrift Ebene 2 `SemanticProperties.HeadingLevel`), Chips im Stil `FuelKindChip` mit `SemanticProperties.Hint` (Name = sichtbarer Text, WCAG 2.5.3, ADR 0009 Zusatz 12c), `DatePicker` für Von/Bis (Format `dd.MM.yyyy`, nur bei Eigener Zeitraum sichtbar), Hinweistext zur Unverschlüsselung und Weitergabe, Schaltfläche **Exportieren** (`TankbookButton`), Statuszeile. AutomationIds: `TankbookEntries.Export.Heading`, `.Format.csv`, `.Format.pdf`, `.Range.all`, `.Range.thisYear`, `.Range.lastYear`, `.Range.last12Months`, `.Range.custom`, `.From`, `.To`, `.RangeError`, `.Start`, `.Status`.

### `MauiProgram` (Registrierung)
- Singleton `ITankbookExportService`, `IRestoreNotifier`; Transient `TankbookExportViewModel`.

### Tests und Hilfe zu den Härtungen (kein Produktcode)
- H1 `TankbookStatisticsE2ETests_TileNumbers`, H2 neue Integrationstests, H3/H4 Hilfetexte, H6 Hinweis in der Doku (siehe Umsetzungsreihenfolge).

## Datenbankmigrationen

Keine. Der Export liest vorhandene Tabellen; es entstehen keine neuen Spalten. (Die Muster „Migrationen erhalten Daten“ bleibt unberührt.)

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Eigener Zeitraum `Von` | Höchstens `Bis`. | Text „Das Startdatum liegt nach dem Enddatum.“ (`…Export.RangeError`), kein Export. |
| Eigener Zeitraum `Bis` | Höchstens heute (`DatePicker.MaximumDate`, zusätzlich im ViewModel geprüft). | Text „Das Enddatum liegt in der Zukunft.“ |
| Format | Eine der beiden Optionen gewählt (Standard CSV). | Nicht möglich (Chips mit Vorauswahl). |
| Fahrzeug | Muss existieren. | `VehicleNotFound`. |
| Vorgänge | Mindestens ein Vorgang im Zeitraum. | `NoEntries`, Hinweistext. |

## Konfigurationsänderungen

Keine. Insbesondere keine Änderung an `MtouchInterpreter` und `TrimmerRootAssembly` und kein neues NuGet-Paket; `Tankradar.MAUI.csproj` bleibt unverändert. Ein Bedarf an solchen Änderungen besteht nach diesem Plan nicht. (`coverlet.runsettings`: keine Ausnahmen nötig; PDF-Schreiber und Formatierer sind reine Logik und werden gemessen.)

## Seiteneffekte und Risiken

- **Datenschutz:** Exporte enthalten Fahrdaten, Tankstellen und Notizen unverschlüsselt und verlassen über Teilen/Speichern das Gerät auf Veranlassung des Anwenders. Der Bereich und die Hilfe sagen das; Belegfotos sind nicht enthalten. Protokolle enthalten nur Ausnahmetypen.
- **Zeitraum und Intervalle:** Der Verbrauch des ersten Vorgangs im Zeitraum nutzt den Vorgänger davor; die Kennzahlen im Zeitraum umfassen nur Intervalle, deren Abschluss im Zeitraum liegt. Das ist dokumentiert.
- **Grundschriften im PDF:** Nicht eingebettet; unter allen gängigen Betrachtern verfügbar. Zeichen außerhalb WinAnsi werden ersetzt.
- **iOS/Android-Verhalten des Teilen-Blatts:** wird nur kompiliert und über Testdoppel abgesichert (wie ADR 0011); Sichtprüfung auf einem Gerät bleibt Teil der Abnahme.
- **Cache-Verzeichnis:** Die Teilen-Anfrage kehrt auf iOS/Windows nach Abschluss zurück; ist das nicht der Fall (Android), räumt der nächste Export auf.
- **Seitenaufbau `TankbookEntriesPage`:** Die Seite wird länger; E2E-Tests mit Bildlauf (`EnsureVisible`-Muster) und Kachelzahlen-Test (H1) prüfen das mit.
- **H5:** Der zusätzliche Neuladevorgang während geöffnetem Formular: `Form.Cancel()` wie bisher bei fehlendem Fahrzeug; offene Bearbeitungen gehen nach einer Wiederherstellung verloren (erwartet, die Daten wurden ersetzt).
- **Pre-Commit-Hook `csproj-xmldoc-check.py`:** Alle `public`/`protected`-Member brauchen XML-Dokumentation; bei Eigenschaften die Ausweichform `get { return …; }` statt Ausdruckskörper verwenden, falls der Hook sie nicht erkennt. Hook `enum-coverage-check.py` verlangt Tests für jeden Wert der drei neuen Enums.

## Umsetzungsreihenfolge

1. **Enums und Modelle anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `TankbookExportFormat`, `TankbookExportRangeKind`, `TankbookExportStatus`, `TankbookExportRequest`, `TankbookExportResult`, `TankbookExportData`.
2. **Reine Logik: Zeitraum, Daten, Formatierung**
   - Voraussetzungen: Schritt 1; vorhanden `ConsumptionCalculator`, `TankbookStatisticsCalculator`, `TankbookEntryOrder`.
   - Beschreibung: `TankbookExportRangeResolver`, `TankbookExportDataBuilder`, `TankbookExportFormatter`, `TankbookExportFileNamer` mit Unit-Tests.
3. **CSV-Schreiber**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `TankbookCsvWriter` mit Unit-Tests (Format laut Designentscheidung).
4. **PDF-Grundlage und Bericht**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `PdfTextEncoder`, `PdfFontMetrics`, `PdfDocumentBuilder` mit Strukturtests (Kopfzeile, Seitenbaum, xref-Offsets, `startxref`, `%%EOF`); danach `TankbookPdfReportWriter` (Kopf, Kennzahlen je Art, Monatskosten, Vorgangstabelle mit Seitenumbruch und Kopfzeilenwiederholung, Fußzeile „Seite x von y“). Prüfung per Test-Hilfe `PdfTestReader` (liest Seiten und Textstellen aus den unkomprimierten Streams, kein neues Paket).
5. **Export-Dienst**
   - Voraussetzungen: Schritte 3 und 4; vorhanden `IVehicleService`, `ITankbookEntryService`, `IAppDataPathProvider`, `TimeProvider`.
   - Beschreibung: `ITankbookExportService`, `TankbookExportService` (Ablage unter `<Cache>/exports`, Bereinigung, Fehlerbehandlung), Registrierung in `MauiProgram`; Unit- und Integrationstests.
6. **Neuladen nach Wiederherstellung (H5)**
   - Voraussetzungen: Keine.
   - Beschreibung: `IRestoreNotifier`/`RestoreNotifier`, Aufruf in `BackupViewModel`, schwache Abonnements in `TankbookEntriesViewModel` und `TankbookViewModel`; Unit-Tests.
7. **Texte, ViewModel, Seite**
   - Voraussetzungen: Schritte 5 und 6; vorhanden `IBackupSharer`, `BackupSharerSelector`, `ChoiceOptionBase`, `IScreenReaderAnnouncer`, Stile in `TankbookStyles`.
   - Beschreibung: `TankbookExportTexts`, `TankbookExportViewModel` (Chips über das vorhandene Auswahlmuster), Einbindung in `TankbookEntriesViewModel`, XAML-Abschnitt mit AutomationIds und Semantik; ViewModel-Tests.
8. **E2E-Tests Export**
   - Voraussetzungen: Schritt 7; vorhanden `TankbookE2ETestBase`, `TankbookTestDatabase.AddEntry`, Mock-Preisserver aus `E2ETestBase`.
   - Beschreibung: `TankbookExportE2ETestBase` und Testklassen (siehe Tests); `E2EInputPolicyTests_NoFocus` muss grün bleiben.
9. **Härtungen H1 und H2**
   - Voraussetzungen: Keine.
   - Beschreibung: `TankbookStatisticsE2ETests_TileNumbers` schärfen (nach `EnsureVisible` und Stabilisierung der Position muss das Element vollständig sichtbar sein: Höhe mindestens eine Schriftzeile, `Top >= Fenster.Top`, `Bottom <= Fenster.Bottom`, Messung wiederholt, bis zwei Werte gleich sind). Neue Integrationstests `BackupServiceTests_EmbeddedNul` für Sicherungen mit `'a' || char(0) || <200.000 Zeichen>` in `CustomName`, `Name`, `Brand`, `Note`: Ergebnis ist entweder Ablehnung (`InvalidFile`, Bestand unverändert) oder Annahme mit gekürztem Wert (höchstens Grenzlänge); Bestand außerhalb unverändert, kein Absturz.
10. **Dokumentation und Hinweise (H3, H4, H6)**
    - Voraussetzungen: Schritte 1 bis 9 inhaltlich bekannt.
    - Beschreibung: neues `docs/adr/0013-tankbook-export.md` (CSV-Format, PDF-Technik mit Alternativen und Begründung, Zeitraum und Kennzahlen, Ablage und Bereinigung, Datenschutz, Wiederverwendung von `IBackupSharer`, Tests, Folgen); `docs/adr/0011-local-backup-restore.md` um die Byteprüfung (`length(CAST(col AS BLOB))`) und das NUL-Verhalten ergänzen; Hilfe `docs/help/Tankbuch/{index,beschreibung,ablauf-anwender,ablauf-technisch}.md` um Export erweitern (Formate, Zeitraum, CSV-Eigenschaften, PDF-Inhalt, Datenschutz, Kennzahlen im Zeitraum) und H4 (Hinweis zur Teilladung aus dem Klammersatz in Zeile 29 an die Tabellenzeile/den Absatz zum Schalter **Teilbetankung** verschieben); H3 (`docs/help/Tankstellendetails/beschreibung.md`, Absatz „Eigener Name“: doppelte Aussagen zu Preisabruf und Sicherung zu je einer zusammenführen); `docs/help/index.md` (Tankbuch-Eintrag) und `docs/help/Sicherung/*` (Abgrenzung Export gegen Sicherung, ein Satz); H6 Hinweis zum Start von Zwischenständen ohne `Start-Process` in der Dokumentation der Zwischenstände (`docs/help/ci-cd`, falls dort beschrieben, sonst `README.md`); `README.md` Stand-Zeile; `changes.log` neuer Eintrag „Export des Tankbuchs als CSV oder PDF (Tankradar Schritt 16, Version 0.1.21)“ mit Export, Härtungen und Hinweis „keine neue Abhängigkeit“.
11. **Abschlussprüfungen und Zwischenstand**
    - Voraussetzungen: Schritte 1 bis 10 abgeschlossen.
    - Beschreibung: Format-/Hook-Prüfung (`.githooks`), `scripts/local-ci.ps1` (Unit, Integration, E2E off-screen, iOS-Compile), `scripts/test-ios-deployment.ps1`, danach `scripts/create-review-version.ps1 -Version 0.1.21 -ChangelogFile <Text>` nach `review-versions/0.1.21_<Datum>/` (nicht committen). Prüfung, dass `MtouchInterpreter`/`TrimmerRootAssembly` unverändert und keine Pakete hinzugekommen sind (`git diff` auf `*.csproj`).

## Tests

### Neue Tests

Unit (`src/Tankradar.Tests.Unit/Unit/`, Testklassen je Thema klein, mit `BaseTest`):

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Voreinstellungen, Jahreswechsel, Schaltjahr, Von/Bis-Grenzen | `TankbookExportRangeResolverTests` | Alle Werte von `TankbookExportRangeKind`, `ManualTimeProvider`. |
| Filter inklusive 00:00 und 23:59, Vorgänger vor dem Zeitraum liefert den Verbrauch des ersten Vorgangs, Hybrid, Teilbetankung | `TankbookExportDataBuilderTests_Range` | Zeitraumfilter und Intervalle. |
| Ohne Zeitraum identisch mit `TankbookStatisticsCalculator` (Durchschnitt, Kosten pro 100 km, Monate) | `TankbookExportDataBuilderTests_Statistics` | Keine zweite Rechenlogik. |
| BOM, Kopfzeile, Semikolon, CRLF, Dezimalkomma, Einheiten Liter/kWh, Datum/Uhrzeit, Ja/Nein, „ab Startstand“, Umlaute | `TankbookCsvWriterTests_Format` | CSV-Format. |
| Maskierung von `;`, `"`, Zeilenumbruch; Formelschutz für `=`, `+`, `-`, `@`; Zahlen nicht präfixiert | `TankbookCsvWriterTests_Escaping` | Sicherheit und Lesbarkeit. |
| Leere Eingabe (nur Kopfzeile) und Hybrid mit zwei Arten | `TankbookCsvWriterTests_Empty` | Randfälle. |
| Breiten, Kürzen, Umbruch (Stichprobenwerte Helvetica) | `PdfFontMetricsTests` | Metriken. |
| `ä ö ü ß €`, nicht abbildbare Zeichen, Maskierung `( ) \` | `PdfTextEncoderTests` | Kodierung. |
| Kopf `%PDF-1.4`, Objekte, xref-Offsets zeigen auf `n 0 obj`, `startxref`, `%%EOF`, Seitenanzahl, Info-Dictionary | `PdfDocumentBuilderTests_Structure` | Gültigkeit der Datei. |
| Seitenumbruch bei 200 Vorgängen, Kopfzeilenwiederholung, „Seite x von y“ | `TankbookPdfReportWriterTests_Pagination` | Mehrseitiger Bericht. |
| Kennzahlen (Durchschnitt, Kosten pro 100 km, Monatskosten) und Einträge im Text, Zeitraumangabe, Fahrzeug ohne Verbrauch | `TankbookPdfReportWriterTests_Content` | Inhalt gemäß Anforderung. |
| `PdfTestReader` (Test-Hilfe) | `Support/PdfTestReader.cs` | Liest Objekte, Seiten und Textstellen aus unkomprimierten Streams. |
| Dateiname: Sonderzeichen, Länge, leerer Name | `TankbookExportFileNamerTests` | Dateinamen. |
| Fahrzeug fehlt, keine Vorgänge, leerer Zeitraum, ungültiger Zeitraum, Erfolg CSV/PDF, Bereinigung alter Dateien, Fehlerfall (Schreibfehler) | `TankbookExportServiceTests_*` (mit `StubTankbookEntryService`, `TempDirectory`) | Dienst. |
| Standardwahl, Chips, Eigener Zeitraum zeigt Datumsfelder, Validierung, Sperre bei Doppelauslösung, `ShareAsync` genau einmal mit Pfad und Titel, Fehlertexte, Abbruch, Ansage, `Discard` | `TankbookExportViewModelTests_*` (mit `FakeBackupPlatform`/Fake-Sharer) | ViewModel und „nur auf Veranlassung“ (ohne Befehl kein Dienstaufruf). |
| Benachrichtigung, schwache Abonnements, Neuladen | `RestoreNotifierTests`, `TankbookEntriesViewModelTests_Restore`, `BackupViewModelTests_RestoreNotification` | H5. |
| Texte (Hinweise, Statusmeldungen, de-DE) | `TankbookExportTextsTests_*` | Texte. |
| Enum-Abdeckung | in den obigen Klassen | Hook `enum-coverage-check.py`. |

Integration (`src/Tankradar.Tests.Integration/Integration/`):

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Export aus echter SQLite-Datenbank (migriert): CSV und PDF, Hybrid, Zeitraum, Fahrzeug ohne Vorgänge | `TankbookExportServiceTests_RealDatabase` | Zusammenspiel mit `TankbookEntryService`. |
| NUL-Zeichen in Textfeldern der Sicherung (H2) | `BackupServiceTests_EmbeddedNul` | Ablehnung oder gekürzte Annahme, Bestand unverändert. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookEntriesViewModelTests_*` (Konstruktor, Fakes) | Neue Konstruktorparameter `TankbookExportViewModel`, `IRestoreNotifier`. |
| `TankbookViewModel`-Tests | Neuer Konstruktorparameter `IRestoreNotifier`. |
| `BackupViewModelTestBase` und abgeleitete | Neuer Konstruktorparameter `IRestoreNotifier`. |
| `StubTankbookEntryService` | Keine Änderung erwartet; nur falls der Export neue Aufrufe braucht. |

### E2E-Tests (primärer Funktionsnachweis)

Alle FlaUI-Tests nutzen `TankbookE2ETestBase`/`TankbookE2EDriver`, `SetFieldValue`, `Press` (Invoke), Mock-Server (keine Produktivendpunkte), Testdaten mit `TankbookTestDatabase.AddEntry` und ausschließlich UIA-Muster (kein Fokus, keine Tastatur, keine Maus; `E2EInputPolicyTests_NoFocus`). Neue Basisklasse `TankbookExportE2ETestBase` stellt `SharedDirectory` (`test-shared`), `ExportFiles()`, `ChooseFormat`, `ChooseRange` und `StartExport` bereit.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Seite öffnen und wieder verlassen erzeugt keine Datei; **Exportieren** mit CSV und „Alle Vorgänge“ legt `Tankbuch_*.csv` in `test-shared` ab; Inhalt: BOM, Kopfzeile, Semikolon, Dezimalkomma, Umlaut in Tankstellenname, Einheit „Liter“ bzw. „kWh“, Zeilenzahl | `TankbookExportE2ETests_Csv` | Export nur auf Veranlassung; CSV für deutsche Tabellenkalkulationen; Testmodus ohne Dialog | Nur im laufenden Programm sind Auslösen, Testmodus-Sharer und tatsächliche Datei prüfbar. |
| Pflicht | Format PDF: Datei beginnt mit `%PDF-`, endet mit `%%EOF`, enthält Fahrzeugname, Kennzahlen und Einträge | `TankbookExportE2ETests_Pdf` | PDF-Bericht mit Einträgen und Kennzahlen | Prüft die echte Erzeugung unter dem Windows-Build. |
| Pflicht | Zeitraumfilter: Vorgänge in Vorvorjahr, Vorjahr und laufendem Jahr per `TankbookTestDatabase.AddEntry`; „Letztes Jahr“ und „Dieses Jahr“ exportieren jeweils nur passende Zeilen (CSV), „Eigener Zeitraum“ zeigt die Datumsfelder und prüft die Fehlermeldung bei Von nach Bis | `TankbookExportE2ETests_Range` | Zeitraumfilter | Wahl der Chips und Datumsfelder ist reine UI. |
| Pflicht | Fahrzeug ohne Einträge: **Exportieren** zeigt „… noch keine Vorgänge …“, keine Datei; Zeitraum ohne Treffer zeigt „Im gewählten Zeitraum …“ | `TankbookExportE2ETests_Empty` | Fahrzeuge ohne Einträge | Sichtbare Rückmeldung und Ausbleiben der Datei. |
| Pflicht | Hybridfahrzeug: CSV enthält Zeilen mit „Liter“ und „kWh“ | `TankbookExportE2ETests_Hybrid` | Einheiten Liter bzw. kWh | Zusammenspiel von Erfassung und Export. |
| Pflicht | Wiederherstellung bei geöffneter Vorgangsseite (Vorgang nach Sicherung ergänzen, in Optionen wiederherstellen, zurück zur Vorgangsseite): Liste und Auswertung zeigen den wiederhergestellten Stand ohne erneutes Öffnen | `TankbookEntriesE2ETests_RestoreReload` (Basis `BackupE2ETestBase`) | H5 | Das Verhalten hängt von Navigation und Seitenlebenszyklus ab. |
| Pflicht | Chips und Schaltfläche haben UIA-Namen gleich dem sichtbaren Text (Label in Name), Statusmeldung erreichbar | `TankbookExportE2ETests_Accessibility` | Barrierefreiheit wie ADR 0009 | Nur UIA liefert die tatsächlichen Namen. |

Bestehende E2E-Tests, die angepasst werden:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookStatisticsE2ETests_TileNumbers` | H1: volle Elementhöhe prüfen (siehe Schritt 9). |
| Seiten-Tests mit Bildlauf (`TankbookEntriesE2ETests_*`, `TankbookStatisticsE2ETests_*`) | Keine Änderung erwartet; wegen der längeren Seite im Gesamtlauf mitprüfen. |

## Offene Punkte

Keine.
