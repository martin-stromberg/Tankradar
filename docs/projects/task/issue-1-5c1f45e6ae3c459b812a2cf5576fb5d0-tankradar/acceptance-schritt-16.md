# Abnahmeprüfung – Entwicklungsschritt 16 (Export des Tankbuchs als CSV oder PDF), Runde 3

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Geprüft wurde der Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-16-export` (HEAD `481b0fa`) gegen den Projekt-Basisbranch (`39f6d7c`). Grundlage waren die Berichte `acceptance-schritt-16.1.md` und `acceptance-schritt-16.2.md`, die Korrektur-Commits `3b18fe3`, `c8faf93` und `481b0fa`, das Protokoll des gescheiterten CI-Laufs `39287091864` (Versuch 1) samt Diagnose-Artefakt sowie der Zwischenstand `review-versions/0.1.21_2026-10-20` (Produktversion `0.1.21+481b0fa1…`, gebaut 20.10., 01:12).

- **Abweichung 1 aus 16.2 (LargeResult-Stabilisierung unwirksam) ist behoben.**
  - Der Container-Ansatz ist samt `AutomationId="Search.Results"` und stillem Rückfall entfernt.
  - Der Test liest die Zahl der verbleibenden Treffer aus dem Namen der bereits gefundenen Schaltfläche „Weitere anzeigen (N weitere)“.
  - In den Phasen, in denen die Liste neu aufgebaut wird („Weitere anzeigen“, Sortierwechsel), läuft keine Suche über den ganzen Baum mehr. Genau dort ist der CI-Lauf gescheitert.
  - Die Wartephasen haben eine UIA-Antwortfrist von 5 s bei 90 s Gesamtfrist, sodass `TransientWait` wiederholen kann. Ein UIA-Timeout wird getrennt von einer zu langsamen App gemeldet.
  - `changes.log` und XML-Kommentare beschreiben das tatsächliche Verhalten.
  - Der Ansatz ist plausibel (Hinweis 1). Der Nachweis im CI steht noch aus; das ist eine Restunsicherheit, keine Abweichung.
- **Navigator (Hinweis 4 aus 16.2):** `CloseAsync(vehicleId)` schließt nur Seiten, deren ViewModel dieses Fahrzeug zeigt. Code, Unit-Tests und Regressionen am Zwischenstand sind in Ordnung (Hinweis 2).
- **Beleg-E2E-Test (Hinweis 5 aus 16.2):** Alle drei Namensprüfungen lesen den Namen jetzt über `Properties.Name.ValueOrDefault` (Hinweis 3).
- **Dokumentation (Hinweis 7 aus 16.2):** `docs/help/Sicherung/ablauf-technisch.md` ist aktualisiert.
- **Fachlich unverändert abnahmereif:** Alle Exporte am neuen Zwischenstand stimmen mit Runde 2 überein, die Wiederherstellung ohne das Fahrzeug der offenen Vorgangsseite funktioniert (Hinweis 5).

**Prüfläufe (selbst ausgeführt; `FUEL_PRICE_API_KEY`, `TANKRADAR_E2E_WINDOW` und `TANKATLAS_TEST_WINDOW` per `env -u` entfernt, Schlüssel nie gelesen; E2E immer off-screen):**

| Prüfung | Ergebnis |
|---|---|
| `scripts/local-ci.ps1` vollständig (20.10., 01:13:52–01:26:41) | Exit 0, alle Schritte OK bzw. wie vorgesehen übersprungen (Windows-Paket). Unit **1502/1502** (Runde 2: 1500, +2 `ShellTankbookNavigatorTests_SelectPages`), Integration **254/254**, **E2E 124/124** (11 min 18 s), Zeilenabdeckung 93,7 %, Build mit Warnungen als Fehler 0 Warnungen, iOS-Compile OK (nur `MAUI1001`), keine anfälligen Pakete. Workloads vorhanden, **keine** `Include*Target`-Anpassung nötig. Keine E2E-Diagnosedaten. `SearchE2ETests_LargeResult` im Gesamtlauf 4,4 s (Runde 2: 8–9 s). |
| `DiagnosticsCaptureE2ETests` | Beide Tests im Gesamtlauf grün. „Screenshot einfarbig“ trat nicht auf, eine Einzelwiederholung war daher nicht nötig. |
| Einzelwiederholungen (eigener `dotnet test`-Aufruf je Klasse und Lauf, 01:29:17–01:31:08) | `SearchE2ETests_LargeResult` **5/5** (je 4 s); `TankbookEntriesE2ETests_RestoreReload` **3/3** (beide Tests, 10–11 s bzw. 6–8 s); `TankbookEntriesE2ETests_Receipt` **3/3** (6–7 s). Keine Diagnosedaten. Mit dem Gesamtlauf lief LargeResult 6×, die beiden anderen 4×. |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push (Refs auf stdin, Basis `39f6d7c`, 01:27:45–01:28:55) | Exit 0: Commits 16/16, verbotene Muster (975 Dateien), Format, XML-Doku (724), Übersetzung (keine `.resx`), Enums, Platzhalter (720), Build 0 Warnungen, Unit 1502/1502, Integration 254/254. E2E ist im Hook standardmäßig aus. |
| Conventional Commits | 16/16 im Bereich `39f6d7c..481b0fa`, auch direkt mit `conventional-commits-check.py --base 39f6d7c --end HEAD`. |

## Abweichungen

Keine.

## Hinweise

### 1. `SearchE2ETests_LargeResult` im Einzelnen

**CI-Fehlerbild (Lauf `39287091864`, Versuch 1, `TANKRADAR_E2E_WINDOW=foreground`, Code-Stand `dc54b41`):**

- Nach dem Sortierwechsel lief `ReadTexts` → `FindAll` über das ganze Fenster dreimal in einen Timeout von 60 s. Fehler um 16:54:20 UTC, der Test dauerte insgesamt 6 min 11 s.
- Das Diagnose-Artefakt zeigt: Auch der UI-Baum-Abzug danach scheiterte (`<Element nicht lesbar: COMException>`, 16:54–16:56). Danach folgte „Application failed to exit“.
- Die App beantwortete nach dem Sortierwechsel also minutenlang keine UIA-Anfragen und reagierte nicht auf das Schließen.
- Das passt dazu, dass wiederholte Suchen über den ganzen Baum während des Neuaufbaus (50 → 25 Karten) den UI-Thread auslasten. Eine Blockade der App selbst ist allein aus dem Protokoll aber nicht auszuschließen.

**Messung am Zwischenstand** (eigener Mock mit 306 Stationen, UIA-Client von PowerShell, off-screen):

| Abfrage | Dauer lokal |
|---|---|
| Name der bereits gefundenen Schaltfläche, 20 Abfragen | 17 ms zusammen (< 1 ms je Abfrage) |
| `FindFirst` „Search.ShowMore“ über das ganze Fenster (25 Karten, 425 Elemente) | 444 ms |
| `FindAll` „Search.Station.Name“, 25 Karten | 346–522 ms |
| `FindAll` „Search.Station.Name“, 50 Karten (800 Elemente) | 1 363 ms |
| „Weitere anzeigen“ bis zur neuen Zahl / Sortierwechsel bis zur alten Zahl | 203 ms / 51 ms |

Suchen über den ganzen Baum sind rund drei Größenordnungen teurer als eine Eigenschaftsabfrage. Auf einem 30- bis 50-mal langsameren Runner erreicht eine solche Suche bei 50 Karten die Größenordnung des 60-s-Timeouts. Der Mock erhielt genau eine Listenanfrage.

**Verbleibende Suchen über den ganzen Baum im neuen Test** (wann, wie oft):

1. **Vor der Ergebnisliste** (kleiner Baum, etwa 48 Elemente): Reiterwechsel und `OpenSearch`, je eine Suche nach `Search.Sort.Name` und `Search.Submit`. Unverändert gegenüber vorher.
2. **Phase 1, bis „Weitere anzeigen“ erscheint:**
   - Je Abfrage ein `FindFirst` nach `Search.ShowMore`, also alle ca. 100 ms plus Suchdauer.
   - Meist ist der Baum dabei noch klein. Nur die letzten ein bis zwei Abfragen durchlaufen den Baum mit 25 Karten vollständig, weil die Schaltfläche in der Dokumentreihenfolge zuletzt kommt.
   - Das entspricht der alten Phase 1, die im CI bestanden hat.
3. **„Weitere anzeigen“ und Sortierwechsel:** keine Suche über den Baum. `Invoke` und das Lesen des Namens laufen an bereits gefundenen Elementen. Neu gesucht wird nur, wenn die Schaltfläche nicht mehr lesbar ist (kein Timeout).
4. **Am Ende:** genau ein `FindAll` „Search.Station.Name“ (25 Karten). Es läuft erst, wenn die Schaltfläche wieder N zeigt, die App also ruht. Dafür gilt `TransactionTimeout` 30 s, mit Wiederholung bis 120 s.
   - Das ist dieselbe Art Aufruf wie im CI-Fehler, aber im Ruhezustand und mit 25 statt 50 Karten.
   - In dieser Lage lief der Aufruf im CI schon früher durch (alte Phase 1).
5. **Nur bei einem Fehlschlag:** der UI-Baum-Abzug der Diagnose.

**Bewertung:**

- Das Muster, an dem der CI-Lauf scheiterte (Suche über den ganzen Baum im 100-ms-Takt während des Neuaufbaus), kommt nicht mehr vor. Der Ansatz vermeidet das Fehlerbild deshalb plausibel.
- Blockiert dagegen die App selbst, bleibt der Test rot, jetzt aber mit klarer Meldung (Zahl und Dauer der UIA-Timeouts). Das wäre dann ein echter Befund.
- Bestätigung erst nach mehreren CI-Läufen: HEAD ist noch nicht gepusht, ein CI-Lauf fehlt.

**Zweck des Tests:**

- **Erste Seite:** Erfüllt. Die Schaltfläche nennt N ≥ 275, die Schlusszählung bei derselben Zahl N ergibt 25 Einträge.
- **Zweite Seite:** Erfüllt, aber nur über die Zahl N−25 im Schaltflächennamen (siehe Lücke unten).
- **Sortierwechsel zurück auf Seite 1:** Erfüllt. Die Zahl springt auf N zurück; das geschieht nur, wenn der Befehl mit Seiten-Rücksetzung ausgeführt wurde.
- **Genau eine Listenanfrage:** Erfüllt.
- Eine Regression „alle 300 dargestellt“ fiele weiterhin auf: Dann gäbe es keine Schaltfläche.

**Mögliches falsches Bestehen (wünschenswerte Härtung):**

- Die Zahl in der Schaltfläche stammt aus dem ViewModel (`_allStations.Count - _stations.Count`), nicht aus der dargestellten Liste.
- Die Schlusszählung (25) kann nicht unterscheiden, ob die Liste nach dem Sortierwechsel neu aufgebaut wurde oder seit der ersten Anzeige gar nicht mehr aktualisiert wurde.
- Aktualisiert sich also nur die Liste nicht, das ViewModel aber schon, bliebe der Test grün. Dass nach „Weitere anzeigen“ 50 Einträge dargestellt werden, prüft kein E2E-Test mehr (`Search.ShowMore` kommt nur in diesem Test vor).
- Das Risiko ist klein: Alle Wechsel gehen über denselben `BindableLayout`-Pfad, und `SearchE2ETests_FilterSort` prüft den Neuaufbau nach einem Sortierwechsel mit kleinen Listen.
- Vorschlag ohne zusätzliche Baumsuche: Bei der Schlusszählung zusätzlich die Namen auswerten, z. B. die Reihenfolge nach Namen bzw. den alphabetisch ersten Eintrag. Optional einmal nach „Weitere anzeigen“ im Ruhezustand 50 Einträge zählen.

**30-s-Grenze:**

- Vertretbar: Die Anforderung nennt keine Leistungsvorgabe (vgl. `acceptance-schritt-6.1.md`). Den eigentlichen Leistungsschutz leistet die Seitenbildung, nicht die Zeitgrenze. Lokal dauert der Sortierwechsel etwa 50 ms, der ganze Test 4 s.
- Zu beachten: Die großzügigere Grenze greift genau dann, wenn eine einfache Eigenschaftsabfrage 5 s unbeantwortet blieb, also meist bei blockiertem UI-Thread der App. Gerade in diesem Fall wird die Aussage „zügig“ auf etwa 30 s aufgeweicht.
- Alternative: die 10 s auf die Messzeit abzüglich der UIA-Wartezeit anwenden.

**Kleinigkeiten:**

- `UiaTimeouts` zählt eine Suche mit drei inneren Timeouts (`TransientRetry` in `FindByAutomationId`) als einen Timeout.
- Die Meldung lautet „keine Schaltfläche“ auch dann, wenn die Schaltfläche existiert, ihr Text aber nicht zum Muster passt.
- Der Kommentar „danach sucht der Test nur noch am Rand des Baums“ ist ungenau; Phase 1 durchläuft den ganzen Baum bis zur Schaltfläche.
- `TransactionTimeout` gilt je Testinstanz (eigene `UIA3Automation`) und färbt nicht auf andere Tests ab.

**Simulation langsamer UIA-Antworten:**

- Nicht durchgeführt. Das Anlegen eines CPU-Last-Skripts im Scratchpad wurde vom Berechtigungssystem abgelehnt.
- Ersatzweise wurde nichts versucht: keine andere Last, keine Prioritätsänderung.

### 2. Navigator (`ITankbookNavigator.CloseAsync(long vehicleId)`)

**Code:**

- Gesucht wird der erste Reiter, dessen Stapel eine `TankbookEntriesPage` mit `TankbookEntriesViewModel { VehicleId: id }` enthält. `GoToAsync("..")` wird nur verwendet, wenn dieser Reiter aktiv ist und die Seite dieses Fahrzeugs oben liegt; sonst entfernt `RemovePage` nur die passenden Seiten.
- `_vehicleId` bleibt gesetzt, wenn das Fahrzeug fehlt. Der Abgleich greift daher auch beim erneuten Versuch in `OnAppearing`.
- Seite und ViewModel sind transient registriert, `BindingContext` ist das ViewModel.
- Ein ViewModel einer längst geschlossenen Seite findet keine passende Seite mehr und kehrt ohne Wirkung zurück.

**Tests:**

- `ShellTankbookNavigatorTests_SelectPages` (eigenes Fahrzeug; anderes Fahrzeug, kein oder fremdes ViewModel).
- `Restored_VehicleGone_ClosesPage` prüft `ClosedVehicles = [vehicle.Id]`.
- Der Shell-Teil selbst ist wie bisher nicht unit-testbar, aber durch E2E abgedeckt.

**Regressionen am Zwischenstand:**

- **Wiederherstellung ohne das Fahrzeug der offenen Seite:** Fahrzeug „Neu nach Sicherung“ anlegen, seine Vorgangsseite öffnen, die Sicherung vom 20.10. 00:24 im Reiter Optionen wiederherstellen, zurück in den Reiter Tankbuch. Ergebnis direkt und nach 5 s: Die Seite ist geschlossen, die Fahrzeugliste zeigt die 6 Fahrzeuge der Sicherung, eine Zurück-Schaltfläche gibt es nicht.
- **Vorgangsseite bei vorhandenem Fahrzeug, mit veraltetem ViewModel:**
  - Ablauf: neue Sicherung anlegen, „Temp B“ anlegen, dessen Vorgangsseite öffnen und schließen, Diesel1 öffnen, wiederherstellen („Temp B“ fehlt danach).
  - Ergebnis: Die Seite von Diesel1 bleibt offen, „Vorgang erfassen“ ist aktiv, „Export“ sichtbar, Zurück vorhanden. Nach 4 s ist der Zustand unverändert.
  - Danach zeigt Diesel1 4 Vorgänge („ab Startstand“), der PDF-Export ist korrekt, „Zurück“ führt zur Fahrzeugliste.
  - Ob das alte ViewModel noch lebte, ist von außen nicht beobachtbar. Der Fall ist jetzt durch den Code ausgeschlossen.
- **Wechsel zwischen Reitern:** Vorgangsseite Diesel1 → Karte → Optionen → Tankbuch: Die Seite ist weiter offen; „Zurück“ führt zur Fahrzeugliste, danach gibt es keine Zurück-Schaltfläche.
- **E2E:** `TankbookEntriesE2ETests_RestoreReload` 4/4.

**Dokumentation (Kleinigkeiten):**

- ADR 0013 („Neuladen nach Wiederherstellung“) erwähnt die Beschränkung auf die Seite des Fahrzeugs nicht.
- `docs/help/Sicherung/ablauf-technisch.md` nennt `SettingsViewModel` als Abonnenten von `IRestoreNotifier`. Es hängt aber an `BackupViewModel.Restored`.

### 3. Beleg-E2E-Test

- `WaitForName` ersetzt alle drei Stellen („Foto auswählen“, „Beleg anzeigen“, „Beleg ausblenden“). `ValueOrDefault` deckt die kurzzeitig fehlende Eigenschaft ab.
- **Rest (optionale Härtung):** Wird das Element zwischen `FindByAutomationId` und dem Lesen des Namens neu aufgebaut, beendet eine `ElementNotAvailableException` das Warten sofort, weil sie nicht als vorübergehend gilt. Das ist sehr selten. `ReadRemaining` in LargeResult fängt diesen Fall bereits ab.
- In dieser Abnahme 4/4 grün.

### 4. Name „Weitere anzeigen (N weitere)“ für die Sprachausgabe

- **UIA am Zwischenstand:** Button, Name „Weitere anzeigen (281 weitere)“, kein Hilfetext, fokussierbar, aktiv.
- Die Sprachausgabe liest „Weitere anzeigen (281 weitere), Schaltfläche“. Aktion und Restmenge sind verständlich, das doppelte „weitere“ ist leicht redundant. Die Formulierung besteht seit Schritt 6 und wurde in diesem Schritt nicht geändert.
- **Möglicher Ausbau außerhalb dieses Schritts:** Nach dem Antippen gibt es keine Ansage; `MapViewModel` hat keinen Announcer. Die neuen 25 Einträge oberhalb der Schaltfläche werden nicht gemeldet. Der Test koppelt sich an das Textformat; eine Änderung der Beschriftung ließe ihn deutlich scheitern.

### 5. Zwischenstand `0.1.21_2026-10-20`

**Start und Version:**

- Gestartet wie die E2E-Tests über `Process.Start` mit `UseShellExecute = false`, mit `TANKATLAS_TEST_DATA_PATH` (Kopie der Daten aus Runde 2 im Scratchpad) und `TANKATLAS_TEST_WINDOW=offscreen`, mit lokalem Preis-Mock.
- Bedienung nur über UIA-Muster.
- EXE und DLL: `FileVersion` 0.1.21.0, `ProductVersion` `0.1.21+481b0fa1ba9141337311f9051b18afaedd2fef73`.
- Neue Sicherung: Manifest `appVersion` „0.1.21.1“, Migration `20261018094636_AddStationCustomName`.

**Exporte:**

- 6 PDF (Golf „Alle“ mit 2 Seiten, Golf „Letzte 12 Monate“, Hybrid1, Elektro1, Vielfahrer mit 4 Seiten, Diesel1) und 1 CSV.
- Elektro1 „Letztes Jahr“: Meldung „Im gewählten Zeitraum gibt es keine Vorgänge.“, keine Datei.
- Strukturprüfung: 0 Probleme. Kein Überlappungsbefund.
- Der extrahierte Text aller PDF ist identisch mit Runde 2, z. B. „Strecke der berechneten Intervalle | 700 km“ und „1.000 km“ (Hybrid).
- Die CSV ist byteweise identisch mit Runde 2 (Maskierung, Formelschutz, Unicode).
- Titel `<FEFF…00DC>`.

**Dokumentation:**

- `changes.log` und `CHANGELOG.md` des Zwischenstands beschreiben Runde 2 zutreffend.
- `docs/help/ci-cd/workflows.md` („UIA-Timeouts bei Baumsuchen“) beschreibt noch den Stand vor `TransientWait` und vor dem Ansatz mit dem Schaltflächennamen. Eine Aktualisierung wird empfohlen.

### 6. Sonstiges

**Repository:**

- Keine Commits, keine Pushes, kein Code geändert; angelegt wurde nur dieser Bericht.
- `review-versions/`, `drafts/` und `design-draft/` sind unverändert.
- `TestResults/` und `coverage-report/` stammen aus `local-ci` (gitignoriert).

**Eigene Prozesse, per PID beendet:**

- App 6692 (`Tankradar.MAUI`, Zwischenstand).
- Mock 29312 (`python -I mock_big.py 18765 300`, nur 127.0.0.1). Er brauchte `/F`, weil der Konsolenprozess kein Fenster hat.
- `testhost` endete selbst. Am Ende lief kein eigener Prozess mehr; fremde `dotnet`-Prozesse wurden nicht berührt.

**Vom Berechtigungssystem abgelehnt (nicht umgangen):**

1. Das Anlegen des CPU-Last-Skripts. Es war im selben Aufruf wie ein Wiederholungsskript; die Wiederholungen liefen danach ohne Skriptdatei direkt per `dotnet test`.
2. Eine Prüfung des Mocks mit `netstat`/`curl`.
   - Die PID des Mocks wurde stattdessen über `tasklist` und die Befehlszeile (CIM) bestimmt.
   - Seine Funktion zeigt das Suchergebnis der App: 306 Treffer, eine Listenanfrage.

**Eigenes Werkzeug:** Der erste Exportversuch mit Windows PowerShell 5.1 scheiterte an meinen Skripten ohne BOM: „Ausgewählt“ wurde falsch gelesen. Mit PowerShell 7 lief er fehlerfrei. Das hat keinen Bezug zur App.

**Ablage im Scratchpad:**

- Logs und Daten: `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn16r3`
  - `localci.log`, `prepush.log`, `rep\`, `out\`, `showmore1.txt`, `gone.txt`, `regress.txt`, `ci-attempt1-failed.log`, `ci-diag\`, `test-results-*.trx`
- Skripte: `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn16r3-tools`
