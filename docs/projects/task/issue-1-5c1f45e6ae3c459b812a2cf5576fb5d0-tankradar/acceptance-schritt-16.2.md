# Abnahmeprüfung – Entwicklungsschritt 16 (Export des Tankbuchs als CSV oder PDF), Runde 2

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Geprüft wurde der Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-16-export` (HEAD `c17f047`) gegen den Projekt-Basisbranch (`39f6d7c`). Grundlage waren die erste Abnahme (`acceptance-schritt-16.1.md`, zwei Abweichungen), die Korrektur-Commits `3b2147b`, `2ea83f0`, `07f5a58` und `c17f047`, der Projektplan (Schritt 16 mit Zusatz aus 15a) sowie ADR 0009 bis 0013. Der Zwischenstand `review-versions/0.1.21_2026-10-19` ist aus `c17f047` gebaut (Produktversion `0.1.21+c17f0472…`, Dateien vom 19.10., 23:59, Commit 23:56).

**Fachlich ist der Schritt abnahmereif.** Beide Abweichungen aus Runde 1 sind behoben und unabhängig nachgeprüft:

1. **PDF-Kennzahlen (Abweichung 1 aus 16.1):**
   - Die Beschriftungsspalte ist jetzt 172 pt breit (163,4 pt, aufgerundet, plus 8 pt Abstand); die Werte beginnen bei x = 212 pt.
   - Eine eigene Prüfung mit den Standard-AFM-Breiten von Helvetica und Helvetica-Bold, unabhängig vom App-Code, fand in 6 exportierten Berichten (1 bis 4 Seiten) **keine Überlappung** und keinen Text über den rechten Rand hinaus. Dieselbe Prüfung findet in den Berichten aus Runde 1 die alte Überlappung von 43,4 pt.
   - Xpdf liest „Strecke der berechneten Intervalle 700 km“ sauber; die LibreOffice-Darstellung ist einwandfrei.
2. **Wiederherstellung ohne das Fahrzeug der offenen Vorgangsseite (Abweichung 2 aus 16.1):**
   - Am Zwischenstand nachgestellt: Nach dem Zurückwechseln in den Reiter Tankbuch ist die Vorgangsseite geschlossen, die Fahrzeugliste ist korrekt neu geladen und es gibt keine Zurück-Schaltfläche; nach 5 s ist der Zustand unverändert.
   - Der neue E2E-Fall war 4 von 4 Mal grün.
   - ADR 0013 beschreibt das tatsächliche Verhalten.

Ebenfalls in Ordnung: die Härtungen (Dateiname plattformunabhängig, PDF-Titel als UTF-16BE mit BOM) und die Version. Es gibt keine Regression bei vorhandenem Fahrzeug, beim Wechsel zwischen Reitern und beim Export nach einer Wiederherstellung.

**Nicht erfüllt ist eine in dieser Runde gemeldete Korrektur** (Details unter „Abweichungen“): Die Stabilisierung des E2E-Tests `SearchE2ETests_LargeResult`, dessen UIA-Timeout den Pre-Release-Lauf vom 19.10. scheitern ließ, ist unwirksam.

- Den Container `Search.Results`, auf den die Suche begrenzt werden soll, gibt es im UI-Automation-Baum nicht. Der Rückfall auf das ganze Fenster greift daher immer und unbemerkt.
- `TransientWait` kann in diesem Test nicht greifen, weil ein einzelner UIA-Timeout dort mindestens 60 s dauert, die Frist aber 20 s beträgt.
- `changes.log` beschreibt die Begrenzung als umgesetzt.

Das ist kein Produktfehler, betrifft aber das Auslieferungs-Gate (grüne E2E-Tests) und ist schnell behebbar.

**Prüfläufe (selbst ausgeführt, `FUEL_PRICE_API_KEY` per `env -u` entfernt, nie gelesen):**

| Prüfung | Ergebnis |
|---|---|
| `scripts/local-ci.ps1` vollständig, E2E off-screen (20.10., 00:03:38–00:17:00) | Exit 0, alle 15 Schritte OK bzw. wie vorgesehen übersprungen (Windows-Paket). Unit **1500/1500** (Runde 1: 1487), Integration **254/254**, **E2E 124/124** (11 min 32 s; Runde 1: 123), Zeilenabdeckung 93,8 %, Build mit Warnungen als Fehler 0 Warnungen, iOS-Compile OK (nur `MAUI1001`), keine anfälligen Pakete. Workloads (android, ios, maccatalyst, maui-windows) vorhanden, **keine** `Include*Target`-Anpassung nötig. Keine E2E-Diagnosedaten. |
| `DiagnosticsCaptureE2ETests` | Beide Tests grün im Gesamtlauf; „Screenshot einfarbig“ trat nicht auf, deshalb keine Einzelwiederholung nötig. |
| Je 3× einzeln (eigener `dotnet test`-Aufruf je Klasse und Lauf, off-screen, 00:17:17–00:19:09) | `SearchE2ETests_LargeResult` 3/3 (8 s, 9 s, 8 s); `TankbookEntriesE2ETests_RestoreReload` mit 2 Tests 3/3 (`Restore_WhileEntriesPageIsOpen…` 10–11 s, `Restore_WithoutOpenVehicle_ClosesEntriesPage` je 6 s); `TankbookEntriesE2ETests_Receipt` 3/3 (6–9 s). Keine Diagnosedaten. Mit dem Gesamtlauf liefen alle Tests 4×. |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push (Refs auf stdin, Basis `39f6d7c`, 00:19:31–00:20:35) | Exit 0: Commits 13/13, verbotene Muster (973 Dateien), Format, XML-Doku (723), Übersetzung (keine `.resx`), Enums, Platzhalter (719), Build 0 Warnungen, Unit 1500/1500, Integration 254/254. E2E ist im Hook standardmäßig aus. |
| Conventional Commits | 13/13 im Bereich `39f6d7c..c17f047`, gleich `origin/task/…-tankradar..HEAD` (dort liegt `39f6d7c`). |

## Abweichungen

### 1. Stabilisierung des Tests `SearchE2ETests_LargeResult` unwirksam, `changes.log` beschreibt sie falsch (niedrig; Testinfrastruktur und Dokumentation, kein Produktfehler)

- **Anlass:**
  - Im Pre-Release-Lauf `39287091864`, Versuch 1 (19.10., 16:56 UTC), scheiterte der Test nach 6 min 11 s.
  - Meldung: „Nach dem Sortierwechsel wird nicht die erste Seite angezeigt.: … nach 3 Versuchen … (UIA-Timeout, 0x80131505)“.
  - Der Test setzt `Automation.TransactionTimeout = 60 s`. Jeder der drei Versuche hing also mindestens 60 s.
- **Befund 1: Der Container `Search.Results` ist im UIA-Baum nicht vorhanden.**
  - Geprüft am Zwischenstand mit dem lokalen Preisdienst-Mock und 6 Treffern:
    - `FindFirst` nach `Search.Results` liefert nichts, weder in der Control- noch in der Raw-Ansicht (`RawViewCondition`).
    - Die Vorfahren eines `Search.Station.Name` sind Pane → Custom → Custom → Pane → Pane → Window, alle ohne AutomationId.
  - Das `VerticalStackLayout` mit `AutomationId="Search.Results"` (`MapPage.xaml`, Zeile 316) erzeugt unter Windows kein Automation-Element.
  - Folge für `ReadResultTexts`: `FindByAutomationId("Search.Results") ?? MainWindow` fällt **immer** auf das ganze Fenster zurück.
    - Je Abfrage läuft zuerst eine erfolglose `FindFirst`-Suche über den ganzen Baum, danach `FindAll` über den ganzen Baum.
    - Die UIA-Last ist damit größer als vorher, nicht kleiner.
  - Der Rückfall verdeckt das still. Der Test bestand deshalb lokal, obwohl die Begrenzung nie wirkte.
- **Befund 2: `TransientWait` greift in diesem Test nicht.**
  - Mit `TransactionTimeout = 60 s` dauert ein UIA-Timeout mindestens 60 s, bei `FindByAutomationId` (3 Versuche) bis zu etwa 3 min.
  - Die Frist von `WaitUntil` beträgt 20 s. Nach dem ersten Timeout ist sie abgelaufen; `Until` wertet die Bedingung nicht erneut aus, und der Test scheitert wie im CI, nur mit anderer Meldung.
  - Zudem misst die Stoppuhr für die 10-s-Grenze den Sortierwechsel einschließlich aller UIA-Aufrufe. Ein einziger UIA-Timeout lässt den Test also ohnehin scheitern.
  - Das ist streng, aber kein falsches Bestehen.
- **Testzweck:** bleibt erhalten.
  - Durch den Rückfall zählt der Test wie bisher im ganzen Fenster: 25, nach „Weitere anzeigen“ 50, nach dem Sortierwechsel wieder 25.
  - Weiter geprüft werden genau eine Listenanfrage und ein Sortierwechsel unter 10 s (UIA-Zeit eingeschlossen).
  - Die 4 lokalen grünen Läufe (8–9 s) sagen über den CI-Fall nichts aus, weil der UIA-Stau lokal nicht auftritt.
- **Dokumentation:**
  - `changes.log` („der Test mit der großen Trefferliste sucht nur noch in der Ergebnisliste“) ist falsch.
  - Ebenso falsch ist der XML-Kommentar von `ReadResultTexts` („Die Suche bleibt auf die Liste begrenzt …“).
- **Umsetzbar:**
  - Rückfall auf `MainWindow` entfernen: Fehlt der Container, soll der Test mit klarer Meldung scheitern, statt still breiter zu suchen.
  - Einen im UIA-Baum sichtbaren Container schaffen und das mit einem UI-Baum-Abzug belegen, oder die Begrenzung samt Behauptung streichen.
  - Damit `TransientWait` wirken kann, für die Wartephasen eine kurze `TransactionTimeout` setzen (z. B. 5–10 s), sodass innerhalb der Frist mehrere Versuche Platz haben. Alternativ dem Test eine eigene, längere Frist geben.
  - Zeitmessung und UIA-Erreichbarkeit trennen. Beispiel: Die Stoppuhr endet bei der ersten erfolgreichen Beobachtung; ein UIA-Timeout scheitert mit eigener Meldung („UI-Automation antwortet nicht“) statt als vermeintlich langsamer Sortierwechsel.
  - `changes.log` und XML-Kommentar an das tatsächliche Verhalten anpassen.
  - Danach mehrere CI-Läufe abwarten, bevor die Stabilisierung als erledigt gilt.

## Hinweise

### 1. Anforderungstabelle

| Anforderung (Plan Schritt 16, Zusatz aus 15a, Abweichungen aus 16.1) | Umsetzung | Nachweis | Bewertung |
|---|---|---|---|
| Export mit Zeitraum, CSV für deutsche Tabellenkalkulationen, PDF mit Einträgen und Kennzahlen, nur auf Veranlassung, Teilen/Speichern | unverändert gegenüber Runde 1 (dort vollständig geprüft); Runde 1 ändert nur PDF-Layout, Titel und Dateinamen | Unit 1500, Integration 254, E2E 124; Zwischenstand: 6 PDF und 1 CSV exportiert, 1 leerer Zeitraum („Im gewählten Zeitraum gibt es keine Vorgänge.“, keine Datei) | erfüllt |
| Abweichung 1 aus 16.1 (PDF-Überdruck) | `LabelWidth` nach breitester Beschriftung + 8 pt; `TankbookPdfReportWriterTests_Layout` (Diesel, Elektro mit Zeitraum, Hybrid mit 80-Zeichen-Namen, Abstandstest) | eigene AFM-Prüfung, Xpdf, LibreOffice-Darstellung (Hinweis 2) | erfüllt |
| Abweichung 2 aus 16.1 (tote Vorgangsseite) | `ShellTankbookNavigator.CloseAsync` mit `RemovePage` im eigenen Reiter, sonst `GoToAsync("..")`; erneuter Versuch beim Erscheinen; `HasVehicle` deaktiviert „Vorgang erfassen“ und blendet „Export“ aus; ADR 0013 | E2E `Restore_WithoutOpenVehicle_ClosesEntriesPage` 4/4, Unit `…_Restore`, Zwischenstand (Hinweis 3) | erfüllt |
| Härtungen Dateiname und PDF-Titel | feste Menge `\0 : " / \ * ? < > \|`; Titel `<FEFF…>` | Unit; Zwischenstand: `Tankbuch_Golf_GTI_Kombi_Ü_2026-10-20.csv`, Titel dekodiert zu `Tankbuch Golf "GTI": Kombi/Ü` | erfüllt |
| Windows-Zwischenstand | `review-versions/0.1.21_2026-10-19` aus `c17f047` | gestartet, Exporte, Wiederherstellung | erfüllt |
| CI-Stabilisierung LargeResult (in dieser Runde gemeldet) | `TransientWait`, `ReadResultTexts` | UIA-Prüfung am Zwischenstand, CI-Log | **nicht wirksam** (Abweichung 1) |
| Zusatz aus 15a | unverändert aus Runde 1 | siehe 16.1 | erfüllt |

