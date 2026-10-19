# Abnahmeprüfung – Entwicklungsschritt 15a (Anzeigename von Tankstellen und erster Verbrauch ab Startstand), Runde 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Geprüft wurde der Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-15a-anzeigename-verbrauch` (HEAD `64a06ef`) gegen den Projekt-Basisbranch (`194d49e`). Alle drei Abweichungen der ersten Abnahme (`acceptance-schritt-15a.1.md`) sind behoben und am Zwischenstand `review-versions/0.1.20_2026-10-18` (Build aus `64a06ef`) praktisch bestätigt:

1. **Tankbuch-Auswahl:** Zwei JET-Favoriten in Berlin heißen jetzt „JET · JET BERLIN HAUPTSTR. 1“ und „JET · JET BERLIN NEBENWEG 22“, sichtbar und als UIA-Name (Sprachausgabe).
2. **Präparierte Sicherung:** Überlange Texte in `CustomName`, `Name` und `Brand` (61, 201, 5.000 und 200.000 Zeichen, BLOB) werden mit „Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“ abgelehnt. Der Bestand bleibt unverändert, die App stürzt nicht ab. Der Grenzfall mit eingebettetem NUL-Zeichen wird angenommen, aber beim Kopieren auf „a“ gekürzt und ist damit unschädlich.
3. **Version:** Die App meldet 0.1.20.1, sowohl im Manifest einer neu erzeugten Sicherung (`appVersion`) als auch in der Rückfrage der Wiederherstellung.

Die übrigen Punkte der Nachbesserung sind umgesetzt:

- Fokus und Tastatur wurden aus den E2E-Hilfen entfernt; ein Quelltext-Test sichert das ab.
- Alle Mock-Server wiederholen den Start auf einem neuen Port, wenn der erste belegt ist.
- Texte sind korrigiert (Akku/Teilladung, Startverbrauch, Hinweis zum eigenen Namen).
- Die Beschreibung der Kartenmarkierung nennt die Adresse.

Die beiden CI-Testkorrekturen sind für den Vordergrundmodus plausibel und prüfen weiter ihren Zweck. Beim Kachelzahlen-Test bleibt eine kleine Prüflücke (Hinweis 2).

Es gibt keine echte Abweichung. Unter Hinweise stehen Härtungen und eine Beobachtung zum Vordergrundverhalten des Off-Screen-Fensters beim Start über `Start-Process`.

**Prüfläufe (selbst ausgeführt, `FUEL_PRICE_API_KEY` per `env -u` entfernt, nie gelesen):**

| Prüfung | Ergebnis |
|---|---|
| `local-ci.ps1` vollständig, E2E off-screen (18.10., 18:03–18:16) | Exit 0, alle 15 Schritte OK bzw. wie vorgesehen übersprungen (Windows-Paket). Unit 1345/1345, Integration 241/241, **E2E 114/114** (10 min 56 s), Zeilenabdeckung 93,5 %, Build 0 Warnungen, iOS-Compile OK (nur `MAUI1001`), keine anfälligen Pakete. Workloads android/ios/maccatalyst/maui-windows vorhanden, keine `Include*Target`-Anpassung nötig. `DiagnosticsCaptureE2ETests` grün, „Screenshot einfarbig“ trat nicht auf. |
| Vollständiger Neubau `dotnet build -c Release --no-incremental -p:TreatWarningsAsErrors=true` (Windows-Job-Umgebung) | 0 Warnungen, 0 Fehler |
| Wiederholung der geänderten E2E-Klassen und der Formular-Klassen (19.10., 08:30–08:57, off-screen) | 24 Klassen mit 36 Tests: alle 16 geänderten Klassen (23 Tests) sowie weitere Formular-Klassen mit `SetFieldValue` (`TankbookE2ETests_Flow`/`_Validation`, `FavoritesE2ETests_Groups`, `BackupE2ETests_Flow`, `TankbookEntriesE2ETests_Receipt`/`_Validation`, `TankbookStatisticsE2ETests_Hybrid`/`_Months`). **5 × 36/36 bestanden** (je etwa 5 min 20 s), keine Diagnosedaten. Mit dem Gesamtlauf liefen die geänderten Klassen damit 6×. |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push (Refs auf stdin, Basis `194d49e`) | Exit 0 (08:57–08:58): Commits 14/14; verbotene Muster (904 Dateien); Format; XML-Doku (660); Übersetzung (keine `.resx`); Enums; Platzhalter (656); Build, Unit 1345/1345, Integration 241/241. E2E sind im Hook standardmäßig aus. |
| Conventional Commits | 14/14 (`194d49e..HEAD`), 174/174 (`main..HEAD`) |
| Vordergrund-Beobachtung während der E2E-Läufe (rein lesend: `GetForegroundWindow` alle 100 bzw. 50 ms) | Kein Tankradar-Fenster wurde Vordergrundfenster: weder im Gesamtlauf (114 Tests) noch in den 5 Wiederholungen. Dort gab es 582 Vordergrundwechsel, fast alle durch parallel laufende fremde Oberflächentests (`Softwareschmiede.App`) und Visual Studio. Auch die Gegenprobe mit einem Einzeltest ergab keinen Wechsel. |