### 2. PDF im Einzelnen (Zwischenstand, Exporte vom 20.10.)

- **Exporte:**
  - Golf „GTI“: Kombi/Ü: „Alle“ (2 Seiten) und „Letzte 12 Monate“.
  - Hybrid1, Elektro1, Diesel1 (vor und nach der Wiederherstellung) sowie Vielfahrer (4 Seiten, 60 KB).
- **Strukturprüfung** (Skript aus Runde 1): Kopf, xref im 20-Byte-Format, Offsets, `/Size`, `/Length`, `/Count`, ASCII-Körper, Textstellen in der MediaBox. **0 Probleme** in allen Dateien.
- **Überlappung (neues, unabhängiges Skript `pdfoverlap.py`):**
  - Breiten aus den Standard-AFM-Metriken, nicht aus `PdfFontMetrics`.
  - Gegen die Runde-1-Dateien gegengeprüft: findet dort 43,4 pt Überlappung, bei Hybrid zweimal.
  - Neue Dateien: **0 Überlappungen** auf gleicher Grundlinie, kein Text über 555 pt.
  - „Strecke der berechneten Intervalle“ endet bei 203,4 pt, der Wert beginnt bei 212,0 pt (Abstand 8,6 pt). Alle anderen Beschriftungen enden spätestens bei 137,8 pt.
- **Xpdf `pdftotext` 4.00:**
  - Alle Dateien ohne Fehlermeldung.
  - `-raw` liefert „Strecke der berechneten Intervalle 2.900 km“ (Golf), „700 km“ und „1.000 km“ (Hybrid, Kraftstoff und Strom), „900 km“ (Diesel1) und „70.800 km“ (Vielfahrer).
  - In Runde 1 stand dort „I2n.9te0r0vaklmle“.
- **Darstellung** (LibreOffice 26.2 headless, PDF-Import `draw_pdf_import`, eigenes Profil im Scratchpad): Golf, Hybrid, Elektro und Vielfahrer jeweils Seite 1. Die Kennzahlen-Zeilen sind sauber getrennt und gut lesbar. Der Abstand bei der längsten Beschriftung ist knapp, aber eindeutig.
- **Kennzahlen unverändert gegenüber Runde 1:**
  - Golf 5,89 / 10,34 / 362,86 €
  - Hybrid Kraftstoff 4,29 / 7,54 / 124,77 €
  - Hybrid Strom 3,70 / 1,54 / 19,43 €
- **Wertspalte:** Sie ist um 52 pt schmaler (343 statt 395 pt). Die längste Fahrzeugzeile („Vielfahrer · Diesel · Start 100.000 km · Startverbrauch 6 L/100 km“) passt weiterhin ungekürzt. Längere Werte kürzt `Truncate` mit „…“; das ist laut Test so vorgesehen.
- **Titel:**
  - `/Title <FEFF0054…00DC>`; BOM `FE FF`, Inhalt UTF-16BE `Tankbuch Golf "GTI": Kombi/Ü`.
  - Kein Prüfwerkzeug für Dokumenteigenschaften vorhanden (`pdfinfo` fehlt); die Dekodierung erfolgte per Skript.