## Abweichungen

Keine.

## Hinweise

### 1. Prüfung der Abweichungen aus 15a.1 (praktisch, off-screen, Testpfad im Scratchpad)

**Testaufbau:**

- Zwischenstand `review-versions/0.1.20_2026-10-18`: EXE-Dateiversion 0.1.20.0, Produktversion `0.1.20+64a06ef…`, Assembly-Metadatum `AppInfo.Version` = 0.1.20.1.
- Gestartet nur mit gesetztem `TANKATLAS_TEST_DATA_PATH` (Kopie der 0.1.19-Daten der ersten Abnahme) und `TANKATLAS_TEST_WINDOW=offscreen`.
- Preisdienst: lokaler Mock auf `127.0.0.1:18765` (sechs Stationen, darunter „Klaus Heinrich“/Westfalen und zwei JET in Berlin).
- Bedienung nur über UI-Automation-Muster (Invoke, SelectionItem, Value, Toggle, Transform, Scroll, ScrollItem); kein Fokus, keine Maus- oder Tastatureingabe.

| Prüfpunkt | Ergebnis |
|---|---|
| Upgrade 0.1.19 → 0.1.20 | Migration angewendet. Startseite: „ARAL / Aral Tankstelle“, „JET / JET BERLIN HAUPTSTR. 1“, „JET / JET BERLIN NEBENWEG 22“, „Westfalen / Klaus Heinrich“. |
| Tankbuch-Auswahl (Abw. 1) | Chips „ARAL“, „JET · JET BERLIN HAUPTSTR. 1“, „JET · JET BERLIN NEBENWEG 22“, „Westfalen“, „Wallbox“. Der UIA-Name ist identisch mit dem sichtbaren Text. Chipbreite 236 bzw. 248 px. ✓ |
| Auswahl „JET · … NEBENWEG 22“ und Speichern | Das Feld „Tankstelle“ zeigt „JET“, der Vorgang heißt in der Liste „JET“ (Momentaufnahme laut ADR 0009/0012, Zuordnung über die Kennung), siehe Hinweis 4. |
| Version (Abw. 3) | Neue Sicherung: `appVersion` „0.1.20.1“, Migration `20261018094636_AddStationCustomName`. Rückfrage: „Sicherung vom 19.10.2026 08:17, Version 0.1.20.1 wiederherstellen? …“. Die 0.1.19-Sicherung zeigt weiterhin „Version 0.1.0.1“; so wurde sie damals erzeugt. ✓ |
| Präparierte Sicherungen (Abw. 2) | Abgeleitet aus der neuen 0.1.20-Sicherung, Prüfsummen neu berechnet, jeweils erste Station (Westfalen). Einzeln über „Wiederherstellen“ der Liste geprüft, siehe Tabelle unten. |
| 0.1.19-Sicherung in 0.1.20 | Angenommen und wiederhergestellt. Migration auf `AddStationCustomName`, `integrity_check` ok, WAL-Modus, 6 Vorgänge, kein eigener Name. Diesel1 zeigt sofort „Verbrauch 10,00 L/100 km (ab Startstand)“ mit Hinweis. ✓ |
| Verbrauch ab Startstand (Regression) | Diesel1 nach Upgrade: „10,00 (ab Startstand)“, „6,00“. Nach einem weiteren vollen Vorgang (25 L bei 11.400 km): 6,25. Auswertung 6,11 L/100 km „Berechnet aus vollen Vorgängen“, Verlauf „3 Werte … ältester Wert ab Startstand berechnet“. ✓ |