### 3. Abweichung 2 aus 16.1 am Zwischenstand (Testpfad im Scratchpad, off-screen, ohne `Start-Process`)

- **Ablauf:**
  1. Sicherung erstellen (20.10.2026 00:24; Rückfrage „Version 0.1.21.1“, 6 Fahrzeuge).
  2. Fahrzeug „Neu nach Sicherung“ anlegen und dessen Vorgangsseite öffnen. Vorher: „Vorgang erfassen“ aktiviert, „Export“ sichtbar, Zurück vorhanden.
  3. In den Optionen die Sicherung wiederherstellen; die Optionen-Seite bleibt stehen.
  4. Zurück in den Reiter Tankbuch.
- **Ergebnis:**
  - Direkt nach dem Wechsel und nach 5 s: Die Vorgangsseite ist geschlossen.
  - Die Fahrzeugliste zeigt „Diesel1 | Elektro1 | Golf "GTI": Kombi/Ü | Hybrid1 | Ohne Vorgänge | Vielfahrer“; eine Zurück-Schaltfläche gibt es nicht. ✓
- **Nicht praktisch beobachtbar:**
  - der Zustand „Vorgang erfassen deaktiviert / Export ausgeblendet“ (die Seite schließt sofort)
  - der erneute Versuch in `OnAppearing`
  - Beides ist per Unit-Test abgedeckt (`Restored_VehicleGone_HidesExportAndBlocksAdd`, `Restored_VehicleGone_CloseFails_RetriesOnAppearing`).
- **ADR 0013:** Der Text (`RemovePage` im eigenen Reiter, `GoToAsync("..")` nur im aktiven Reiter mit Seite oben, erneuter Versuch beim Erscheinen, Dateiname) stimmt mit dem Code überein.

### 4. Regressionssuche

- **Vorgangsseite bei vorhandenem Fahrzeug:**
  - E2E `Restore_WhileEntriesPageIsOpen_ShowsRestoredState` 4/4.
  - Am Zwischenstand bleibt die Seite von Diesel1 nach zwei Wiederherstellungen offen, mit aktivem „Vorgang erfassen“ und sichtbarem „Export“.
- **Navigation zwischen Reitern:**
  - Vorgangsseite Diesel1 → Karte → Optionen → Tankbuch: Die Seite ist weiter offen; „Zurück“ führt zur Fahrzeugliste, danach gibt es keine Zurück-Schaltfläche.
  - Nach dem Schließen per `RemovePage` öffnet und schließt sich die Seite eines anderen Fahrzeugs normal; ein veralteter Stapel bleibt nicht zurück.
- **Export nach Wiederherstellung:**
  - Diesel1 nach der Wiederherstellung: Liste korrekt (4 Vorgänge, „ab Startstand“).
  - Der PDF-Export war erfolgreich (Meldung „Export bereit: Tankbuch_Diesel1_2026-10-20.pdf.“).
  - In `exports/` liegt nur die letzte Datei.
- **Theoretisches Risiko (Härtung, nicht nachstellbar):**
  - `ShellTankbookNavigator.CloseAsync` entfernt *jede* `TankbookEntriesPage` des ersten passenden Reiters, nicht die eigene Seite des aufrufenden ViewModels.
  - `TankbookEntriesViewModel` abonniert `IRestoreNotifier.Restored` schwach und meldet sich nie ab. Lebt das ViewModel einer bereits geschlossenen Seite noch (nicht eingesammelt) und fehlt dessen Fahrzeug nach der Wiederherstellung, schlösse es die gerade offene Seite eines *anderen, vorhandenen* Fahrzeugs.
  - Zwei gezielte Versuche ließen sich nicht nachstellen. Beide Male wurde zuerst die Seite eines neuen Fahrzeugs geöffnet und wieder geschlossen, beim zweiten Mal erst nach mehreren Bedienschritten und 10 s; danach wurde Diesel1 geöffnet und wiederhergestellt. Die Seite von Diesel1 blieb beide Male offen; das alte ViewModel war offenbar schon eingesammelt.
  - Abhilfe: nur die eigene Seite schließen (z. B. Seite oder ViewModel an `CloseAsync` übergeben) oder das Abonnement beim Verlassen der Seite lösen.

### 5. Weitere Teständerungen

- **`TransientWait` / `WaitUntil` allgemein:**
  - Sinnvoll: UIA-Timeouts zählen als „noch nicht erfüllt“ bis zur Frist.
  - Andere Ausnahmen und Assertions werden nicht verschluckt (Unit `TransientWaitTests`, 5 Fälle).
  - Die Meldung nennt den letzten Timeout.
  - Für LargeResult siehe Abweichung 1.