**Präparierte Sicherungen im Einzelnen** (App lief durchgehend und reagierte; Bestand vor und nach den Ablehnungen gleich: Fahrzeuge 3, Vorgänge 7, Gruppen 1, Favoriten 4, Stationen 6 mit unverändertem Hash über Id/Name/Marke/eigener Name; nur die Preisstände wuchsen durch den regelmäßigen Abruf):

| Variante | Erwartet | Ergebnis |
|---|---|---|
| `CustomName` 61 Zeichen | ablehnen | abgelehnt ✓ |
| `CustomName` 200.000 Zeichen | ablehnen | abgelehnt ✓ |
| `Name` 200.000 Zeichen | ablehnen | abgelehnt ✓ |
| `Brand` 200.000 Zeichen | ablehnen | abgelehnt ✓ |
| `CustomName` als BLOB (`zeroblob(200000)`) | ablehnen | abgelehnt ✓ |
| `Name` 201 Zeichen | ablehnen | abgelehnt ✓ |
| `Brand` 5.000 Zeichen (mit Leerzeichen) | ablehnen | abgelehnt ✓ |
| `CustomName` 60 × „Ä“ (60 Zeichen, 120 Byte) | annehmen | Rückfrage ✓ (abgebrochen) |
| `Name` 200 Zeichen | annehmen | Rückfrage ✓ (abgebrochen) |
| `CustomName` = `'a' ‖ char(0) ‖ 200.000 Zeichen` (SQLite: `length` 1, 200.002 Byte) | ablehnen oder unschädlich | Rückfrage. Nach dem Wiederherstellen steht „a“ in der Datenbank (`length` 1, 1 Byte): Der Kopiervorgang kürzt am NUL-Zeichen. Startseite „a / Klaus Heinrich“, „Vorgang erfassen“ mit Chip „a“ funktioniert. Unschädlich, aber ohne Regressionstest (Hinweis 5). |

### 2. CI-Testkorrekturen (Bewertung per Code, Messung an der eigenen unsichtbaren App)

**Ausgangslage im GitHub-CI** (Lauf 39137136224, Vordergrundmodus, Code des Basisstands):

- `TankbookStatisticsE2ETests_TileNumbers` meldete „Die Zahl ragt aus dem Fenster: {X=0,Y=0,Width=0,Height=0} gegenüber {X=26,Y=26,Width=360,Height=519}“: Das Element hatte leere Maße, weil es außerhalb des Sichtbereichs lag.
- `SearchE2ETests_LargeResult` scheiterte in Zeile 30 (`SelectSort` nach 50 Treffern) mit einem UIA-Timeout. Der Test dauerte 12 min 21 s.

**Kachelzahlen-Test:**

- **Plausibel für den Vordergrundmodus:**
  - Das Off-Screen-Fenster ist standardmäßig 1.460 px hoch, die Kacheln lagen dort immer im Bild. Deshalb war der Fehler lokal nicht reproduzierbar.
  - Mit der festen Höhe von 520 px laufen beide Betriebsarten durch denselben Pfad (Kacheln einblenden).
  - Gemessen an der eigenen App (360 × 520 per TransformPattern): `ScrollItemPattern` wird unterstützt. `ScrollIntoView` bringt die Kachelzahl auch bei 470 und 440 px Höhe vollständig ins Bild (33 px hoch).
  - Die Prüfungen auf Einzeiligkeit (Höhe ≤ 2 × Schriftgröße) und horizontal innerhalb des Fensters bleiben erhalten.
- **Restlücke (niedrig, Testqualität):**
  - WinUI beschneidet die UIA-Grenzen auf den sichtbaren Teil. Gemessen: Bei teilweise sichtbarer Kachelzahl meldet UIA 9 statt 33 px Höhe.
  - `EnsureVisible` kehrt aber schon zurück, wenn *irgendwelche* Maße vorhanden sind. Ebenso bricht der Rückfall über `SetScrollPercent` bei der ersten Teilsichtbarkeit ab.
  - Ist die Zahl am unteren Rand nur angeschnitten, wird eine umgebrochene Zahl mit verkürzter Höhe gemessen. Der Test bestünde dann fälschlich.
  - In meinen Daten lag die Zahl bei 520 px Höhe genau am Rand (350–383 px in einem 384 px hohen Sichtbereich).
  - Empfehlung: `ScrollIntoView` immer aufrufen und prüfen, dass das Element vollständig im Sichtbereich des `ScrollViewer` liegt.
- **Suche mit großer Ergebnismenge:**
  - Der Sortierchip wird vor dem großen UI-Baum gesucht, `SelectChip(element)` prüft zuerst am gefundenen Element, das UIA-Zeitlimit beträgt 60 s.
  - Der Zweck bleibt erhalten: nur die erste Seite, „Weitere anzeigen“ ergänzt die zweite, der Sortierwechsel führt in unter 10 s auf die erste Seite zurück, genau eine Listenanfrage.
  - Wird der Chip neu aufgebaut, scheitert der Test sichtbar (`ElementNotAvailable`) statt fälschlich zu bestehen.
  - Für den Vordergrundmodus plausibel; nicht im Vordergrund ausgeführt (nicht erlaubt).

### 3. E2E ohne Fokus (Punkt 2 des Auftrags)

**Code:** Im E2E-Projekt kommen nur UI-Automation-Muster vor:

| Muster bzw. Aufruf | Verwendung |
|---|---|
| `Invoke` | 27× |
| `Toggle` | 5× |
| `SelectionItem.Select` | nur Reiter |
| `Value.SetValue` | genau eine Stelle, `SetFieldValue` |
| `Transform.Resize` | Kachelzahlen-Test |
| `ScrollItem.ScrollIntoView`, `Scroll.SetScrollPercent` | Kachelzahlen-Test |
| Native Aufrufe | nur lesend: `IsIconic`, `GetWindowRect`, `PrintWindow`, `GetDpiForWindow` |
| Start und Ende | `Application.Launch` (`Process.Start` ohne ShellExecute), `Application.Close` (`WM_CLOSE`) |