- **Beleg-E2E-Test (`c17f047`):** Gehärtet ist nur die Stelle „Foto auswählen“ (`Properties.Name.ValueOrDefault` in `WaitUntil`).
  - Dasselbe Rennen kann an `Assert.Equal("Beleg anzeigen", WaitForAutomationId("TankbookEntry.Item.ShowReceipt").Name)` auftreten.
  - Ebenso in `WaitUntil(() => FindByAutomationId("TankbookEntry.Item.ShowReceipt")?.Name == "Beleg ausblenden", …)`. Dort ist eine `PropertyNotSupportedException` nicht „vorübergehend“ und beendet das Warten sofort.
  - Empfehlung: auch dort `Properties.Name.ValueOrDefault` verwenden.
  - In dieser Abnahme 4/4 grün.

### 6. Version

- **Dateiversion und Produktversion:** EXE/DLL `FileVersion` 0.1.21.0, `ProductVersion` `0.1.21+c17f04726dff5d7ca44d107688f7172973fbb700`.
- **App:** Rückfrage der Wiederherstellung „Version 0.1.21.1“, Manifest der neuen Sicherung `appVersion` „0.1.21.1“, Migration `20261018094636_AddStationCustomName`; wie in Runde 1.
- **`CHANGELOG.md` des Zwischenstands:** nennt die Nachbesserung Runde 1, mit derselben zu weit gehenden Formulierung „stabileres Warten in den E2E-Tests“.

### 7. Dokumentation

- `docs/help/Sicherung/ablauf-technisch.md`, Zeile 40: „…die übrigen Seiten laden in `OnAppearing`.“ ist für das Tankbuch überholt; dort lädt `IRestoreNotifier` neu bzw. schließt die Seite. `docs/help/Tankbuch/ablauf-technisch.md` ist richtig. Kleinigkeit.
- Die Hilfe zum Dateinamen (`Tankbuch/beschreibung.md`) nennt die feste Zeichenmenge korrekt.

### 8. Sonstiges

- **Repository:**
  - Keine Commits, keine Pushes, kein Code geändert; angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` sind unverändert.
  - `TestResults/` und `coverage-report/` stammen aus `local-ci` (gitignoriert).
  - Das unversionierte `docs/features/` und `acceptance-schritt-16.1.md` bestanden schon vorher.
- **Eigene Prozesse, per PID beendet:**
  - App 25424 (0.1.21 ohne Preisdienst) und App 9148 (0.1.21 mit Mock)
  - Preisdienst-Mock 29124 (`python -I mock_tk.py 18765`, nur 127.0.0.1)
  - testhost-Prozesse und LibreOffice (headless) endeten selbst.
  - Am Ende lief kein eigener `Tankradar.MAUI`-, `testhost`-, Mock- oder `soffice`-Prozess mehr.
  - Fremde `dotnet`-Prozesse (Build-Server seit dem 19.10.) wurden nicht berührt.
- **Bedienung:**
  - Start wie die E2E-Tests über `Process.Start` mit `UseShellExecute = false`, mit `TANKATLAS_TEST_DATA_PATH` (Kopie der Testdaten aus Runde 1) und `TANKATLAS_TEST_WINDOW=offscreen`.
  - Bedienung nur über UI-Automation-Muster: keine Maus-, Tastatur- oder Fokusaufrufe, keine Vordergrund-Überwachung, keine Systemeinstellungen geändert.
- **Kleinere Abläufe ohne Bezug zur App:**
  - Mein UIA-Skript lief zweimal in ein eigenes Rennen (`FindAll` nach `Wait-Id` kurz leer) und wurde mit robuster Zurück-Funktion wiederholt.
  - `soffice.exe` erzeugte headless keine Ausgabe; `soffice.com` mit `--infilter=draw_pdf_import` funktionierte.
- **Vom Berechtigungssystem abgelehnt:** nichts.
- **Ablage im Scratchpad:**
  - Daten, Exporte, Texte, Bilder und Logs: `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn16r2` (`d21`, `out`, `txt`, `render`, `rep`, `localci.log`, `prepush.log`)
  - Skripte: `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn16r2-tools` (`phase-r2.ps1`, `pdfoverlap.py`, `ancestors.ps1`, `e2e-repeat.sh`)