- **Nicht vorhanden** sind `Focus`, `FocusNative`, `SetForeground`, `Click`/`DoubleClick`/`RightClick`, `Keyboard`, `Mouse`, `SendKeys`/`SendInput`, `AsTextBox().Enter`, der Setter `TextBox.Text` (Rückfall auf die Tastatur), `ComboBox.Expand` (Rückfall auf Klick), `Hover` und `Drag`.
- In der App selbst gibt es keine Aufrufe von `Activate()`, `SetForegroundWindow` oder `Focus()`.
- `E2EInputPolicyTests_NoFocus` sichert das zeilenweise ab. Mehrzeilige Aufrufe erkennt er nicht; das ist bekannt und vertretbar.
- `OffscreenWindow` ist seit Schritt 6a unverändert (`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, `SWP_NOACTIVATE`).

**Laufzeit:** Beobachtet wurde rein lesend per `GetForegroundWindow` alle 100 bzw. 50 ms.

- Während des vollständigen E2E-Laufs (114 Tests) und der Wiederholungen wurde kein Tankradar-Fenster zum Vordergrundfenster. Der Anwender arbeitete parallel (Edge, VNC-Viewer, Explorer, Visual Studio).
- Fremde Eingaben waren in keinem Lauf zu sehen; es gab keine Fehlschläge mit Diagnosedaten.
- Zusätzlich lief zeitweise ein WinEvent-Hook (`EVENT_SYSTEM_FOREGROUND`). Er meldete für jede E2E-Instanz ein Vordergrundereignis. Die gleichzeitige Abfrage von `GetForegroundWindow` zeigte jedoch keinen Wechsel (Gegenprobe mit einem Einzeltest: Hook 1 Ereignis, Abfrage 0). Auch fehlte der Rückwechsel zum Anwenderfenster.
- Ich werte die Hook-Ereignisse deshalb als Aktivierung des Fensters innerhalb seines Threads ohne echten Tastaturvordergrund. Wegen dieser zunächst falsch gelesenen Ereignisse habe ich den ersten Wiederholungslauf um 08:25 abgebrochen und neu gestartet.

**Synchronisation ohne Fokuswechsel (Bewertung):**

- Die MAUI-`Entry` überträgt den Text bei jedem `TextChanged` an die Bindung, nicht erst beim Fokusverlust. `Unfocused`/`Completed`-Handler, die einen Fokuswechsel bräuchten, gibt es nicht (vom Review bestätigt). Der Fokuswechsel war also nie fachlich nötig.
- `ValuePattern.SetValue` setzt den Text synchron auf dem UI-Thread. WinUI löst `TextChanged` danach asynchron über den Dispatcher aus.
- Die acht UIA-Abfragen mit je 50 ms Pause geben dem UI-Thread mindestens 400 ms, die eingereihten Ereignisse abzuarbeiten. Streng geordnet ist das nicht: UIA-Anfragen kommen als gesendete Nachrichten und können vor gebuchten Dispatcher-Einträgen bearbeitet werden. Der Mechanismus ist also zeitbasiert, wie zuvor der Fokuswechsel, der selbst asynchron war.
- Praktisch ausreichend: Der Gesamtlauf (114/114) und 5 × 36 Tests liefen ohne Fehlschlag. Darin speichern alle Formular-Klassen über `SetFieldValue`, teils während fremde Oberflächentests den Rechner belasteten. Für eine strenge Absicherung bleibt die Empfehlung des Reviews, nach dem Schreiben auf einen vom Modell abhängigen Zustand zu warten (z. B. Rechenhinweis oder aktivierte Schaltfläche).

**Beobachtung außerhalb des E2E-Pfads (niedrig, Infrastruktur aus 6a):**

- Mein eigener Start des Zwischenstands per `Start-Process` (ShellExecute) wurde am 18.10. um 18:18:11 zum echten Vordergrundfenster und blieb es bis 18:24:28. Das bestätigen sowohl die Abfrage als auch der Hook; der Anwender war in dieser Zeit offenbar nicht aktiv.
- `WS_EX_NOACTIVATE` verhindert also nicht, dass WinUI das Fenster beim ersten `Activate()` in den Vordergrund holt, wenn der startende Prozess das Recht dazu weitergibt. Der E2E-Start (`Process.Start` ohne ShellExecute) war davon in keinem beobachteten Lauf betroffen.
- Empfehlung:
  - Manuelle bzw. Abnahme-Starts von Zwischenständen ebenfalls ohne ShellExecute ausführen.
  - Optional im Testmodus die erste Aktivierung unterdrücken, z. B. das Fenster vor `Activate()` mit `SW_SHOWNOACTIVATE` zeigen.

### 4. Tankbuch: gespeicherter Name bleibt „JET“ (niedrig)

- Die Auswahl ist unterscheidbar. Als Name des Vorgangs wird aber weiterhin nur der Anzeigename übernommen. In der Vorgangsliste stehen zwei Vorgänge an verschiedenen JET-Stationen beide als „JET“; welche Station gemeint ist, sieht man nur beim Bearbeiten (markierter Chip).
- Das ist in ADR 0012 bewusst so entschieden (Momentaufnahme, Zuordnung über die Kennung) und keine Planabweichung, denn der Plan nennt die Tankbuch-*Zuordnung*.
- In 0.1.19 hätte der Vorgang „JET BERLIN HAUPTSTR. 1“ geheißen.
- Empfehlung: Bei mehrdeutigem Anzeigenamen die Chip-Beschriftung übernehmen oder in der Liste den Stammdatennamen über die Kennung klein darunter zeigen.

### 5. Textgrenzen der Sicherung

- `BackupTextLimits` deckt alle `string`-Spalten des Modells ab (Abgleich mit dem Model-Snapshot). Datums- und Zahlenspalten werden weiterhin über das Lesen mit Entity Framework geprüft.
- Die Prüfung läuft auf der bereits kopierten Referenz nach der letzten Migration und vor jedem Eingriff in die Livedaten.
- NUL-Fall: Die Byteprüfung greift hier nicht, weil die Kopie bereits gekürzt ist. Die Unschädlichkeit beruht auf dem Verhalten von Microsoft.Data.Sqlite/SQLitePCL beim Binden.
- Empfehlung (wie im Re-Review): Regressionstest „Quelle mit `'a'||char(0)||…` → nach Wiederherstellung höchstens 60 Zeichen oder Ablehnung“.
- ADR 0011 nennt nur „SQL `length()` in Zeichen“, nicht die zusätzliche Byteprüfung (Dokukleinigkeit).

### 6. Kleinigkeiten in Doku und Oberfläche (niedrig)

- `docs/help/Tankstellendetails/beschreibung.md`, Absatz „Eigener Name“: Er nennt zweimal, dass der Name bei jedem Preisabruf erhalten bleibt und Teil der Sicherung ist.
- `docs/help/Tankbuch/beschreibung.md` Z. 29: Der Klammerzusatz zur „Teilladung“ steht mitten im ersten Satz, die Aufzählung danach nennt weiter nur „Teilbetankung“.
- Nach einer Wiederherstellung zeigt eine bereits geöffnete Seite „Tank- und Ladevorgänge“ die alten Chips, bis sie neu geöffnet wird. Das gibt es schon seit Schritt 15 und betrifft 15a nicht direkt.

### 7. Anforderungstabelle (Kurzfassung, Stand nach Nachbesserung)

| Anforderung | Bewertung |
|---|---|
| Marke prominent, Stammdatenname klein darunter (Startseite, „In der Nähe“, Gruppe, Suche, Karte, Detail) | erfüllt (E2E, Unit, Zwischenstand) |
| Tankbuch-Zuordnung | erfüllt; gleichnamige Stationen unterscheidbar (Chip und Sprachausgabe); Hinweis 4 |
| Eigener Name: vergeben, ändern, entfernen, Vorrang, lokal, Migration, Sicherung | erfüllt; überlange Namen aus Sicherungen werden abgelehnt |
| Erster Verbrauch ab Startstand inkl. Teilbetankungen, Kennzeichnung, Auswertung, Hilfe, ADR | erfüllt (Regression am Zwischenstand bestätigt) |
| Tests laut Plan (inkl. FlaUI) | erfüllt; zusätzlich E2E für zwei JET im selben Ort |
| Windows-Zwischenstand | `review-versions/0.1.20_2026-10-18` aus `64a06ef`, startfähig, meldet 0.1.20.1 |
| Härtungen aus 15 | alle drei wirksam |
| Mock-Port-Wiederholung | `LoopbackHttpListener` für Preisdienst-, Nominatim- und Kachel-Mock; Integrationstests `LoopbackHttpListenerTests_Retry` grün; verbleibende `TcpListener`-Sonden dienen nur der Ermittlung eines unbelegten Ports für „Dienst nicht erreichbar“ |

### 8. Sonstiges

- **Repository:**
  - Keine Commits, keine Pushes, kein Code geändert. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` unverändert; `../ta-push` nicht angefasst.
  - `TestResults/`, `coverage-report/` und `e2e-diagnostics/` erzeugen `local-ci` bzw. die Testläufe selbst (gitignoriert).
- **Unterbrechung:** Die Abnahme war vom 18.10., etwa 18:22, bis zum 19.10., 08:12, wegen eines Nutzungslimits unterbrochen. Danach wurden die Läufe geprüft und fortgesetzt.
- **Eigene Prozesse, per PID beendet:**
  - App 44788 (lief seit 18.10.).
  - Mock 35264 und 31040.
  - E2E-Prozessbäume 20324, 25976 und das Wiederholungsskript 28008 (abgebrochener erster Wiederholungslauf).
  - Beobachtungsprozesse sind von selbst ausgelaufen bzw. wurden per Aufgabensteuerung beendet.
  - Fremde Prozesse (Softwareschmiede-Tests in einem anderen Repository, MSBuild-Knoten) blieben unberührt.
- **Parallelbetrieb:** Am 19.10. liefen zeitweise fremde Oberflächentests (`Softwareschmiede.App`) auf demselben Desktop. Auf die Ergebnisse hatte das keinen erkennbaren Einfluss.
- **Keine Systemeinstellungen geändert.** Keine Maus- oder Tastatureingaben; Fenstergrößen nur am eigenen, unsichtbaren App-Fenster.
- **Vom Berechtigungssystem abgelehnt:** nichts. Ein `sleep 240` zum Warten blockierte die Werkzeugumgebung. Stattdessen wurde, wie dort vorgesehen, mit Bedingungsschleifen bzw. Monitoren gewartet.
- Am Ende lief kein eigener `Tankradar.MAUI`-, `testhost`-, Mock- oder Beobachtungsprozess mehr.
- **Testdaten, präparierte Sicherungen, Skripte und Protokolle** liegen im Scratchpad (`abn15a2/`, `abn15a2-tools/`).
